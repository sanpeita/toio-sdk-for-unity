# 2026-06-13 Phase 5 Device Test Log

## Summary

Phase 5 reached the Saturday recording goal: Scout can move on the converted field, scan radius 2, and reveal hidden obstacles on the Unity field.

Today stops here. The Scout discovery loop is the main visible feature for the weekend device post, and it is working well enough for recording.

## Confirmed

- Three-role connection succeeds.
- UI role assignment is usable with the current connection-order flow.
- Manual recovery after setup is possible.
- Scout movement works after the roles are placed on the start line.
- Scout scan works and reveals obstacles on the Unity field.
- The crash reported earlier was traced during device testing to discharged Core Cubes. With all three cubes charged, the crash did not reproduce.

## Remaining Issue

- During automatic start-line movement after three-role connection, one of the three cubes still sometimes does not move.
- In the latest run, Transporter and Builder moved, but Scout did not.
- Pressing `Retry Start Line` still did not move that third cube.
- The UI showed BLE connection and ID acquisition for all three cubes.
- The cube was moved manually to the start line, and Scout gameplay then worked.

## Current Hypothesis

- The non-moving cube may have older or inconsistent firmware.
- Another Core Cube should be tested before changing the game flow further.
- Because BLE connection and ID acquisition succeeded, this looks more like a motor command / firmware / device-specific behavior issue than a role assignment issue.

## Current Workaround

1. Connect the three roles.
2. Let automatic start-line movement run.
3. If one cube does not move, place it manually on its assigned start cell.
4. Continue with Scout movement and scan.

## Tomorrow Candidates

### Option A: Obstacle Collision

Add rule handling for movement into obstacle cells.

Recommended first pass:
- Hidden obstacles do not block Scout before discovery.
- Discovered obstacles block future Scout movement.
- The UI should explain the block with a short status message.

Reason:
This directly extends today's successful Scout loop and makes the field feel more tactical.

### Option B: Builder Implementation

Start Builder's first role.

Possible first pass:
- Builder can clear or mark one adjacent discovered obstacle.
- Builder movement can remain manual or fixed at first.

Reason:
This adds the third role's identity, but it is larger than obstacle collision because it needs role ability rules and likely more UI.

### Option C: Device Reliability Pass

Test the non-moving cube against another cube and record firmware / motor command behavior.

Recommended checks:
- Try a different Core Cube in the same third-role slot.
- Try the suspect cube as Transporter, Scout, and Builder.
- Confirm whether `TargetMove` fails only for start-line setup or also for Scout movement.

Reason:
This is useful before spending more time on start-line automation. It should not block the next visible feature if manual placement is acceptable.

## Recommended Next Step

Start tomorrow with Option C for 10-15 minutes. If the issue follows the cube, treat it as device-specific and move on. Then implement Option A, obstacle collision for discovered obstacles.
