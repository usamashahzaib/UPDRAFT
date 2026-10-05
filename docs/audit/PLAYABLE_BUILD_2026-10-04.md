# HOTDROP playable Android build — 4 October 2026

## Delivered artifact

- APK: `ProjectHotdrop/Builds/Android/HotdropPlayable_2026-10-04.apk`
- Size: 88,350,817 bytes.
- Packaged: 2026-10-04 01:12:29 Asia/Karachi.
- SHA-256: `163405a76f1749587bdf37908bdcb3857047abc5085507b5852323ad4c32e344`.
- Package: `com.techspirex.hotdrop`, version 1.0 (1).
- Android minimum API 28, target API 36; ARM64 (`arm64-v8a`).
- Development/debug APK; apksigner verified APK Signature Scheme v2, one signer.

The build contains the implemented playable presentation pass described in `PLAYABLE_PASS_2026-10-03.md`, along with workspace changes available when Unity compiled the player. The prior verified APK is preserved as `HotdropPlayable_2026-10-03.apk`.

## Build evidence

`Build-AndroidDev.ps1` completed with exit code 0. Unity encountered the identified Java loopback packaging failure after native compilation; the script's direct Gradle recovery completed `assembleDebug` successfully in 35 seconds (67 tasks, 14 executed). The freshly generated APK was copied to the dated artifact and independently verified using apksigner and aapt.

`ProjectHotdrop/TestResults/PlayableAndroid_2026-10-04.log` preserves the Unity portion, including its packaging exception. It does not contain the successful direct Gradle recovery output, which was returned by the build process in this chat. The Unity exception alone is not the final wrapper result.

## Source boundary

`PLAYABLE_BUILD_2026-10-04.source-sha256.txt` records 444 selected script, resource, package and project-settings files during the build; it is not a hermetic repository snapshot. Post-build comparison found two changed files: `Assets/Scripts/UI/HudMarkerLayer.cs` and `Assets/Scripts/UI/MatchHUD.cs`. Their last writes were 01:10:39 and 01:12:18, after `Library/Bee/PlayerScriptAssemblies/Hotdrop.Runtime.dll` was compiled at 01:04:07. These subsequent HUD edits are not included in this APK. Other work in the shared checkout was preserved.

## Validation boundary

- Presentation pass: 296 EditMode tests passed (`PlayableEditMode.xml`).
- Full PlayMode suite after legacy-body/overlay fixes: 18 passed (`PlayableFinalPlayMode.xml`).
- Subsequent final presentation checks: 4 focused PlayMode tests passed (`PlayableHudFinal.xml`), including Home → six-player Relay → Home.
- Latest existing EditMode report inspected on 4 October: 356 passed, zero failed, ended 2026-10-03 19:53:25 UTC. This report was inspected, not rerun as part of packaging.
- APK packaging, signature and manifest checks passed. No attached Android device was available; installation, touch comfort, thermal performance and six-client online acceptance were not tested by this build pass.

Automated gameplay evidence predates this packaging step. This artifact is a development build, not a claim of 100% device or production acceptance.
