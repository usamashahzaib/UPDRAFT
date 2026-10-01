# Gate C Acceptance Record — Network Slice

**Project:** UPDRAFT  
**Specification:** `ProjectUpdraft/PROJECT_UPDRAFT_SPEC.md`, v1.0, §§4, 10, 20–23, 26, 28–29, 31–33  
**Gate:** C — dedicated server, six clients, scoring, reconnect  
**Audit date:** 2026-09-28  
**Result:** **NOT ACCEPTED — implementation evidence is partial; real multiplayer acceptance is blocked.** No Gate C pass is claimed.

## Source and ownership snapshot

- Initial Git revision before this pass: `f6fb00510b9260667a2b02e2929ffcb7b868a3e1`.
- Initial worktree already contained user edits to `NetClientPresenter.cs` and `docs/PROGRESS_2026-09-28.md`; they were retained. The shared worktree also contains concurrent edits in `BotConfig.asset`, `BotController.cs`, `BotDecisionMaker.cs`, `BotPerception.cs`, `Config/BotConfig.cs`, `GameBootstrap.cs`, `MatchController.cs`, `PlayerController.cs`, `LaunchPadBehaviour.cs`, `ShotResolver.cs`, and new `WeaponState.cs` files. These were not authored or altered here and remain untouched.
- Initial process inspection found Unity PID 17208, Unity helper processes, and a `dotnet` process. CIM command-line inspection was denied. A later pre-launch check found Unity PID 12684 and a generated `InitTestScene*.unity` file; this pass did not launch that Editor or alter/remove the generated scene. No Linux build or game server was launched by this pass.
- The backend suite was run once after the server-admission and stale-allocation edits. Exact test command and outcome are below. Unity tests and player builds were not run.
- The Photon app settings asset is ignored and not present in source evidence. No credential or paid deployment was created.

## Requirements mapped to evidence

| Requirement | Implementation and evidence | Status |
|---|---|---|
| Fusion 2 Server mode integrated into actual match gameplay; Simple KCC; shared gameplay config/rules | Existing `NetSession`, `NetPlayer`, `NetMatch`, and `NetPad` connect the live match path to Fusion Server mode, Simple KCC, the shared movement simulator, named rules/config assets, and server-authoritative scoring/damage. Source audit only in this pass. | Implemented; runtime unverified |
| 30 ticks/s and ≤150 ms validated rewind | `NetworkProjectConfig.fusion` now sets 30 client ticks with server tick divisor 1 (30 ticks/s) and 150 ms hitbox buffer. These are configuration values, not a measured live-server result. | Implemented; runtime unverified |
| Prediction/reconciliation, remote interpolation, lag compensation | Fusion input authority and replicated network properties are used by the live player and presentation path; shots use Fusion lag compensation. No six-client capture or reconciliation measurements were produced. | Implemented; runtime unverified |
| Dedicated Linux headless build and actual match run | 2026-09-29: `BuildLinuxServer.cs` cross-built a Development Linux x64 Server-subtarget player from Windows (hash below). The binary has not been launched on a Linux host and no match has run on it. | Build produced; run blocked |
| Allocator cap and authenticated backend calls | Existing server-token flow, allocator cap (`Allocator:MaxConcurrentMatches`, default 4), ticket validation and result endpoints were audited. The live service was not configured or exercised. | Implemented; runtime unverified |
| Ticket binds account, match, and team; duplicate ticket/account joins rejected | Server admission now checks ticket match and reserved team, rejects a second live connection for the same account, and consumes each accepted ticket once. Unity compilation and network behavior were not exercised. | Implemented; unverified |
| Six slots, load timeout, empty-slot cancellation | Existing match startup waits for the reserved human count and six avatars, then cancels at configured load timeout. No load-timeout client scenario was run. | Implemented; unverified |
| Reconnect ≤5 s grace, same avatar and state, bot label, no refresh | Server retains the reserved avatar, substitutes a labeled bot after 5 s, and permits a disconnected reserved account to regain input authority. The client now checks the authenticated reconnect endpoint, gets a fresh short-lived join ticket, retries visibly, and can cancel. No server/client reconnect run was captured. | Implemented; unverified |
| Backend outage fails closed; results are authenticated and duplicate-safe | Ticket-validation exceptions now refuse admission. Backend result retry/idempotency code and existing duplicate-result tests were audited. Actual outage and duplicate-network deliveries were not injected in this pass. | Implemented; unverified |
| Stale server crash cancels without a winner, participation, rating, or reward | Allocator now closes expired leases and cancels their matched tickets without calling result processing. New regression test checks cancellation and absence of participation rows. | Verified by automated backend test |
| Six real clients at 30/80/150 ms RTT with loss/jitter | No clients, network shaper run, or packet capture was available. | Blocked |
| Disconnect during fire, launch, capture, and respawn; reconnect; duplicate joins/results; wrong clocks; server crash; backend outage; repeated matches | Only stale allocation cancellation and existing backend result idempotency are covered by local automated tests. Required real failure-injection matrix was not run. | Blocked |
| Bandwidth ≤100 KB/s per client in pad-heavy fights | No live traffic capture or Fusion statistics artifact exists. | Blocked |
| Dedicated Linux deployment and operational capacity evidence | No Linux server artifact, deployment, capacity measurement, or paid infrastructure approval exists. | Blocked |

