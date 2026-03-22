import json
import time
import traceback
from pathlib import Path

import bmesh
import bpy


BRIDGE_FILENAME = "toio_blender_bridge_commands.jsonl"
STATUS_TEXT_NAME = "toio_blender_bridge_status"
TIMER_NAMESPACE_KEY = "toio_blender_bridge_poll_commands"
DUPLICATE_WINDOW_SECONDS = 2.5


def resolve_command_file():
    candidates = []

    script_file = globals().get("__file__")
    if script_file:
        script_path = Path(script_file)
        if script_path.suffix == ".py" and script_path.exists():
            candidates.append(script_path.resolve().parent)

    cwd = Path.cwd().resolve()
    candidates.append(cwd / "BlenderBridge")
    candidates.append(cwd / "toio-sdk-unity" / "BlenderBridge")

    home = Path.home().resolve()
    candidates.append(home / "Documents" / "toio-sdk-for-unity" / "toio-sdk-unity" / "BlenderBridge")
    candidates.append(home / "Documents" / "toio-sdk-unity" / "BlenderBridge")
    candidates.append(home / "Documents" / "BlenderBridge")

    if bpy.data.filepath:
        blend_dir = Path(bpy.data.filepath).resolve().parent
        candidates.append(blend_dir / "BlenderBridge")
        candidates.append(blend_dir / "toio-sdk-unity" / "BlenderBridge")
    else:
        blend_dir = None

    for directory in candidates:
        if directory.exists():
            return directory / BRIDGE_FILENAME

    fallback_dir = blend_dir / "BlenderBridge" if blend_dir else home / "Documents" / "BlenderBridge"
    return fallback_dir / BRIDGE_FILENAME


COMMAND_FILE = resolve_command_file()
STATE = {
    "offset": 0,
    "registered": False,
    "status": "not started",
    "last_command": "",
    "last_command_key": "",
    "last_command_time": -999.0,
}


def set_status(message):
    STATE["status"] = message
    print(message)

    text_block = bpy.data.texts.get(STATUS_TEXT_NAME)
    if text_block is None:
        text_block = bpy.data.texts.new(STATUS_TEXT_NAME)

    text_block.clear()
    text_block.write(message + "\n")


def ensure_object_mode():
    obj = bpy.context.active_object
    if obj is None:
        return

    if obj.mode != "OBJECT":
        try:
            bpy.ops.object.mode_set(mode="OBJECT")
        except Exception:
            traceback.print_exc()


def link_object(obj):
    collection = bpy.context.collection or bpy.context.scene.collection
    collection.objects.link(obj)

    view_layer = bpy.context.view_layer
    for existing in view_layer.objects:
        existing.select_set(False)

    obj.select_set(True)
    view_layer.objects.active = obj


def add_plane():
    ensure_object_mode()
    mesh = bpy.data.meshes.new("ToioPlane")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=1.0)
    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new("ToioPlane", mesh)
    obj.location = bpy.context.scene.cursor.location.copy()
    link_object(obj)


def add_cube():
    ensure_object_mode()
    mesh = bpy.data.meshes.new("ToioCube")
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new("ToioCube", mesh)
    obj.location = bpy.context.scene.cursor.location.copy()
    link_object(obj)


def handle_command(payload):
    command = payload.get("command", "")
    command_id = payload.get("id")
    unity_time = payload.get("unityTime")
    command_key = f"{command_id}:{command}" if command_id is not None else command
    command_time = float(unity_time) if unity_time is not None else time.monotonic()

    if (
        command == STATE["last_command"] and
        command_key == STATE["last_command_key"]
    ):
        set_status(f"toio_blender_command_bridge: duplicate id suppressed {command_key}")
        return

    if (
        command == STATE["last_command"] and
        command_time - STATE["last_command_time"] < DUPLICATE_WINDOW_SECONDS
    ):
        set_status(f"toio_blender_command_bridge: duplicate command suppressed {command}")
        return

    if command == "add_plane":
        add_plane()
        STATE["last_command"] = command
        STATE["last_command_key"] = command_key
        STATE["last_command_time"] = command_time
        set_status("toio_blender_command_bridge: add_plane")
        return

    if command == "add_cube":
        add_cube()
        STATE["last_command"] = command
        STATE["last_command_key"] = command_key
        STATE["last_command_time"] = command_time
        set_status("toio_blender_command_bridge: add_cube")
        return

    set_status(f"toio_blender_command_bridge: unknown command '{command}'")


def poll_commands():
    try:
        if not COMMAND_FILE.exists():
            return 0.1

        file_size = COMMAND_FILE.stat().st_size
        if file_size < STATE["offset"]:
            STATE["offset"] = 0

        with COMMAND_FILE.open("r", encoding="utf-8") as handle:
            handle.seek(STATE["offset"])
            for raw_line in handle:
                line = raw_line.lstrip("\ufeff").strip()
                if not line:
                    continue

                try:
                    payload = json.loads(line)
                except json.JSONDecodeError:
                    print(f"toio_blender_command_bridge: skipped invalid json: {line}")
                    continue

                handle_command(payload)

            STATE["offset"] = handle.tell()
    except Exception:
        traceback.print_exc()
        set_status("toio_blender_command_bridge: poll error")

    return 0.1


def register():
    namespace = bpy.app.driver_namespace
    existing_timer = namespace.get(TIMER_NAMESPACE_KEY)
    if existing_timer is not None:
        try:
            if bpy.app.timers.is_registered(existing_timer):
                bpy.app.timers.unregister(existing_timer)
        except Exception:
            traceback.print_exc()

    COMMAND_FILE.parent.mkdir(parents=True, exist_ok=True)
    COMMAND_FILE.touch(exist_ok=True)
    STATE["offset"] = COMMAND_FILE.stat().st_size
    bpy.app.timers.register(poll_commands, first_interval=0.1, persistent=True)
    namespace[TIMER_NAMESPACE_KEY] = poll_commands
    STATE["registered"] = True
    set_status(f"toio_blender_command_bridge: watching {COMMAND_FILE}")


register()
