# HOTDROP — completion audit and 100% fix list

Date: 3 October 2026. Reviewer stance: end user / publisher-side QA with a "ship it complete or don't ship it" bias.
Scope: current checkout at branch `feature/prompt-07-offline-challenges`, HEAD `4c70079`, dirty tree.
Method: read authoritative spec (`ProjectHotdrop/PROJECT_UPDRAFT_SPEC.md`), amendment (`docs/HOTDROP_PRODUCT_AMENDMENT_2026-10-01.md`), implementation prompts (`docs/AI_IMPLEMENTATION_PROMPTS/`), all prior audit/acceptance records, and the Unity + backend source that implements each spec section. Reran the backend suite. Did **not** play a phone session or a live multiplayer match; no device attached.

## Remediation status (updated after the first fix pass)

Applied and verified — backend `189/189`, Unity EditMode `320/320`, Unity PlayMode `18/18` (204.3 s, rerun 3 Oct 2026):

| ID | Change |
|---|---|
| C01 | Authored reachable Silver/Gold thresholds for all 12 challenges (`ApplyAuthoredMedalThresholds`); medals are no longer Bronze-only. Regression test `AuthoredEntries_ExposeReachableSilverAndGold`. |
| C02 | `badge_first_flight` granted exactly once on the first eligible public completion, win or loss; configurable via `Progression:FirstMatchBadge`. Test `FirstEligibleMatch_GrantsFirstFlightBadgeExactlyOnce_EvenOnALoss`. |
| C03 | Four validated slots (`BodyStyleId`, `WeaponWrapId`, `BadgeId`, `BodyRigId`) + legacy adapter; category/ownership validation, rig free; EF migration `AddCosmeticSlots` with backfill; composite `body\|wrap\|badge\|rig` replicated to all clients; Unity applies all slots at once. Tests: `Equip_WritesEachSlotIndependently_AndRigNeedsNoOwnership`, `Get_Profile_BackfillsLegacySingleValueIntoBodyStyleSlot`, `Parse_Composite_KeepsEverySlotIndependent`, `Apply_Composite_WearsBodyAndWrapAtTheSameTime`. |
| C09 | Offline practice no longer silently weakens the player's own team's bots. |
| C10 | AFK warn/remove windows read from `ProgressionConfig` instead of literals; `NetConfigs.Progression` added. |
| C11 | Backend allowlist accepts `tutorial_show_me`, `afk_removed`, `offline_challenge_started`, `offline_challenge_completed` — these were being silently dropped. |
| C12 | Challenge attempts/completions are emitted and uploaded as client-owned UI facts; offline match facts stay device-local for `adb pull` (spec §24: server owns match facts). |
| C14 | `RelayScoringController.EndReason` (`ScoreLimit`/`TimeExpired`/`OvertimeHold`/`OvertimeDraw`) drives the results label instead of guessing from scores. |
| C16 | Bot fill now seats as many waiting humans as the 3v3 allows before the match server spawns bots, so a four-player queue becomes one 2v2-plus-bots match instead of four separate one-human-vs-five-bots matches. Parties stay whole and the rating window still applies. Tests: `FourWaitingGroups_PastBotFillWait_ShareOneMatch_NotFourSeparateOnes`, `BotFill_KeepsPartiesWhole_AndFillsTheRestOfTheLobby`, `BotFill_SeatsTheLongestWaitingGroups_AndRespectsTheRatingWindow`. |
| C17 | The amendment's session definition is now implemented rather than prose-only: `SessionRules` (30-minute idle ends a session, `same_day_return` = a new session ≥30 min after the prior one ended on the same UTC day, brief background switches are not returns, first session is never a return), rolled into a new `AnalyticsDailySessions` table (EF migration `AddDailySessionRollup`) with sessions / same-day returns / voluntary sessions reported separately. Only gameplay events count as activity, so startup and crash reports cannot inflate a session. Tests: 10 in `SessionRulesTests`, plus `SessionRollup_CountsSessionsAndSameDayReturns_AndIsIdempotent` and `SessionRollup_ExcludesNonGameplayEventsAndIncentivizedSessions`. |
| C15 | Aim assist is now verified rather than merely advertised. The cone/distance/line-of-sight test was duplicated inline in the offline and online aim paths; both now call one shared pure `AimAssistRules.Qualifies` and `SensitivityScale`, so the tested rule is the rule that runs. 13 tests pin spec §8: ≤3° cone, ≤25 m inclusive, 70% slowdown inside the region, no assist through cover, sensitivity only ever drops (never rises), and exactly two possible scales so no snap or magnetism path exists. |
| C19 | A human whose slot became a labeled `[BOT] ` bot (quit or AFK-removed) stays reportable. The client remembers the join-time alias, and the server now also accepts the prefixed alias an older client would send — while the prefix remains a comparison, not a wildcard, so it cannot resolve to an unrelated player. Tests: `Report_ByPrefixedBotAlias_StillResolvesToTheHumanWhoLeft`, `Report_ByPrefixedBotAlias_DoesNotResolveABotFilledSeatToAnotherPlayer`. |
| C20 | Docs a new tester actually runs now work: the project was renamed (`ProjectUpdraft/` → `ProjectHotdrop/`, `PROJECT_HOTDROP_SPEC.md` → `PROJECT_UPDRAFT_SPEC.md`, `UpdraftBackend` → `HotdropBackend`) but runnable commands still used the old names, and `STEP_C.md` omitted the `-File` switch `Run-UnityTests.ps1` requires. Every pasted command was corrected and re-run; the README documents the rename so old paths in dated records are explained rather than silently contradictory. |
| C13 | The crash-free gate is labeled honestly: `client_error` is an error signal, not a crash, and `KpiGateConfig.minCrashFreeSessionRate` now states it must come from Google Play vitals, never from client telemetry. The real crash/ANR figure still needs a release build on Play and cannot be produced here. |
| — | **New:** results now show the next reachable goal (`NextGoal`), shared with the Home goal card so they can never disagree — Amendment 1, first-run journey step 4. |

