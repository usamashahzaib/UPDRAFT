# Installed offline travel implementation — 2026-10-03

Status: implementation and editor validation follow-up, **not 100% product completion, Android acceptance or production readiness**. The owner will reconnect a test phone later; no device is currently attached. This pass closes the concrete offline view/world gap identified in the October 2 UIUX audit. It does not attempt all five amendment packages or change gate decisions.

## Implemented route

Home → Play Offline → installed Library → Detail → real gameplay → local Pause/Resume → LOCAL RECORD → Retry / Next / Library. The Library also preserves the standard three-minute 3v3 offline Relay option. No account, ad, internet, streamed asset or online reward is required by the challenges. Startup's direct offline-practice exit remains available.

The concrete `OfflineChallengeScreen` binds the existing presenter/controller to menus and an objective tracker. `OfflineChallengeRuntime` owns actual PlayerController, TutorialTarget, LaunchPad, BotController and MatchController objects. Practice re-registers its shared match presentation after leaving a drill; tutorial/practice transitions clean up the previous local journey. The normal Relay authority, scoring schedule, weapons, capsule, launch velocity and competitive map geometry are unchanged.

| Challenge | Actual completion condition |
|---|---|
| Basic Route | Grounded arrival at the three authored checkpoints in order. |
| Own-Pad Route | Real owner-pad activation followed by a qualifying touchdown on the marked deck. |
| Enemy-Pad Route | Real opponent-pad activation followed by a qualifying touchdown. |
| Safe Landing | Actual pad-flight touchdown inside the authored near-deck target (2 m radius), not a later walk/slide into the target. |
| Stationary Targets | Real weapon damage destroys four installed range targets. |
| Moving Targets | Three installed targets move on authored patrols and are destroyed by actual weapon damage. |
| Reload Discipline | Three real target eliminations plus a completed real magazine reload. |
| Launch and Hit | Two targets destroyed during an actual pad flight; grounded fire or an unrelated later jump cannot advance the drill. |
| Capture One Zone | Fifteen seconds of valid uncontested Center occupation; alive, unprotected, inside radius and below the existing feet-height limit. |
| Rotate Between Zones | Valid occupation in the scheduled Center/West/East windows; inactive occupation does not count. |
| Defend Against Bots | Real local eliminations of the two distinct authored easy bots while the player is inside the active zone. |
| Complete Offline Relay | The existing real match-end event, including preparation/countdown/regulation/possible overtime; no premature drill timeout at 180 seconds. |

All routes and targets are authored initial configuration, not a claim of human difficulty tuning. Completion earns Bronze. Silver/Gold remain disabled (`thresholdsValidated = false`); no medal time/accuracy thresholds were invented. Config/challenge version is 2. Defend Against Bots records its actual Easy difficulty; ordinary Relay records Standard. Mid-attempt control changes are recorded as Mixed controls instead of a falsely comparable Classic best.

## Persistence and interruption

- Device save schema v2 records challenge ID/version, control preset, difficulty, completion, medal and successful personal bests. Failed attempts cannot create/improve a best or grant a medal. Multiple contexts do not overwrite each other's records.
- Full-field binary canonical FNV checksum detects accidental corruption; it is **not** anti-cheat or server verification. Nothing in this local record format is an online grant/receipt.
- Temporary write is flushed before `File.Replace` with a previous-commit backup (or first-write `File.Move`). The live file is not deleted first. A failed replacement reports NOT SAVED and preserves the committed file. A valid backup can recover a corrupt primary.
- Legacy v1 records migrate on the next write. Legacy interrupted state is discarded because its old checksum did not protect the world/timing fields. A future schema is not loaded or overwritten. Corrupt/unavailable data produces an explanation, not fabricated medals.
- Same-process pause freezes/resumes the actual live world, including bots/Relay. A zero-time player update no longer clears grounded contact.
- After process loss, safe grounded movement/objective checkpoints may reconstruct position, ammo, elapsed time, progress/occupation context. Combat, airborne/pad, reload or bot-Relay worlds that cannot be reconstructed safely explicitly show clean RESTART, not a fake resumed match. Completed records are retained. This conservative recovery policy is intentional; exact serialized combat-world recovery is not claimed.
- Save failures and filesystem behavior must still be exercised on the Android device, including low storage/process kill. Editor recovery tests are not OS-process-loss acceptance.

## Files changed in this follow-up

Unity gameplay/config/save:

