# Gate E Acceptance Record — Complete Player Product

**Project:** HOTDROP  
**Specification:** `ProjectUpdraft/PROJECT_HOTDROP_SPEC.md`, v1.0, §§4, 10, 13–18, 20–24, 28, 31–33  
**Audit date:** 2026-09-28  
**Scope amendment:** Amendment v2.1 (2026-10-01) — two body rigs (Male/Female) with a one-time first-launch rig choice and an Appearance rig switch — adds work items to this gate (spec §§15, 17); see "Amendment v2.1 work items" below. The acceptance result is unchanged: **NOT ACCEPTED**.  
**Result:** **NOT ACCEPTED.** Source includes several service-backed journeys, including Support / Report and deletion, but Google Play Games client integration, measured region selection, queued-practice runtime, end-to-end failure/reinstall behavior, and the complete product gate remain incomplete or unverified. No Gate E pass is claimed.

## Current-state correction (2026-10-09; updated 2026-10-10)

The dated audit below records the state observed on 2026-09-28. Since that
snapshot, the public deletion path gained email verification and completion:
GET only renders a confirmation form, and POST uses a single-use expiring link
to delete matching player data while retaining purchase records. Production
startup now requires both delivery-capable SMTP and a configured HTTPS public
origin for verification links. The latest change makes token claiming
conditional and atomic, with the claim and deletion inside one transaction; the
authenticated in-app deletion path also uses a transaction. The in-game
journey is now present in source: Home > More opens `PlayerProductScreens` and
`SupportHubView`; the report flow submits a recent-match participant and reason
to the authenticated report endpoint, and the page includes typed-confirmation
in-app deletion plus the public web deletion link. The web form's verification
and completion paths are present in `SupportEndpoints` and
`AccountDeletionService`. These paths are source-implemented, but a current
Unity journey, live backend report/deletion, publisher SMTP/domain
configuration, production email delivery, and real-account/device verification
remain unaccepted. This correction does not change Gate E's **NOT ACCEPTED**
result. The requirement map below was reconciled on 2026-10-10 where its
2026-09-28 snapshot had said the Profile/Appearance, Party, Support/Report, or
deletion screens did not exist; it now distinguishes source availability from
live and device acceptance.

## Source snapshot and ownership

- Initial revision before implementation: `f6fb00510b9260667a2b02e2929ffcb7b868a3e1`.
- Exact tested source revision: `2d2436fe35f637f72a182beed51e81b53cbcc74c`. At initial inspection, the shared worktree was dirty with Gate B/C source, test, config, scene, and progress-report edits; those were preserved. The source changes tested here now match the committed revision. The acceptance and progress records remain documentation changes in the worktree.
- No `AGENTS.md` was present in the repository or the relevant parent folders.
- Process inspection used `Get-CimInstance Win32_Process` (denied by Windows permissions) and `Get-Process`; the available listing showed no Unity, dotnet, MSBuild, or Updraft process before work. No Unity editor, player, Linux server, or paid service was launched.
- Gate C's record, [`STEP_C.md`](STEP_C.md), is **NOT ACCEPTED**. Gate E's real online journey therefore has no accepted multiplayer service foundation.
- Exact source revision and SHA-256 values for every Gate E implementation/test file: [`STEP_E.source-manifest.txt`](STEP_E.source-manifest.txt). The manifest is tied to the exact tested commit; no generated output is included.

## Requirement map