In progress in a concurrent session: **C04** (server-emitted `AuthoritativeProgressFacts` for contracts/mastery).

Withdrawn after verification: **C08** (see below).

### In-match view pass (3 Oct 2026) — the view no earlier pass had looked at

Every prior pass compared **Home**. The gameplay view was only ever exercised by assertions. It was
rendered for the first time here at 1920x1080 via the existing PlayMode capture harness and compared
against `UIUX/.../screen_08_gameplay_hud_classic_preset`. Two real defects were found and fixed; the
rest are recorded rather than papered over.

| Observation in the live match capture | Outcome |
|---|---|
| Score bar, kill feed, health plate and ammo plate rendered as near-opaque chrome (a hardcoded `Color(0.063,0.094,0.153,0.82)` in `SceneSetup.CreateUIPanel`), hiding the arena a player is tracking targets in | **Fixed.** `UIDesignTokens.HudGlass` (0.66 alpha) + `MatchHUD.ApplyHudGlass()` re-tints the six floating panels at runtime. The capture now shows the arena through the panels, as the reference does. |
| A world-projected ping landed on the score/zone chrome, so the ping caption and the zone status were drawn over each other | **Fixed in code and unit-tested.** `PingSystem.ClampToReadableBand` keeps markers out of the reserved top (24%) and bottom (26%) bands. *Not conclusively verified in the capture* — see the caveat below. |
| Touch controls absent from the capture | **Not a defect.** They are `OnGUI`-drawn (`TouchInputManager.OnGUI`), so they never appear in a Canvas render but do render on device. Recorded so nobody "fixes" it by rebuilding them. |

