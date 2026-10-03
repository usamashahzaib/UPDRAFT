# HOTDROP product and retention amendment

Date: 1 October 2026. Status: recommended design and implementation handoff, not implemented gameplay or gate certification.

## Decision

Keep HOTDROP as a three-minute 3v3 rooftop objective shooter. Make shared launch routes its identity: create a shortcut, take an objective, and watch an opponent use your own route against you. Build a polished version of that experience before investing in a battle royale conversion. A battle royale is a separate hypothesis, not HOTDROP's inevitable destination.

Add a useful offline experience for travel, permanent personal goals, and visible cosmetic identity. These are explicit amendments to the original limited scope. Retention and acquisition must be measured; neither a reward system nor a Play Store upload guarantees demand.

## What this checkout actually establishes

Reviewed HEAD: `5fa1dab`, with existing uncommitted scene, prefab, body-rig, HUD, specification and acceptance changes. Those changes were preserved. This review read source, saved test XML, repository screenshots and deployment records. It did not run an APK, rerun tests, contact the deployed services, or execute multiplayer sessions.

The shared working tree changed during review, including additional UI-metrics, runner and CI work. Findings describe the inspected implementations and evidence; this is not an atomic source snapshot or certification of another agent's in-flight changes.

| Area | Source or evidence | Finding |
|---|---|---|
| Core experience | `GameBootstrap.cs`, `MatchController.cs`, `TutorialController.cs`, `NetMatch.cs` | Offline tutorial and Relay loop, three weapons, bots, pads, scoring and a network implementation exist. Reuse them. |
| First impression | `OfflineHomeScreen.cs`; inspected `TestResults/home_screen.png` | Nine similar navigation actions compete for attention. No visible character, selected personal goal, or reward preview gives the player a compelling next action. |
| Characters and world | `NetVisuals.cs`, `SportAvatarAnimation.cs`, `SwitchyardGenerator.cs`; inspected gameplay screenshots | Network avatars are cubes; prototype animation swings legs; the map is generated graybox geometry. These are real presentation limits, not proof that the core cannot work. Screenshots are historical evidence, not certification of today's APK. |
| Progression | `ProgressionConfig.cs`, `MatchResultProcessor.cs` | 120 base XP, 30 win bonus, 400 XP per level, level cap 20. Level 20 requires 7,600 XP: 51 all-win or 64 all-loss eligible matches, roughly 153–192 regulation minutes before queues, prep, results and overtime. This is a short onboarding track, not a complete long-term progression system. |
| Identity | `Entities.cs`, `ProfileEndpoints.cs`, `CosmeticLook.cs`, `BodyRig.cs` | One `EquippedCosmeticId` prevents combining body, wrap and badge. Male/female gameplay-parity definitions exist, but no complete selection/storage/replication journey was found in the reviewed home, appearance and bootstrap flow. |
| Return systems | Focused searches of Unity and backend source | Local personal bests already exist. No implemented player mission, daily challenge, weapon mastery or persistent contract system was found. Analytics daily-presence records are measurement, not a player reward system. |
| Reliability | Latest saved `TestResults/EditMode.xml` and `PlayMode.xml` | 199/199 EditMode and 8/8 PlayMode passes, recorded 1 October 2026. These are existing artifacts, not fresh results from this review or proof of phone ergonomics. |
| Online deployment | `docs/deployment/ORACLE_DEPLOYMENT_CHECKPOINT.md` | Records HTTPS backend availability and a real Linux server smoke test. Authenticated backend-matched gameplay and the six-client latency/failure matrix remain unverified. Do not recreate this infrastructure or describe it as nonexistent. |

The old QA report is a starting index, not a list of 35 defects that can all be presumed still open. Newer source has repairs for touch ownership, camera collision, results lifetime, party watching, pad previews, hit confirmation, account recovery and other issues. Verify current behavior before fixing any of them again.

## Amendment 1 Make the first five minutes convincing

### The home screen

Replace the equally weighted button grid with one clear primary action and a visible character. Preserve all existing account, support and deletion functionality behind ordinary navigation.

