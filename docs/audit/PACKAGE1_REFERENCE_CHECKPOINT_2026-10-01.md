# Package 1 reference checkpoint — 2026-10-01

Status: **source and build checkpoint; current-device evidence is pending Samsung reconnection.** This does not certify any gate, device target, online flow, retention result, or production-art readiness.

## Exact current source baseline

Captured after the Package 1 source updates and before further art work:

- Git HEAD: `5fa1dab54341c77add76b88f3631b830fccdf3b4`
- Git working-tree tracked patch object: `cfa7c2b92da9bc78c77ff562bb221036880efe9f`
- Source scope: 1,039 files under `ProjectUpdraft/Assets`, `Packages`, and `ProjectSettings`, including untracked source/meta files.
- Deterministic source-tree SHA-256: `543ae9b4ab14f502297c8c669f8a228c37deded830d66c7b618f17b26df89195`
- Unity editor: `6000.3.25f1`.

The working tree was deliberately retained. In particular, this baseline includes the existing body-rig, prefab, scene, HUD, and evidence work.

## Package 1 source changes reviewed

- `OfflineHomeScreen`, `GameBootstrap`, and `HomeCharacterPreview`: one prominent public Relay action, a separate always-available offline action, a first-run skippable runner choice, actual shared-rig preview, and one local-only training-medal goal.
- `OfflinePauseMenu`, `TouchInputManager`, `ThirdPersonCamera`, and `GameBootstrap`: two-thumb control editing, persisted control/audio/comfort/graphics settings, labeled slider values, real low/smooth frame-target and runtime render-scale changes, truthful gyro availability, shake/vibration/larger-HUD settings.
- `BodyRig`, `AvatarRigVisual`, `SportAvatarAnimation`, prefab generation, and parity tests: two cosmetic-only body appearances share the capsule, hitbox profile, and animation drives. Automatic torso leaning was removed; idle and fired-shot presentation remain.
- `SwitchyardGenerator` and `SwitchyardMirrorTests`: a single original, collider-free Center communications-tower presentation sample. It cannot change cover, line of sight, paths, hit surfaces, zone occupation, scoring zones, or balance. West/East remain graybox pending review.
- `CosmeticLookTests`: weapon-wrap regression now follows the actual weapon node under the shared avatar rig.

All visual geometry in this package is project-authored Unity primitive/material construction. No third-party asset was added; `ASSET_LICENSE_REGISTER.md` continues to identify production mesh/texture work as outstanding.

## Build and test evidence

Current source-mapped APK:

- APK: `ProjectUpdraft/Builds/Android/UpdraftDev.apk`
- APK SHA-256: `81fe5b90ff395c6789738a505525a607f7a266fd637b67d11e635647aa6a103d`
- Build log: `ProjectUpdraft/TestResults/Package1_AndroidBuild_Final.log` reports `Succeeded`, 0 errors, 8 warnings, size 1,166,614,733 bytes, duration 00:02:16.0576631.
- This APK was built from the exact source-tree hash and tracked-patch object above.

Fresh regression evidence from that source baseline:

- `ProjectUpdraft/TestResults/EditMode.xml`: **200/200 passed**, 0 failed, 0 skipped.
- `ProjectUpdraft/TestResults/PlayMode.xml`: **8/8 passed**, 0 failed, 0 skipped.
- The stale `CosmeticLookTests` weapon lookup was updated for `Mannequin/Rig/Gun`. First-run PlayMode now selects the visible runner choice and verifies tutorial, match, Results, rematch, Home, touch and pad flows.
- The test runner's Unity-owner check now distinguishes the editor executable from Unity Hub's helper process, preserving the one-editor rule.
- A phone screenshot from the preceding APK exposed the Home runner portrait crossing the label/action. The portrait was reduced to 248 design units with height-controlled aspect fit; the final APK above contains this repair. This corrected screen still needs a fresh phone capture.

## Device evidence and remaining work

The Samsung SM-A065F (`R8VXC02QEVB`, Android 14) was previously connected. On the immediately preceding APK (SHA-256 `9b4af67df2908fa90af37f0f31b346f8f7a11f1575eae1c65ea523777bcc46b5`), the cleared first-run screen showed the runner chooser without combat controls; the tutorial skip returned to Home. That Home screenshot showed the portrait overlap now fixed in the final source. Unity's build/test shutdown of ADB was followed by the Samsung disappearing from ADB; both `adb devices -l` and Windows present-device enumeration currently return no phone. The final APK has not yet been installed or represented as phone-verified.

Two complete offline matches were recorded from that identified older build: `TestResults/Package1_prebaseline_match1.mp4` (`c60f7614d288351bb6b207abfb3d9c627ab40c7a62fe6fdb1076cfa3936bb4dd`) and `TestResults/Package1_prebaseline_match2.mp4` (`cdac6f92fa3a7d7fcae468cec65623844458b706f2c42b10d002b436e4e59969`). `Package1_match1_result.png` and `Package1_match2_result.png` show Results, with the second match entered through `PLAY AGAIN`. The device log recorded 30-second windows around 30.0 FPS and 101–102 MB Unity-allocated memory. These observations are limited to that old development APK; the touch diagnostic is visible and the pre-baseline Home does not meet the Package 1 reference hierarchy.

Remaining delivery evidence:

1. Reconnect the reference phone, install the current APK above, clear app data, capture the repaired Home and record the actual fresh first-run journey and two complete offline matches, including Results, rematch and Home.
2. Capture current-build settings persistence across restart and performance observations. Previous 30 FPS and 101–102 MB readings came from the older development APK and do not apply to this source.
3. Review the Center landmark, grounded humanoid, Home hierarchy, combat readability, and two-thumb layout before producing any additional art. Do not treat recordings as evidence of online matchmaking, retention, or a gate pass.
