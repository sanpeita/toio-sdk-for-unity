# ToioTacticalField

## Purpose

`ToioTacticalField` is the Ordia tabletop auto-tactics prototype scene. Phase 5 keeps the Phase 4 player-side team recognition flow, then gives Scout one visible discovery loop for Saturday recording: move one grid cell, scan a radius of two cells, and reveal hidden obstacles on the Unity field.

The repeated victory promise remains:

> The Transporter reaches the goal from the player-side field.

## Scene

- Scene: `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity`
- Launcher entry: `ToioLauncher -> Open TacticalField`
- Device: three player-side toio Core Cubes
- Current boundary: Phase 5 `Scout Discovery Effect`
- Mat: space-themed toio playmat, with logical field coordinates `x=-3..3` and `y=2..-2`

## Implemented Features

- Connects three real toio Core Cubes one by one for the friendly team.
- Assigns the connected cubes, ordered by BLE address, to Transporter, Scout, and Builder.
- Runs a short left-right turn appeal after each role connection so the physical cube-to-role pairing is visible without sound.
- Shows a Japanese setup guide using the bundled Noto Sans JP font asset.
- Uses the Transporter cube button twice: first press records the start anchor, second press records the goal anchor.
- Converts the observed axis into a `7 x 5` logical field matching `x=-3..3` and `y=2..-2`.
- Renders start-side and goal-side colors so the player-side and fixed far side are easy to read.
- Generates hidden random obstacle cells when `Convert Tactical Field` is pressed.
- Places Scout at logical `(-1,2)`, with Transporter at `(0,2)` and Builder at `(1,2)` as the intended start-line setup.
- Moves Scout one cell at a time with separate Forward / Back / Left / Right controls.
- Scans Scout's Manhattan radius of two cells and reveals detected obstacle cells on the Unity field.
- Reveals a placeholder enemy marker if Scout's scan radius reaches the fixed enemy-side marker.
- Keeps the Transporter grid-route movement separate from Scout movement.
- Keeps `Run Grid Route` available as the regression check for `GOAL REACHED`.

## Saturday Test Flow

1. Open `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity` directly.
2. Play the scene.
3. Press `Connect Friendly Team`.
4. Place the three player-side cubes on the start line: Scout / Transporter / Builder, with Transporter in the center.
5. Put the Transporter cube at the start anchor and press its physical button.
6. Move the same Transporter cube to the goal anchor and press its physical button again.
7. Press `Convert Tactical Field`.
8. Check that the field uses logical coordinates `x=-3..3` and `y=2..-2`.
9. Use Scout controls to move one cell at a time.
10. Press `Scan` after each move and check that detected obstacle cells appear on the Unity field.
11. Use the revealed obstacles as the Saturday short's stage overview.
12. Return the Transporter cube to the start anchor.
13. Press `Run Grid Route`.
14. Confirm route progress advances and Unity shows `GOAL REACHED`.

If BLE or mat reading is unstable during setup, use `Capture Current Anchor` to verify the screen flow with fallback coordinates.

## Verification Status

- Confirmed by static implementation review: Phase 5 UI entry points, Scout movement controls, scan radius logic, hidden obstacle generation, and detected obstacle rendering.
- Confirmed by static implementation review: the Transporter route now uses logical center cells `(0,2)` through `(0,-2)` before the observed goal anchor.
- Confirmed by `dotnet restore` + `dotnet build Assembly-CSharp.csproj`: C# build succeeds with no `ToioTacticalField` errors.
- Needs Unity Editor asset import / Play Mode check after the bundled `Resources/Fonts/NotoSansJP-VF.ttf` is imported.
- Needs Play Mode check: fallback anchors, field conversion, Scout movement, scan reveal, and Transporter `GOAL REACHED`.
- Needs device check: three-cube role assignment, Scout physical movement, real mat coordinate alignment, and Transporter route recovery after map construction.

## Saturday Short Hook

```text
toioのScoutが、見えない障害物を探します。
今日は半径2マスをサーチして、Unityの戦域に反映します。
```
