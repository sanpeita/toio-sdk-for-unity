# ToioTacticalField

## Purpose

`ToioTacticalField` is the Ordia tabletop auto-tactics prototype scene. Phase 5.3 keeps the scanned terrain route loop, then adds Scout auto-scan movement and automatic Transporter launch when a safe scanned route opens.

The repeated victory promise remains:

> The Transporter reaches the goal from the player-side field.

## Scene

- Scene: `Assets/Experiments/ToioTacticalField/ToioTacticalField.unity`
- Launcher entry: `ToioLauncher -> Open TacticalField`
- Device: three player-side toio Core Cubes
- Current boundary: Phase 5.3 `Scout Auto Scan / Transporter Auto Launch`
- Mat: space-themed toio playmat, with logical field coordinates `x=-3..3` and `y=2..-2`
- Player start line: `x=-3`, `y=2..-2`
- Enemy standby / player goal line: `x=3`, `y=2..-2`

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
- Generates terrain cells when `Convert Tactical Field` is pressed: unknown until scanned, then randomized plain / rough / debris.
- Places the player-side roles on the `x=-3` start line: Scout `(-3,1)`, Transporter `(-3,0)`, Builder `(-3,-1)`.
- Moves Scout one cell at a time with separate Forward / Back / Left / Right controls.
- Defines Scout `Forward` as `+X`, from the player start line toward the enemy / goal line. `Left` is `+Y`; `Right` is `-Y`.
- Shows the same Scout controls in `FIELD VIEW` after `Convert Tactical Field`, so Scout can move and scan without returning to the control screen.
- Uses the toio simple playmat Position ID range `x=98..402`, `y=142..358` to calculate the center of each `7 x 5` logical cell.
- Blocks Scout movement into unknown cells and debris. Scout must scan first, then move into known passable cells.
- Rough terrain remains passable, but movement speed is reduced while entering that cell.
- Blocks Scout movement into the friendly Transporter / Builder start cells to reduce physical cube collisions.
- Scans Scout's Manhattan radius of two cells and reveals terrain on the Unity field.
- Reveals a placeholder enemy marker if Scout's scan radius reaches the fixed enemy-side marker.
- Computes the Transporter route only through scanned passable cells.
- Treats the friendly Scout / Builder occupied cells as blocked while the Transporter calculates its shortest route.
- Stops Transporter movement when the scanned route is missing or cut by debris.
- Adds Transporter start / stop controls.
- Keeps Auto Transporter enabled by default. When scan results open a safe route, the Transporter starts automatically from `(-3,0)`.
- Adds `Scout Auto`, which scans, moves through known passable cells, and follows the current waypoint plan: `(-3,1) -> (1,1) -> (1,-2) -> (-2,-2)`.
- Keeps Builder gameplay unimplemented, with a debug self-appeal button only.

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
10. Press `盤面を見る` manually after the three start-line moves are confirmed.
11. Press `scan` before moving Scout; unknown cells cannot be entered.
12. Use the `FIELD VIEW` Scout controls to move one cell at a time through scanned passable cells.
13. Check that scanned cells reveal as plain / rough / debris on the Unity field.
14. Use Scout scan until a passable route reaches the `x=3` goal line.
15. Return the Transporter cube to `(-3,0)` if it was moved during Scout testing.
16. Either let Auto Transporter launch when the route opens, or press `移動開始` manually.
17. Confirm route progress advances only through scanned passable cells and Unity shows `GOAL REACHED`.
18. Press `移動中断` to stop the Transporter during a route test.
19. Press `Scout自動` to test the automated scan route. Press it again to request stop.

`Set Fixed Lines` and `Convert Tactical Field` are optional in normal use. They remain on screen as manual recovery / rebuild controls.

## Crash Note

- A crash reported on 2026-06-13 was traced during device testing to discharged Core Cubes. Keep all three cubes charged before the friendly-team connection test.
- The scene still stops async follow-up work when the controller is destroyed and reduces the default post-connect motor command burst by skipping role appeal unless explicitly enabled.

## Verification Status

- Confirmed by static implementation review: Phase 5 UI entry points, Scout movement controls, scan radius logic, hidden obstacle generation, and detected obstacle rendering.
- Confirmed by static implementation review: Phase 5.1 route planning uses scanned passable cells instead of the fixed center line.
- Confirmed by static implementation review: unknown cells and debris block movement; rough cells reduce move speed.
- Confirmed by static implementation review: the fixed far-side goal line is `x=3`, and randomized terrain is generated on each tactical-field conversion by default.
- Confirmed by static implementation review: Transporter pathfinding avoids the friendly Scout / Builder occupied cells.
- Confirmed by static implementation review: Scout auto route and Auto Transporter entry points are available from both the control view and `FIELD VIEW`.
- Confirmed by static implementation review: Transporter start / stop controls and Builder debug appeal are available in the JP-priority UI.
- Confirmed by static implementation review: connected roles auto-convert the fixed field and queue start-line moves to Scout `(-3,1)`, Transporter `(-3,0)`, Builder `(-3,-1)`.
- Confirmed by `dotnet build Assembly-CSharp.csproj -v:minimal`: C# build succeeds with no `ToioTacticalField` errors.
- Confirmed by 2026-06-13 device test: Scout movement and scan reveal work after manual start-line recovery.
- Known device-test issue: one cube may ignore automatic start-line `TargetMove` and `Retry Start Line` even though BLE connection and ID acquisition are visible. Test another Core Cube / firmware state before adding more setup logic.
- Needs Unity Editor asset import / Play Mode check after the bundled `Resources/Fonts/NotoSansJP-VF.ttf` is imported.
- Needs Unity Editor Play Mode visual check for UI panel overlap after Phase 5.1 controls.
- Needs future device check: Transporter `GOAL REACHED` after Scout-driven map reveal.
- Needs future device check: Scout auto route timing and Auto Transporter launch during real Core Cube movement.

## Device Logs

- `Logs/2026-06-13-phase5-device-test.md`
- `Logs/2026-06-14-phase5-1-scanned-route.md`
- `Logs/2026-06-20-phase5-2-foundation.md`
- `Logs/2026-06-20-phase5-3-auto-scout-transporter.md`

## Saturday Short Hook

```text
toioのScoutが、見えない障害物を探します。
今日は半径2マスをサーチして、Unityの戦域に反映します。
```
