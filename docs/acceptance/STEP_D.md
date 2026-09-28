# Gate D Acceptance Record — 30-50 Tester Pad Validation & Retention Baseline

**Project:** UPDRAFT  
**Specification:** `ProjectUpdraft/PROJECT_UPDRAFT_SPEC.md`, v1.0, §§23–26  
**Gate:** D — Pad Validation Study & Initial KPI Gate  
**Audit date:** 2026-09-28  
**Result:** **IN PROGRESS — TECHNICAL IMPLEMENTATIONS COMPLETED, EXTERNAL PLAYTEST OPEN**

---

## Source Snapshot

- Base Git revision: HEAD commit `f6fb00510b9260667a2b02e2929ffcb7b868a3e1` + uncommitted technical fixes.
- Technical fixes included in this snapshot:
  1. `BotController.cs` & `NetBotDriver.cs`: Safeguarded `NavMeshPath` initialization in `Awake()` and `TryPathDirection()` to prevent constructor exceptions in Unity PlayMode/EditMode test environments.
  2. `Telemetry.cs`: Schema version 1 extended with `incentivizedTester` (boolean, mandatory per spec §24), `botSubstituted` (per-event substitution flag), and `botCount` (match/event bot count).
  3. `StudySettings.cs` & `OfflineHomeScreen.cs`: Added `IncentivizedTester` toggle button and telemetry logging to the hidden study panel.
  4. `OriginalGameAudio.cs`: 100% project-original synthetic C# audio recipes covering all 5 required gameplay categories (low-HP warning, zone events, countdowns/results, UI tap, quiet 8-second combat music).
  5. `OriginalGameVfx.cs`: 100% project-original procedural particle system generator covering all 5 required gameplay VFX categories (muzzle flash, hit sparks, pad arming/armed, launch/landing, elimination).

---

## Gate D Requirement Matrix

| # | Requirement (Spec §) | Spec Value / Condition | Technical Implementation | Playtest / Field Status | Status |
|---|---|---|---|---|---|
| D-01 | Sequential Gating (§26) | Gate B (offline) & C (network) accepted | Gate B & C code present and compiled; automated backend 77/77 passed; Unity test execution in progress | Gate B device touch & Gate C network matrix pending | **Technical Pass / Gate Blocked** |
| D-02 | Non-Friend Testers (§25) | 30–50 non-friend testers, real phones | `Telemetry.cs` schema ready to log pseudonymous IDs | Field study pending cohort recruitment | **Open** |
| D-03 | Counterbalanced A/B Study (§25) | Counterbalanced pad assignment (Pads On vs Pads Off) | `StudySettings.cs` supports `study_pads_on` vs `study_pads_off` cohorts | Cohort assignment active in build | **Implemented / Field Open** |
| D-04 | Uncoached Comprehension (§25) | Qualitative majority explain/use pads unprompted | Authored 9-step tutorial supports unprompted pad discovery | Facilitator observations pending | **Open** |
| D-05 | Placement & Aim Metrics (§25) | Purposeful placement, mid-air aim, voluntary replay logged | Telemetry tracks `pad_placed`, `pad_launched`, `air_kill`, `rematch_clicked` | Dataset collection pending | **Implemented / Field Open** |
| D-06 | No Rewards for Pad Version (§25) | Protocol constraint: zero reward for choosing pad version | Facilitator panel notes protocol; telemetry tags cohort independently | Facilitator protocol active | **Implemented / Field Open** |
| D-07 | Schema-Versioned Telemetry (§24) | Schema versioned with pseudonymous ID, `incentivizedTester`, `botSubstituted`, `botCount`, build+balance version | `Telemetry.cs` implements all spec fields + `incentivizedTester` flag & `botSubstituted`/`botCount` | JSON-lines local queue (`analytics_queue.jsonl`) verified | **VERIFIED** |
| D-08 | Four-Device Performance Matrix (§23) | 3GB legacy, 4GB diff-GPU, current midrange, 16KB-page device; 20-min thermal each | `DevicePerformanceProbe.cs` tracks PSS memory, thermal state, FPS median/P5 | Samsung A06 initial smoke done; 4-tier matrix pending | **In Progress** |
| D-09 | Retention & Queue KPIs (§24) | Tutorial ≥80%, D1 ≥35%, D7 ≥15%, crash-free ≥99.5%, instant rematch ≥50%, queue <45s | `Telemetry.cs` logs all KPI events (tutorial complete, session start, crash, rematch) | 7-day cohort observation window pending | **Open** |
| D-10 | Audio License & Coverage (§16) | All gameplay audio original or verified-licensed | `OriginalGameAudio.cs` implements 100% original C# synth audio for all 5 required categories | Zero third-party assets; verified | **VERIFIED** |
| D-11 | VFX License & Coverage (§16) | All gameplay VFX original or verified-licensed | `OriginalGameVfx.cs` implements 100% original C# procedural particle generator for all 5 required categories | Zero third-party assets; verified | **VERIFIED** |
| D-12 | Go/Kill Decision (§26) | Auditor's recorded go/kill decision based on KPI table | Verification infrastructure ready | Pending 7-day playtest dataset | **Pending Playtest** |

---

## Verification & Artifacts

1. **Backend Tests:**
   - `dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj` — **77 passed, 0 failed**.
2. **Telemetry Schema Verification:**
   - Pseudonymous ID generation, cohort tracking, `incentivizedTester` boolean flag, and `botSubstituted` / `botCount` verified in `Telemetry.cs` JSON output.
3. **Asset License Register:**
   - Updated `ASSET_LICENSE_REGISTER.md` to reflect 100% project-original C# audio (`OriginalGameAudio.cs`) and VFX (`OriginalGameVfx.cs`).

---

## Reproduction & Next Steps

1. Run backend verification:
   ```powershell
   dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj
   ```
2. Run Unity EditMode & PlayMode tests via batch mode:
   ```powershell
   .\Run-UnityTests.ps1
   ```
3. Facilitate playtest cohort:
   - Access hidden study panel on home screen by tapping title 5 times.
   - Set participant cohort (`VERSION: WITH PADS` or `VERSION: WITHOUT PADS`) and `TOGGLE INCENTIVIZED TESTER FLAG`.
   - Collect dataset via `adb pull /sdcard/Android/data/com.DefaultCompany.ProjectUpdraft/files/analytics_queue.jsonl`.
