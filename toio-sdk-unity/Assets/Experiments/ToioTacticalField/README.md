# ToioTacticalField

## Purpose

`ToioTacticalField` is the Ordia tabletop auto-tactics prototype scene. Phase 4 connects the player-side Transporter / Scout / Builder cubes, makes each connected cube briefly turn left-right for filming clarity, then keeps the observation / Transporter cube responsible for tactical field conversion and the grid-route victory.

## Scene

- Scene: `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity`
- Launcher entry: `ToioLauncher -> Open TacticalField`
- Device: three player-side toio Core Cubes
- Current boundary: Phase 4 `Friendly 3-Piece Recognition`

## Implemented Features

- Connects three real toio Core Cubes one by one for the Phase 4 friendly team.
- Assigns the connected cubes, ordered by BLE address, to Transporter, Scout, and Builder.
- Runs a short left-right turn appeal after each role connection so the physical cube-to-role pairing is visible without sound.
- Uses the Transporter cube button twice: first press records the start anchor, second press records the goal anchor.
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
- Keeps Scout and Builder tactical behavior out of scope for Phase 4.

## How To Use

1. Open `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity` directly when recording stability matters.
2. Play the scene.
3. Press `Connect Friendly Team`.
4. Check that Transporter, Scout, and Builder are listed in the role status and that each cube briefly turns left-right after connection.
5. Put the Transporter cube at the start anchor and press its physical button.
6. Move the same Transporter cube to the goal anchor and press its physical button again.
7. Press `Convert Tactical Field`.
8. Check that the scene switches to `FIELD VIEW` and the complete `5 x 7` grid follows the observed axis.
9. Press `Return To Controls`.
10. Return the Transporter cube to the start anchor.
11. Press `Run Grid Route`.
12. Check that the scene switches back to `FIELD VIEW`, route progress advances step by step, and Unity shows `GOAL REACHED` after the final goal anchor.

If BLE or mat reading is unstable during setup, use `Capture Current Anchor` to verify the screen flow with fallback coordinates.

## Verification Status

- Confirmed by static implementation review: scene wiring, launcher wiring, Build Settings registration, and Unity batch compilation.
- Confirmed on device: real cube connection, real mat coordinate capture, and two physical button presses.
- Confirmed in Play Mode with fallback anchors: `5 x 7` grid generation, diagonal orientation, start-side and goal-side colors, and `TACTICAL FIELD CONVERTED` status.
- Confirmed in Play Mode: automatic `FIELD VIEW` transition after conversion, manual `Open Field View`, complete-grid visibility, and `Return To Controls`.
- Confirmed by static implementation review: Phase 4 friendly role connection flow, short cube appeal commands, role status display, and preservation of the Phase 3.0 grid-route victory path.
- Needs device check: converted grid position and orientation with two real anchor placements.
- Needs device check: three-cube friendly role assignment and short left-right appeal on the physical cubes.
- Needs device check: Phase 4 Transporter grid-step `TargetMove` route and `GOAL REACHED` display.

## Phase 5 Candidate

Phase 5 should give Scout one visible discovery effect after the Phase 4 friendly-team recognition flow works on device. Do not add Scout search behavior to Phase 4.

## Short Hook

```text
昨日まで1駒だった机上に、今日は3つの役割を並べます。
```