**Structural problem this pass exposed (worth more than the two fixes):**
`SceneSetup` is **editor-only** — `BuildOfflineSlice.cs` runs it to bake `Assets/Scenes/Main.unity`.
Every HUD colour is therefore *serialized into the scene file*, and editing `CreateUIPanel` changes
nothing until someone regenerates the scene. My first attempt edited `CreateUIPanel` and the capture
was byte-for-byte near-identical, which is how this surfaced. The durable fix is to keep the editor
generator (so a regeneration is correct) **and** apply the token at runtime in `MatchHUD`, which owns
the live HUD. Both now agree. Anyone else editing HUD colours in `SceneSetup` alone will hit the same
wall.

**Caveat, stated honestly:** the ping clamp is unit-tested against the measured band, but I could not
prove from a single static capture that the two visible pings were re-clamped in that frame (a capture
renders one frame; ping lifetimes are 3 s). The band math is derived from the capture's own geometry,
but treat it as tested-not-confirmed.

Tests added: `PingBandAndHudGlassTests` (6). EditMode **332/332**, PlayMode **18/18** (202.97 s).
Evidence: `ProjectHotdrop/UIEvidence/playable_2026-10-03/08_playable_relay_1920x1080.png`.

Still open: C05 (Gate C six-client), C06 (device acceptance), C07 (presentation), C21–C25.

### UIUX presentation pass (started 3 Oct 2026, after the UIUX folder was located)

`docs/audit/UIUX_PARITY_2026-10-02.md` correctly refused to treat the UIUX folder as pixel-parity
evidence, but the folder itself is the **design reference of record** and had not been acted on.
The reference Home (`UIUX/.../screen_3_home_lobby_production_clean`) carries three structural
elements the runtime lacked. Two are now implemented, both with real data only:

| Reference element | Status |
|---|---|
| Top identity strip (alias · level · Flight Seals · region · settings) | **Done.** `UIKit.StatusStrip`, wired into Home. Values come from the player's own backend profile; an offline guest shows `GUEST` and omits the seals/region rows entirely rather than showing `0` or a fabricated region. |
| Determinate progress bar on the goal/contract card | **Done.** `UIKit.ProgressBar` + `UiProgressBar`, filled from authored challenge records in the current context only. With no library yet it reads `START WITH GUIDED TRAINING`, never a fake 100%. |
| Translucent cards floating over the live 3D scene, character composited into the world | **Not done.** Requires the live arena behind the menu with the player's own avatar standing in it. This is the largest remaining gap between the runtime and the reference and it is presentation work, not a correctness defect. |

New primitives are covered by 6 EditMode tests (`ProgressBar_FillTracksTheFractionAndStaysClamped`,
`ProgressBar_HasATrackAndItsFillIsNeverTheWholeBar`, `ProgressBar_FillIsADriverSoCallersCanUpdateIt`,
`StatusStrip_ShowsIdentityAndOmitsEmptySections`, `StatusStrip_WithoutSealsOrRegion_BuildsWithoutThoseRows`,
`StatusStrip_KeepsIdentityLeftAndSettingsRight`).

**Not attempted, and why:** the reference's type stack (Space Grotesk / Hanken Grotesk / JetBrains Mono)
is not in the project and fetching Google Fonts at runtime is not acceptable in a shipping Android
build. The project ships Liberation Sans (SIL OFL) and the existing `TabularNumbers` component
normalises digits against it. Matching the reference typography needs licensed font files added to
the asset register — a decision for the publisher, not a code change.

## Verdict

This is a **strong prototype with unusually good engineering discipline and an honest paper trail — it is not a complete game.** The architecture is sound (shared pure-rule core for offline/online, config-driven balance, server-authoritative results, atomic local save). What is missing is not "more menus": it is the **presentation/feel layer, real-device proof, live network proof, and three explicitly-amended systems (first-match badge, three-slot cosmetics, contracts/mastery) that are either absent or dark-flagged.**

Because the amendment says "100% according to the design," the fix list below is measured against **spec v1.0 + amendment 2026-10-01**, section by section. Nothing here is invented scope.

## What is genuinely correct (do not "fix" these)

