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
- Dedicated-server service credentials support per-instance keys keyed by
  `server_id`; production startup requires a complete map with unique values
  and rejects the legacy shared-key fallback. Development and Testing can
  still use the fallback. See `docs/acceptance/PART8_SERVER_IDENTITY.md`.
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
the service is reachable. Maintenance ETAs are returned only when they parse as
explicit UTC (`Z` or `+00:00`) and are normalized before display; malformed,
local-time, and non-UTC values are omitted.

When a request uses an obsolete protocol, the backend returns the required
minimum protocol and a platform-specific HTTPS store URL from
`Compatibility:AndroidStoreUrl` or `Compatibility:IosStoreUrl`. The Unity
startup recovery action opens that URL when configured; blank release values
remain retry-only, and offline play stays available. The release owner must
populate and verify the real store listings before enabling this path. A
non-positive configured minimum protocol fails backend startup. A
non-positive client protocol in the Unity backend asset disables online
backend configuration rather than sending a stale/invalid request. A
present but malformed `X-Client-Protocol` header is rejected as a bad request
instead of bypassing the minimum-protocol check. Player API calls without the
header receive `client_protocol_required`; omission is allowed only on the
dedicated-server identity routes (token exchange, claim, ticket validation,
result submission, and server analytics) and the public browser account-
deletion page/forms. The in-app JSON deletion request remains protocol-gated.

## Localization status and prerequisite

Unity Localization is configured in `ProjectHotdrop/Assets/Localization` with
an English `UIStrings` table and the shared `UiDraw.L(key, fallback)` lookup.
Profile, Appearance, Match Search, the online queue/reconnect journey, Party
Room, Results, Preparation, Startup, guided training, and Offline Challenges
use the table for migrated authored copy. Support's
authored navigation, account identity and recovery, report form, compliance
links, and account deletion copy use `support.*` keys. `SetupLocalizationPilot`
is the idempotent editor command that seeds or refreshes English entries. The
settings console is migrated and recorded in
`docs/acceptance/A7_LOCALIZATION_SETTINGS.md`; Match HUD, squad, zone, combat,
touch-control, ping, and critical sound-caption copy are recorded in
`docs/acceptance/A7_LOCALIZATION_HUD.md`. Home's authored copy and shared
next-goal strings are recorded in `docs/acceptance/A7_LOCALIZATION_HOME.md`.
Contracts/Mastery lookup coverage and its gated behavior are recorded in
`docs/acceptance/A7_LOCALIZATION_CONTRACTS_MASTERY.md`.
The queue, reservation, reconnect, and stable client/network error messages
are recorded in `docs/acceptance/A7_LOCALIZATION_ONLINE_JOURNEY.md`.
Guided training instructions, hints, and skip confirmation are recorded in
`docs/acceptance/A7_LOCALIZATION_TUTORIAL.md`.
The gated cosmetic shop preview is recorded in
`docs/acceptance/A7_LOCALIZATION_SHOP_PREVIEW.md`.
Party, profile, appearance, account-recovery, and report-target status copy
authored by the product-screen controller is recorded in
`docs/acceptance/A7_LOCALIZATION_PRODUCT_SCREENS.md`.
The shared `MenuLayout.Create` top-bar BACK button (used by every menu screen,
including ones already marked migrated) now reads through `menu.nav.back`;
previously it was a literal "BACK" missed by every per-screen pass.
Recognized party, matchmaking, and Google Play API error codes are retained
through typed client responses and localized as recorded in
`docs/acceptance/A7_LOCALIZATION_BACKEND_CODES.md`.
`SetupLocalizationPilot` emits 1,470 unique English seed keys, including the
nearby-footsteps and nearby-weapon-fire captions, Support footer, pad-study
facilitator panel, Party Room code-placeholder, touch-layout editor, north-up
minimap, cardinal compass, and high-contrast objective-marker additions. The
checked-in shared and English tables each contain the same 1,470 unique IDs;
six unreachable legacy rows with no seed assignment were removed after a
current-source lookup audit. Static source/key/value comparison is recorded in
`docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`; all current seed values match
after whitespace normalization. Unity Editor import/re-save remains open. Live
locale switching, translated wrapping, and device review remain open; batch
startup stalled while the Licensing Client was reconnecting.

Urdu RTL shaping and checked-in Urdu/Naskh or Nastaliq and Devanagari fallback
fonts are still absent. The readiness and browser account-deletion Problem
Details now carry stable codes; the browser fallback remains English until
localized web content is supplied. Unmapped JSON API errors and backend-
provided free-form status messages still need reviewed codes or per-locale
content before they can be translated reliably. Add reviewed font sources with
licensing records, then verify translated wrapping and right-to-left navigation
on devices before claiming full A7 acceptance.

## Acceptance evidence

- Unity compile and relevant automated checks for code changes.
- Dedicated Linux server build and multi-client network verification for
  networking changes.
- Real-device performance and memory measurements for device-budget claims.
- Record unverified acceptance items explicitly in the change report.
