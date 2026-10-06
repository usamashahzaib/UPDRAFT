# Shared weapon movement-state threshold acceptance

## Goal

Keep offline weapon spread classification consistent with authoritative network fire.

## Player-facing behavior

Grounded movement below 0.5 m/s remains classified as stationary for weapon spread; movement above it remains moving. Airborne spread selection is unchanged.

## Configured value

`MovementConfig.weaponMovingGroundSpeedThreshold = 0.5` m/s, preserving the existing offline threshold and online squared threshold.

## States and edge cases

- Both offline and authoritative network fire compare grounded horizontal speed against the same `MovementConfig` value.
- At exactly the threshold, both classify the player as stationary; above it, both classify as moving.
- Airborne state takes precedence over the grounded threshold.

## Acceptance

- The checked-in MovementConfig asset and editor generator use the same value.
- Offline `PlayerController` and network `NetPlayer` read the threshold from the movement config.
- Existing combat and config tests were read but not run; Unity runtime build and source review are used for this slice.

## Telemetry

No new telemetry is emitted; existing shot/match measurements remain unchanged.

## Unmet

- No automated parity regression or multiplayer spread comparison was run.
- Weapon spread balance remains subject to playtest tuning.
