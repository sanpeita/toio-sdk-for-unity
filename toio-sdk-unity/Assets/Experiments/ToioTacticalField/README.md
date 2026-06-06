# ToioTacticalField

## Purpose

`ToioTacticalField` is the Ordia tabletop auto-tactics prototype scene. Phase 3.0 converts a start anchor and a goal anchor from one observation toio Core Cube into a fixed `5 x 7` tactical grid, then runs the same cube as a Transporter through the generated center-lane route.

## Scene

- Scene: `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity`
- Launcher entry: `ToioLauncher -> Open TacticalField`
- Device: one observation / Transporter toio Core Cube
- Current boundary: Phase 3.0 `Grid-Step Transporter`

## Implemented Features

- Connects one real toio Core Cube through `CubeManager.MultiConnect(1)`.
- Uses the same cube button twice: first press records the start anchor, second press records the goal anchor.
- Shows the live observation marker, captured start marker, captured goal marker, and observed axis.
- Uses real toio mat coordinates when `cube.pos` is readable.
- Provides `Capture Current Anchor` as a recording fallback and demo coordinates when the mat ID is unavailable.
- Reuses the observation cube as a Transporter.
- Requires `Convert Tactical Field` before Transporter movement so Phase 3 uses the generated battlefield, not only the raw observed goal.
- Builds a Phase 3.0 route from the converted grid's center column and appends the observed goal anchor as the final point.
- Advances the Transporter one grid step at a time with `Cube.TargetMove`.
- Shows route progress such as `step 3/8` while the Transporter is moving.
- Shows `GOAL REACHED` when the live mat position reaches the final goal tolerance.
- Converts the observed axis into a fixed `5 x 7` Unity grid when `Convert Tactical Field` is pressed.
- Rotates the generated grid to follow the observed start-to-goal direction.
- Uses start-side and goal-side colors to keep the generated battlefield readable.
- Clears stale grid cells when anchors are cleared or recaptured.
- Switches automatically to a low-chrome `FIELD VIEW` after conversion so the complete grid remains visible.
- Keeps a compact `Return To Controls` action in `FIELD VIEW`; `Open Field View` can also be used manually from the control-view header.
- Leaves Phase 3.1 as the richer shooting increment after this baseline route is checked on device.

## How To Use

1. Open `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity` directly when recording stability matters.
2. Play the scene.
3. Press `Connect Observation Cube`.
4. Put the cube at the start anchor and press its physical button.
5. Move the same cube to the goal anchor and press its physical button again.
6. Press `Convert Tactical Field`.
7. Check that the scene switches to `FIELD VIEW` and the complete `5 x 7` grid follows the observed axis.
8. Press `Return To Controls`.
9. Return the same cube to the start anchor.
10. Press `Run Grid Route`.
11. Check that the scene switches back to `FIELD VIEW`, route progress advances step by step, and Unity shows `GOAL REACHED` after the final goal anchor.

If BLE or mat reading is unstable during setup, use `Capture Current Anchor` to verify the screen flow with fallback coordinates.

## Verification Status

- Confirmed by static implementation review: scene wiring, launcher wiring, Build Settings registration, and Unity batch compilation.
- Confirmed on device: real cube connection, real mat coordinate capture, and two physical button presses.
- Confirmed in Play Mode with fallback anchors: `5 x 7` grid generation, diagonal orientation, start-side and goal-side colors, and `TACTICAL FIELD CONVERTED` status.
- Confirmed in Play Mode: automatic `FIELD VIEW` transition after conversion, manual `Open Field View`, complete-grid visibility, and `Return To Controls`.
- Confirmed by static implementation review: Phase 3.0 center-lane route generation, route progress status, grid-route gating after conversion, and final `GOAL REACHED` state.
- Needs device check: converted grid position and orientation with two real anchor placements.
- Needs device check: Phase 3.0 grid-step `TargetMove` route and `GOAL REACHED` display.

## Phase 3.1 Candidate

Phase 3.1 should be a plus-one shooting upgrade after Phase 3.0 works on device. Good candidates are route highlight effects, a visible current-step marker, or a short pause / pulse on each locked grid cell. Do not add this before the physical Phase 3.0 route is checked.

## Short Hook

```text
観測した2点の間を、トランスポーターが直進します。
```
