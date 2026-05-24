import json
import math
import time
import traceback
from pathlib import Path

import bmesh
import bpy


BRIDGE_FILENAME = "toio_blender_bridge_commands.jsonl"
STATUS_TEXT_NAME = "toio_blender_bridge_status"
TIMER_NAMESPACE_KEY = "toio_blender_bridge_poll_commands"
DUPLICATE_WINDOW_SECONDS = 2.5
DISTANCE_OBJECT_PREFIX = "ToioDistance"


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


def get_or_create_material(name, color, emission=False, emission_strength=1.0, alpha=1.0):
    material = bpy.data.materials.get(name)
    if material is None:
        material = bpy.data.materials.new(name)

    material.diffuse_color = (color[0], color[1], color[2], alpha)
    material.use_nodes = True
    material.blend_method = "BLEND" if alpha < 1.0 else "OPAQUE"
    material.use_screen_refraction = alpha < 1.0

    nodes = material.node_tree.nodes
    principled = nodes.get("Principled BSDF")
    if principled is not None:
        try:
            principled.inputs["Base Color"].default_value = (color[0], color[1], color[2], alpha)
            principled.inputs["Alpha"].default_value = alpha
            principled.inputs["Roughness"].default_value = 0.28
            principled.inputs["Metallic"].default_value = 0.08
            if emission:
                principled.inputs["Emission Color"].default_value = (color[0], color[1], color[2], 1.0)
                principled.inputs["Emission Strength"].default_value = emission_strength
        except Exception:
            traceback.print_exc()

    return material


def clear_distance_cube():
    ensure_object_mode()
    for obj in list(bpy.data.objects):
        if obj.name.startswith(DISTANCE_OBJECT_PREFIX):
            bpy.data.objects.remove(obj, do_unlink=True)


def create_cube_object(name, location, rotation_z, dimensions, material):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=(0.0, 0.0, rotation_z))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material is not None:
        obj.data.materials.append(material)

    return obj


def add_bevel(obj, amount, segments):
    bevel = obj.modifiers.new(name="rich_bevel", type="BEVEL")
    bevel.width = amount
    bevel.segments = segments
    bevel.affect = "EDGES"

    weighted_normals = obj.modifiers.new(name="weighted_normals", type="WEIGHTED_NORMAL")
    weighted_normals.keep_sharp = True


def add_endpoint_marker(name, x, y, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.32, location=(x, y, 0.42))
    obj = bpy.context.object
    obj.name = name
    if material is not None:
        obj.data.materials.append(material)
    return obj


def add_distance_label(distance_dots, mid_x, mid_y):
    font_curve = bpy.data.curves.new(f"{DISTANCE_OBJECT_PREFIX}_LabelCurve", type="FONT")
    font_curve.body = f"{distance_dots:.1f} dots"
    font_curve.align_x = "CENTER"
    font_curve.align_y = "CENTER"
    font_curve.size = 0.34
    font_curve.extrude = 0.01

    obj = bpy.data.objects.new(f"{DISTANCE_OBJECT_PREFIX}_Label", font_curve)
    obj.location = (mid_x, mid_y, 0.92)
    obj.rotation_euler = (math.radians(62), 0.0, 0.0)
    obj.data.materials.append(get_or_create_material("ToioDistance_Label_White", (0.92, 1.0, 0.96), True, 0.6))
    link_object(obj)
    return obj