| Requirement | Existing implementation and evidence | Status |
|---|---|---|
| Guest entry, short access token, rotating refresh token, session-derived ownership | `/api/auth/guest`, `/refresh`, authenticated API ownership claims, refresh-token rotation/reuse rejection; backend auth tests. Unity `BackendClient` persists the refresh token in app-private preferences and refreshes after access-token expiry. | Implemented; backend tests verified. Android reinstall recovery is not verified and guest-only preferences do not establish recovery after uninstall. |
| Google Play Games link, refresh, revoke | This pass replaces the 501 placeholder with a server-side one-use auth-code exchange and `players/me` identity verification. The server rejects caller-supplied IDs, refuses links when credentials are missing, prevents linking an identity already owned by another account, and has an authenticated unlink route. A unique identity index and migration prevent concurrent duplicate ownership. | Backend source implemented; missing server OAuth client ID/secret and Android Play Games SDK/client auth-code acquisition block integration and real identity verification. |
| Profile, XP, credits, inventory, appearance, equip | Profile/wallet/inventory/equip endpoints and Home-routed Profile and Appearance screens exist. Startup hydrates the account rig when possible; rig choice saves through the existing authenticated equip endpoint. Cosmetic inventory carries a supported-rig mask under one item entitlement. | Source implementation is present, including the v2.1 Male/Female switch and both-rig cosmetic data. No current Gate E client journey or live API/device verification was performed here. Authored per-rig character meshes and matching silhouette/readability evidence remain open; purchases remain gated. |
| Cosmetics do not change power; offline practice cannot mint value | Cosmetic-only inventory representation; result submission is restricted to game-server service auth; offline-practice rejection regression exists. | Source/backend-tested; offline save editing and client runtime journey not tested in this gate. |
| Party, 8-character code, leader and readiness | Backend create/join/leave/current/readiness and leader/readiness queue checks exist. Home opens the Party Room screen, which has join/create, polling, readiness, leave, and queue-availability paths in source. | Backend evidence is historical; no current client-to-live-service party journey, dropped-member recovery, six-client behavior, or device verification was performed for this gate. |
| Measured single-region selection and search widening | Search window is configured at 150, expands by 50 every 10 seconds after 20 seconds to 350, and worker polls every 2 seconds. Bot fill threshold is 45 seconds. App settings currently say Fusion region `eu`. | Search behavior implemented in backend; `eu` is not supported by a concentrated-market RTT measurement, so region choice is blocked and must not be treated as accepted. |
| Practice while queued | Practice starts while the existing queue ticket remains active. The drill has Return to Queue and Cancel Search actions; a ready match stops the local drill before online ticket handoff. | Source implemented; live ticket retention, match handoff, and failure recovery still need backend/device acceptance. No separate timed choice is required by the current lifecycle spec. |
| Public bot fill after 45 seconds; labeling and telemetry | Matchmaking composer assigns bot-filled matches after configured wait; ticket carries `HasBots`; network bot driver and presentation labels exist. | Source implemented; six-client/public runtime behavior and truthful telemetry remain unverified under Gate C. |
| AFK warning at 30 seconds; removal/substitution at 45 seconds | `NetMatch.HandleAbsence` warns at 30 seconds, substitutes at 45 seconds; `NetClientPresenter` displays a warning. Eligibility excludes substituted players. | Source implemented; timing, input activity coverage, and user-visible live behavior not runtime verified. |
| Transactional ledger and exact rewards; duplicate/out-of-order result safety | Unique ledger and participation keys, server-only result endpoint, idempotent duplicate result tests, server-failure ineligible/no-reward case. | Backend-tested locally (see automated results); concurrent duplicate delivery, database outage/retry and production PostgreSQL behavior are not verified. |
| Retry/recovery for auth, party, queue, server, capacity and outages | Unity online flow has visible retry/cancel and reconnect states; queue practice, Party, Profile, Appearance, Account, and Support surfaces are present in source. Backend errors are mapped to stable client codes for recognized cases. | Partial. Live outage/capacity injection, all failure transitions, reconnect races, and proof that no user is stranded remain unverified; unmapped/free-form errors still need reviewed codes. |
| Support, reporting, in-app and web account deletion | Home > More reaches Support / Report and the typed-confirmation in-app deletion flow. The public form has email-verification and completion routes; completion claims a single-use token and deletes matching player data while retaining purchase records. | Source-implemented; no current Unity journey, live report/deletion API, production SMTP/domain delivery, or real-account/device verification was performed. |
| All required screens and explicit failures | Home routes to Party and Appearance, and More provides Profile, Account, Support / Report, settings, and account deletion entry points. Startup, HUD, tutorial, pause/settings, queue overlay, results, and v2.1 rig choice are also present in source. | Still NOT ACCEPTED as a product gate: navigability and error states were not verified in Unity; live service, real-account recovery, authored rig assets, visual acceptance, release configuration, and several outage/device cases remain open. Purchase/reward screens remain gated. |
| Reinstall recovery + deletion end-to-end | No real linked Android identity was available; no reinstall or production deletion run was performed. | Blocked on Play Games app/client setup and real test accounts; no pass evidence. |

## Amendment v2.1 work items (2026-10-01)

Spec §15/§17 (Amendment v2.1) add the following to this gate. Items 1–3 remain unimplemented as of 2026-10-01; item 4's parity guards landed the same day over a data-only rig identity (no UI, art or network change), and the account still carries one `EquippedCosmeticId` with no rig dimension.

