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

Implemented on 2026-05-23 for the weekend device short.

- Connects two toio Core Cubes.
- Cube A button captures point A.
- Cube B button captures point B.
- Unity generates endpoint markers and a green distance cube between A and B.
- Supports real mat coordinates when a readable toio mat is available.
- Falls back to demo coordinates when mat ID is unavailable, so the short can still show cube-button driven distance generation.
- Verified by Unotchi:
  - A/B distance cube generation without a mat.
  - A/B distance cube generation with a simple mat.
  - Re-capture after moving A/B with mat reaction.

## Operation Notes

- The Windows BLE plugin can crash Unity during scene transitions or Bluetooth update timing. For recording, open the target scene directly when stability matters.
- `ToioDistanceUnityLab` does not send anything to Blender yet. The next planned step is to send captured A/B points or the distance value to Blender and generate a richer object there.
- If Unity project files are regenerated, `Assembly-CSharp.csproj` may be temporary and should not be treated as the source of truth.