def add_distance_cube(payload):
    ensure_object_mode()
    clear_distance_cube()

    point_a = payload.get("pointA", {})
    point_b = payload.get("pointB", {})
    ax = float(point_a.get("worldX", 0.0))
    ay = float(point_a.get("worldZ", 0.0))
    bx = float(point_b.get("worldX", 0.0))
    by = float(point_b.get("worldZ", 0.0))
    distance_dots = float(payload.get("distanceDots", 0.0))

    dx = bx - ax
    dy = by - ay
    length = math.sqrt(dx * dx + dy * dy)
    if length <= 0.01:
        set_status("toio_blender_command_bridge: distance_cube skipped zero length")
        return

    mid_x = (ax + bx) * 0.5
    mid_y = (ay + by) * 0.5
    angle = math.atan2(dy, dx)

    core_material = get_or_create_material("ToioDistance_Rich_Green_Core", (0.12, 1.0, 0.46), True, 1.7)
    edge_material = get_or_create_material("ToioDistance_Cyan_Edge", (0.1, 0.72, 1.0), True, 1.1)
    glow_material = get_or_create_material("ToioDistance_Soft_Glow", (0.48, 1.0, 0.72), True, 0.9, 0.32)
    point_a_material = get_or_create_material("ToioDistance_Point_A", (0.24, 0.82, 1.0), True, 1.2)
    point_b_material = get_or_create_material("ToioDistance_Point_B", (1.0, 0.58, 0.24), True, 1.2)

    glow = create_cube_object(
        f"{DISTANCE_OBJECT_PREFIX}_GlowShell",
        (mid_x, mid_y, 0.34),
        angle,
        (length + 0.26, 0.58, 0.42),
        glow_material,
    )
    add_bevel(glow, 0.18, 10)

    core = create_cube_object(
        f"{DISTANCE_OBJECT_PREFIX}_RichCube",
        (mid_x, mid_y, 0.42),
        angle,
        (length, 0.32, 0.32),
        core_material,
    )
    add_bevel(core, 0.1, 12)

    upper_edge = create_cube_object(
        f"{DISTANCE_OBJECT_PREFIX}_CyanHighlight",
        (mid_x, mid_y, 0.64),
        angle,
        (length * 0.94, 0.06, 0.06),
        edge_material,
    )
    add_bevel(upper_edge, 0.025, 4)

    marker_a = add_endpoint_marker(f"{DISTANCE_OBJECT_PREFIX}_PointA", ax, ay, point_a_material)
    marker_b = add_endpoint_marker(f"{DISTANCE_OBJECT_PREFIX}_PointB", bx, by, point_b_material)
    add_bevel(marker_a, 0.015, 3)
    add_bevel(marker_b, 0.015, 3)
    add_distance_label(distance_dots, mid_x, mid_y)

    bpy.context.scene.camera = ensure_distance_camera(mid_x, mid_y, length)
    ensure_distance_light(mid_x, mid_y)

    STATE["last_command"] = "distance_cube"
    STATE["last_command_key"] = str(payload.get("id", ""))
    STATE["last_command_time"] = float(payload.get("unityTime", time.monotonic()))
    set_status(f"toio_blender_command_bridge: rich distance_cube {distance_dots:.1f} dots")


def ensure_distance_camera(mid_x, mid_y, length):
    camera = bpy.data.objects.get(f"{DISTANCE_OBJECT_PREFIX}_Camera")
    if camera is None:
        camera_data = bpy.data.cameras.new(f"{DISTANCE_OBJECT_PREFIX}_CameraData")
        camera = bpy.data.objects.new(f"{DISTANCE_OBJECT_PREFIX}_Camera", camera_data)
        bpy.context.scene.collection.objects.link(camera)

    camera.location = (mid_x, mid_y - max(5.0, length * 0.55), max(5.0, length * 0.45))
    camera.rotation_euler = (math.radians(60), 0.0, 0.0)
    camera.data.lens = 32
    return camera


def ensure_distance_light(mid_x, mid_y):
    light = bpy.data.objects.get(f"{DISTANCE_OBJECT_PREFIX}_KeyLight")
    if light is None:
        light_data = bpy.data.lights.new(f"{DISTANCE_OBJECT_PREFIX}_KeyLightData", type="AREA")
        light = bpy.data.objects.new(f"{DISTANCE_OBJECT_PREFIX}_KeyLight", light_data)
        bpy.context.scene.collection.objects.link(light)

    light.location = (mid_x - 2.8, mid_y - 3.2, 5.5)
    light.data.energy = 520
    light.data.size = 4.0
    return light


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
        command not in {"distance_cube", "clear_distance_cube"} and
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

    if command == "distance_cube":
        add_distance_cube(payload)
        return

    if command == "clear_distance_cube":
        clear_distance_cube()
        STATE["last_command"] = command
        STATE["last_command_key"] = command_key
        STATE["last_command_time"] = command_time
        set_status("toio_blender_command_bridge: clear_distance_cube")
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