1. **First launch:** offer a one-time body rig choice — Male/Female — before the tutorial (Startup flow).
2. **Appearance:** add a body rig switch, available any time, alongside the existing inventory preview/equip.
3. **Cosmetic variants:** equipped cosmetics must present in both-rig variants (same silhouette quality, same readability, same price); the catalog/inventory model needs a rig dimension.
4. **Parity:** rig choice must not alter capsule size (1.8 m × 0.35 m), hitboxes, or animation results; cover this with tests. **Guard added 2026-10-01:** `Assets/Scripts/Player/BodyRig.cs` defines the two rig ids and one shared capsule/hitbox/animation profile; `Assets/Scripts/Tests/BodyRigParityTests.cs` proves both rigs resolve to that same profile, that the generated NetPlayer/PlayerPrefab assets use it, and that scripted movement traces are identical per rig. Unity EditMode **199/199 passed** including the five new tests, PlayMode **8/8 passed** (`Run-UnityTests.ps1`, 2026-10-01).

Art production for either rig stays gated to Gate G (spec §15); the approved graybox remains the intended presentation until then.

## 2026-10-10 follow-up — body rig choice UI status

The Amendment v2.1 choice and Appearance switch are now present in the current
Unity source. `GameBootstrap.ContinueFromStartup` opens `RigChoice` before the
first tutorial when no rig has been selected. The choice is held until
CONTINUE (or the explicit Decide Later default), saved to `BodyRigStore`, and
the tutorial starts afterward. `PlayerProductScreens` exposes the same choice
from Appearance through the `ChangeRunner` control in `AppearanceLockerView`.
`OfflineSliceSmokeTest` and `PlayerJourneyVisualEvidenceTests` contain checks
for the first-run selection, deferred save/continue, both-ratio screen capture,
and Appearance control reachability. Tests were not run for this follow-up.

The account/profile side is only partly connected. The backend profile model
has a `BodyRigId` field and accepts the free `rig_male` / `rig_female` slots,
and the Unity client now requests the saved rig during Startup, hydrates valid
account choices before continuing when possible, and updates a first-run
chooser that is still open if the player has not acted. This request runs in
the background and does not hold the cold-launch route. Local choices write
through the authenticated equip endpoint. A pending local choice wins until
its save succeeds, so a late profile response cannot undo an explicit choice.
The server recognizes only the two defined rig IDs. The catalog bundle and
inventory item carry `supportedBodyRigMask` (Male=1, Female=2; both=3),
defaulting to both rigs under one cosmetic ID and one entitlement. Body-style
ownership is checked against the selected account rig, and Appearance filters
out variants unsupported by the current rig. `CosmeticLook` uses the selected
rig when fitting procedural body-style geometry; the existing style and
reference price remain one shared SKU. The backend migration adds the inventory
mask with a both-rig default, preserving existing grants.

This establishes data and runtime support parity; it does not provide distinct
authored character meshes per rig or captured evidence that both variants meet
the same silhouette/readability bar. The shop remains disabled, so localized
store price equality and purchase behavior were not exercised. Shared
capsule/hitbox and movement guards remain in `BodyRigParityTests`; their
2026-10-01 pass is historical, not a run performed for this update.

10 October follow-up: Home now displays and titles the choices with localized
Male/Female rig names. This is a UI-copy/localization correction only; it does
not close authored-mesh, silhouette, or rendered-device acceptance.

## 11 October first-launch question follow-up

The first-run startup splash now shows the localized Bible question “Do you
want to fly?” once for a fresh tutorial journey. On the normal path it holds
for the Bible's full 2.5 s app-launch window; a recoverable connection state
keeps it visible until the player chooses an available action. It then enters
the accepted v2.1 body-rig choice and starts
the movement tutorial after the player continues. Returning players and
existing rig/tutorial progress skip this question. The first-flight seen flag
is persisted locally; no account, permission, movement, or gameplay state
changes.

Runtime, Network, and Editor builds passed with 0 errors (41 existing Runtime
warnings; Network and Editor printed none). Static localization checks confirm
the source key and English table row use the same exact copy and stable ID. No
tests, Unity captures, or device checks were run for this follow-up.

The backend project compiled with 0 errors and one NuGet vulnerability-feed
warning; EF also generated an idempotent PostgreSQL migration script. Migration
source was not applied to PostgreSQL or SQLite. A restore-enabled
`dotnet build ProjectHotdrop/Hotdrop.Editor.csproj -m:1 -v:q` initially exposed
CS0136 in `CosmeticLook.Apply`: an inner `rig` local collided with the later
selected-rig local. Renaming the inner transform variable to `rigRoot` keeps
the selected-rig variant fitting unchanged and fixes compilation. The rerun
completed with 0 errors and 41 existing Runtime warnings. This generated
project C# build is not Unity asset-import or runtime evidence. No tests, Unity
asset import, live service, store, or device checks were run; remote profile and
rig-variant behavior therefore remains unverified at runtime.

