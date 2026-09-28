# STEP 1 ACCEPTANCE — GATE A: TRUSTWORTHY ANDROID FOUNDATION + TOUCH

**Project:** UPDRAFT
**Specification:** `ProjectUpdraft/PROJECT_UPDRAFT_SPEC.md` (v1.0 Final Master Specification)
**Date:** 2026-09-28
**Gate / Step:** Gate A — Foundation (Android build, touch, camera, movement correctness)

**Source revision (certified):** `f6fb005` ("chore: snapshot current work") **plus** working-tree deltas this step made on top of it (§2). The tree is shared with one other active implementation owner (Milestone C/B files kept changing during this step; their activity is recorded in §7 rather than discarded). The device evidence in §5 is tied to the exact APK hash built at 20:46 local time; `PlayerController.cs` (21:03) and `GameBootstrap.cs` (21:21) changed *after* that build, so the APK artifact does not include those later edits (§7.4).

**Automated result snapshot at certification:**
- `ProjectUpdraft/TestResults/EditMode.xml` — **44/44 passed** (run 18:48 UTC+5)
- `ProjectUpdraft/TestResults/PlayMode.xml` — **4/4 passed** (run 19:02 UTC+5, 65.6 s)
- APK `ProjectUpdraft/Builds/Android/UpdraftDev.apk` — 87,400,422 bytes, **SHA-256 `0f6fd0c9070c895fafcfc185fb3803a69de474b026d6a0415954eb088aa19d65`** (built 20:46)

---

## 1. Process & active implementation ownership

Inspected before any editor/build/test launch:

- 19:0x — `Unity.exe`: none; `dotnet.exe`: none. This session was the only build/test owner at start.
- 20:47 (device preflight) — `Unity.exe`: none confirmed before device phase.
- During the step a second implementation owner worked the same tree (commits `f6fb005`; live edits to `Scripts/Network/*`, `Scripts/Bots/*`, `PlayerController.cs`, `GameBootstrap.cs`, TestResults). Their in-flight work that **broke compilation** was completed with minimal one-line fixes (§2 REQ-A.16); their red/green loops and files were otherwise left untouched. No build or device session was run concurrently with a live Unity process.

## 2. Requirements mapping — Gate A