- Relay rules match the spec exactly: 1 pt/s uncontested, contested/empty 0, 120 to win, 180 s regulation, 30 s OT / 3 s hold / draw, exact zone schedule, feet ≤1.5 m, spawn-protected can't score (`RelayScoringController.cs`, `ZoneOccupancy.cs`, `MatchRulesConfig.cs`).
- Weapons/pads/movement config values match the spec tables (`WeaponConfig.cs`, `PadConfig.cs`, `MovementConfig.cs`).
- Server authority is real: client sends input; damage/score/pads/results are server-only; unique `(PlayerId, Reason, SourceId)` ledger index prevents double grants; result outbox retries exactly-once; allocation lease + capacity cap (`NetMatch.cs`, `NetPlayer.cs`, `MatchResultProcessor.cs`, `MatchEndpoints.cs`).
- Offline challenge collection is a real system (12 authored entries, atomic versioned save, FNV checksum, legacy migration, conservative resume) — a genuinely good build for the branch.
- Anti-cheat touches (aim direction from replicated yaw/pitch, muzzle-obstruction, seeded spread) are above prototype grade.
- Backend suite rerun today: **167 passed / 0 failed** (NU1900 = no vulnerability feed, not a clean security check).

## P0 — Blocks "complete"

| # | Player-facing defect | Evidence | Fix (design-matched) |
|---|---|---|---|
| C01 | The 12 offline challenges store and display **only Bronze**. Silver/Gold are permanently disabled (`thresholdsValidated=false`, `isTuned` gate), so there is nothing to chase after completion. | `OfflineChallengeConfig.CalculateMedal`, every authored entry | Author real Silver/Gold from human completion distributions (Amendment 4C): time/accuracy/score per challenge, versioned, disclosed. Enable per-challenge only after a human route-feasibility pass. No invented thresholds. |
| C02 | **No first-match cosmetic badge.** Amendment 1 requires `badge_first_flight` granted once after the first eligible public completion (win or loss). It does not exist anywhere in client or backend. | grep: zero `badge_first_flight`; `MatchResultProcessor` grants only starter + level + 3-match unlock | Add the catalog entry, grant-once on first eligible public participation (server-side, unique source key), show it on Results and in Appearance. |
| C03 | **Cosmetic identity is still one slot.** Amendment 3 requires independent `bodyStyleId` / `weaponWrapId` / `badgeId` + `bodyRigId`. Backend/entity/endpoint/claim/replication all still use a single `EquippedCosmeticId`. | `Entities.cs`, `ProfileEndpoints.cs`, `MatchEndpoints.cs`, `NetPlayer.Cosmetic` | Split into three validated slots + rig; backfill old value; keep adapter for old clients; replicate public identity (no account ids); weapon wraps must bind to the real weapon mesh. |
| C04 | **No contracts or mastery** for real players. Service, endpoints, and catalog exist but `ContractsMasteryEnabled=false`; the match server never emits `AuthoritativeProgressFacts` (grep: zero producer), so even when enabled, progress cannot advance. | `appsettings.json`, `ContractsMasteryService.cs`, `NetMatch`/`DedicatedServer` | Emit bounded per-player facts (valid uncontested occupation seconds, distinct human-teammate pad uses, majority weapon) from the authenticated server; verify exactly-once seals; surface goal selection/progress UI. Ship behind the flag only after result-pipeline acceptance. |
| C05 | **Network gate never accepted.** Six-client run, 30/80/150 ms RTT + loss/jitter, disconnect/reconnect, Linux headless ops evidence are all missing. | `MILESTONE_STATUS.md`, `docs/acceptance/STEP_C.md` | Run and record the Gate C matrix on the deployed Linux server; fix whatever it exposes. Until then, offline must be the labelled primary path. |
| C06 | **No real-device acceptance.** Thermal/frame-time/memory/startup, background/kill/call/Wi-Fi↔mobile, cutout/larger-text/left-handed, all 12 drills in airplane mode. | `OFFLINE_TRAVEL_DEVICE_CHECKLIST.md` — every row NOT RUN | Execute the checklist on the reference phone; report frame-time distribution and thermals, not a screenshot. |
| C07 | **Greybox presentation.** Cubes/primitives, no authored humanoid, no animation set, weapons/pads read poorly at phone size. | `PLAYER_AUDIT_2026-10-03.md` P01; asset register | One coherent vertical slice: shared-rig humanoid, three distinct weapon silhouettes, readable pad states, modular rooftop kit, matching lighting — inside the Low budget (30 FPS, ≤150 draw calls, ≤250 k tris). |
| C08 | **Withdrawn — false finding.** The tutorial already implements all nine spec §11 steps (move, target, reload, place pad, ride own pad, use enemy pad, destroy enemy pad, capture zone, bot fight), per-step contextual help in `TutorialController.ShowMe()`, and death→respawn handling that returns the player to the exercise (`ReturnAfterRespawn`, QA-013). | `TutorialController.cs` `GetNextStep` / `ShowMe` / `ReturnAfterRespawn` | None. Recorded so a later reviewer does not re-open it. |
| C09 | **Bots on the player's team are hardcoded Easy** in offline practice. | `MatchController.CreateBots`: `easyDifficulty = teamId == localPlayerTeam` | Follow `BotConfig`; make practice difficulty explicit, not a hidden buff/nerf. |

