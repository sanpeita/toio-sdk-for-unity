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
- Assigns the connected cubes by connection order: `1 Transporter -> 2 Scout -> 3 Builder`.
- Automatically builds the fixed tactical field after the three roles connect, then sends Transporter / Scout / Builder to the `x=-3` start line.
- Keeps the control screen open after the automatic start-line move; the player opens `FIELD VIEW` manually after confirming all three cubes moved.
- Provides `Retry Start Line` as a recovery control. It checks each role's current mat position and resends start-line movement only to roles that are not yet readable at their assigned start cell.
- Keeps the short left-right role appeal available as an Inspector option, but leaves it off by default to reduce BLE command density after connection.
- Shows a Japanese setup guide using the bundled Noto Sans JP font asset.
- Keeps `Capture Current Anchor` out of the main Phase 5 flow; fixed-map setup replaces the old observation-first flow.
- Uses `Set Fixed Lines` as a manual confirmation of the fixed player line and goal/enemy line.
- `Convert Tactical Field` remains available as a manual rebuild button, and applies the fixed lines to create a `7 x 5` logical field matching `x=-3..3` and `y=2..-2`.
- Renders start-side and goal-side colors so the player-side and fixed far side are easy to read.
- Generates hidden random obstacle cells when `Convert Tactical Field` is pressed.
- Places the player-side roles on the `x=-3` start line: Scout `(-3,1)`, Transporter `(-3,0)`, Builder `(-3,-1)`.
- Moves Scout one cell at a time with separate Forward / Back / Left / Right controls.
- Defines Scout `Forward` as `+X`, from the player start line toward the enemy / goal line. `Left` is `+Y`; `Right` is `-Y`.
- Shows the same Scout controls in `FIELD VIEW` after `Convert Tactical Field`, so Scout can move and scan without returning to the control screen.
- Uses the toio simple playmat Position ID range `x=98..402`, `y=142..358` to calculate the center of each `7 x 5` logical cell.
- Lets Scout move into hidden obstacle cells for the Saturday discovery demo; obstacles are discovery targets, not physical blockers in this phase.
- Blocks Scout movement into the friendly Transporter / Builder start cells to reduce physical cube collisions.
- Scans Scout's Manhattan radius of two cells and reveals detected obstacle cells on the Unity field.
- Reveals a placeholder enemy marker if Scout's scan radius reaches the fixed enemy-side marker.
- Keeps the Transporter grid-route movement separate from Scout movement.
- Keeps `Run Grid Route` available as the regression check for `GOAL REACHED`.

## Saturday Test Flow

1. Open `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity` directly.
2. Play the scene.
3. Power on the three player-side Core Cubes.
4. Press `Connect Friendly Team`.
5. Check the UI assignment order: `1 Transporter -> 2 Scout -> 3 Builder`. The current build uses connection order for role assignment.
6. Wait for the automatic fixed-field conversion and start-line move.
7. Check that Unity shows the fixed logical field: `x=-3..3`, `y=2..-2`.
8. Check that the physical roles were sent to the player start line: Scout `(-3,1)`, Transporter `(-3,0)`, Builder `(-3,-1)`.
9. If a role did not reach its assigned start cell, press `Retry Start Line`.
10. Press `Open Field View` manually after the three start-line moves are confirmed.
11. Use the `FIELD VIEW` Scout controls to move one cell at a time.
12. Press `Scan` after each move and check that detected obstacle cells appear on the Unity field.
13. Use the revealed obstacles as the Saturday short's stage overview.
14. Return the Transporter cube to `(-3,0)` if it was moved during Scout testing.
15. Press `Run Grid Route`.
16. Confirm route progress advances toward the `x=2` goal line and Unity shows `GOAL REACHED`.

`Set Fixed Lines` and `Convert Tactical Field` are optional in normal use. They remain on screen as manual recovery / rebuild controls.

## Crash Note

- A crash reported on 2026-06-13 was traced during device testing to discharged Core Cubes. Keep all three cubes charged before the friendly-team connection test.
- The scene still stops async follow-up work when the controller is destroyed and reduces the default post-connect motor command burst by skipping role appeal unless explicitly enabled.

## Verification Status

- Confirmed by static implementation review: Phase 5 UI entry points, Scout movement controls, scan radius logic, hidden obstacle generation, and detected obstacle rendering.
- Confirmed by static implementation review: the Transporter route now uses logical cells from `(-3,0)` to `(2,0)`.
- Confirmed by static implementation review: connected roles auto-convert the fixed field and queue start-line moves to Scout `(-3,1)`, Transporter `(-3,0)`, Builder `(-3,-1)`.
- Confirmed by `dotnet restore` + `dotnet build Assembly-CSharp.csproj`: C# build succeeds with no `ToioTacticalField` errors.
- Confirmed by 2026-06-13 device test: Scout movement and scan reveal work after manual start-line recovery.
- Known device-test issue: one cube may ignore automatic start-line `TargetMove` and `Retry Start Line` even though BLE connection and ID acquisition are visible. Test another Core Cube / firmware state before adding more setup logic.
- Needs Unity Editor asset import / Play Mode check after the bundled `Resources/Fonts/NotoSansJP-VF.ttf` is imported.
- Needs future check: Transporter `GOAL REACHED` after Scout-driven map reveal.

## Device Logs

- `Logs/2026-06-13-phase5-device-test.md`

## Saturday Short Hook

```text
toioのScoutが、見えない障害物を探します。
今日は半径2マスをサーチして、Unityの戦域に反映します。
```
