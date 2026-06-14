# 2026-06-14 Phase 5.1 Scanned Route Work Log

## Summary

Phase 5.1 starts from the Phase 5 Scout discovery loop and connects scan results to movement rules.

The new rule direction is:

- Unknown cells are blocked.
- Plain cells are normal passable terrain.
- Rough cells are passable but use half movement speed while entering the cell.
- Debris cells are blocked.
- Transporter can start only when a scanned passable route reaches the `x=2` goal line.
- Builder gameplay remains unimplemented; only a debug self-appeal button is available.

## Implemented

- Added terrain state for plain / rough / debris, with unknown derived from unscanned cells.
- Added scanned-cell tracking.
- Scout scan now reveals every cell in Manhattan radius 2.
- Scout movement now requires scanned passable cells and rejects unknown / debris.
- Transporter route planning now uses BFS over scanned passable cells instead of the fixed center line.
- Transporter stops with a status message when the route is missing or cut by debris.
- Added Transporter start / stop controls.
- Added Builder debug appeal control.
- Updated visible player-facing UI labels toward JP-first wording.
- Kept `GOAL REACHED` as the visible victory phrase.

## Verification

- `dotnet build Assembly-CSharp.csproj -v:minimal` succeeded.
- Final build result: 0 warnings, 0 errors.
- Existing Unity Editor log showed `Tundra build success`, `Mono: successfully reloaded assembly`, and the `ToioTacticalField` scene loaded.

## Needs Visual / Device Check

- Unity Editor Play Mode UI overlap check after the new right-side control panels. Codex could confirm the scene was open in Unity, but the Play-mode screenshot capture did not complete in this run.
- Device check for Scout scan -> Transporter route -> `GOAL REACHED`.
- Device check that rough-cell speed reduction is visually readable enough for Shorts.