## P1 — Breaks design conformance / correctness

| # | Defect | Evidence | Fix |
|---|---|---|---|
| C10 | **AFK windows hardcoded** to 30 s / 45 s in the server instead of `ProgressionConfig` (the standing rule is "never hardcode"). | `NetMatch.HandleAbsence` | Read from config; keep the warn/remove semantics. |
| C11 | **Telemetry allowlist drift.** Client emits `tutorial_show_me` and `afk_removed`; neither is in the backend allowlist, so both are silently rejected. | `Telemetry.Track` calls vs `AnalyticsEndpoints.ClientEvents` | Add both (versioned), and add offline-challenge events (attempt/completion) required by the amendment; keep client/server event ownership. |
| C12 | **Offline analytics cannot reach the server.** Offline match facts (`offline_sim`) are written only to a local JSONL queue; no uploader path for them. | `Telemetry.TrackOfflineMatchFact` | Either upload offline facts through the client batch endpoint with `source=offline_sim`, or state plainly that offline KPI data is device-pulled only. Amendment requires offline challenge attempts/completions to be measurable. |
| C13 | **Documentation/labeling hardened.** `client_error` is defined in code and in the Gate D analysis plan as an *error* signal, not a crash, and the crash-free gate is attributed to Google Play vitals. `KpiGateConfig.minCrashFreeSessionRate` now carries an explicit warning against deriving it from client telemetry. | `GameBootstrap.ReportClientError`, `GATE_D_ANALYSIS_PLAN.md`, `KpiGateConfig` | Done — see remediation table. The actual crash/ANR figure still requires Play vitals from a real release, which cannot be produced here. |
| C14 | **Score-limit end condition wrong.** UI reports "SCORE LIMIT" whenever either score ≥120, even if the match actually ended on the timer at a higher score. | `MatchController.EndMatch` | Have `RelayScoringController` expose the real end reason (score / time / overtime hold / draw). |
| C15 | **Aim assist advertised but unproven.** Spec: slowdown-only, ≤3° cone, ≤25 m, 70% inside region, off switch. A toggle exists; the actual cone/drop behaviour isn't evidenced. | `OfflinePauseMenu` toggle; no matching test | Implement/verify the exact rule and add a focused test; or remove the claim until real. |
| C16 | **Fixed.** Bot fill was giving every waiting group its own separate match (so four queued humans got four 1-vs-5 lobbies). The composer now fills one 3v3 with as many waiting humans as fit and lets the match server spawn bots for the empty seats. | `MatchmakingComposer.RunOnceAsync` | Done — see remediation table. |
| C17 | **Fixed.** `same_day_return` / session definitions required by the amendment are now implemented as code (`SessionRules`) and rolled up daily. | amendment §5 measurement | Done — see remediation table. |
| C15 | **Fixed.** The aim-assist limits existed but the behaviour was untested and the cone/LOS rule was duplicated inline in the offline and online aim paths. Both now call one shared, pure `AimAssistRules.Qualifies` / `SensitivityScale`, and 13 tests pin the spec-8 rule (≤3° cone, ≤25 m inclusive, 70% slowdown, no assist through cover, only ever slower, two scales only so no snap/magnetism is possible). | `AimAssistRules`, `PlayerController.IsAimAssistRegionActive`, `NetClientPresenter.UpdateAimAssist`, `AimAssistTests` | Done — see remediation table. |
| C18 | **Withdrawn — resolved by C03.** The Home preview already builds its avatar through the same `AvatarRigVisual.Build` the offline and networked players use (not a hand-made stand-in), and after C03 it applies the full `CosmeticLook.LocalComposite`, so body style, weapon wrap, badge and rig all match what the player spawns with. | `HomeCharacterPreview.cs`, `CosmeticLook.Apply`, `NetVisuals.cs`, `PlayerController.ApplyStoredBodyRig` | None. Recorded so a later reviewer does not re-open it. |
| C19 | **Fixed.** A human whose slot becomes a labeled `[BOT] ` after quitting or an AFK removal stays reportable: the client remembers the join-time alias (`NetPlayer.ReportAlias`), and the server additionally tolerates a prefixed alias from an older client without letting the prefix become a wildcard. | `NetPlayer.ReportAlias`, `SupportEndpoints.AliasMatches`, `ReportByMatchIdentityTests` | Done — see remediation table. |
| C20 | **Fixed for every command someone can paste.** The project was renamed (`ProjectUpdraft/` → `ProjectHotdrop/`, `PROJECT_HOTDROP_SPEC.md` → `PROJECT_UPDRAFT_SPEC.md`, `UpdraftBackend` → `HotdropBackend`), but runnable instructions still used the old names and `STEP_C.md` omitted the `-File` switch `Run-UnityTests.ps1` requires. Corrected paths, the `-File` form, and a README note explaining the rename; dated acceptance prose is left verbatim as evidence. | `ProjectHotdrop/README.md`, `docs/acceptance/STEP_*.md`, `docs/UNITY_LICENSE_RECOVERY.md` | Done — see remediation table. |