### Rules applied and unmet

- Applied: one-time pre-tutorial choice, a reachable Appearance rig change,
  account profile persistence/hydration using the existing equip/profile
  contract, exact validation of both rig IDs, a both-rig catalog/inventory
  mask under one entitlement and SKU, fit-adjusted procedural body-style
  variants, cosmetic-only parity guards, and the Gate G character-art bar.
- Unmet: authored per-rig character meshes and visual evidence for matching
  silhouette/readability remain open. Unity compilation, fresh test execution,
  live API/store behavior, migration application, and device acceptance were
  not verified in this follow-up.

## Automated results

- **Backend:** `dotnet test Backend/HotdropBackend.Tests/HotdropBackend.Tests.csproj --no-restore --logger 'console;verbosity=minimal'` — **79 passed, 0 failed, 0 skipped**, .NET 10.0, 2026-09-28. Includes regressions for untrusted caller-supplied Google IDs and party queue readiness/leadership. Warnings remain: NU1903 for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 and existing nullable warnings in auth/party tests.
- **Unity EditMode/PlayMode:** not run. Gate E UI/integration changes were not made to Unity; prior Gate B/C evidence remains as recorded in those gate files and does not count as Gate E evidence.
- **Android/Google Play Games:** no Gate E APK/AAB built, installed, linked, reinstalled, or tested. No artifact hash is available.
- **PostgreSQL migration/runtime:** migration source added; it was not applied to a real PostgreSQL instance.
- **Paid services/deployment:** none enabled or launched.

## Reproduction commands

Backend test suite:

```powershell
dotnet test Backend/HotdropBackend.Tests/HotdropBackend.Tests.csproj --no-restore --logger 'console;verbosity=minimal'
```

Unity suites (after resolving the previously documented Package Manager/licensing startup issue and confirming no Editor owns the project):

```powershell
ProjectUpdraft/Run-UnityTests.ps1 -Platform EditMode
ProjectUpdraft/Run-UnityTests.ps1 -Platform PlayMode
```

The backend Google Play Games config is supplied through secret configuration, not source: `GooglePlayGames:ServerClientId` and `GooglePlayGames:ServerClientSecret`. Do not use fabricated credentials. Client-side Play Games setup requires the publisher-owned Android application and registered signing certificates. Region selection requires the actual target-market latency sample before changing `Allocator:FusionRegion`.

## Failure cases and known issues

- In-game Support / Report and deletion journeys are now present and wired to the service APIs in source (current-state correction above). Unity runtime navigation, live API success/failure behavior, and production deletion delivery remain unverified; other product journeys retain the gaps in the requirement map.
- Google linking remains unavailable until the Android SDK produces a server auth code and the backend is configured with the publisher's real server OAuth client credentials. Server-side code alone does not provide identity recovery.
- Guest refresh-token storage is app-private preferences. Reinstall recovery requires a linked Google Play Games account and a real device uninstall/reinstall run.
- Public deletion now has a single-use, expiring email-verification and completion path in source. Publisher-controlled SMTP/domain setup and live delivery/real-account verification remain required before claiming production web deletion acceptance.
- Queued practice is now implemented in source, but no live queue session has verified ticket retention, cancellation, transition to a ready match, or queue failure while the local drill is active. Region `eu` is a configuration value, not a measured choice.
- Gate C is not accepted; six real clients, server allocation, reconnect, capacity/outage, duplicate delivery under retries, bandwidth and server-failure behavior remain unverified.
- No end-to-end evidence covers no-internet/auth outage, party member drop, server full, queue cancellation, update-required, server crash, or repeated retries without duplicate rewards/stranded state.
- No shop or ads are enabled; this is correct before Gate D.

## Artifacts, versions and licenses

- Gate E Android artifact SHA-256: **not available; no artifact built**.
- Source snapshot: exact Git revision `2d2436fe35f637f72a182beed51e81b53cbcc74c`; the Gate E implementation files match HEAD. Acceptance records and progress prose are uncommitted documentation.
- Balance configuration: `Telemetry.BalanceVersion` currently labels `v1.0-spec`, while backend progression and match settings are source/config values. This record does not freeze a verified client/server balance snapshot; balance-version consistency is open.
- Backend test assembly SHA-256 and changed source-file hashes are listed in the source manifest. Test outputs are command output only; no detached test-results artifact was created.
- No third-party assets or licenses changed. Google integration adds no SDK package; Android client SDK remains an integration dependency.
- No accepted artifacts are frozen. Gate E remains **NOT ACCEPTED**.