| Location | Content | Behavior |
|---|---|---|
| Main area | Selected character, selected weapon, recognizable Switchyard backdrop | Shows the actual in-game model and equipped items. No separate misleading promotional model. |
| Primary action | `PLAY 3-MINUTE RELAY` | Enters public matchmaking. Shows availability and region without a developer status paragraph. |
| Secondary action | `PLAY OFFLINE` | Opens saved travel challenges or the existing bot Relay. Always works without login or network. |
| One small goal card | Selected goal, current progress, exact reward preview | Example: `Complete 2 matches — 1/2 — earn a Flight Seal`. Never show more than one active goal on Home. |
| Persistent navigation | Party, Appearance and a More menu | More contains Profile, Training, Settings, Account and Support. Preserve reachable Back paths and account deletion. |

Use the current palette as a starting point. Cyan identifies allies, coral identifies enemies, and gold identifies objectives. Reserve intense color for meaning. Use clear icons with labels for unfamiliar actions. Remove development touch diagnostics from release presentation. The current tagline says "three teammates" although a player has two teammates; use "Three players. One team. Three minutes."

### First-run journey

1. Offer a skippable character choice, with a ready-to-play default. No required name, account, shop or notification prompt.
2. Start a compact guided encounter using the existing tutorial machinery: move, hit a target, launch to a platform, capture an objective. Target about 60–90 seconds, with no timer forcing a slower learner to fail.
3. Teach enemy-pad use, pad destruction and reload through contextual prompts during the first bot match; retain the full nine-step training as a replayable lesson. Do not silently remove tutorial coverage.
4. Give specific feedback after the first match: outcome, one contribution, and the next reachable goal. Offer `PLAY AGAIN` immediately and an equally usable Home exit.
5. Invite public play or continued offline play. Online availability failures must leave offline play usable.

A first-match cosmetic reward is a new cosmetic badge `badge_first_flight`, granted once after the first eligible public completion, whether win or loss. Tutorial completion produces a local training medal, not online currency or an online entitlement. Keep the existing third-match appearance unlock.

Acceptance: a fresh player finds Play, moves/aims/fires together, understands the active objective, uses a pad and reaches a second match without coaching. Measure time to first controllable scene, first launch, first completed match and voluntary second match. Initial usability target: at least 8 of 10 observed target players complete the journey unaided. This is a project decision threshold, not an industry guarantee.

## Amendment 2 Deliver a grounded visual identity and comfortable touch play

### Presentation direction

Switchyard becomes a believable industrial rooftop complex with a communications tower at Center, a water-treatment structure at West and a loading gantry at East. These landmarks decorate the existing objectives; do not move scoring zones or alter competitive routes during the art pass. Add ventilation equipment, pipes, safety rails, signage, surface wear and a distant skyline. Use daylight, baked lighting where supported, restrained materials and readable silhouettes. Decorative geometry must not introduce accidental cover or change line of sight.

Replace cube avatars with commercially usable, consistently styled humanoids with realistic proportions and recognizable faces in the lobby. Two body appearances share the authoritative gameplay geometry and compatible skeleton/animation set. A body choice is character appearance, not a demand that the player disclose their own gender. Record licenses and editable source. Do not promise AAA art from the current prototype budget.

Minimum animation coverage: idle, forward/backward movement, left/right strafe, aim, fire, reload, jump, launch, landing, elimination and respawn. Connect transitions to real replicated/offline states. A character standing still during a firefight is not an accepted art replacement.

Move representative characters, one finished arena section, combat audio and HUD polish before external gameplay/retention evaluation. Full asset production and release optimization remain later work. Replace the specification clause that forbids all art investment before G with this distinction.

### Controls and accessibility