## P2 — Polish / honesty

| # | Defect | Fix |
|---|---|---|
| C21 | Challenge result copy is thin (numbers, generic failure). | Add specific failure cause, objective count, previous-best delta (partly done — finish it). |
| C22 | Home goal card can say "Replay from More". | Resolve next goal from real state; single direct action (partly done — verify all states). |
| C23 | 12 challenges ship but a rotating featured challenge + archive (amendment 4C post-step) is absent. | Only after retention is measured; keep as documented follow-up, not fake content. |
| C24 | Low-end graphics preset mostly sets target FPS/render scale. | Verify real quality differences or label honestly. |
| C25 | Personal bests don't distinguish offline vs online before any public comparison. | Separate contexts (spec/amendment). |

## The 100% definition of done

A fix counts as closed only when its acceptance test passes **on an identified candidate build**:

1. One frozen candidate: source revision + config + APK hash + test XML.
2. Every P0 row has device/network evidence, not editor evidence.
3. Contracts/mastery/cosmetics/first-match badge enabled only after their own exactly-once and migration tests pass.
4. Gate C and Gate D records updated with real numbers, including sample sizes and uncertainty.
5. No claim of retention, fun, or launch readiness from automated tests or screenshots.

## Notes on the existing audit trail

The repository already contains a truthful player audit (`PLAYER_AUDIT_2026-10-03.md`) and implementation report (`OFFLINE_TRAVEL_IMPLEMENTATION_2026-10-03.md`). This dossier **does not contradict them**; it adds the amendment-conformance gaps (C02/C03/C04) and the config/telemetry drift (C10–C13) they did not enumerate, and it converts their "acceptance gaps" into concrete design-matched fixes.
