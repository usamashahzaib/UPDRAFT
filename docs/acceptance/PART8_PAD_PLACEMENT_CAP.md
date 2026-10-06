# Server pad placement cap acceptance

## Goal

Keep the server-side per-player pad spam safeguard configurable without changing current Relay behavior.

## Player-facing behavior

Pad placement and recharge work as before. A player may place at most 60 pads during one match, even if earlier pads have expired or been destroyed; this limit is separate from the six-pad simultaneous arena cap.

## Configured value

`PadConfig.maxPlacementsPerPlayerPerMatch` is 60, preserving the existing `maxActivePadsPerMatch * 10` result with the current active-pad cap of six. No new gameplay threshold is inferred.

## States and edge cases

- The authoritative server checks the per-player count before spawning a pad.
- Rejected placements do not increment the count.
- Accepted placements increment the count once and reset with the per-match player stats.
- Changing the simultaneous arena cap no longer silently changes the per-player lifetime cap.

## Acceptance

- Placement cap is loaded from the shared `PadConfig` ScriptableObject on server authority.
- Existing owner, arena, charge, and placement-validity checks remain in place.
- Unity runtime build passed with 0 errors and 41 existing warnings; source review confirms the server guard reads `PadConfig`. Existing config/pad tests were read but not run.

## Telemetry

Existing authoritative `pad_placed` events continue to count accepted placements. Rejected cap attempts remain visible through the existing pad-rejection counter; no new telemetry event is added.

## Unmet

- Automated config and multiplayer boundary coverage were not run in this pass.
- Device and six-client acceptance remain open under the existing combat and delivery gates.