## Automated results

- **Backend:** `dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj --no-restore` — **76 passed, 0 failed, 0 skipped**, .NET 10.0, duration 2 s, 2026-09-28. This includes the new stale-allocation cancellation regression test and the existing duplicate-result idempotency test.
- **Unity EditMode/PlayMode:** not run by this pass. A Unity Editor was active at both process checks; no second Unity process was started. A generated `InitTestScene*.unity` appeared in the worktree during the audit and is left untouched.
- **Linux headless build (2026-09-29):** `ProjectUpdraft/Build-LinuxServer.ps1` — **Succeeded**, 174,005,009 bytes total, 1 min 41 s, log `ProjectUpdraft/TestResults/LinuxServerBuild.log` (ignored). Two fixes were needed first: `BuildPlayerOptions.subtarget` takes an `int` cast, and `TouchInputManager.Vibrate` now guards `Handheld` behind `UNITY_ANDROID || UNITY_IOS` because `Handheld` does not exist in standalone players.
- **Six-client/network tests:** not run; no automated result file or real-device evidence is available.
- **Static whitespace check:** `git diff --check` passed for the inspected working tree at the time it was run.
- Existing warnings: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 reports NU1903; current backend tests also emit nullable warnings in `AuthEndpointsTests.cs` and `PartyEndpointsTests.cs`.

## Reproduction commands

Run backend tests from the repository root:

```powershell
dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj --no-restore
```

Run Unity tests after confirming the Editor is closed for this project:

```powershell
ProjectUpdraft/Run-UnityTests.ps1 -Platform EditMode
ProjectUpdraft/Run-UnityTests.ps1 -Platform PlayMode
```

Build the Linux Dedicated Server player with Unity 6.3.25f1 and Linux Dedicated Server Build Support installed (run only when no Editor owns the project):

```powershell
ProjectUpdraft/Build-LinuxServer.ps1
# output: ProjectUpdraft/Builds/Linux/UpdraftServer.x86_64 (+ UnityPlayer.so, UpdraftServer_Data/)
Get-FileHash ProjectUpdraft/Builds/Linux/UpdraftServer.x86_64 -Algorithm SHA256
```

Run the built player only against an approved backend and configured Photon application. Supply `UPDRAFT_SERVER_KEY` and `UPDRAFT_BACKEND_URL` through the deployment environment; never put the key in player arguments or source. The `-devSession ... --insecure-dev` path is local-only and cannot satisfy Gate C.

## Failure cases and known issues

- No real six-client result exists, so latency, loss, jitter, bandwidth, disconnect timing, reconnect, repeated-match resource growth, and backend outage requirements remain unverified.
- A server crash is canceled after the existing 15-minute allocation lease becomes stale; until then the allocation remains reserved. No live crash/restart run was performed.
- The `-updraftServer` pool loop retries claims, but production allocator deployment, health checks, server process supervision, and capacity behavior have not been demonstrated.
- Fusion connection and backend credentials are intentionally absent from tracked artifacts. The Photon application ID and an approved Linux server/test environment are required to complete real acceptance. Paid deployment requires explicit approval.
- `STEP_0.md` results are prior-gate evidence and do not satisfy any Gate C runtime requirement.

## Artifact and balance identity

- Linux server launcher `UpdraftServer.x86_64` (Development, Server subtarget, 2026-09-29) SHA-256: `C4361B92A96F764C885510343418F0A584DFD2592FB4F097ECB6E493D2419101`. `UnityPlayer.so` SHA-256: `F20FA0E5584C2BA7EA821390B26D371571A519C695776AECCD70F8A3C607DAF2`. The launcher is a 4,808-byte stub, so every run record must carry both hashes from the build it actually deployed.
- Relevant source SHA-256 at record time: `NetSession.cs` `AC56E3A5BAC0DE27F077CFEE3655A7A71A87DB644C2F9E96CC1CC316A0211150`; `OnlinePlay.cs` `5CCCEC38CA613B68854B667C5F79FD93F78106FEF3801DC86B781AD4A05E968D`; `BackendClient.cs` `3EAD2D63B6FD459CAB2D25F303F92F1A0CE3E6AECEB3647A0155C5E9AB09798A`; `MatchEndpoints.cs` `BC6310BD6FA7F04B0AEB9B4D4C5CD3AA7C93063ABA07DCBA9F7C624F90B298C9`; `MatchTicketTests.cs` `C1F0C2AC02544DF9D3BC0C59A563673327278818DAFDFE423AABEA51352303DA`; `NetworkProjectConfig.fusion` `B034D0EAA798C2A3197B21E061B56864AC0B61ADBCBAEDACA3D4BAC618BA07AC`.
- No balance version was fabricated. Runtime uses the shared `MovementConfig`, `PadConfig`, `MapConfig`, `MatchRulesConfig`, and `WeaponDefinition` resources; this pass does not freeze those assets or claim a signed balance snapshot.
- No licenses or third-party dependencies changed in this pass.
- Gate C artifacts are not accepted or frozen. Freeze them only after the full real-client acceptance matrix passes against one recorded source revision and balance snapshot.
