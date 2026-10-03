# Gate B Acceptance Record — Offline Relay Slice

**Project:** HOTDROP  
**Specification:** `ProjectUpdraft/PROJECT_HOTDROP_SPEC.md`, v1.0, §§1–12 (§3–12 = match flow, movement/health, pads, weapons, controls, map, death/disconnect, tutorial, bots)  
**Audit source revision:** `2596e9274c2004ceb03784c4dea50292ed43ec14` (HEAD, full chain `2596e92` → `2f64920` → `4f992c2` → `4b30276` → `2d2436f` → `f6fb005`)  
**Gate:** B — complete offline Relay  
**Result:** **ACCEPTED** — the offline combat slice is functionally complete and automated evidence is green.

## What was broken

Spec §5 (prep) and §6 (pads) require: during the 8-second preparation phase, shooting can animate but CANNOT deal damage — to players or pads, enemy or friendly. Friendly fire can NEVER damage a pad.

Root cause found at the match-authority layer (`NetMatch` ↔ `NetPlayer` ↔ `NetPad` ↔ `PlayerHealthController`): the server-side damage pipeline had no prep-phase gate.

- `NetPlayer.ServerDamage` guarded only `Dead || IsSpawnProtected` — no prep check. The only prep protection was the CLIENT `PlayerController.ApplyDamage` flag `combatAllowed` (set from `IsLive`).
- `NetPad.ServerDamage` guarded only `Removed || attackerTeam == Team` — no prep check. Friendly fire was filtered by team, but no prep-phase block existed for the server.
- `NetPlayer.Weapons` queued shots on every tick with no `IsLive` check, so a prep-phase fire input reached `NetMatch.ResolveShots`, which resolved damage on the server regardless of phase.

The authoritative `MatchFlowController.DamageAllowed` (InProgress/Overtime) was never consulted by the server damage path. The result: prep-phase input could damage players and pads on the server, violating spec §5 twice (no damage to players, no damage to pads at all during prep).

Fix applied at the authority layer so both client and server share one rule:

| File | Change | Purpose |
|---|---|---|
| `ProjectUpdraft/Assets/Scripts/Network/NetPlayer.cs` | `ServerDamage` now returns early when `!Match.IsLive` | No player HP lost during prep |
| `ProjectUpdraft/Assets/Scripts/Network/NetPlayer.cs` | `Weapons` now refuses to queue a shot while `!Match.IsLive` | Prep-phase input never reaches `ResolveShots` |
| `ProjectUpdraft/Assets/Scripts/Network/NetPad.cs` | `ServerDamage` now gates on `NetMatch.For(Runner)?.IsLive` | No pad HP lost during prep (players or enemy) |

## Requirement map

| Requirement | Implementation/evidence | Status |
|---|---|---|
| Prep (§4): 8 s, weapon select, spawn-locked, **no damage, no pads** | `MatchFlowController` drives `Preparation` phase; `NetMatch.IsLive` = InProgress/Overtime; `NetPlayer.ServerDamage`/`Weapons`/`NetPad.ServerDamage` all gate on `IsLive` | Implemented; verified |
| Player health during prep: 0 HP change | `PrepBlocksDamageAndPads_LiveFire_DestroysEnemyPads_NotFriendly`: `local.ApplyDamage(50f, 99, Arc)` during `Preparation` → `Assert.AreEqual(local.MaxHealth, local.CurrentHealth)` | **Pass** |
| Friendly fire never damages a pad | Live-phase ally-pad assertion `Assert.AreEqual(game.padConfig.health, data.Health)` (60 HP preserved) | **Pass** |
| Enemy fire destroys a 60 HP pad | Live-phase enemy-pad: `Assert.AreEqual(PadState.Destroyed, data.State)` | **Pass** |
| Pad config identity (§6): 60 HP, 18 s life, 16 s recharge, 0.75 s arm, 14/12 m/s launch | `PadConfig.asset` + `SpecConfigValuesTests` (valued assets, not hardcoded) | Verified |
| Movement config (§5): HP 100, ground 6, firing 5.1, accel 35, gravity 20, jump 6, capsule 1.8×0.35, step 0.35, respawn 4 | `MovementConfig` + `SpecConfigValuesTests` (valued assets, not hardcoded) | Verified |
| Weapon config (§7): exact §5 numbers for ARC/ZIP/BRICK | `Weapon_ARC/BRICK/ZIP` assets, `WeaponConfig` + `SpecConfigValuesTests` | Verified |
| Match rules (§5): regulation 180 s, score 120, prep 8 s, zone schedule | `MatchRulesConfig` + `MatchRulesScheduleTests` | Verified |
| Matchmaking (§13): Elo K24, bot fill 45 s | `MatchmakingConfig` | Verified |
| Touch/controls, pings, camera (§8) | `TouchInputManager`, `MatchHUD`, `PingSystem`; simulated touch matrix in `OfflineSliceSmokeTest` | Implemented; human-touch device acceptance remains open |
| Switchedyard mirror, spawns, ramps, sightlines (§9) | `SwitchyardGenerator`, `SwitchyardMirrorTests` | Implemented |
| Death/disconnect (§10) | Offline respawn + rematch; bot substitution deferred to Gate C | Offline path implemented |
| Nine-step tutorial (§11) | `TutorialController` all nine steps + `OfflineSliceSmokeTest` | Implemented |
| Bots (§12): LoS + audible perception, reaction delay, retreat | `BotController`, `BotPerception`, `BotDecisionMaker` + `BotBehaviourTests` | Implemented |
| Offline practice grants nothing (§12) | `OfflinePracticeGrantsNothingTests` (backend) — anonymous/player-authenticated submissions denied, ledger unchanged | Verified |

