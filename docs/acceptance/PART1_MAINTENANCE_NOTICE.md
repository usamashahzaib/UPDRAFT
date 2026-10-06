# Maintenance and queue-pause notice

## Rules applied

- Show the operator's maintenance message and UTC ETA when supplied; do not
  invent an ETA.
- Show an operator-supplied compensation note when provided without promising
  an amount or creating an automatic grant.
- Keep queue allocation disabled during maintenance while Home browsing and
  offline practice remain available.

## Implementation

`ServiceAvailabilityOptions` carries the maintenance flag, message, ETA, and
optional compensation note. The client-status and matchmaking endpoints return
those fields while maintenance is enabled. `ClientStatusDto` and
`QueueStatusDto` preserve the fields, and both the startup notice and
matchmaking failure notice append the compensation note only when it is
non-empty. The configured default is blank, so the UI does not claim a
compensation policy the operator has not supplied.

## Verification

- Source inspection confirms the availability fields pass from backend options
  through startup and queue failure presentation.
- No maintenance compensation amount, payout, or account entitlement is
  created by this feature.
- Tests and device presentation checks were not run.

## Unmet

- Maintenance copy is not managed through Unity Localization because that
  framework is not configured. Localization remains a prerequisite recorded
  in `docs/design/06_TECH.md`.
- Operational response and Home/offline behavior still need live service and
  device acceptance.
