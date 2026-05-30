# ToioTacticalField

## Purpose

`ToioTacticalField` is the Ordia tabletop auto-tactics prototype scene. Phase 1 records a start anchor and a goal anchor from one observation toio Core Cube, then displays the observed axis in Unity.

## Scene

- Scene: `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity`
- Launcher entry: `ToioLauncher -> Open TacticalField`
- Device: one observation toio Core Cube
- Current boundary: Phase 1 `Start/Goal Observation`

## Implemented Features

- Connects one real toio Core Cube through `CubeManager.MultiConnect(1)`.
- Uses the same cube button twice: first press records the start anchor, second press records the goal anchor.
- Shows the live observation marker, captured start marker, captured goal marker, and observed axis.
- Uses real toio mat coordinates when `cube.pos` is readable.
- Provides `Capture Current Anchor` as a recording fallback and demo coordinates when the mat ID is unavailable.
- Leaves Phase 2 grid generation and Transporter movement for later increments.

## How To Use

1. Open `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity` directly when recording stability matters.
2. Play the scene.
3. Press `Connect Observation Cube`.
4. Put the cube at the start anchor and press its physical button.
5. Move the same cube to the goal anchor and press its physical button again.
6. Check Unity: the start marker, goal marker, and observed axis are visible.

If BLE or mat reading is unstable during setup, use `Capture Current Anchor` to verify the screen flow with fallback coordinates.

## Verification Status

- Confirmed by static implementation review: scene wiring, launcher wiring, Build Settings registration, and Unity batch compilation.
- Needs device check: real cube connection, real mat coordinate capture, and two physical button presses.

## Short Hook

```text
机の上の2点から、戦場の向きを観測します。
```
