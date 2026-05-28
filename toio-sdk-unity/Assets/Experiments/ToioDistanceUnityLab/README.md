# ToioDistanceUnityLab

## Purpose

`ToioDistanceUnityLab` is the distance visualization scene for `toio x Unity x Blender`.

Two toio Core Cubes are connected at the same time. Press Cube A to capture point A, press Cube B to capture point B, and Unity visualizes the distance between them with endpoint markers, a green distance cube, and live distance text. The same A/B distance is also sent to Blender through the JSONL bridge so Blender can generate a richer distance object.

## Scene

- Scene: `Assets/Experiments/ToioDistanceUnityLab/ToioDistanceUnityLab.unity`
- Launcher entry: `ToioLauncher -> Open DistanceUnityLab`
- Devices: two toio Core Cubes
- Output: Unity visualization plus Blender rich distance object

## Implemented Features

- Connects two real toio Core Cubes through `CubeManager.MultiConnect(2)`.
- Uses Cube A button to capture point A.
- Uses Cube B button to capture point B.
- Shows live/captured A and B coordinates.
- Generates a green Unity cube between A and B to represent the distance.
- Recalculates the distance when A/B are captured again after moving the cubes.
- Uses real toio mat coordinates when `cube.pos` is readable.
- Uses fallback coordinates when the mat ID is missing, so button-driven distance generation still works without a mat.
- Writes `distance_cube` commands to `BlenderBridge/toio_blender_bridge_commands.jsonl`.
- Blender bridge creates a beveled green distance bar, soft glow shell, cyan highlight, endpoint markers, and a distance label.

## How To Use

1. Open Blender.
2. Run `BlenderBridge/toio_blender_command_bridge.py` in Blender.
3. Open `Assets/Experiments/ToioDistanceUnityLab/ToioDistanceUnityLab.unity` directly when recording stability matters.
4. Play the scene.
5. Press `Connect Cubes`.
6. Put both cubes on a readable mat if available. Without a mat, fallback coordinates are used.
7. Press Cube A's button to capture point A.
8. Press Cube B's button to capture point B.
9. Check Unity: A marker, B marker, green distance cube, and distance value are updated.
10. Check Blender: a richer distance cube is generated between the same A/B points.
11. Move A/B and press the buttons again to regenerate the distance cube in both Unity and Blender.

## 2026-05-23 Verification

- Confirmed: A/B distance cube generation without a mat.
- Confirmed: A/B distance cube generation with a simple mat.
- Confirmed: A/B distance cube regeneration after moving A/B while the simple mat reacted.
- Note: If the UI shows `[mat]`, real mat coordinates were used. If it shows `[fallback]`, the cube was generated from demo coordinates because no mat ID was available.

## 2026-05-24 Blender Bridge

- Added `distance_cube` command output from Unity.
- Added `clear_distance_cube` command when Unity points are cleared.
- Added Blender-side rich distance object generation.
- The intended recording hook is: `Press A/B on two toio cubes -> distance cubes appear in both Unity and Blender`.

## Short Framing

Today's hook:

```text
Two toio cubes define A and B. Unity shows the distance immediately, and Blender turns the same distance into a richer beveled cube object.
```

## Notes

- This scene keeps the Unity distance cube as-is and adds Blender output through the bridge.
- The next step is visual polish: tune bevel width, material colors, camera framing, or add measurement ticks.
- Local VFX texture polish notes are in `VFX_Texture_Library_README.md`. The downloaded texture payload is copied under `Assets/External/VFX_Texture_Library_v1.0.0` for local Unity use, but is intentionally ignored by git because the source license prohibits redistribution.
- Future update candidate: create a dedicated `.blend` file with the bridge bootstrap saved into it, so `toio_blender_command_bridge.py` does not need to be manually run every time Blender starts.
- The Windows BLE plugin may be unstable during scene transition. If recording stability matters, open `ToioDistanceUnityLab.unity` directly.