| Setting | Initial implementation | Acceptance |
|---|---|---|
| Layout | Classic two-thumb and Simple presets, both using the existing control editor | Movement, fire-drag aim, jump/launch, pad and reload fit safe areas; at least 48dp touch targets; no touch-owner theft. |
| Focus Aim | Optional hold/toggle shoulder zoom from current 55-degree FOV to initial 40 degrees | Separate sensitivity; smooth reversible transition; same authoritative weapon accuracy, damage and hit geometry. Call it Focus Aim until real sights/animations justify ADS. |
| Readability | Grounded target silhouettes, existing team shape cues, visible-target highlights, contextual damage direction | No outlines through cover; no permanently exposed enemy positions; combat cues distinguish hit, elimination, reload and invalid pad placement. |
| Comfort | Camera shake toggle, vibration toggle, larger HUD/text option, color-plus-symbol cues | Settings persist across restart and work in offline and online play. Do not infer suitability from the player's age. |
| Settings | Controls, Graphics, Audio and Account/Support tabs | Replace cyclic sensitivity/volume buttons with labeled sliders and values; preview changes; per-tab reset; truthful graphics availability. |

Do not add crouch or automatic corner leaning to solve a bad layout. Those change movement, hit presentation and network behavior and require a separate gameplay decision. Focus Aim initially changes camera/control behavior only. If it later changes spread or movement, implement and test the same rule on the server.

`GraphicsConfig` contains budgets, while `GameBootstrap.ApplyGraphicsConfig` mainly sets the frame target. A settings label is insufficient: implement and verify actual quality changes. Retain the existing Low targets (30 FPS sustained, 750 MB steady memory, 150 draw calls, 250k visible triangles) as targets to measure on physical phones. Do not increase them just to make art pass.

## Amendment 3 Give the shooter memorable moments and a complete identity

Keep one public queue and one online mode. The interesting question is whether shared pads create readable team opportunities that ground routes cannot provide, not whether more menus can disguise repetitive combat.

Make three moments unmistakable: a launch into a contested objective; an opponent taking your shortcut; and your team retaking a zone just before rotation. Reinforce them with readable trajectory/landing cues, spatial audio and a short objective announcement. Do not fabricate highlight events or interrupt the fight with full-screen reward effects.

Results should show the player's contribution before the detailed scoreboard: objective time, a verified useful route if one occurred, personal best, and next-goal progress. Keep loss participation rewards. Never invent "MVP" or a supportive message for an action the player did not perform. Existing personal-best storage is reusable but should distinguish offline and online contexts before any comparison is public.

### Cosmetic slots

Replace the single cosmetic slot with three validated slots: `bodyStyleId`, `weaponWrapId`, and `badgeId`, plus `bodyRigId`. The backend validates category and ownership; the network replicates public cosmetic identity without internal account identifiers. Backfill the old `EquippedCosmeticId` into its correct slot. Keep a backward-compatible adapter until old clients are retired. Body selection is free; it does not require earning a cosmetic.

Show the same equipped combination in Home, Appearance, Results, offline play and other clients' online views. Weapon wraps must apply to the real weapon mesh, not a disconnected preview. Cosmetic silhouettes and team identification retain gameplay parity.

Retain the existing party infrastructure. Add a post-match opt-in `PLAY TOGETHER` action only after normal parties reliably queue all members. Use explicit acceptance; no automatic friendship or messaging. Do not create a second matchmaking pool for events or newcomers until population evidence supports one.

## Amendment 4 Build three useful return systems

The existing 20-level XP track remains a beginner track. Do not simply multiply XP requirements to manufacture more hours. Extend reasons to return through permanent chosen goals, mastery and a useful offline challenge collection.

Candy Crush's Chocolate Box is a useful reference for understandable tasks and visible rewards, not proof those rewards will retain shooter players: https://candycrush.zendesk.com/hc/en-us/articles/360020946198--Chocolate-Box.

### A Selectable contracts for short sessions

Unlock after two completed public matches. Present three choices, with one active at a time. Progress survives closing the app and missing days. Switching contracts pauses its progress rather than resetting it. New contracts become available after completion; do not require a daily login streak.

