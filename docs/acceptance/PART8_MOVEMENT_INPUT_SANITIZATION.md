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
- Pad recharge, weapon lockout, and pad-launch activation lockouts are selected from server-loaded `PadConfig` values. `NetConfigs.Pad` now fails closed on a missing asset or negative/non-finite cooldown duration; zero remains valid and configured values are not clamped or rewritten. Clients submit only movement/button intent; they do not submit cooldown durations or mutate the networked timers. The authoritative simulation starts and expires those timers.
- The same `NetConfigs.Pad` load validates pad charge/capacity counts, lifecycle, health, diameter, launch, placement, exclusion, and air-steering values for valid finite ranges before authoritative gameplay consumes them. See `docs/acceptance/PART8_PAD_CONFIG_VALIDATION.md`.
- `NetConfigs.Movement` now fails closed when the required `MovementConfig` is missing, when required health/capsule dimensions are non-finite or non-positive, or when a movement, recovery, protection, or respawn value is non-finite or negative. Existing authored values are unchanged; zero remains allowed for non-negative fields.
- Hits and damage are resolved by `NetMatch.ResolveShots`, then applied through `NetPlayer.ServerDamage` on state authority.

## Acceptance evidence required

- Runtime C# compilation succeeds.
- Review confirms that movement sanitization runs in `NetPlayer.FixedUpdateNetwork` before movement simulation and runs on state authority.
- Source review confirms fire cadence, movement speeds, pad recharge, hit resolution, and damage use the server's existing config/state-authority paths.
- Source review confirms configured pad cooldown durations are validated once when the authoritative config is loaded, applied through state-authoritative `TickTimer` values, and cannot be supplied by client input.
- Source review confirms the server loads the required movement asset once and validates movement-critical values before simulation uses them.
- Automated tests and multiplayer/network verification are not claimed for this change.

## Compile evidence follow-up (10 October)

The generated Unity Editor and Network .NET projects both compile the shared
Runtime source. Builds after the MovementConfig validation change completed
with 0 errors and 41 existing Runtime warnings each. This satisfies source
compilation for these generated project targets; it does not establish Unity
Editor import, actual Unity player assembly compilation, or runtime behavior.
The Unity CLI attempt still failed during Licensing initialization before its
own compile phase.

## Unmet

- Automated regression coverage for non-finite movement values and malformed weapon cadence was not added or run in this pass.
- Unity CLI compile/import acceptance remains open because batch startup failed during Licensing initialization before C# compilation. Generated Editor/Network project source compilation passed as recorded above.
- Network-spike, replay/integrity, emulator-handling, and device acceptance remain open as listed in `docs/design/IMPLEMENTATION_STATUS.md`.
