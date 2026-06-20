# 2026-06-20 Phase 5.3 Device Follow-up

## Device Result

What worked:

- Scout auto movement and auto scan worked from button input.
- Transporter auto movement worked from button input.
- Today's main implementation target is good enough to stop feature expansion here.

What did not work:

- Three-cube connection / setup is still fragile.
- In this run, Builder did not move during start-line setup.
- `Retry Start Line` also did not recover Builder, so Builder was placed manually.
- The issue appears related to the third connected cube / command timing rather than Builder-specific behavior.
- In `FIELD VIEW`, the map and right-side button panels overlapped. The map can move farther left.

## Follow-up Implemented

- Increased start-line command spacing from `450ms` to `800ms`.
- Increased start-line move attempts from 2 to 3.
- Added a `Builder再配置` button in the control view.
- Added a `Builder再配置` button in `FIELD VIEW`.
- Added a field-view world offset so the rendered tactical map shifts left without changing real toio mat coordinates.
- Narrowed and moved the `FIELD VIEW` Transporter / Scout control panels farther right.
- Compressed `FIELD VIEW` button sizes and panel heights so the Scout panel is less likely to run off the bottom of the screen.

## Verification

- `dotnet build toio-sdk-unity/Assembly-CSharp.csproj -v:minimal` succeeded.
- Build result: 0 errors.
- Existing unrelated warnings remain in `ToioLeftHandLabController`.

## Needs Device Check

- Confirm Builder can recover with `Builder再配置` when the third cube misses start-line movement.
- Confirm increased command spacing does not make setup feel too slow.
- Confirm the left-shifted map and narrowed panels do not overlap at the recording viewport.

## Tomorrow Candidate

Proceed to Phase 6: Builder terrain conversion, after one quick setup check.
