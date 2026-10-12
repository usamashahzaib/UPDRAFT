# Maintenance and queue-pause notice

## Rules applied

- Show the operator's maintenance message and UTC ETA when supplied; do not
  invent an ETA.
- Show an operator-supplied compensation note when provided without promising
  an amount or creating an automatic grant.
- Keep queue allocation disabled during maintenance while Home browsing and
  offline practice remain available.
- If the startup availability request fails or returns an unknown state while
  the screen is still open, leave the syncing state and show the existing
  service-unavailable or no-internet recovery actions. Keep the brief automatic
  advance for a request that is still pending.

## Implementation

`ServiceAvailabilityOptions` carries the maintenance flag, message, ETA, and
optional compensation note. The client-status and matchmaking endpoints return
those fields while maintenance is enabled. `ClientStatusDto` and
`QueueStatusDto` preserve the fields, and both the startup notice and
matchmaking failure notice append the compensation note only when it is
non-empty. The configured default is blank, so the UI does not claim a
compensation policy the operator has not supplied.

Before returning an ETA to the client, the backend now accepts only an
explicit UTC value (`Z` or `+00:00`), validates it, and formats it as
`yyyy-MM-dd HH:mm`. Invalid, local-time, or non-UTC values are omitted, so the
client never labels an unverified operator value as UTC.

Startup and Party Room status labels render operator messages and compensation
notes as plain text, matching the Home status label. TMP markup in service data
therefore cannot change the UI's style or layout.

The Party Room now treats `maintenance` and `queue_paused` as unavailable
queue states: it disables only the leader's start-search action and shows the
server-supplied notice. A failed or unknown queue-status response also disables
start-search until a successful idle response confirms that matchmaking is
available. Party browsing and readiness remain usable.

If availability fails, returns an unreadable response, or reports an unknown
state before the startup screen advances, it holds on its existing Retry / Play
Offline actions. The brief automatic advance remains available if the check is
still pending, so a slow service response does not block local play.

## Verification

- Source inspection confirms the availability fields pass from backend options
  through startup and queue failure presentation.
- The Network assembly compiled after the Party Room now honors maintenance,
  queue-pause, failed, and unknown status responses. The PlayMode test assembly
  also compiled; no test or live queue simulation was run.
- Runtime build after the startup failure-state change: 0 errors, 41 existing
  warnings. No simulated availability outage or Unity/device capture was run.
- No maintenance compensation amount, payout, or account entitlement is
  created by this feature.
- Tests and device presentation checks were not run.

## Unmet

- Authored maintenance-state labels use the configured Unity Localization
  system. Operator-provided maintenance copy remains service data until the
  backend exposes reviewed message codes; see A7 localization acceptance.
- Operational response and Home/offline behavior still need live service and
  device acceptance.