| Contract | Initial objective | Qualifying evidence |
|---|---|---|
| Finish together | Complete 2 eligible public matches | Existing authoritative participation receipts. Both wins and losses count; offline practice does not. |
| Hold the relay | Earn 45 seconds of valid scoring-zone occupation across eligible matches | Server-counted valid uncontested occupation; inactive/contested/protected time does not count. |
| Build a team route | Two distinct human teammates use your pads in eligible matches | Server launch events; distinct `(match, pad, teammate)` use records; owner use and bots do not count. Show when no qualifying teammate is available. |

The route contract is optional, not a prerequisite for progression. Leave it visible as a choice for social players; completion contracts work in labeled bot-filled public matches. Distinct team-use telemetry is new work; current `PadLaunches` alone does not establish ownership or helpfulness.

Each contract awards one permanent Flight Seal. Six seals unlock one exact, previewable cosmetic choice from an original/licensed three-item set. Three sets give an initial 18-seal path; repeat contracts after that retain mastery progress but do not imply another unavailable reward. No random boxes. New sets require real produced assets, a content version and explicit catalog entries.

Each assignment has a server-generated goal-instance ID distinct from its reusable template ID. One match may advance the selected contract and applicable permanent mastery tracks; it cannot advance paused contracts. Duplicate submissions cannot replay that match's progress. Completing the same template later creates a new instance rather than reusing its old reward key. Once a goal completes, auto-grant its seal once; a cosmetic choice remains pending until the player chooses, then grants once.

Seals are a separate progression counter, not purchasable currency. Keep current credit payouts unchanged in the first experiment to avoid accidental economic inflation. Display capped reward-track completion honestly. Extend it only after observing how many players reach it and what they choose afterward.

### B Permanent mastery that recognizes different play styles

| Track | Bronze / Silver / Gold initial thresholds | Reward |
|---|---|---|
| Relay specialist | 60 / 300 / 1,200 cumulative valid objective seconds | A track-specific badge tier. |
| Route builder | 3 / 12 / 40 distinct qualifying human teammate pad uses | A route-builder badge tier. |
| Weapon familiarity | 5 / 20 / 60 eligible matches completed with each weapon as the majority-equipped weapon | A weapon-specific badge tier. Not a claim of aiming skill. |

These are starting balance values, not proven optimal targets. Completion depends on observed behavior. Rewards change appearance only. Never reduce recoil or increase damage through mastery. Players can equip one badge and preview the next tier, including after level 20. All progress comes from server facts; a client cannot submit its own completed mastery claim.

Bot-filled public matches can advance participation and occupation tracks when the player is eligible. Bot uses never advance human-team route counts. Keep human/bot cohort dimensions in analysis; a bot farm must not be described as human competitive mastery.

### C Offline travel challenges

Promote existing offline Relay into an honest, accessible travel option. Add 12 authored challenges using the same arena, movement, weapons, pads and bots. This explicitly permits offline drills beyond the original mode exclusion; it adds no online queue.

| Chapter | Four initial challenges | Session shape |
|---|---|---|
| First flight | Basic route, own-pad route, enemy-pad route, safe landing | Short movement tasks with optional time improvement. |
| Rooftop operator | Stationary targets, moving targets, reload discipline, launch-and-hit | 60–90 second drills using the existing gun rules. |
| Relay captain | Capture one zone, rotate between zones, defend against authored bots, complete an offline Relay | Introductory objective tasks and the existing three-minute match. |

Each challenge stores completion, an optional medal and personal best locally. Completion earns bronze; silver/gold times or accuracy thresholds are authored after route feasibility and target-device tests. Do not generate impossible thresholds from arbitrary numbers. A medal or score must represent the challenge version, device control preset and difficulty. Permit retry immediately and pause/resume at any time.

Ship all 12 challenges and required assets in the installed build. No connection, ads, account or streamed download is required. Save between steps and on interruption; for paused bot Relay, resume the local match snapshot or explicitly offer a clean restart if recovery fails. Never imply an online match can pause for travel.

Offline medals and records are separate local achievements, not authoritative online currency, inventory or leaderboard submissions. Linking an account may back up this local save as unverified personal progress; it must not mint online entitlements. Surface the network boundary through clear player-facing labels, not technical explanations.

