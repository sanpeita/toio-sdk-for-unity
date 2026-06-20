# 2026-06-20 Phase 5.2 Foundation Work Log

## Summary

Phase 5.2 keeps the current Phase 5.1 scanned-terrain route loop, then fixes the field foundation before adding deeper Scout / Builder automation.

Today's priority:

- Correct the far-side goal / enemy standby line from `x=2` to `x=3`.
- Randomize plain / rough / debris terrain on each tactical-field conversion.
- Make the Transporter shortest-route search avoid friendly occupied cells.
- Replan the following phases from the target experience: Scout scans, Builder changes terrain, Transporter auto-launches when a safe route opens.

## Implemented

- Updated `EnemyGoalLineX` to `3`.
- Updated fixed-line goal source text to follow the `EnemyGoalLineX` constant.
- Changed Phase 5 terrain generation so it uses a fresh random seed on each tactical-field conversion by default.
- Kept the old deterministic seed path available through `randomizePhase5TerrainEachConvert = false` for repeatable debug runs.
- Updated Transporter BFS route planning so Scout's current logical cell and Builder's start / occupied cell are blocked.
- Updated the Unity-side README to document `x=3`, randomized terrain, and friendly route avoidance.
- Added repo-side phase replan memo: `toioJetHand/toioTacticalField/docs/phase-replan-2026-06-20.md`.

## Verification

- Static implementation review completed.
- C# build still needs to be run after this change.
- Device check still needs to confirm:
  - goal column appears at `x=3`,
  - terrain changes across multiple conversions,
  - Transporter route avoids Scout / Builder during an actual table run.

## Next Candidate

Next implementation should be Phase 5.3: Scout auto scan route.

Suggested first route:

- `(-3,1) -> (1,1) -> (1,-2) -> (-2,-2)`

This is close to the requested image of moving from the friendly start line, passing near the goal side, then returning toward the friendly side.