- `ProjectHotdrop/Assets/Scripts/Config/OfflineChallengeConfig.cs`
- `ProjectHotdrop/Assets/Scripts/Core/OfflineChallengeController.cs`
- `ProjectHotdrop/Assets/Scripts/Core/LocalChallengeSave.cs`
- `ProjectHotdrop/Assets/Scripts/Core/OfflineChallengeRuntime.cs` and `.meta` (new)
- `ProjectHotdrop/Assets/Scripts/Core/GameBootstrap.cs`
- `ProjectHotdrop/Assets/Scripts/Core/OfflineHomeScreen.cs`
- `ProjectHotdrop/Assets/Scripts/Core/OfflinePauseMenu.cs`
- `ProjectHotdrop/Assets/Scripts/Core/StartupConnectionScreen.cs`
- `ProjectHotdrop/Assets/Scripts/Player/PlayerController.cs`
- `ProjectHotdrop/Assets/Scripts/Tutorial/TutorialTarget.cs`
- `ProjectHotdrop/Assets/Scripts/Match/MatchController.cs`
- `ProjectHotdrop/Assets/Scripts/Match/MatchView.cs`

Presentation, regression and build:

- `ProjectHotdrop/Assets/Scripts/UI/OfflineChallengeScreen.cs` and `.meta` (new)
- `ProjectHotdrop/Assets/Scripts/UI/OfflineChallengeUIBinding.cs`
- `ProjectHotdrop/Assets/Scripts/UI/MatchHUD.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/OfflineChallengeControllerTests.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/OfflineChallengeSaveTests.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/PlayMode/OfflineChallengeRuntimeTests.cs` and `.meta` (new)
- `ProjectHotdrop/Assets/Scripts/Tests/PlayMode/OfflineSliceSmokeTest.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/PlayMode/UiPhysicalSizeTests.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/PlayMode/PlayerJourneyVisualEvidenceTests.cs`
- `ProjectHotdrop/Assets/InputSettings.asset` and `.meta` (new); `ProjectHotdrop/ProjectSettings/EditorBuildSettings.asset`
- `ProjectHotdrop/Build-AndroidDev.ps1` (wait for the editor, not its licensing child; reject stale APK output)
- `ProjectHotdrop/ASSET_LICENSE_REGISTER.md`, `ProjectHotdrop/MILESTONE_STATUS.md`, October 2 audit, this report and `docs/acceptance/OFFLINE_TRAVEL_DEVICE_CHECKLIST.md` (unrun device plan)
- Dated pre/post source manifests and Android artifact-verification evidence (new); the build automatically registered `Assets/InputSettings.asset` in `ProjectSettings/ProjectSettings.asset` and generated performance-package JSON/meta files, detailed below.

No Backend source, database migration, online ownership rule, billing implementation or production feature flag was changed. Local schema migration is not a PostgreSQL migration. No paid catalog/license record or bundle contents were produced. The asset register records the actual reused primitive prefabs and original code/UI geometry; they are **not** accepted final production art. `explore-codebase` guided source tracing; its graph service was unavailable, so direct source/test tracing was used.

## Verification and artifact mapping

- Unity 6000.3.25f1 final EditMode: **295/295 passed**, 0 failed/skipped, 3.4866489 s; ended 2026-10-03 01:22:10 PKT.
- Final full PlayMode: **17/17 passed**, 0 failed/skipped, 190.4310837 s; ended 2026-10-03 01:29:21 PKT. Includes all twelve drill completion paths and simulated reference-phone physical minimums.
- Fresh Backend build/test (`dotnet test --no-restore`): **167/167 passed**, 0 failed/skipped. No Backend source changed. NuGet vulnerability-feed lookup produced NU1900 network warnings; this was not a clean dependency-security audit.
- Durable XML/log/TRX copies: `ProjectHotdrop/TestResults/travel_2026-10-03_0129/`. Ordinary runner paths are overwritten by future runs; use these dated copies.
- The prior Input System exit assertion was **not reproduced** in the final full log after installing a persistent settings asset and restoring/disposal of test-only clones. No package code was changed or assertion hidden. Existing audio-source, external Unity-service/license/network diagnostic warnings remain; test pass counts do not mean every log line is clean.
- Scoped `git diff --check` passed (only CRLF-normalization warnings). The large pre-existing dirty/staged checkout was preserved, not reset or certified wholesale.
- Source Git HEAD: `4c70079a7e606a3cf5aa32962f6ab38a8d3790ec`, dirty checkout. Pre-build source scope has **1,101 files**, including untracked files, under Assets/Packages/ProjectSettings. Git porcelain at handoff contains 1,321 entries (not all authored by this pass): ` M` 8, `??` 47, `A ` 130, `AM` 11, `D ` 9, `M ` 16, `MM` 3, `R ` 1,062, `RM` 35. Untracked-directory entries are not individual-file counts. No staging/commit/reset was performed.
- Pre-build source-tree SHA-256: `7234bcda0f0047194e9ec4dbdef5f158965d02208b479f0aecf35b2f471ca682`. Exact sorted per-file hashes and algorithm: `docs/audit/DEVICE_BUILD_2026-10-03.source-manifest.txt`. This snapshot was captured at 01:26:16 PKT during the final PlayMode run and includes that runner's temporary scene; it is not asserted byte-identical to the post-build tree.
- Fresh Android artifact mapping and the exact source differences follow below. No older APK is claimed as the new artifact.