After these challenges retain players, trial one rotating featured challenge drawn from the shipped collection, with an optional fixed local medal. Retain an archive so a missed day does not destroy access. Measure whether variety produces return visits before committing to a daily content production schedule.

## Amendment 5 Implement in evidence order and measure the right result

### Five implementation packages

| Order | Work package | Main files and systems | Exit evidence |
|---|---|---|---|
| 1 | Reference experience and controls | `OfflineHomeScreen`, `OfflinePauseMenu`, `MenuLayout`, `MatchHUD`, `ThirdPersonCamera`, `TouchInputManager`, tutorial, avatar presentation and Switchyard art | One source-mapped APK; real-phone fresh run, full match, rematch, settings persistence; owner approves representative presentation. |
| 2 | Real online journey and identity | Existing `NetSession`, `NetClientPresenter`, `NetMatch`, `OnlinePlay`, backend identity/party/profile, cosmetic slots | Six clients on the Linux server; authenticated allocation/admission; results/reconnect/cancel/failure matrix; all views agree on cosmetics. |
| 3 | Travel experience | New `OfflineChallengeConfig`, `OfflineChallengeController`, `LocalChallengeSave`, using shared existing rules | All 12 challenges playable in airplane mode; app restart/interruption/pause tests; no online rewards from local edits. |
| 4 | Goals and mastery | New backend progression records/services, result facts, Unity goal UI; reuse ledger/receipt/outbox principles | Exactly-once grants, real progress UI, reinstall recovery, bot handling, concurrent submission and migration tests. |
| 5 | External validation and controlled release readiness | Telemetry, existing recruitment/study kits, performance matrix, launch checklist | Core comprehension, voluntary replay and real D1/D7/D30 cohorts; device/network evidence; measured operating capacity and cost. |

Do not implement all five packages in one agent session. Keep one implementation owner for overlapping Unity assets and builds; use review handoffs with exact source/build identities. Preserve current changes and do not regenerate or replace the entire project. No broad asset purchase is required before approving a small representative art sample.

### Gate amendments

Split D into two decisions. `D-core` evaluates controls, pad comprehension, readability and voluntary replay using representative presentation; its pad/no-pad study gives no reward for picking a version. `D-retention` evaluates the playable product with working progression, parties and recovery over real observation windows. Minimal E journey functionality must therefore be available before D-retention; waiting for D-retention before any player journey exists is circular.

Prototype contracts/mastery behind flags after D-core. Compare the same core with and without the return layer before claiming an improvement. Monetization stays disabled until both D decisions and the account/purchase prerequisites pass. G still handles complete production art, optimization and operational release readiness. Earlier presentation work is now required, not forbidden.

### Measurement

| Question | Metric | Decision |
|---|---|---|
| Can people play comfortably? | Task completion, control failures, tutorial abandonment and observed reasons | Fix input/readability before interpreting low retention as a missing reward. |
| Is the match itself worth repeating? | Voluntary immediate rematch, purposeful pad use, repeated gameplay choices and interviews | Keep the current initial 50% rematch target; report counts and conditions. |
| Do players return on their own? | D1/D7/D30 retained unique players, with explicit windows and cohort definitions | Keep current initial 35%/15%/5% goals as hypotheses. Separate scheduled/incentivized testers, human/bot play, offline and online users. |
| Does the return layer help? | Controlled cohort comparison of return visits and voluntary matches per returning player | Evaluate direction and uncertainty. Small samples cannot certify an uplift. Do not change combat and rewards simultaneously in this comparison. |
| Can the game sustain real users? | Crash/ANR, frame-time/thermal, queues, successful matches, reconnects, capacity and cost per match | Complete device/network/operations evidence before scaling acquisition. |

Define `same_day_return` as a new playable session at least 30 minutes after the prior session ended. A session ends after 30 minutes with no gameplay activity. Count unique people as well as sessions; brief background/foreground switches are not return visits. Report voluntary play separately from incentive/task-driven sessions. More hours alone are not the product-quality criterion.

