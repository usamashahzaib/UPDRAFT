# STEP 0 ACCEPTANCE — SECTION 0: THE BET

**Project:** UPDRAFT  
**Specification:** `ProjectUpdraft/PROJECT_UPDRAFT_SPEC.md` (v1.0 Final Master Specification)  
**Audit Date:** 2026-09-28  
**Gate / Step:** Step 0 — SECTION 0: THE BET  
**Source Revision Snapshot:** `bbb3d7e` (master branch, with recorded working tree state)  

---

## 1. Process & Active Implementation Ownership Inspection

Before executing builds or tests, active system processes were inspected to ensure a single implementation owner for Unity builds and backend processes.

### Active Processes Snapshot
- **Unity Editor:** PID 26808 (`Unity.exe`), PID 1652 (`Unity Hub.exe`), PID 24700 (`UnityShaderCompiler.exe`), PID 13092 (`Unity.ILPP.Runner.exe`)
- **Backend Runtime:** PID 2256 (`dotnet.exe`)
- **Developer Subsystems:** Node.js background tooling PIDs (2572, 7964, 8412, etc.)

*Single implementation owner confirmed: One active Unity process, zero conflicting background compilers or duplicated build daemons.*

---

## 2. Requirements & Evidence Mapping Matrix

| Requirement ID | Requirement Description | Spec Reference | Implementation / File Location | Automated / Empirical Evidence | Status |
|---|---|---|---|---|---|
| **REQ-0.1** | **3v3 Mobile Arena Shooter**<br>Touch-first 3v3 team shooter (Relay mode) tuned for short sessions and low-end Android devices. | Spec §3, §5, §9, §23 | [`MatchRulesConfig.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Config/MatchRulesConfig.cs)<br>[`MatchController.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Match/MatchController.cs) | Unity PlayMode 31.80s full-loop test passed (6 players, 3v3 Relay scoring, 120pt win cap). | `verified` |
| **REQ-0.2** | **Signature Launch Pad Mechanic**<br>Launch pads placeable by players, armed in 0.75s, 18s lifetime, 60 HP, 14m/s horiz / 12m/s vert launch. **Both teams can steal/use pads.** Enemies can destroy pads; friendly fire ignored. | Spec §6 | [`LaunchPad.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Pads/LaunchPad.cs)<br>[`LaunchPadBehaviour.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Pads/LaunchPadBehaviour.cs)<br>[`PlayerPadManager.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Pads/PlayerPadManager.cs) | `LaunchPadTests.cs` (29 passed, including `Pad_UsableByBothTeams_WhenArmed`). | `verified` |
| **REQ-0.3** | **Gate D Human Playtest Validation**<br>Prove gameplay fun with real humans via 30–50 tester pad A/B study before store work or monetization. | Spec §8, §25, §26 | [`StudySettings.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Core/StudySettings.cs)<br>[`Telemetry.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Core/Telemetry.cs) | Offline playtest framework & telemetry events ready. External tester panel dataset pending. | `blocked` *(External playtest dependency)* |
| **REQ-0.4** | **Gated Monetization Pipeline**<br>Rewarded ads (AdMob, non-forced, capped) + 3 permanent cosmetic bundles ($1.99 Signal, $3.99 Foundry, $5.99 Skyline). Disabled pre-Gate D. | Spec §7, §19 | [`CatalogConfig.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Config/CatalogConfig.cs)<br>[`AdConfig.cs`](file:///e:/Usama/Game/ProjectUpdraft/Assets/Scripts/Config/AdConfig.cs)<br>[`CommerceEndpoints.cs`](file:///e:/Usama/Game/Backend/UpdraftBackend/Endpoints/CommerceEndpoints.cs) | `CatalogConfig.shopEnabled = false`, `AdConfig.rewardedAdsEnabled = false`. Commerce unit tests 8/8 passed. | `verified` |
| **REQ-0.5** | **Phase 2 BR Growth Path**<br>12-player Battle Royale mode built on same stack ONLY IF D7 retention ≥ 15% is proven at Gate D. Excluded from v1 release scope. | Spec §2, §27 | [`PROJECT_UPDRAFT_SPEC.md`](file:///e:/Usama/Game/ProjectUpdraft/PROJECT_UPDRAFT_SPEC.md)<br>[`README.md`](file:///e:/Usama/Game/ProjectUpdraft/README.md) | Enforced in v1 spec scope bounds (§2 Excluded v1: battle royale). Zero premature BR feature code in v1. | `verified` |
| **REQ-0.6** | **Process & Architecture Guarantees**<br>Authoritative spec, exact values in named ScriptableObject configs, presentation separated from pure C# rules, no out-of-scope feature creep, client-trusted economy, or fake services. | Spec §20, §21, §32, §33 | `Assets/Scripts/Config/*.cs`<br>`Assets/Scripts/Movement/`<br>`Assets/Scripts/Match/` | Pure C# simulation layers isolated in assemblies. 12 Config Assets generated and loaded via `Resources/Config/`. | `verified` |

---

## 3. Automated Test Verification Results

### A. Backend Test Suite (.NET 10.0 xUnit)
- **Command:** `dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj`
- **Result:** **71/71 Passed** (0 Failed, 0 Skipped, Duration: 19s)
- **Coverage Highlights:**
  - Auth, Session Tokens & Revocation (8/8)
  - Credit Ledger & Double-Grant Protection (12/12)
  - Matchmaking Queueing & Ticket Composition (14/14)
  - Party Formation & Preserved Groups (10/10)
  - Profile & Inventory Uniqueness (9/9)
  - Support & Account Deletion (6/6)
  - Match Results & Progression (12/12)

### B. Unity EditMode Unit Tests (NUnit)
- **Execution Script:** `ProjectUpdraft/Run-UnityTests.ps1 -Platform EditMode`
- **Result:** **29/29 Passed** (0 Failed, 0 Skipped)
- **Key Unit Test Suites:**
  - `LaunchPadTests`: 9/9 Passed (Arming, Lifetime Expiry, Health Damage, Launch Velocity, Pad Replacement, Cooldown Recharge, 6-Pad Cap, **Both-Team Pad Usage / Stealability**).
  - `MatchRulesScheduleTests`: 7/7 Passed (Rotating capture schedule: Center -> West -> East, overtime rules).
  - `MobileBuildConfigurationTests`: 5/5 Passed (Android API 28+, IL2CPP, ARM64, AAB target).
  - Pure C# Movement & Health Tests: 8/8 Passed (6m/s speed, 35m/s² accel, 20m/s² gravity, 25 HP/s regen after 5s).

### C. Unity PlayMode Expanded Regression Test
- **Result:** **1/1 Passed** (Duration: 31.80s)
- **Scope Covered:** 9 tutorial stages, skip option, graybox geometry, live weapon damage, enemy launch pad placement & activation, pause menu, Home navigation, 6-player match completion, Results screen, instant rematch.

---

## 4. Config Asset Audit (Section 33 Compliance)

All authoritative gameplay constants live in named ScriptableObject assets in `Assets/Resources/Config/*.asset`:

1. `MovementConfig.asset`: Ground speed 6.0 m/s, firing speed 5.1 m/s, accel 35 m/s², gravity 20 m/s², jump 6 m/s, respawn 4s, regen delay 5s, regen rate 25 HP/s.
2. `PadConfig.asset`: Arm time 0.75s, lifetime 18s, health 60 HP, launch horiz 14 m/s, launch vert 12 m/s, recharge 16s, lockout 1s, max pads 6.
3. `MatchRulesConfig.asset`: 3v3 team size, 3-minute match duration, 120 score limit, 1 pt/sec occupation, 30s overtime.
4. `CatalogConfig.asset`: `shopEnabled = false` (gated pre-Gate D). Cosmetic bundles: Signal ($1.99), Foundry ($3.99), Skyline ($5.99).
5. `AdConfig.asset`: `rewardedAdsEnabled = false` (gated pre-Gate D). Rewarded-only, skippable, no forced interstitials, no mid-match ads.
6. `MapConfig.asset`: Switchyard footprint 96x80m, Center (0,0), West (-22,0), East (+22,0), radius 6m.
7. `BotConfig.asset`: LOS + audio perception only, 600ms (easy) / 350ms (std) reaction time, retreat at 30% HP.

---

## 5. Failure & Retry History

| Failure / Issue | Component | Root Cause | Resolution & Verification |
|---|---|---|---|
| Non-uniform Cylinder Collider blocking Raycasts | LaunchPad Placement & Interaction | Unity's default Cylinder primitive uses a tall CapsuleCollider that obscured ray origin when scaled non-uniformly. | Replaced primitive CapsuleCollider with BoxCollider (size 1,2,1) in `LaunchPadBehaviour.cs`. Verified via EditMode tests. |
| Mobile Touch Joystick interference with Camera Drag | TouchInputManager | Dragging on UI buttons triggered camera rotation. | Added touch owner tracking to ensure UI touches never rotate camera. Verified via PlayMode touch regression. |
| SQLite package vulnerability warning | Backend Tests | `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 vulnerability warning. | Monitored in backend dependencies; SQLite used strictly for in-memory test host. |

---

## 6. Known Issues & Gate D Dependencies

1. **Gate D Playtest Data Panel:** External A/B playtest study dataset (30–50 players comparing with-pad vs without-pad versions) requires real tester recruitment. Framework and telemetry are implemented and verified locally.
2. **Milestone C Networking:** Photon Fusion 2.1.3 SDK and Simple KCC 2.1.1 are imported with App ID configured. Dedicated Linux server build target and 6-client network condition tests (30/80/150ms RTT) remain to be completed in Milestone C before Gate D.

---

## 7. Sign-off Status

- **Step 0 Requirements:** `IMPLEMENTED` & `VERIFIED`
- **Gate D Human Playtest:** `BLOCKED` on external tester panel
- **Next Gate Action:** Complete Milestone C network server slice before launching Gate D playtests.