### Fresh Android development artifact

The repository script completed with exit 0 and produced **`ProjectHotdrop/Builds/Android/HotdropDev.apk`**, **60,965,271 bytes**, last written **2026-10-03 01:49:35 PKT**. SHA-256: **`6563cb81ba250dfcfa78b5161f95d0c91d5eeea8dfe6fa9b5bf4c413bd8fd6bd`**.

A byte-identical dated handoff copy is preserved at **`ProjectHotdrop/Builds/Android/HotdropDev_2026-10-03_0149.apk`** so the ordinary output path can be rebuilt later without losing this device-test candidate. The hash above applies to both files.

Actual `aapt` identifiers: package **`com.techspirex.hotdrop`**, label **HOTDROP**, versionName **1.0**, versionCode **1**, minimum SDK **28**, target/compile SDK **36**, ABI **arm64-v8a**, `application-debuggable`. This is an installable development APK, **not** a publisher-signed production release/AAB or proof of a registered Play identity.

Build qualification: native IL2CPP/Burst output completed, but the Unity packaging wrapper reported failure on the already identified **Java loopback connection** error (Unity summary: 2 errors, 8 warnings). The script's narrowly scoped existing recovery invoked Gradle `assembleDebug` on the freshly generated native/Gradle project: **BUILD SUCCESSFUL in 1m; 67 actionable tasks executed**. Do not label the Unity wrapper itself successful. Gradle/AGP deprecation and read-only SDK `package.xml` marshalling warnings remain. Full Unity log: `ProjectHotdrop/TestResults/travel_2026-10-03_0129/AndroidUnity.log`; recovery console summary plus artifact-tool output: `AndroidArtifactVerification.txt` in that same directory.

`apksigner verify --verbose --print-certs` exited 0, verifying APK v2 signing. Certificate DN is **`C=US, O=Android, CN=Android Debug`**, certificate SHA-256 `474b08cef54786df0d2371e2ef3366b455a3b62ed2af97f77eb634cb080f4c74`. This confirms debug integrity, **not publisher ownership/signing acceptance**. `zipalign -c -P 16 4` exited 0; this is a static ZIP-alignment check, not real-device/page-size/performance acceptance. The manifest includes the existing Unity IAP dependency's `com.android.vending.BILLING` permission; permission presence is **not configured Google Play Billing or purchase verification**.

Post-build snapshot at **01:50:24 PKT** contains **1,103 files**, SHA-256 **`e52d9ee9ab5303e6185ee6d7b7accd0a77d94c43dfd18f454ea76f354296e71b`**, recorded in `docs/audit/DEVICE_BUILD_2026-10-03.postbuild-source-manifest.txt`. Pre/post trees are not byte-identical:

- The completed test runner removed its temporary `Assets/InitTestScenec3317a95-b826-429a-b26f-23078d620dd5.unity` and `.meta`.
- The installed performance-test package generated `Assets/Resources/PerformanceTestRunInfo.json`, `PerformanceTestRunSettings.json` and both `.meta` files at build time. These contain blank/no-result metadata, **not measured device performance**.
- The build registered the already tested persistent `Assets/InputSettings.asset` as a preload in `ProjectSettings/ProjectSettings.asset`. Removing only that generated preload block **in memory** reproduces the exact pre-build settings hash `c669d2c3ae2732fd4bd3ee9549b6154c40020a142c19bd58bb3e4a81b0d7c17a`; no settings file was reverted.
- All **1,098 other files** retain their exact per-file hashes. No gameplay/test `.cs`, authored challenge config, prefab or package dependency changed between the final test snapshot and build. The preload's player behavior still requires the connected-device test; editor tests are not Android acceptance.

