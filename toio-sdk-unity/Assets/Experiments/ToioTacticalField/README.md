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
- Player start line: `x=-3`, `y=2..-2`
- Enemy standby / player goal line: `x=2`, `y=2..-2`

## Implemented Features

- Connects three real toio Core Cubes one by one for the friendly team.
- Assigns the connected cubes by BLE address order: `1 Transporter -> 2 Scout -> 3 Builder`.
- Runs a short left-right turn appeal after each role connection so the physical cube-to-role pairing is visible without sound.
- Shows a Japanese setup guide using the bundled Noto Sans JP font asset.
- Keeps `Capture Current Anchor` out of the main Phase 5 flow; fixed-map setup replaces the old observation-first flow.
- Uses `Set Fixed Lines` as a manual confirmation of the fixed player line and goal/enemy line.
- `Convert Tactical Field` automatically applies the fixed lines and creates a `7 x 5` logical field matching `x=-3..3` and `y=2..-2`.
- Renders start-side and goal-side colors so the player-side and fixed far side are easy to read.
- Generates hidden random obstacle cells when `Convert Tactical Field` is pressed.
- Places the player-side roles on the `x=-3` start line: Scout `(-3,1)`, Transporter `(-3,0)`, Builder `(-3,-1)`.
- Moves Scout one cell at a time with separate Forward / Back / Left / Right controls.
- Shows the same Scout controls in `FIELD VIEW` after `Convert Tactical Field`, so Scout can move and scan without returning to the control screen.
- Scans Scout's Manhattan radius of two cells and reveals detected obstacle cells on the Unity field.
- Reveals a placeholder enemy marker if Scout's scan radius reaches the fixed enemy-side marker.
- Keeps the Transporter grid-route movement separate from Scout movement.
- Keeps `Run Grid Route` available as the regression check for `GOAL REACHED`.

## Saturday Test Flow

1. Open `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity` directly.
2. Play the scene.
3. Power on the three player-side Core Cubes.
4. Press `Connect Friendly Team`.
5. Check the UI assignment order: `1 Transporter -> 2 Scout -> 3 Builder`. The current build uses BLE address order, not the physical order on the desk.
6. Press `Convert Tactical Field`.
7. Check that Unity shows the fixed logical field: `x=-3..3`, `y=2..-2`.
8. Place Scout / Transporter / Builder on the player start line `x=-3`.
9. Recommended placement: Scout `(-3,1)`, Transporter `(-3,0)`, Builder `(-3,-1)`.
10. Use the `FIELD VIEW` Scout controls to move one cell at a time.
11. Press `Scan` after each move and check that detected obstacle cells appear on the Unity field.
12. Use the revealed obstacles as the Saturday short's stage overview.
13. Return the Transporter cube to `(-3,0)`.
14. Press `Run Grid Route`.
15. Confirm route progress advances toward the `x=2` goal line and Unity shows `GOAL REACHED`.

`Set Fixed Lines` is optional in normal use. It is there to make the fixed-map assumption visible on screen before conversion.

## Verification Status

- Confirmed by static implementation review: Phase 5 UI entry points, Scout movement controls, scan radius logic, hidden obstacle generation, and detected obstacle rendering.
- Confirmed by static implementation review: the Transporter route now uses logical cells from `(-3,0)` to `(2,0)`.
- Confirmed by `dotnet restore` + `dotnet build Assembly-CSharp.csproj`: C# build succeeds with no `ToioTacticalField` errors.
- Needs Unity Editor asset import / Play Mode check after the bundled `Resources/Fonts/NotoSansJP-VF.ttf` is imported.
- Needs Play Mode check: fallback anchors, field conversion, Scout movement, scan reveal, and Transporter `GOAL REACHED`.
- Needs device check: three-cube role assignment, Scout physical movement, real mat coordinate alignment, and Transporter route recovery after map construction.

## Saturday Short Hook

```text
toioのScoutが、見えない障害物を探します。
今日は半径2マスをサーチして、Unityの戦域に反映します。
```
