# Movement input sanitization acceptance

## Rules applied

- Treat client movement as intent and validate it on the authoritative server.
- Clamp movement intent to the configured movement model's normalized input domain before simulation.
- Invalid numeric movement input must not propagate into authoritative movement state.

## Implementation

`NetInput.Sanitize` now rejects NaN and positive or negative infinity in either movement axis by replacing the movement vector with zero before the existing unit-magnitude clamp. This prevents non-finite values from reaching `PlayerMovementSimulator` on the server and keeps the player's facing/buttons independently usable for that tick.

The adjacent A4 integrity requirements are enforced by existing authoritative paths:

- Movement is simulated from sanitized input by `NetPlayer.Move` using shared `PlayerMovementSimulator` rules and configured movement speeds.
- Firing is limited in `NetPlayer.Weapons` by `FireCadence.Step` and the selected server-side weapon's configured `SecondsPerShot`.
- `FireCadence.Step` rejects non-finite or non-positive simulation intervals and shot intervals, and resets a non-finite cadence debt. A malformed weapon fire rate therefore cannot produce a shot every server tick.
- Pad charge recharge uses the server-owned `TickTimer` in `NetPlayer.TickPad`; placement and cooldown start are performed only under state authority.
- Hits and damage are resolved by `NetMatch.ResolveShots`, then applied through `NetPlayer.ServerDamage` on state authority.

## Acceptance evidence required

- Runtime C# compilation succeeds.
- Review confirms that movement sanitization runs in `NetPlayer.FixedUpdateNetwork` before movement simulation and runs on state authority.
- Source review confirms fire cadence, movement speeds, pad recharge, hit resolution, and damage use the server's existing config/state-authority paths.
- Automated tests and multiplayer/network verification are not claimed for this change.

## Unmet

- Automated regression coverage for non-finite movement values and malformed weapon cadence was not added or run in this pass.
- Network-spike, replay/integrity, emulator-handling, and device acceptance remain open as listed in `docs/design/IMPLEMENTATION_STATUS.md`.