Coverage includes real Main-scene initialization for all twelve entries, actual raycasts/reload, own/opponent pad activation and physical touchdown, grounded-hit rejection, height/inactive-zone occupation rejection, rotation, real damage to defenders and the normal Relay-end event. Deterministic tests script movement/aim; some waypoint/bot condition tests reposition actors or disable bot AI, and local-match clocks are accelerated. They **do not prove human route comfort, authored bot difficulty, touchscreen aim, performance or a gate pass**.

Actual local editor renders live in `ProjectHotdrop/UIEvidence/challenges_2026-10-03/`: Library/empty progress, Detail, gameplay, Pause, Result, safe checkpoint Resume, explicit combat-world Restart and a scripted completed First Flight chapter, at 1920×1080 and 2400×1080. Shared journey captures are regenerated by the full suite. Camera captures do not capture the immediate-mode touch-control overlay; these are editor layout/game-state evidence, not Android screenshots or complete device HUD evidence.

## Commerce prerequisite check and unchanged flags

Repository evidence does **not** satisfy the original Shop prerequisites:

1. Gate D is in progress with external study/go-kill decision pending (`docs/acceptance/STEP_D.md`); neither D-core nor D-retention is accepted by this work.
2. No explicit owner authorization to implement/enable `CatalogConfig.shopEnabled` is recorded; the publisher-input record explicitly preserves the commerce lock.
3. Publisher-controlled Play Console app identity/signing evidence is missing from acceptance records; Android keystore/alias settings are blank. Publisher brand/email are not that evidence.
4. `BillingMode.json` only names GooglePlay. No configured product/credential/real store-purchase evidence is recorded. Current commerce verification still stores `pending_verification` without Google Developer API verification.
5. Approved exact bundle contents/provenance and real backend/Play verification, acknowledgment, pending recovery, reinstall restore, refund/revocation, duplicate/concurrent-request, offline/unavailable, legal-copy, localized-price and device evidence are missing.

`CatalogConfig.shopEnabled = 0`; `AdConfig.rewardedAdsEnabled = 0`; backend `Features:PurchasesEnabled = false`; contracts/mastery remain disabled by default. Shop remains absent from Home. **Purchase evidence from this pass: none.** No mocks, sandbox purchase or editor screenshot is presented as production acceptance.

## What still prevents 100% completion

- Real-device first run, all twelve challenges in airplane mode, pause/background/force-stop/relaunch, low-storage/corruption/atomic replacement, saved settings, left-handed/larger-text/cutouts, full match/rematch, sustained FPS/memory/thermal and actual screenshots. The promised phone has not yet been connected.
- Owner-approved representative art and the final produced/licensed runner skeleton/animations, weapons, pad, environment and all cosmetic-slot variants; current primitive art is not 100% design-reference/production parity.
- Approved six-client authenticated Oracle/Photon/server/backend staging runs, disconnect/reconnect/outage matrix and all online UI/cosmetic consistency evidence. Backend unit tests cannot stand in for that.
- Remaining corrected per-screen composition/context-state evidence and complete cosmetic-slot/earned reward asset/catalog/provenance work. Existing disabled contracts/mastery are not accepted as complete production journeys. Offline challenge analytics event/allowlist additions are not included in this gameplay-only pass.
- Publisher entity/address and reviewed legal drafts, public privacy/deletion/support URLs, transactional email delivery, Google Play/Play Games identity/signing/configuration, store declarations and release/operations evidence. Full street/city fields remain placeholders; nothing was fabricated.
- Gate D human gameplay/retention decisions and all commerce prerequisites above. Payments/ads/gated systems must stay off; no production enablement is authorized by automated tests.

## Device handoff

When the phone is attached, enable USB debugging and approve this computer. Verify the APK SHA-256/build mapping above before installation. Preserve existing game data first; uninstall/clear-data is destructive and needs a deliberately scoped fresh-run test. Do not silently erase publisher/user progress. Record package/build identity on every screenshot and performance run. Test online menu semantics separately: online simulation must never be time-scale paused.

`docs/acceptance/OFFLINE_TRAVEL_DEVICE_CHECKLIST.md` provides the all-twelve-drill/interruption/layout/performance test plan. Every row is NOT RUN; a checklist is not acceptance evidence.
