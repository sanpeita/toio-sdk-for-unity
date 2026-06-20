# 2026-06-20 Phase 5.3 Auto Scout / Auto Transporter Work Log

## Summary

Phase 5.3 adds two automation layers on top of Phase 5.2:

- Scout can run an automatic scan route.
- Transporter can start automatically when a scanned, passable, friendly-safe route reaches the goal line.

This keeps the current tactical goal visible on the tabletop:

> Scout opens the route, then the Transporter moves when the route is ready.

## Implemented

- Updated the scene label to `Phase 5.3`.
- Added `Scout自動` controls in both the control view and `FIELD VIEW`.
- Added `Auto搬送` controls in both the control view and `FIELD VIEW`.
- Auto Transporter is enabled by default.
- Scout auto route starts with scan, then follows the current waypoint plan:
  - `(-3,1) -> (1,1) -> (1,-2) -> (-2,-2)`
- Scout auto route only moves through scanned passable cells.
- If the direct known route is not open, Scout chooses an adjacent passable cell that reveals the most unknown cells.
- Scout auto movement stops when the Transporter starts.
- Transporter auto launch reuses the same route checks as manual start:
  - Transporter is connected.
  - Fixed tactical field exists.
  - Transporter is readable at `(-3,0)`.
  - The route is scanned and passable.
  - Scout / Builder occupied cells are avoided.

## Verification

- Static implementation review completed.
- C# build still needs to be run after this log.

## Device Check Needed

- Confirm Scout auto movement timing with real Core Cubes.
- Confirm Auto Transporter starts only after the route is visibly open.
- Confirm Scout stops or no longer interferes after Transporter starts.
- Confirm UI panels remain usable in `FIELD VIEW` during recording.

## Next Candidate

After device verification, the next implementation candidate is Phase 6: Builder terrain conversion.

Builder should begin with the smallest visible behavior:

- Move from `(-3,-1)` toward `(-1,1)`.
- If the next cell is rough, convert it to plain after 1 second.
- If the next cell is debris, convert it to rough after 2 seconds.
- Mark each converted cell as already changed, so it cannot be changed twice.
