# QA continuation checkpoint — 2026-09-29

Resume the QA remediation from this point. The request is to continue through the remaining work in `QA_REPORT.md` in order and not claim acceptance without evidence.

## Current state

- Source-level work has been made for QA-029 through QA-035. QA-034's bot-brain cleanup already existed; this pass also adds the bot label and restores a reconnecting player's alias.
- QA-029 now cancels matched tickets before practice. The backend refuses cancellation after an active server allocation is claimed, and the client stays in the match flow instead of entering practice.
- QA-030–033 now have gyro neutral-bias calibration/settings guidance, reload state/progress HUD, attack-direction death text, and bot substitution labeling.
- QA-035 hot paths now reuse pellet buffers, use a deterministic allocation-free PRNG, and avoid splitting control-label strings. Device allocation/performance acceptance is still outstanding.
- QA-001 now fails closed for release builds configured with `dev` or a non-HTTPS endpoint. A real publisher-owned HTTPS backend URL/deployment is still unavailable.

## Verification performed

- `dotnet build Backend/UpdraftBackend/UpdraftBackend.csproj --no-restore`: passed, 0 warnings/errors.
- Focused matchmaking tests after adding cancellation coverage: **12/12 passed**.
- Full backend test project: **162/162 passed**.
- `dotnet build ProjectUpdraft/Updraft.Runtime.csproj --no-restore`: passed, 0 errors (existing warnings remain).
- Unity EditMode ran on the current tree and reported **189/190 passed**. Result: `ProjectUpdraft/TestResults/QA_Remaining_EditMode.xml`; log: `ProjectUpdraft/TestResults/QA_Remaining_EditMode.log`.
- One EditMode failure: `Updraft.Tests.PadRulesTests.EveryRejectReason_RoundTripsThroughNetworkCode`. Its broad regex scans every uppercase `return` literal across `PlayerController.cs`, so the new attack-direction return value `FRONT` is mistaken for a pad rejection. Fix the test to scope extraction to pad-placement methods, then rerun EditMode.
- Unity PlayMode has **not** been rerun yet.
- The Android ADB device is Samsung SM-A065F (`R8VXC02QEVB`), but the installed app/APK is version 1.0 and has no manifest mapping it to this source snapshot. Do not use it as acceptance evidence for this tree.

## Next steps

1. Fix the pad rejection round-trip test's method scoping; rerun EditMode until all tests pass.
2. Run PlayMode and capture a fresh result for this working tree.
3. Continue with the feasible device procedures from U02 onward only against a build mapped to this tree. Do not treat the existing APK as current evidence.
4. QA-001 still needs the real HTTPS backend URL and deployed service. U04/U11 and the hands-on U02–U20 procedures require infrastructure, multiple clients/devices, or recruited participants; keep each result explicitly blocked until its required setup is available.
5. Reassess QA-029–035 after successful tests; source changes alone do not close runtime/performance acceptance.

## Update — 2026-09-30

- Step 1 done: `PadRulesTests.EveryRejectReason_RoundTripsThroughNetworkCode` now scans all of `PadPlacementRules.cs` plus only the bodies of `NetPlayer.EvaluatePadPlacement` and `PlayerController.ResolvePadPlacement` (brace-matched `MethodBody` helper), so unrelated labels such as the death-direction `FRONT` no longer count as pad reasons.
- Unity EditMode on the current tree: **190/190 passed**. Result: `ProjectUpdraft/TestResults/QA_Remaining_EditMode2.xml` (log alongside).
- Step 2 done: Unity PlayMode on the current tree: **8/8 passed** (127 s). Result: `ProjectUpdraft/TestResults/QA_Remaining_PlayMode.xml` (log alongside).
- Step 3 build prepared: `Builds/Android/UpdraftDev.apk` (Development) built from this tree. Source hashes were identical before and after the build. APK SHA-256 and the per-file source hashes are in `docs/audit/DEVICE_BUILD_2026-09-30.source-manifest.txt`. Installed with `adb install -r` on SM-A065F (`R8VXC02QEVB`) at 13:34:26, replacing the unmapped v1.0 install; app data was kept. Launch smoke check: the process started, there was no crash in logcat, and the Home screen rendered. This is not a U-procedure result.
- Device evidence for this tree only counts if the installed APK hash matches the manifest. The hands-on U02–U20 procedures still need a human operator. U02 (first run) needs `adb shell pm clear com.DefaultCompany.ProjectUpdraft` first, because data was kept and the app opens straight to Home. The dev config's backend is `localhost:5000`, which is unreachable from the phone. Online flows need the backend running locally plus `adb reverse tcp:5000 tcp:5000`, or the real HTTPS deployment (QA-001). Steps 4–5 are unchanged.

No Unity tests or device interactions were in progress when this checkpoint was written. The working tree already contained many unrelated/uncommitted changes before this session; inspect `git status` before staging or reverting anything.

## Update — 2026-09-30 (independent review pass)

Re-reviewed the QA-029–035 fixes and hunted for defects the audit did not list. New defects found and fixed in source:

- **AFK lockout (P1, not in audit).** After AFK substitution a still-connected player kept an open session with dead controls, watching a bot play as them until results, with no message. Spec 18 says "removal". Now: `NetPlayer.AfkRemoved` (networked) is set by `NetMatch.HandleAbsence`; the client shows "REMOVED FOR INACTIVITY" (Home / Practice) and leaves cleanly (no reconnect loop); `NetSession.OnPlayerJoined` refuses the seat for the rest of the match.
- **Silent-aim exploit (P1 security, not in audit).** The server aimed at the raw client-supplied `NetInput.AimPoint`, so a modified client could face one way and shoot another. The server now keeps only the client's aim *distance* (third-person parallax) and takes direction from the replicated Yaw/Pitch. Honest clients derive Yaw/Pitch from the same ray, so their shots are unchanged.
- **Account-id broadcast (privacy).** `NetPlayer.AccountId` was `[Networked]`, so every client received every opponent's internal account GUID. It is now a server-only field. All uses were already server-side.
- **Quitters/AFK players unreportable.** The results roster skipped every `BotControlled` seat, including substituted humans. Substituted humans are now listed under their real alias.
- **Online panel NRE.** `OnlinePlay.Show(..., null, null)` left a live button wired to a null action. It is now hidden. `PracticeInstead` is guarded against a double tap while the queue release is in flight.
- **Deletion PII retention.** Completed web deletion requests kept the requester's plaintext name/email forever. They are now scrubbed on completion, and unverified requests are purged 7 days after link expiry (retention job).
- **Release pipeline.** Added `Updraft.Editor.BuildAndroidRelease` (signed AAB, pinned target API 36). It refuses placeholder package ids and missing keystore/version env vars.

Verification: backend **163/163** (new retention test), Unity EditMode **194/194** (new `NetAuthorityTests`), Unity PlayMode **8/8** (126 s, full Unity compile incl. the new release build script, 0 errors). Still source-level only for network behaviour: no six-client run was possible.

Still blocked on the publisher (not code): permanent package name, upload keystore, HTTPS backend + hosting (QA-001), Photon production app id, store listing/Data safety/content rating, closed test (12 testers × 14 days if the account is personal and post-Nov-2023), and all hands-on U02–U20 procedures.
