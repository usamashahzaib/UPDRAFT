# Package 1 build manifest — 2026-10-01

Status: source/build verified. Real-device evidence is pending reconnection of the Samsung SM-A065F; no device certification or gate claim is made.

## Source mapping

- Repository: `E:\Usama\Game\ProjectUpdraft`
- Git HEAD: `5fa1dab54341c77add76b88f3631b830fccdf3b4`
- Tracked source patch object (`Assets`, `Packages`, `ProjectSettings`): `cfa7c2b92da9bc78c77ff562bb221036880efe9f`
- Source scope: 1,039 files under `Assets`, `Packages`, and `ProjectSettings`, including untracked source and metadata files.
- Deterministic source-tree SHA-256: `543ae9b4ab14f502297c8c669f8a228c37deded830d66c7b618f17b26df89195`
- Unity: `6000.3.25f1`

## APK

- Path: `ProjectUpdraft/Builds/Android/UpdraftDev.apk`
- APK SHA-256: `81fe5b90ff395c6789738a505525a607f7a266fd637b67d11e635647aa6a103d`
- File size: 87,929,323 bytes
- Build log: `ProjectUpdraft/TestResults/Package1_AndroidBuild_Final.log`
- Result: succeeded, 0 errors, 8 warnings, 1,166,614,733 reported build bytes, 00:02:16.0576631.

## Regression

- `ProjectUpdraft/TestResults/EditMode.xml`: 200 passed, 0 failed.
- `ProjectUpdraft/TestResults/PlayMode.xml`: 8 passed, 0 failed.

The final build contains the first-run touch-control suppression and the Home runner-preview layout correction. The final APK has not been installed on the phone because the device disappeared from USB enumeration after Unity shut down ADB. A screenshot and recording from the preceding APK are not current-build verification.

## Remaining evidence

Install this APK on the Samsung SM-A065F after it reconnects, reset local app data, and record first run through Home, two complete offline matches with Results/rematch/Home, settings persistence after restart, and performance observations. Treat measured results as observations only; they do not certify a gate or retention.
