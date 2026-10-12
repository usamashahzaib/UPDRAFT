# Shared network aim-ray configuration acceptance

## Goal

Keep the client camera ray and authoritative server clamp on the same configured bounds.

## Player-facing behavior

Camera aiming, muzzle-parallax compensation, blocked-shot feedback, and server hit resolution keep their current behavior. The camera ray is 100 m, and the server's minimum submitted aim point is 0.5 m from the eye; weapon maximum range still controls damage resolution.

## Configured values

`MatchRulesConfig.cameraAimRayDistanceMeters = 100` and `minimumAimPointDistanceMeters = 0.5`, moved unchanged from the duplicated client/server literals.

## States and edge cases

- The client initializes its empty ray target and nearest-hit limit from the shared ray distance.
- The server clamps client aim-point distance between the shared minimum and maximum.
- Invalid aim coordinates continue to sanitize to zero; a zero aim point continues to use weapon range.
- This setting does not extend the weapon's configured damage range or let the client select a shot direction independently of replicated yaw/pitch.

## Acceptance

- Default `MatchRulesConfig` asset and editor generator carry the same values.
- Both client ray call sites and the server clamp read the shared config.
- The network config loader fails closed when `MatchRulesConfig` is missing, either aim distance is non-finite or below 0.5 m, or the minimum exceeds the camera-ray maximum. The authored 0.5 m / 100 m values are unchanged.
- Existing shot-direction authority coverage was read. Runtime builds and source review were used; tests were not run.

## Telemetry

No new telemetry is required; existing latency/completion measurements are unchanged.

## Unmet

- Cross-client/server behavioral and six-client network verification remain open.
- Device aiming and weapon cover/range acceptance remain open under the combat gates.
- Automated regression coverage for missing and malformed aim-ray config was not added or run.
