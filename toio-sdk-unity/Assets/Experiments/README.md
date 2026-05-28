# ToioJetHand Experiments

This folder contains Unotchi's toio device experiment scenes.

## Project Location

- Unity project: `C:\Users\unocy\Documents\toio-sdk-for-unity\toio-sdk-unity`
- Experiment root: `Assets/Experiments`
- Launcher scene: `Assets/Experiments/ToioLauncher/ToioLauncher.unity`

## Scenes

| Scene | Path | Purpose |
| --- | --- | --- |
| `ToioLauncher` | `Assets/Experiments/ToioLauncher/ToioLauncher.unity` | Entry scene for switching between experiment scenes. |
| `ToioLeftHandLab` | `Assets/Experiments/ToioLeftHandLab/ToioLeftHandLab.unity` | toio left-hand input experiment for Minecraft/WASD-style control. |
| `ToioBlenderLab` | `Assets/Experiments/ToioBlenderLab/ToioBlenderLab.unity` | toio input experiment for Blender operation and Blender bridge work. |
| `ToioDistanceUnityLab` | `Assets/Experiments/ToioDistanceUnityLab/ToioDistanceUnityLab.unity` | Two-cube A/B capture scene that visualizes the distance as a Unity cube. |

## Implemented Notes

### ToioDistanceUnityLab

Implemented on 2026-05-23 for the weekend device short. Extended on 2026-05-24 for the `toio x Unity x Blender` connection line.

- Connects two toio Core Cubes.
- Cube A button captures point A.
- Cube B button captures point B.
- Unity generates endpoint markers and a green distance cube between A and B.
- Supports real mat coordinates when a readable toio mat is available.
- Falls back to demo coordinates when mat ID is unavailable, so the short can still show cube-button driven distance generation.
- Sends captured A/B distance data to Blender through `BlenderBridge/toio_blender_bridge_commands.jsonl`.
- Blender bridge command `distance_cube` creates a richer distance object: beveled green bar, soft glow shell, cyan highlight, endpoint markers, and distance label.
- Verified by Unotchi:
  - A/B distance cube generation without a mat.
  - A/B distance cube generation with a simple mat.
  - Re-capture after moving A/B with mat reaction.

## Operation Notes

- The Windows BLE plugin can crash Unity during scene transitions or Bluetooth update timing. For recording, open the target scene directly when stability matters.
- For Blender output, run `BlenderBridge/toio_blender_command_bridge.py` inside Blender before pressing A/B in Unity.
- `ToioDistanceUnityLab/VFX_Texture_Library_README.md` records the 2026-05-28 local intake of C&R Creative Studios' free VFX texture library. The actual library payload is kept under `Assets/External/VFX_Texture_Library_v1.0.0` and ignored by git.
- Future update candidate: save the bridge bootstrap into a dedicated `.blend` file so the JSONL watcher starts automatically when that Blender file is opened. Until then, run the script once per Blender session.
- If Unity project files are regenerated, `Assembly-CSharp.csproj` may be temporary and should not be treated as the source of truth.
