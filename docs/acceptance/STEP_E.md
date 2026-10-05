# Gate E Acceptance Record — Complete Player Product

**Project:** HOTDROP  
**Specification:** `ProjectUpdraft/PROJECT_HOTDROP_SPEC.md`, v1.0, §§4, 10, 13–18, 20–24, 28, 31–33  
**Audit date:** 2026-09-28  
**Scope amendment:** Amendment v2.1 (2026-10-01) — two body rigs (Male/Female) with a one-time first-launch rig choice and an Appearance rig switch — adds work items to this gate (spec §§15, 17); see "Amendment v2.1 work items" below. The acceptance result is unchanged: **NOT ACCEPTED**.  
**Result:** **NOT ACCEPTED.** Source and backend checks cover several services, but the in-game journey, Google Play Games client flow, measured region, queued practice, and end-to-end failure/reinstall acceptance are incomplete. No Gate E pass is claimed.

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
| Profile, XP, credits, inventory, appearance, equip | Profile/wallet/inventory/equip endpoints exist. Progression values are now explicit in backend `Progression` settings and source-defined Unity config. The result processor and duplicate-result progression tests cover exact XP/credit cases. | Backend service implemented; no real Home/Profile/Appearance UI or client inventory/equip journey. Amendment v2.1: the Appearance rig switch (Male/Female) and both-rig cosmetic variants do not exist; the account carries a single equipped cosmetic with no rig dimension. |
| Cosmetics do not change power; offline practice cannot mint value | Cosmetic-only inventory representation; result submission is restricted to game-server service auth; offline-practice rejection regression exists. | Source/backend-tested; offline save editing and client runtime journey not tested in this gate. |
| Party, 8-character code, leader and readiness | Backend create/join/leave/current/readiness exists. Enqueue now requires a party leader and every member ready; new regression covers not-ready rejection, ready enqueue, and non-leader rejection. | Backend-tested; no in-game party screen, party polling, dropped-member recovery, or user journey. |
| Measured single-region selection and search widening | Search window is configured at 150, expands by 50 every 10 seconds after 20 seconds to 350, and worker polls every 2 seconds. Bot fill threshold is 45 seconds. App settings currently say Fusion region `eu`. | Search behavior implemented in backend; `eu` is not supported by a concentrated-market RTT measurement, so region choice is blocked and must not be treated as accepted. |
| Practice while queued and 60-second wait choice | Practice and online queue are separate flows. There is no queue-preserving practice mode, return-to-queue screen, or 60-second “practice or wait” choice while the ticket remains active. | Failed / not implemented. |
| Public bot fill after 45 seconds; labeling and telemetry | Matchmaking composer assigns bot-filled matches after configured wait; ticket carries `HasBots`; network bot driver and presentation labels exist. | Source implemented; six-client/public runtime behavior and truthful telemetry remain unverified under Gate C. |
| AFK warning at 30 seconds; removal/substitution at 45 seconds | `NetMatch.HandleAbsence` warns at 30 seconds, substitutes at 45 seconds; `NetClientPresenter` displays a warning. Eligibility excludes substituted players. | Source implemented; timing, input activity coverage, and user-visible live behavior not runtime verified. |
| Transactional ledger and exact rewards; duplicate/out-of-order result safety | Unique ledger and participation keys, server-only result endpoint, idempotent duplicate result tests, server-failure ineligible/no-reward case. | Backend-tested locally (see automated results); concurrent duplicate delivery, database outage/retry and production PostgreSQL behavior are not verified. |
| Retry/recovery for auth, party, queue, server, capacity and outages | Unity online flow has visible retry/cancel and reconnect states; backend APIs map some errors. | Partial. No complete app screens, party retry, queued-practice recovery, capacity/outage injection, or proof that no user is stranded. |
| Support, reporting, in-app and web account deletion | Authenticated report, deletion request, authenticated data deletion, and public HTML deletion form/routes exist; tests verify request, public page, and account-data removal while retaining purchase records. | Backend-tested; missing in-game report/deletion screens. Public web form creates `pending_verification`; identity verification and processing of that unauthenticated request are not implemented, so end-to-end web deletion is incomplete. |
| All required screens and explicit failures | Existing Home, HUD, tutorial, pause/settings, online queue overlay, and results presentation exist. | Failed as a product gate: Startup/recovery, Party, Profile, Appearance, Account, Support/Report, and deletion are not all reachable as real in-game screens; the Amendment v2.1 first-launch body rig choice and the Appearance rig switch are not implemented; purchase/reward screens are correctly out of scope for this step. |
| Reinstall recovery + deletion end-to-end | No real linked Android identity was available; no reinstall or production deletion run was performed. | Blocked on Play Games app/client setup and real test accounts; no pass evidence. |

## Amendment v2.1 work items (2026-10-01)

Spec §15/§17 (Amendment v2.1) add the following to this gate. Items 1–3 remain unimplemented as of 2026-10-01; item 4's parity guards landed the same day over a data-only rig identity (no UI, art or network change), and the account still carries one `EquippedCosmeticId` with no rig dimension.

1. **First launch:** offer a one-time body rig choice — Male/Female — before the tutorial (Startup flow).
2. **Appearance:** add a body rig switch, available any time, alongside the existing inventory preview/equip.
3. **Cosmetic variants:** equipped cosmetics must present in both-rig variants (same silhouette quality, same readability, same price); the catalog/inventory model needs a rig dimension.
4. **Parity:** rig choice must not alter capsule size (1.8 m × 0.35 m), hitboxes, or animation results; cover this with tests. **Guard added 2026-10-01:** `Assets/Scripts/Player/BodyRig.cs` defines the two rig ids and one shared capsule/hitbox/animation profile; `Assets/Scripts/Tests/BodyRigParityTests.cs` proves both rigs resolve to that same profile, that the generated NetPlayer/PlayerPrefab assets use it, and that scripted movement traces are identical per rig. Unity EditMode **199/199 passed** including the five new tests, PlayMode **8/8 passed** (`Run-UnityTests.ps1`, 2026-10-01).

Art production for either rig stays gated to Gate G (spec §15); the approved graybox remains the intended presentation until then.

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

- No player-facing party, inventory/appearance, account-link, report, or deletion journey currently exercises the service APIs.
- Google linking remains unavailable until the Android SDK produces a server auth code and the backend is configured with the publisher's real server OAuth client credentials. Server-side code alone does not provide identity recovery.
- Guest refresh-token storage is app-private preferences. Reinstall recovery requires a linked Google Play Games account and a real device uninstall/reinstall run.
- The public deletion form only queues an unverified contact request. Add a publisher-controlled verification and completion process before claiming web deletion support.
- Matchmaking still has no queued-practice UX. Region `eu` is a configuration value, not a measured choice.
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
