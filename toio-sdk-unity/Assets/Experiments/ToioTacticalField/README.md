# ToioTacticalField

## Purpose

`ToioTacticalField` is the Ordia tabletop auto-tactics prototype scene. Phase 1.1 records a start anchor and a goal anchor from one observation toio Core Cube, then reuses the same cube as a straight-line Transporter.

## Scene

- Scene: `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity`
- Launcher entry: `ToioLauncher -> Open TacticalField`
- Device: one observation / Transporter toio Core Cube
- Current boundary: Phase 1.1 `Straight Transporter Victory`

## Implemented Features

- Connects one real toio Core Cube through `CubeManager.MultiConnect(1)`.
- Uses the same cube button twice: first press records the start anchor, second press records the goal anchor.
- Shows the live observation marker, captured start marker, captured goal marker, and observed axis.
- Uses real toio mat coordinates when `cube.pos` is readable.
- Provides `Capture Current Anchor` as a recording fallback and demo coordinates when the mat ID is unavailable.
- Reuses the observation cube as a Transporter and sends it directly to the observed goal with `Cube.TargetMove`.
- Shows `GOAL REACHED` when the live mat position reaches the goal tolerance.
- Leaves Phase 2 grid generation and Phase 3 grid-step Transporter movement for later increments.

## How To Use

1. Open `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity` directly when recording stability matters.
2. Play the scene.
3. Press `Connect Observation Cube`.
4. Put the cube at the start anchor and press its physical button.
5. Move the same cube to the goal anchor and press its physical button again.
6. Return the same cube to the start anchor.
7. Press `Run Transporter`.
8. Check the physical cube movement and Unity `GOAL REACHED` display.

If BLE or mat reading is unstable during setup, use `Capture Current Anchor` to verify the screen flow with fallback coordinates.

## Verification Status

- Confirmed by static implementation review: scene wiring, launcher wiring, Build Settings registration, and Unity batch compilation.
- Confirmed on device: real cube connection, real mat coordinate capture, and two physical button presses.
- Needs device check: straight `TargetMove` run and `GOAL REACHED` display.

## Short Hook

```text
観測した2点の間を、トランスポーターが直進します。
```