| ID | Requirement (spec ref) | Implementation | Evidence | Status |
|---|---|---|---|---|
| A.1 | Baseline snapshot; single owner; inspect processes before launch (§33) | git HEAD recorded before edits; process checks before editor/device launches | §1; git log `bbb3d7e`→`f6fb005` | implemented+verified |
| A.2 | Reproduce & fix failing multitouch regression — second finger acquires fire while first owns movement (§8) | `TouchInputManager` EnhancedTouch per-touch ownership (`moveTouchId`/`aimTouchId`/`fireTouchId` separate) | `OfflineSliceSmokeTest.MainScene_…` assertion "A second finger must acquire fire…" passed; red/green mutation in §4 | implemented+verified |
| A.3 | Real simultaneous move+aim+fire (§8) | same | PlayMode regression: move>0.5 while `FireHeld` and `AimDelta>0` (one frame apart, deterministic) | implemented+verified (editor) / recorded on device (§5, video) |
| A.4 | Rapid taps, holds, drag-fire, cancel (§6, §8) | same file; pad hold>180 ms preview, drag-off cancel | PlayMode `TouchCaseMatrix_RapidTapsHoldsDragFireCancelAndUIConsumption` (3 taps, 0.3 s jump hold edge-trigger, fire-drag aim, pad cancel costs nothing) | implemented+verified |
| A.5 | UI touches never rotate camera / one touch one owner (§8) | `IsOverMenu` raycast consumption + button rects | TouchCaseMatrix UI-consumption block: no fire/move/aim from UI touch | implemented+verified |
| A.6 | Handedness + ≥48 dp targets (§8) | `SetLeftHanded` mirrored rects; 48 dp clamp | `TouchInputHygieneTests.LeftHandedLayout_MirrorsButtons_KeepsMinimumTargets` (EditMode) | implemented+verified |
| A.7 | Safe areas / notch (§8) | `RecalculateButtonRects` uses `Screen.safeArea` with batch-mode fallback | code + device landscape run on 720×1600; no clipped controls observed in video | implemented (verified on one device) |
| A.8 | Background/foreground; no stuck input; no lock-screen bypass (§17/§28) | `OnApplicationFocus(false)` releases fire/move/aim/pad owners; no wake-lock/ShowWhenLocked/DismissKeyguard flags anywhere | Home-key + resume exercised twice on device (F05/F06, H07/H08); manifest scan clean; EditMode `LosingFocus_ReleasesStuckFireMoveAimAndPad`, `Disabling_ClearsPerFramePressesAndOwnedTouches` | implemented+verified |
| A.9 | Landscape URP (§8/§23) | Manifest `screenOrientation=0x0` (landscape) on launcher activity; URP asset; no shader failures | aapt dump + logcat: 0 shader/URP errors; **magenta 0.000 %** machine-measured on 10 device frames (`analyze.html` pixel audit) | implemented+verified |
| A.10 | Movement config exact (§5) | `MovementConfig.asset`: 100 HP; 6/5.1 m/s; 35/20 m/s²; jump 6 m/s; capsule 1.8×0.35, step 0.35; respawn 4 s; regen 5 s/25 HP/s | asset file values read directly (matches spec exactly) | implemented+verified |
| A.11 | Spawn/respawn camera facing (§9) | `MatchController.BeginMatch` → `mainCamera.SnapBehindTarget()` after warp; respawn path likewise | PlayMode smoke asserts tutorial/camera flow; device video shows spawn facing arena | implemented+verified |
| A.12 | Wall-clip sphere cast (§8) | `ThirdPersonCamera` `Physics.SphereCast` obstruction mask | code; device camera drags near cover show no through-wall view in captured frames | implemented (device spot-check) |
| A.13 | LOS-only aim slowdown ≤3°/≤25 m/70 %, no snap (§8) | `AimAssistRules` (3°, 25 m, 0.7) + `IsAimAssistRegionActive` raycast; scales `AimSensitivityScale` only | code + offline/online share the same constants | implemented+verified |
| A.14 | Camera-to-muzzle obstruction; blocked-shot indicator (§8) | `PlayerController` camera aim point + muzzle trace, `OnShotBlocked` → HUD | code; device session did not reproduce a blocked shot (not observed) | implemented (device verification open) |
| A.15 | Credential-free offline (§20/§22) | offline practice never touches auth; telemetry local-only queue | device sessions ran with no network/auth; `analytics_queue.jsonl` written locally only | implemented+verified |
| A.16 | Compile-clean tree (working-tree repair) | `Updraft.Network.asmdef` + `Photon.Realtime`; `NetSession` alias fix; `NetClientPresenter`/`OnlinePlay` using-fixes | EditMode 44/44 after repair; tree previously failed with CS0012/CS0234 | implemented+verified |

**Budgets (measured, not inferred):** startup via `adb shell am start -W` TotalTime = **679 / 716 / 744 / 695 / 745 / 831 ms** across cold starts (budget ≤8 s). In-engine markers: `engine_to_first_script` = 2228 / 2238 / 2258 / 2273 ms; `match_load_in` = 1–2 ms. `UPDRAFT_PERF` (30 s windows): **average_fps 29.9–30.1, frames_over_50ms ≤7, unity_allocated_mb 70 (menu) / 99 (match)**; OS-level PSS ≈ **281.7 MB**. All within §23 for the reference device.

## 3. Automated regression results

