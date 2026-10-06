# Technical architecture and implementation constraints

## Goal

Keep client movement responsive while the server remains authoritative for
competitive outcomes, progression, and commerce.

## Current project baseline

- Unity client with Photon Fusion 2 networking and a dedicated-server build
  path. The amendment's networking-library spike is a future selection gate;
  it is not authorization to replace the existing network stack.
- Movement and match tuning already have ScriptableObject configuration under
  `ProjectHotdrop/Assets/Resources/Config`.
- Relay has an existing respawn delay and configurable respawn protection.
  Preserve these for Relay until a reviewed mode spec changes them.
- Backend services and endpoints own account, matchmaking, progression, and
  commerce operations. Do not trust client-submitted prices, grants, contract
  completion, or match results.
- Server progression configuration is validated at startup so invalid level
  thresholds, negative awards, or unreachable/malformed unlock entries cannot
  corrupt authoritative rewards. The specific checks are recorded in
  `docs/acceptance/PART8_PROGRESSION_CONFIG.md`.

## Movement and networking requirements

- Treat player inputs as movement intent; validate movement, fire cadence,
  pad cooldowns, damage, inventory and match results on the authoritative
  server.
- Client prediction must use the same movement rules as server simulation.
  Reconcile on authoritative state; do not assume Unity CharacterController
  provides deterministic simulation.
- Offline and authoritative weapon firing also read the same configured
  grounded moving-state threshold for spread selection.
- Any lag-compensation limit is an [ESTIMATE] and must be an explicit config
  value. The amendment proposes a 200 ms cap; measure RTT distributions before
  tuning it. Do not silently change the deployed rewind window.
- Online clients now sample Fusion-measured RTT every two seconds during active
  match phases and attach fixed latency-bucket counts plus the maximum sample
  to the existing completion telemetry event. These client observations support
  tuning analysis; they are not authoritative match facts or market acceptance.
- Keep replay/event data server-owned and avoid treating client replay data as
  proof of an authoritative hit. The dedicated server now captures sanitized
  inputs and avatar state at 10 Hz in a duration-bounded in-memory match record;
  persistent post-match review and playback remain gated on data ownership,
  reviewer access, and retention rules. See
  `docs/acceptance/PART8_SERVER_REPLAY_CAPTURE.md`.

## Security requirements

- Validate platform purchase receipts on the server before granting items.
- Persist inventory grants and contract completion server-side and make
  retries idempotent.
- Match result retries use durable per-player receipts and unique ledger/grant
  keys. Concurrent submission conflicts are accepted as idempotent only after
  every submitted participant receipt is confirmed persisted; otherwise the
  database error is preserved. See
  `docs/acceptance/PART8_MATCH_RESULT_IDEMPOTENCY.md`.
- Apply server-side bounds to fire rate, movement speed, pad cooldowns and
  damage. Never accept client match results as the progression source of
  truth.
  The shared fire-cadence step fails closed for non-finite or non-positive
  simulation/weapon intervals, so malformed weapon cadence cannot bypass the
  configured server shot rate. See
  `docs/acceptance/PART8_MOVEMENT_INPUT_SANITIZATION.md`.
- Player/pad damage, elimination, shot queueing, pad spawning, and pad event
  mutation points independently require state authority and matching runner
  ownership. Damage amounts must be finite and positive. See
  `docs/acceptance/PART8_DAMAGE_STATE_AUTHORITY.md`.
- Evaluate Play Integrity, emulator handling, and replay review as release
  work; these are not assumed complete merely because the architecture calls
  for them.

## Device and region targets

- Low-tier memory target near 1.5 GB is [ESTIMATE], not a verified device
  guarantee. Performance applies a one-step global texture mip reduction and
  lower LOD bias/maximum detail level to reduce resident texture and geometry
  cost. Procedural particle effects also receive a configured per-effect particle
  cap and reduced lifetime/size in Performance. These are starting quality
  settings, not proof of the memory target; profile on representative 3–4 GB
  devices and tune from measured peak memory.
- Candidate South Asia regions and providers are [VERIFY]. Measure RTT from
  Karachi, Lahore, and Islamabad on representative mobile and fixed networks
before choosing production regions.

## Runtime service status

The backend exposes a public, protocol-gated `/api/client/status` endpoint for
the configured minimum client protocol and queue availability. Operators can
set `ServiceAvailability:MaintenanceEnabled`, a player-facing message, an
optional UTC ETA, and an optional compensation note. Match enqueue is rejected
while maintenance is enabled, the matchmaking worker pauses new allocations,
and already matched tickets can still enter their active session. The client
shows the supplied message and only shows an ETA or compensation note when the
server actually supplies one. Startup and Home read the public status endpoint;
Home refreshes its visible connection label every 30 seconds, including while
playing as a guest, so a saved account session is never treated as proof that
the service is reachable.

When a request uses an obsolete protocol, the backend returns the required
minimum protocol and a platform-specific HTTPS store URL from
`Compatibility:AndroidStoreUrl` or `Compatibility:IosStoreUrl`. The Unity
startup recovery action opens that URL when configured; blank release values
remain retry-only, and offline play stays available. The release owner must
populate and verify the real store listings before enabling this path.

## Localization prerequisite

The current Unity package manifest does not include Unity Localization, and
the checked-in runtime font assets do not include Urdu or Devanagari coverage.
Before claiming A7 complete, add localized string tables, RTL shaping support,
and tested Urdu/Naskh or Nastaliq plus Devanagari fallback fonts. Existing UI
contains extensive literal English copy; migrate it screen by screen and verify
right-to-left layout on a device.

## Acceptance evidence

- Unity compile and relevant automated checks for code changes.
- Dedicated Linux server build and multi-client network verification for
  networking changes.
- Real-device performance and memory measurements for device-budget claims.
- Record unverified acceptance items explicitly in the change report.
