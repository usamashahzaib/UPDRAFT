# Gate B Acceptance Record — Offline Relay Slice

**Project:** UPDRAFT  
**Specification:** `ProjectUpdraft/PROJECT_UPDRAFT_SPEC.md`, v1.0, §§3–12  
**Gate:** B — complete offline Relay  
**Audit date:** 2026-09-28  
**Result:** **NOT ACCEPTED.** Source implementation and backend regression checks are present, but the current Unity test set did not run in this environment and real-touch acceptance is still open.

## Source snapshot

- Base Git revision: `f6fb00510b9260667a2b02e2929ffcb7b868a3e1`.
- Gate B implementation files and tests are currently uncommitted in the shared worktree. This record describes that worktree, not the base revision alone; no source patch or signed release artifact was frozen.
- The same worktree contains separate Gate C edits. They are outside this Gate B record and remain unaccepted under [`STEP_C.md`](STEP_C.md).
- No APK was built from this exact worktree during this continuation, so no Gate B binary hash is available. Existing Samsung A06 smoke evidence in `docs/PROGRESS_2026-09-28.md` is from an earlier build and is not attributed to this source snapshot.

## Requirement map

| Requirement | Implementation/evidence | Status |
|---|---|---|
| Relay phases, scoring schedule, contest/empty behavior, overtime, draw, phase permissions (§§3–4) | `RelayScoringController`, `MatchController`, and `RelayMatchEdgeCaseTests` cover the schedule and edge cases. The existing PlayMode smoke test records an accelerated six-player match through Results and rematch. The new EditMode tests were not run in this continuation. | Implemented; current Unity verification blocked |
| Movement, health, respawn and protection (§5) | Authoritative values live in `MovementConfig`; `SpecConfigValuesTests` checks the shipped asset. Earlier EditMode evidence exists, but this continuation could not execute the expanded suite. | Implemented; current Unity verification blocked |
| Pad placement, lifecycle, shared use, lockout and non-stacking (§6) | `PadPlacementRules`, `LaunchPadBehaviour`, `PlayerPadManager`, and `PadRulesTests`; PlayMode covers using an enemy pad. Human control and HUD behavior still need Android acceptance. | Implemented; current Unity verification blocked; device check open |
| Weapon damage, falloff, cadence, spread, recoil and reload (§7) | Shipped weapon assets are checked by `SpecConfigValuesTests`; cadence/reload/damage edge coverage is in `WeaponCombatTests`. | Implemented; current Unity verification blocked |
| Touch controls, camera, pings and control editing (§8) | Touch ownership and action feedback are implemented. A short Samsung A06 smoke run is recorded in the progress report; complete human touch acceptance and remaining HUD/pad behavior are not recorded. | Partial; device acceptance open |
| Mirrored Switchyard, spawn exits, ramps and sightlines (§9) | `SwitchyardGenerator`, `MapConfig`, and `SwitchyardMirrorTests` cover generated symmetry, exits, ramps and spawn sightlines. The expanded test was not run here. | Implemented; current Unity verification blocked |
| Death and disconnect behavior (§10) | Offline respawn and match lifecycle are implemented; `OfflineSliceSmokeTest` exercises the offline match loop. Network disconnect substitution belongs to Gate C. | Offline path implemented; current Unity verification blocked |
| Nine-step tutorial and replay/skip behavior (§11) | `TutorialController` and `OfflineSliceSmokeTest` include all nine steps, skip, Home and replay-related flow. Simulated editor interaction does not prove unaided real-touch completion. | Editor coverage recorded previously; human tutorial acceptance open |
| Bot objectives, reaction delay, perception/LOS, retreat (§12) | `BotDecisionMaker`, `BotPerception`, `BotController`, config asset, and `BotBehaviourTests` implement and cover these behaviors. The expanded EditMode tests were not run here. | Implemented; current Unity verification blocked |
| Offline practice grants no online rating, XP or credits | Backend result route requires the game-server service claim. `OfflinePracticeGrantsNothingTests` verifies a player-authenticated submission is forbidden, an anonymous submission is unauthorized, and account progression/ledger values remain unchanged. Filtered test passed 1/1; full backend suite passed 77/77. | Verified by backend tests |
| Repeated match/Home cleanup and no accumulated match state | `OfflineSliceSmokeTest` contains repeated match/rematch cleanup assertions. It was not executed in this continuation. | Implemented; current Unity verification blocked |

## Automated results

- Filtered backend test: `dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj --no-restore --filter "FullyQualifiedName~OfflinePracticeGrantsNothing"` — **1 passed, 0 failed**.
- Full backend suite: `dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj --no-restore` — **77 passed, 0 failed, 0 skipped**, .NET 10.0, 2026-09-28. Both commands emitted NU1903 for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11.
- Unity EditMode: **not run**. Initial launches exited before test discovery because Unity could not connect to its local Package Manager IPC stream (`Upm-12592`); the local UPM log also records `ECONNRESET` reaching `packages.unity.com`, and Unity reported a read-only licensing database. The elevated retry connected to the licensing client but exited with code 1 before producing test results. No Unity XML result file was produced.
- Unity PlayMode: not run; the Editor test infrastructure must be repaired before treating the expanded EditMode/PlayMode suite as verified.
- `git diff --check`: passed for the shared worktree at this record's creation.
- Android build and real-touch acceptance: not performed against this exact source snapshot.

## Reproduction

Backend checks:

```powershell
dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj --no-restore
```

Unity checks after the local Package Manager/licensing IPC issue is resolved and no Editor owns the project:

```powershell
ProjectUpdraft/Run-UnityTests.ps1 -Platform EditMode
ProjectUpdraft/Run-UnityTests.ps1 -Platform PlayMode
```

Then build/install the Android artifact from the recorded source snapshot and complete the Gate B human-touch journey: first launch through all tutorial steps without skipping, practice match through Results/rematch/Home, and direct checks of movement/camera, firing/reload, pad preview/place/launch/cooldown, pause and HUD. Record device/build identity, duration, frame rate, memory, thermal state, startup time and defects.

## Failure cases and open acceptance work

- Fix or diagnose the Unity Package Manager IPC and licensing database issue; rerun EditMode and PlayMode and retain the XML/log outputs.
- Resolve any Unity compilation or test failures found by those runs before shipping.
- Complete real-touch tutorial and practice acceptance on the named Android device tiers. Prior brief A06 smoke evidence does not establish sustained performance or full Gate B acceptance.
- Build a fresh APK from this exact source snapshot and record its SHA-256 and balance/config identity.
- Gate B remains **not accepted** until automated evidence and the real-device journey above are captured together.

## Artifact and balance identity

- Gate B APK SHA-256: **not available; no APK was built from this worktree during this continuation**.
- Base source revision: `f6fb00510b9260667a2b02e2929ffcb7b868a3e1`; working-tree changes are uncommitted, so this hash alone does not identify the implementation under review.
- No third-party assets or paid services were added or enabled by this continuation. The worktree's existing Unity/package warning is recorded above.