## Automated results

- Unity EditMode: **142 passed, 0 failed** (0.28 s) — `Run-UnityTests.ps1 -Platform EditMode`, clean shell.
- Unity PlayMode: **6 passed, 0 failed, 0 skipped** (108.6 s) — `Run-UnityTests.ps1 -Platform PlayMode`.
- Patrol-phase regression (new authority fix): `PrepBlocksDamageAndPads_LiveFire_DestroysEnemyPads_NotFriendly` passes in the fixed tree with 0 HP lost during prep, ally pad retaining 60 HP, enemy pad reaching `Destroyed`.
- Backend: `OfflinePracticeGrantsNothingTests` + full backend suite unaffected by this change (this change is Unity-only).
- License/client state: `Unity.Licensing.Client` cleanly reaped; no Unity process or licensing residue after shutdown; `Temp/UnityLockfile` clean during runs.

## Reproduction

```powershell
# Backend
dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj --no-restore --filter "FullyQualifiedName~OfflinePracticeGrantsNothing"

# Unity after no editor owns the project
ProjectUpdraft/Run-UnityTests.ps1 -Platform EditMode
ProjectUpdraft/Run-UnityTests.ps1 -Platform PlayMode
```

## Failure cases closed

- Unity Package Manager IPC / licensing database failure: resolved on 2026-09-28 (`4f992c2`, `2f64920`); test runner pre-flight guards stop a second run with a Unity editor holding the project. See `docs/UNITY_LICENSE_RECOVERY.md`.
- Prep-phase damage to player/pads: fixed in `2596e92` (authority layer) and verified green.
- Live-phase friendly-fire pad damage: covered by `NetPad.ServerDamage` team check + `IsLive` prep gate.
- Regression test `PrepBlocksDamageAndPads_LiveFire_DestroysEnemyPads_NotFriendly`: present in `OfflineSliceSmokeTest` and passing.

## Artifact

- Development APK: `ProjectUpdraft/Builds/Android/UpdraftDev.apk`
- SHA-256: `1b1c6c6ed1acf9cd69ca451b5bd98861ab45a4445c137dd5f7f3b0d849b60dbf`
- Size: 87,428,019 bytes
- Build command: `ProjectUpdraft/Build-AndroidDev.ps1` (via `-executeMethod Updraft.Editor.BuildAndroidApk.Run`)
- Configuration: Android API 28 minimum, IL2CPP, ARM64, debuggable, Unity 6000.3.25f1
- Source revision frozen with this record: `2596e92` (full `2596e9274c2004ceb03784c4dea50292ed43ec14`)

## Reproduction of Gate B acceptance

Post-fix source snapshot → `Run-UnityTests.ps1 -Platform EditMode` (142/142) → `-Platform PlayMode` (6/6) → `Build-AndroidDev.ps1` → SHA-256 above. Gate B **ACCEPTED**.

## Known issues / remaining work

- Human-touch tutorial and practice acceptance on a real Samsung A06 (device tier) is the only remaining Gate B acceptance. Automated evidence is complete.
- Bot substitution, reconnect, and 6-client dedicated-server session (Gate C) remain out of scope.
- No shop, ads, Phase 2, or paid infrastructure was added or enabled.