Use 10 people for early observed usability and the existing 30–50 for qualitative pad testing. Recruit larger uncoached cohorts for retention estimates; report actual denominators and uncertainty. A D7 sample of 30 with five returners is not evidence of a stable business. Determine a controlled experiment's sample size from its baseline and desired detectable improvement rather than declaring an arbitrary universal minimum.

Add versioned events for home play choice, first gameplay action, focus-aim/settings use, first-match completion, goal selection/progress/completion, reward shown/equipped, mastery milestones, offline challenge attempts/completion and session return. Update the backend event allowlist and schema with the client. Authoritative goal progress belongs to the server; client events describe UI actions only.

The current client `client_error` event records errors/exceptions and is not a crash-free calculation. Use actual crash/ANR reporting and Google Play vitals, with explicit metric definitions: https://developer.android.com/games/optimize/vitals. Do not call a caught exception a crash or infer no crashes from no uploaded events.

### Backend integration contract

| Record or API | Required behavior |
|---|---|
| Equipped appearance | Profile returns independent body, wrap, badge and rig slots. Equip validates owned item category; body-rig selection is an allowed catalog choice. Existing clients have a migration/compatibility path. |
| Goal catalog and selection | Versioned catalog exposes IDs, objective, reward, prerequisites and progress. Choose from known IDs; arbitrary client objectives/counters are rejected. |
| Goal and mastery progress | Match server sends bounded per-player objective/weapon/owned-pad-use facts with authenticated results. Backend validates them against allocated participants and match bounds and updates progress in the same retry-safe processing boundary. |
| Reward grant | Unique player/source/reward keys and transactional inventory/progression writes prevent duplicate badges, seals and cosmetics. Replayed results, parallel requests, restart, pending outbox delivery and old-schema servers need explicit tests. |
| Local challenge save | Versioned, atomic local save; corrupt/missing data recovers cleanly. Never accept it as evidence for an online inventory, competitive score or wallet. |

### Release and acquisition

The upload is distribution, not demand. Test three honest messages with real phone footage: "Three-minute rooftop team battles", "Build a shortcut your enemies can steal", and "Online with friends. Offline on the journey." The last message is permitted only after the travel experience is shipped and verified.

Compare which message brings retained players, not just views or installs. Store screenshots must show the real game, not concept art. Start one concentrated region and play window to protect queue quality. Measure available server capacity, actual concurrency peaks, bandwidth and cost before growing traffic; free-tier availability is not a capacity promise. Keep ads and purchase work separate from the experiment proving that people enjoy playing.

## Copyable next implementation prompt

```text
Implement package 1 of docs/HOTDROP_PRODUCT_AMENDMENT_2026-10-01.md against
the current HOTDROP checkout. Read the authoritative spec, this amendment,
latest QA/deployment records and applicable repository instructions first.

Preserve existing uncommitted body-rig, prefab, scene, HUD and evidence work.
Record the exact source/build baseline. Use one active Unity build owner.
Recheck old QA findings against current source before attempting repairs.

Deliver one coherent real-phone reference experience: a home screen with
one primary Play action and a clear offline option; representative grounded
humanoid presentation and one dressed Switchyard section; comfortable
two-thumb controls; usable persistent settings; readable combat feedback;
and the full first-run, match, results, rematch and Home journey.

Finish existing body-choice work rather than duplicating it. Keep rig
geometry parity. Use original or verified commercial-compatible assets.
Use existing audio, HUD and gameplay systems wherever they are sound.
Keep current gameplay balance unchanged unless a separate measured defect
requires a documented change. Do not add crouch or automatic leaning.

If implementing Focus Aim, it initially changes camera and sensitivity only;
prove shooting legality and touch ownership are unchanged. Show disabled
or unavailable settings honestly. Do not present mocks as implemented
online features. Do not implement contracts, mastery, monetization,
battle royale or new infrastructure during this package.

Deliver a source-mapped APK and hashes, appropriate regression results,
real-phone recording of first-run and two complete matches, settings
persistence evidence, performance observations and precise remaining gaps.
Do not certify a gate or claim retention from tests or screenshots.
Stop for review of the reference experience before producing more art.
```
