# ToioDistanceUnityLab

## Purpose

`ToioDistanceUnityLab` is the day-one distance visualization scene for `toio x Unity`.

Two toio Core Cubes are connected at the same time. Press Cube A to capture point A, press Cube B to capture point B, and Unity visualizes the distance between them with endpoint markers, a green distance cube, and live distance text.

## Scene

- Scene: `Assets/Experiments/ToioDistanceUnityLab/ToioDistanceUnityLab.unity`
- Launcher entry: `ToioLauncher -> Open DistanceUnityLab`
- Devices: two toio Core Cubes
- Output: Unity-only visualization for the 2026-05-23 device short

## Implemented Features

- Connects two real toio Core Cubes through `CubeManager.MultiConnect(2)`.
- Uses Cube A button to capture point A.
- Uses Cube B button to capture point B.
- Shows live/captured A and B coordinates.
- Generates a green Unity cube between A and B to represent the distance.
- Recalculates the distance when A/B are captured again after moving the cubes.
- Uses real toio mat coordinates when `cube.pos` is readable.
- Uses fallback coordinates when the mat ID is missing, so button-driven distance generation still works without a mat.

## How To Use

1. Open `Assets/Experiments/ToioDistanceUnityLab/ToioDistanceUnityLab.unity` directly when recording stability matters.
2. Play the scene.
3. Press `Connect Cubes`.
4. Put both cubes on a readable mat if available. Without a mat, fallback coordinates are used.
5. Press Cube A's button to capture point A.
6. Press Cube B's button to capture point B.
7. Check the Unity view: A marker, B marker, green distance cube, and distance value are updated.
8. Move A/B and press the buttons again to regenerate the distance cube.

## 2026-05-23 Verification

- Confirmed: A/B distance cube generation without a mat.
- Confirmed: A/B distance cube generation with a simple mat.
- Confirmed: A/B distance cube regeneration after moving A/B while the simple mat reacted.
- Note: If the UI shows `[mat]`, real mat coordinates were used. If it shows `[fallback]`, the cube was generated from demo coordinates because no mat ID was available.

## Short Framing

Today's hook:

```text
Two toio cubes define A and B. Unity turns the distance between them into a visible green cube. Today is Unity visualization; tomorrow can extend the same distance object into Blender.
```

## Notes

- This scene intentionally does not send anything to Blender yet.
- The next step is `toio x Unity x Blender`: send the captured A/B points or distance value to the Blender bridge and generate a richer object there.
- The Windows BLE plugin may be unstable during scene transition. If recording stability matters, open `ToioDistanceUnityLab.unity` directly.