Commands (reproducible):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ProjectUpdraft/Run-UnityTests.ps1 -Platform EditMode
powershell -NoProfile -ExecutionPolicy Bypass -File ProjectUpdraft/Run-UnityTests.ps1 -Platform PlayMode
powershell -NoProfile -ExecutionPolicy Bypass -File ProjectUpdraft/Build-AndroidDev.ps1
```

| When (local) | Suite | Result | Notes |
|---|---|---|---|
| 18:48 | EditMode | **44/44** | includes new `TouchInputHygieneTests` |
| 19:02 | PlayMode | **4/4** (65.6 s) | includes new `TouchCaseMatrix_…` |
| 19:5x | PlayMode (mutation) | **1 run RED** on the exact Gate A assertion, then GREEN after revert | §4 |
| 20:45 | PlayMode | **4/4** (66.2 s) | final instrument fix |
| 21:52 | EditMode (final tree) | **142/142** | parallel owner grew the suite (44→142) |
| 21:55 | PlayMode (final tree) | **5/6** | their new `PrepBlocksDamageAndPads_…` fails — see §7.5 |

Gate A's own suite is green at its certified revision; the newest owner-added Milestone B test is red and is **not** claimed by this step.

## 4. Red/green proof for the multitouch regression

The regression was inherited green (the parallel owner's EnhancedTouch rewrite fixed it). To prove the guard catches the original defect, a temporary mutation replicating the legacy single-touch processing was applied:

```
for (int i = 0; i < touches.Count; i++) { if (i > 0) break; /* MUTATION */ ...
```

Result: `MainScene_LoadsAndRunsForFiveSeconds_WithoutErrors` **FAILED** with exactly
"A second finger must acquire fire while the first owns movement. Expected: True But was: False".
Revert → **1/1 passed** (32.7 s). First mutation candidate (block re-acquisition while `fireTouchId != -1`) did **not** trip the assertion and was discarded — recorded so the guard's catch-scope is honest.

## 5. Real-device evidence (Samsung SM-A065F, Android 14, 720×1600 @ 300 dpi, ARM64)

**Artifact:** `UpdraftDev.apk` (87,400,422 B, SHA-256 `0f6fd0c9…d65`), ARM64-v8a, IL2CPP, debuggable, minSdk 28, targetSdk 36, landscape, launchable `com.unity3d.player.UnityPlayerGameActivity`. Package `com.DefaultCompany.ProjectUpdraft` (publisher-owned ID still absent — §8).

**Recorded session (final build):** `ProjectUpdraft/Builds/AcceptanceStep1/step1_v6.mp4` (150 s, 24,490,996 B fetched; on-device final size 25,000,135 B, SHA-256 `a70d4cd5785d6d47939db8381695d94779e33b4a3370f92b4eebdc8e16abbed1` for the fetched file). Sequence visible in recording + screenshots `H01–H11`, `M01–M06`: Home → PRACTICE → skip training → in-match move/aim/fire → jump → pad tap → reload → pause → RESUME → Home-key background → foreground resume → camera drag → movement → pad hold-cancel → pause/resume again.

**Machine-readable verification:**
- Telemetry (`queue_v6.jsonl`, SHA-256 `52d0db94…`): this build's sessions produced `match_started` (5 bots, pads enabled), `weapon_selected`, bot `pad_placed`/`pad_used`, `match_completed` ×6 (outcomes/360-point enemy blowouts consistent with a passive human), `startup_budget` = 2228 ms.
- Touch pipeline: 33 `[TouchDiag]` began-events incl. exact pad (1328,103 bottom-origin) and pause (84,636) button hits; `overMenu=True firstHit=WeaponBtn_1` on one tap proves UI consumption intercepts gameplay touches.
- Rendering: magenta 0.000 % on every sampled frame; zero shader/URP errors; zero `FATAL EXCEPTION`/`Fatal signal`/ANR across all sessions.
- Thermal: session ran at normal temperature (no `thermalservice` status event; brief single-window run — the 20+ min thermal matrix remains open, §8).

**What the device evidence does NOT yet show (honesty list, §7):** a *human-placed* pad (`pad_placed byBot:false`) from this step's injected touches (placeable spots were rejected by the spawn-exit rule; earlier coordinate errors), and human pad-launch on-device. These actions are proven by automation (PlayMode) but the injected-touch device run has not captured them.

## 6. Reproduction

1. Clone + open `ProjectUpdraft/` with Unity 6000.3.25f1 (revision e1dba0a9aba4).
2. Tests: the two `Run-UnityTests.ps1` commands above.
3. APK: `Build-AndroidDev.ps1` → `Builds/Android/UpdraftDev.apk`; expected ~87.4 MB, ARM64/IL2CPP/debuggable.
4. Device: `adb install -r`; `adb shell am start -W …` (cold TotalTime <1 s); `adb pull /sdcard/Android/data/com.DefaultCompany.ProjectUpdraft/files/analytics_queue.jsonl` for event evidence; logcat greps `UPDRAFT_BUDGET|UPDRAFT_PERF|TouchDiag`.

Balance/config version: `v1.0-spec` (stamped in every telemetry event). Build version: 1.0.

## 7. Failures, defects and corrections during this step (no hidden TODOs)

1. **Baseline didn't compile** (`NetSession.cs` CS0012/CS0234) — in-flight parallel refactor; repaired by this step (asmdef + one alias fix), recorded as REQ-A.16.
2. **`f6fb005` itself does not compile** (`NetClientPresenter`/`AimAssistRules`, `OnlinePlay`/`TimeSpan`) — same owner, same pattern of missing usings; fixed in working tree. Until they commit, the repo HEAD alone does not build.
3. **Startup instrument bug** — first version logged `startup=-103ms` (clock-domain error: static-init runs after Unity realtime zero). Rewritten to `engine_to_first_script` + external `am start -W`; the bogus event remains in old telemetry queues and is superseded.
4. **Artifact/source drift** — `PlayerController.cs` (21:03) and `GameBootstrap.cs` (21:21) changed after the 20:46 APK. The device section certifies the APK hash; re-freezing requires a rebuild after the tree stabilizes.
5. **Parallel owner's new PlayMode test is red (5/6)** — `PrepBlocksDamageAndPads_LiveFire_DestroysEnemyPads_NotFriendly` fails "Friendly fire must not damage a pad" (60 → 45.7 HP). Analysis: the runtime guard is correct (`PadData.TeamId != teamId`); enemy-bot crossfire legitimately damaged the friendly test pad during the live-fire window — test-isolation flaw, Milestone B scope, left to the owning session untouched.
6. **Operator tap-coordinate errors (not code defects)** — two sessions of missed buttons (doubled y-axis flip; ignored `matchWidthOrHeight` blend scale √(0.8333·0.6667)=0.745). Caught by telemetry/screenshot audits; corrected targets used for the recorded sessions.
7. **Earlier recording pulled before the recorder finished** (truncated file); superseded by `step1_v6.mp4` fetched after recorder expiry and cross-checked against on-device file size.
8. **Unity license hiccup** — one EditMode run aborted with exit 198 ("No valid Unity Editor license found") immediately after passing; retry passed (142/142). Transient licensing-client issue.

## 8. Known issues & open dependencies

- **Human pad place/launch on-device** still to be captured from injected or human touches (blocked only by placement-validity positioning; automation proves the actions).
- **Blocked-shot indicator on-device** not yet observed (requires a wall-between camera and muzzle scenario on the phone).
- **20+ min thermal run** and the §23 device matrix (3 GB device, other GPUs, aspect ratios) not started — single reference device only.
- **Publisher-owned application ID / signing** still placeholder (`com.DefaultCompany.*`) — material authority dependency, unchanged from STEP_0.
- **Milestone C** remains unimplemented acceptance-wise (Fusion session code exists but no six-client evidence).
- TestResults XMLs are churned by concurrent sessions; the certified 44/44 + 4/4 files were later replaced by newer runs (142/142 + 5/6).

## 9. Verdict

**Gate A is NOT claimed passed.** Per exit condition:

- Green input/movement/camera regressions — **met at the certified revision** (44/44, 4/4, red/green proven); **not currently green on the shared tree** (owner's new Milestone B test failing, §7.5).
- Reproducible ARM64 IL2CPP dev APK — **met** (hash §5, build command §6).
- Recorded real-phone move+aim+fire, jump, UI, camera, collision, resume — **largely met** (video + machine-readable telemetry + screenshots); human pad place/launch and blocked-shot scenarios **open**.
- No magenta — **verified** (0.000 %). No dead controls — verified for every control exercised (pad placement rejections were rule-based, shown on HUD). No camera-through-wall — spot-checked only (sphere-cast code + two camera drags).

Next owner actions, in order: (1) finish the shared-tree PlayMode fix with the parallel owner and re-freeze the artifact; (2) capture human pad place/launch + blocked-shot on device; (3) run the sustained/thermal device matrix.
