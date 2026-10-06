# Design Bible implementation tracker

Status recorded 6 October 2026. This is a working checklist, not a completion
claim. Recorded test baseline: EditMode 374/374 and PlayMode 32/34. The two
PlayMode failures are
`UiPhysicalSizeTests` (Menus and TutorialAndMatchHud): some design-sized text and
controls fell below the 12 dp / 48 dp floor on the A06 profile. This pass makes
the shared floor account for nested transform scale and display-density changes;
the existing A06 checks still need to be rerun to determine whether any screen
requires responsive reflow to preserve both legibility and containment. Device builds have not been run for this pass;
platform-specific Android/iOS haptics and physical-device behavior remain
unverified. This turn's Unity runtime C# compilation succeeded with 0 errors
and 41 existing warnings; the backend build succeeded with 0 errors (one
existing nullable warning and a NuGet vulnerability-feed warning). A fresh
Unity Editor batch recompile could not run because licensing initialization
shut the Editor down. No tests or device checks were run for the reward reveal.

Recent implementation slices:

- Screen 08's active Relay HUD uses the exact 12/17/22 dp typography tokens
  across HUD panels, touch-control captions, world objective text, and
  live/death feedback. Acceptance and remaining visual review are recorded in
  `docs/acceptance/PART5_HUD_TYPE_SCALE.md`.
- Match Search now uses 12/17/22 dp text roles for all search-screen copy while
  retaining current queue content and actions. Existing 16:9/20:9 fixtures
  were not run; acceptance is recorded in
  `docs/acceptance/PART5_MATCH_SEARCH_TYPE_SCALE.md`.
- Preparation now uses the same 12/17/22 dp roles for weapon cards, live
  objective, countdown, squad chips, and safety copy. Config-sourced weapon
  values remain unchanged; its captures and stat-containment check were not
  run. See `docs/acceptance/PART5_PREPARATION_TYPE_SCALE.md`.
- Results now uses only its 12/17/30 dp caption, value, and outcome roles;
  scoreboard values and player names remain legible at the configured body
  size. Existing journey/session-lifetime coverage was read but not run;
  16:9/20:9 review remains open. See
  `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.
- Appearance Locker text now uses only the 12/17/30 dp caption, UI, and title
  roles while retaining existing inventory, equip, retry, and disclosure
  behavior. Navigation/category and visual-capture coverage was read but not
  run; long-name/20:9 and profile-contrast review remain open. See
  `docs/acceptance/PART5_APPEARANCE_TYPE_SCALE.md`.
- The training instruction card now uses 12/17/22 dp caption, body, and title
  roles. Existing tutorial progression, capture, and physical-size coverage
  was read but not run; in-game occlusion and safe-area review remain open.
  See `docs/acceptance/PART5_TUTORIAL_TYPE_SCALE.md`.
- Contracts and Mastery now use only 12/17/30 dp caption, body/UI, and title
  roles, including removing inline size overrides. Feature-gate/read-only and
  16:9/20:9 capture coverage was read but not run. Reward-catalog enablement
  remains gated. See `docs/acceptance/PART5_GOALS_TYPE_SCALE.md`.
- Offline Challenges now use only 12/17/30 dp caption, body/UI, and title
  roles across the library, detail, local record, and practice panels. Existing
  local-save and runtime-flow coverage was read but not run; layout review is
  open at both landscape ratios. See
  `docs/acceptance/PART5_OFFLINE_CHALLENGE_TYPE_SCALE.md`.
- Profile summary and expanded details now use 12/17/30 dp roles. The level
  label has no inline font override; its permanent-guarantee body is 17 dp
  with room to wrap. Navigation/capture coverage was read but not run. See
  `docs/acceptance/PART5_PROFILE_TYPE_SCALE.md`.
- Party Room entry and active-room text now use only 12/17/22 dp caption,
  body/action, and compact-title roles. Invite-code, readiness, membership,
  and leader queue behavior are unchanged. Existing entry/layout/physical-size
  coverage was read but not run; 16:9/20:9 fit and A/B/C contrast remain open.
  See `docs/acceptance/PART5_PARTY_TYPE_SCALE.md`.
- Support, account recovery, reports, compliance, and account deletion now use
  named 12/17/22 dp roles, including density-aware input text. The two-step
  deletion and report actions are unchanged. Navigation/deletion and physical
  size coverage was read but not run; 16:9/20:9 layout and A/B/C contrast
  review remain open. See `docs/acceptance/PART5_SUPPORT_TYPE_SCALE.md`.
- Support now distinguishes account-service and wallet-load failures from
  loading, provides an in-page retry, and withholds guest/link/recovery actions
  until profile state is known. Reporting and web deletion stay available.
  Runtime C# compilation succeeded; no tests or device/network checks were run.
  Unity layout and A/B/C review and localization remain open. See
  `docs/acceptance/PART5_SUPPORT_ACCOUNT_LOAD_STATES.md`.
- Appearance now labels equipment and level data as unverified when inventory
  loaded but the profile lookup failed; the displayed choice is identified as
  the last locally saved appearance. Unity visual review remains open. See
  `docs/acceptance/PART5_APPEARANCE_ACCOUNT_LOAD_STATE.md`.
- Profile and Appearance failed loads now show a reachable 52 px footer RETRY
  (the shared retry row was hidden under their full-panel visuals), and Profile
  placeholders read "unavailable" rather than "loading". Cancelled online
  matches no longer claim "server verified" or wait forever for a receipt.
  Ineligible or unconfirmed receipts replace the "waiting" save note. Party Room
  labels a failed background refresh as stale until it recovers. Runtime,
  Network, and test assemblies compile with 0 errors; no tests, Unity captures,
  or device/network checks were run. Contracts/Mastery re-fetch retry
  visibility remains open while that feature is disabled. See
  `docs/acceptance/PART5_PRODUCT_LOAD_FAILURE_STATES.md`.
- Home, the first-run rig choice, startup, and settings/pause generators now
  use 12/17/22 dp roles. The graphics-note auto-size cap observes the 12 dp
  floor and the passive-HUD sample no longer uses inline size markup. Existing
  capture and physical-size source was read but not run; 16:9/20:9 reflow,
  A/B/C contrast, and larger-text visual review remain open. See
  `docs/acceptance/PART5_HOME_STARTUP_SETTINGS_TYPE_SCALE.md`.
- A level-5-or-higher guest now sees a one-time account-link prompt in More
  when Google Play linking is available, routed to the existing Account flow.
  Account-state navigation coverage was read but not run; Android/device
  verification remains open. The first-purchase trigger is gated while
  purchases are disabled. A failed profile lookup now says eligibility could
  not be checked and allows the next More open to retry. See
  `docs/acceptance/PART1_ACCOUNT_LINK_REMINDER.md`.
- Disconnect-to-bot grace now comes from `ProgressionConfig` with the existing
  five-second value unchanged. Existing reconnect authority coverage was read
  but not run; multi-client and mobile background/reconnect acceptance remain
  open. See `docs/acceptance/PART1_RECONNECT_POLICY.md`.
- Backend commerce remains a scaffold: `/api/commerce/verify-purchase` records
  only `pending_verification`, does not validate a store receipt or grant an
  item, and purchases default disabled. Receipt acceptance needs configured
  publisher verification and store/legal release setup; do not enable the
  purchase flag before that lifecycle exists.

| Area | Current implementation | Remaining work |
|---|---|---|
| Part 0 / A0 identity | A, B, and C selectable UI plus procedural Switchyard world profiles; switching updates role-based arena and avatar team tints, sky, ambient light, sun, team-neutral West/Center/East zone-ring and roof-trim colors, and landmark glazing/sign color without changing gameplay geometry or collision. Legacy menu hex colors resolve through the selected profile at use time. | Production world meshes/textures, profile post-processing, and final visual review remain. B values and the initial zone tones need art/colorblind review. |
| Part 1 / A5 lifecycle | Existing Home, queue/practice, match, result, and account flows remain. Bible text now identifies its ready-check/fly-through/team-intro phases as proposed rather than shipped. Current Relay respawns are documented from code/config: individual 4-second delay, team spawn exit, dead-state weapon selection for next spawn, and up to 1.5 seconds' protection ending on fire or leaving the spawn room. The backend publishes protocol and availability status; startup UI reads minimum-version and maintenance/queue-pause signals; Home polls availability every 30 seconds and labels states separately even for guests. Home now shows operator-supplied maintenance/queue-pause copy, UTC ETA, and optional compensation note in its status card (`docs/acceptance/PART1_MAINTENANCE_NOTICE.md`). Queue polling retries transient errors with capped exponential backoff; Practice is available from the first active-search frame and cancels the server ticket before opening offline play. Maintenance prevents new tickets and allocations while already matched tickets can join. A 426 response now carries a platform-specific configured HTTPS store link; startup shows an update action when present and leaves offline play available. Queue status shows the allocator-assigned region; Fusion RTT is measured only after joining. | Release Android/iOS store URLs are not yet configured; optional resumable download remains (Addressables, remote catalog, and asset hosting are absent from the project); pre-match measured region selection/manual override needs additional regions; season rollover has no season service yet; age and ban flows need approved product/legal policy. The Level 5 guest-link reminder is implemented; the first-purchase trigger is dormant while purchases are gated. The allocator config and backend expose only one operational Fusion region (`eu`), so a working pre-match selector needs additional provisioned regions and regional allocation pools. The server reconnects a reserved account to the same avatar until match completion (or final AFK removal) and substitutes a labeled bot after the configured 5-second grace; each 90-second join JWT is refreshed and does not define the reconnect window. Mobile background and network behavior remain unverified. |
| Part 2 / A2 palette | UI token roles split primary action from ally and support three selectable profiles. Legacy hex-defined screens plus Match Search, Preparation, Appearance Locker, Contracts, Mastery, Offline Challenges, Support, Party Room, Results, Profile, and status/error surfaces map semantic colors to the active A/B/C palette; `UiDraw.Hex` resolves literal semantic roles from all profiles and live TMP rich-text tags remap on profile changes. Destructive actions use neutral surfaces with warning framing; enemy coral remains for enemy identity and damage feedback. Neutral blueprint/weapon/operative/challenge image tints and bronze/silver/gold award markers remain content-specific. | Verify cross-profile contrast and color-vision distinction, especially destructive controls, offline/error states, and defeat presentation. Production world assets remain separately gated. Source-role audit acceptance is recorded in `docs/acceptance/PART2_ENEMY_SIGNAL_ROLE.md`. |
| Part 3 / A3 combat | Pad-launch weapon lockout is configurable and applied offline and online. ARC, ZIP, and BRICK now use authored eight-shot recoil curves in config; offline and online local camera feedback read the same per-weapon data. Offline and authoritative fire classify grounded moving spread state using the shared 0.5 m/s MovementConfig threshold. Headshot and elimination cues have distinct marker shapes. | Tune recoil in playtests; weapon reference/portrait/mesh set, layered reload/audio, blood levels and regional enforcement. |
| Part 4 map | Current Relay Switchyard and map config remain the baseline; West, Center, and East objective rings/beacons plus north/south roof-edge strips use profile-specific team-neutral tones and update when the presentation profile changes. Procedural collider-free landmark prototypes add a slow rotating crane, an arena-facing “SWITCHYARD” sign, and a translucent skybridge silhouette beyond the north boundary; these do not alter gameplay geometry, collision, or navigation. | Replace prototype landmark visuals with reviewed production art; verify color/contrast, landmark visibility, transparency, safe-area composition, reachability, and spawn/rotation timing in map playtests; fall/out-of-bounds punishment remains an undecided gameplay rule. |
| Part 5 / A8 UI | Ally chevron and enemy diamond cues are shape-distinct; core tokens now carry corrected type, radius, motion, and action/ally roles. Online, practice, and training body-hit, headshot, and elimination feedback use X, Material Symbols diamond, and star markers respectively. Every touch action hit region now retains the 48 dp floor even when a player customizes button scale; drawn controls keep their design size. Cockpit Mode fades roster/radar during nearby combat. Radar now draws map bounds, spawn/building footprints, elevated platform cues, and validated team pings; ping lifetime/cooldown are in MatchRulesConfig. Part 5.1 copy pass: `//` separators and lore jargon removed from player-facing strings across Contracts, Mastery, Profile ("Your pilot record", "Pilot name"), Appearance ("Cosmetic only. Zero gameplay effect."), Shop (red guard banner → neutral "Preview" strip, "Own it forever. No duplicates."), Results, Search, Preparation, Support, offline challenges and pause settings; the HUD armor label now reads "Suit Integrity" (3.4). Results now count up authoritative XP/credits, animate the per-level XP meter from persisted pre/post match bounds, and play one skippable ring burst without delaying the sticky Play Again action. The Appearance locker now distinguishes loading, unavailable, and empty inventory; with no owned wraps it shows the next server-configured public-match level unlock instead of a dead-end count. Shared MenuLayout and the settings console now use 200 ms ease-out parallax entry/exit; Reduce Motion selects opacity-only transitions. Product, Home/startup, runner-choice, online-status, settings, offline-challenge, Preparation, Results, and Match Search entry/exit motion are implemented; reusable roots restore correctly when reopened during an exit. | Profile hierarchy now presents the level and two career summary stats first; remaining stats, credits, offline accomplishments, loadout, and record expand on demand. A `profile_details_toggled` event records only expanded state. Toggle settings use 56 px rows with sentence-case names, one-line descriptions, right-aligned switches, and ellipsized overflow while preserving the 48 px switch hit area. Appearance technical claims now sit behind a 48 px disclosure; the cosmetic-only guarantee and equipped item remain visible. `appearance_tech_specs_toggled` records only expanded state. The UI-script typography migration is token-driven with no numeric font-size arguments, direct numeric TMP font-size assignments, or inline size tags found in the audit. Continue completing remaining screen hierarchy and truthful empty/loading/error/offline states; inspect hierarchy, disclosure, icon rendering, and contrast in Unity at 16:9 and 20:9. Contracts/Mastery and Appearance, Results, and Training type-scale changes are recorded in `docs/acceptance/PART5_*_TYPE_SCALE.md`. A contract-specific wrap card still awaits an enabled contract reward catalog (`ContractsMasteryEnabled` is false and `rewardCatalogComplete` is false); do not promise contract rewards until backend configuration provides it. Unity inspection of hierarchy, transitions, settings rows, appearance disclosure, icon rendering, and contrast remains unrun at 16:9/20:9. Match Search refreshes its timer, region, party labels, and actions in place without restarting entry motion. See `docs/acceptance/PART5_MENU_SCREEN_MOTION.md`. |
| Part 6 audio | Master, Effects, Music, and Voice levels are independently adjustable; the previous Announcer preference migrates to Voice. Current Voice-category match direction, zone and result callouts are synthesized tones; spoken voice assets are not present. ARC, ZIP, and BRICK reports now layer each weapon's generated body/transient clip with a distinct synthetic mechanism transient and environment tail on one spatial source, sharing linear distance rolloff and occlusion low-pass. In-match captions announce nearby pad launch, arm, charging, deployment and destroy cues, objective-zone activation/contest/score milestones, rate-limited distant ARC/ZIP/BRICK fire with clear line of sight, and flight entry. New local non-spatial wind and cloth loops smoothly follow offline and networked local planar/vertical velocity, increase level with speed, raise pitch during dives, and honor Effects volume. Mono output is selectable through Unity's `AudioSettings.Reset` configuration; haptic intensity is adjustable and routed to Android amplitude control and iOS intensity feedback with an older-iOS vibration fallback. | Replace generated weapon layers and synthetic callout tones with reviewed production assets; decide and implement quick-chat/threat-callout content if those systems enter scope; validate remaining critical-caption coverage, distance/mix tuning, and adaptive phase stems. Mono, haptic strength, loop mix, and device behavior need physical-device acceptance. |
| Part 7 accessibility/performance | Three frame/render-scale presets; Performance applies configured Bloom, Motion Blur, and Depth of Field overrides, one-step global texture mip reduction, reduced LOD bias, a lower maximum LOD level, configurable real-time shadow disablement, and per-effect particle caps plus shorter/smaller procedural particles; persistent Reduce Motion removes weapon camera kick and active shake while retaining manual look input; larger text applies to active labels and screens opened afterward via shared UI, HUD, control-caption, and concept-shop text factories; aim-assist Settings disclose the exact enabled slowdown (0.70x, 3-degree cone, 25 m, clear line of sight, no snap/tracking) and OFF state; camera shake, ping mute, gameplay captions, cockpit, handedness, sensitivity, vibration, mono output, and adjustable Android/iOS haptics are exposed. Design dp now converts to canvas units using current canvas scale and display density; shared text, selectable floors, and generated UI reapply the 12 dp / 48 dp requirements on the active device scale and nested transform scale. Runtime C# compiles in default, Android, and iOS conditional builds. | Re-run the existing physical-size acceptance and inspect reflow at 16:9/20:9 and on devices; the shared floor was updated for nested scaling and display changes, but its A06 baseline has not been rerun. Verify frame/memory and audio/haptic behavior on devices; native iOS plugin build remains unverified. SSAO is not configured as a volume effect; baked-lighting authoring and colorblind visual review remain. A true one-handed layout needs an owner-defined interaction model because current input requires simultaneous movement, aim, and fire across separate zones; detail is recorded in `PART7_ONE_HANDED_CONTROL_DECISION.md`. An aim-assist strength range remains undefined and unimplemented. |
| Part 8 / A4 tech | Fusion and dedicated-server baseline; server owns damage, match facts, and backend grants. Fusion project config currently sets 150 ms hitbox history, below the proposed 200 ms cap. Backend enforces minimum client protocol and exposes client availability; maintenance pauses queue allocation server-side. Online matches now sample measured Fusion RTT every two seconds and report bounded latency-bucket counts with completion telemetry. Movement intent rejects non-finite axes before the authoritative movement simulator and clamps finite input to unit magnitude. Shared server fire cadence rejects non-finite or non-positive simulation and weapon intervals. Player/pad damage and related elimination/pad-fact mutations guard state authority, runner ownership, and finite positive damage; server pad-event facts are exposed read-only, and respawn/substitution/weapon-time updates guard authority. The 60-per-player match placement safeguard is explicit in PadConfig, separate from the simultaneous arena pad cap. Client camera aim-ray distance and server submitted-point clamp now share MatchRulesConfig values. A server-only replay foundation samples authoritative position, velocity, alive state, weapon, shot/hit/headshot counters, and sanitized inputs at 10 Hz, bounded by configured match duration plus maximum overtime (`docs/acceptance/PART8_SERVER_REPLAY_CAPTURE.md`). Performance reduces texture mip and mesh detail and applies config-driven particle caps/lifetime/size reductions as low-tier measures. | The 1.5 GB target remains unverified; measure on representative 3–4 GB phones and tune from measured frame/memory data. Analyze accumulated RTT distributions before changing the rewind window; network spike/release checks, integrity/emulator controls, and receipt-validation acceptance remain. Replay persistence, reviewer access, kill-cam/highlight playback, and anti-cheat tooling need replay data ownership, access, and retention rules before extension. Damage authority guards and acceptance are recorded in `docs/acceptance/PART8_DAMAGE_STATE_AUTHORITY.md`; movement-input sanitization acceptance is recorded in `docs/acceptance/PART8_MOVEMENT_INPUT_SANITIZATION.md`; progression configuration fails fast on invalid thresholds, negative rewards, and malformed level unlocks (`docs/acceptance/PART8_PROGRESSION_CONFIG.md`); concurrent duplicate match-result conflicts return success only after all participant receipts are confirmed durable (`docs/acceptance/PART8_MATCH_RESULT_IDEMPOTENCY.md`); the per-player pad placement safeguard is separately configured (`docs/acceptance/PART8_PAD_PLACEMENT_CAP.md`); the shared camera aim-ray limits are config-backed (`docs/acceptance/PART8_AIM_RAY_CONFIG.md`); offline/network grounded weapon spread-state classification reads one threshold (`docs/acceptance/PART8_SHARED_WEAPON_MOVEMENT_STATE.md`). Automated regression and concurrent database coverage for these items was not added or run. |
| Part 9 no distractions | Cockpit Mode combat fade added. Persistent Stream-Safe Display masks account aliases, progression/currency, participant names and party invite codes across Home, account/profile/support, party/search, preparation/HUD/results and report UI; network/account identity and backend records remain unchanged. Current party flow is invite-code entry only: there is no incoming social-invite channel or mid-match social popup to defer. No app-wide notification feed/badge system currently exists, so the two-badge cap and expiring-only notification rule have no consumer to apply to. Results use one 1.15 s skippable reward reveal; Play Again remains available throughout. | If incoming social invitations are added, queue them silently through results. If a notification feed is added, permit badges only for items expiring within 24 hours and cap visible badges at two app-wide. Validate stream-safe capture surfaces and reward motion in game. |
| Part 10 monetization | Store and ads are gated in current build; server commerce code exists. | Owner business confirmation on no ads and legal review of billing/regions; do not enable purchase behavior on this pass. |
| Part 11 delivery gates | Implementation order is represented by the specs and this tracker. | Team/budget plan, playtest gates, six-client network run, phone journey, signed build, and soft-launch baselines. |
| Part 12 / A6 agent inputs | Root `AGENTS.md` plus 00–06 focused design specs added. | Acceptance and telemetry are defined before profile, settings-row, and appearance-disclosure work. A/B/C palette acceptance records are defined for Match Search, Preparation, Appearance Locker, Contracts/Mastery, Offline Challenges, Support, Party Room, Results, Profile, and native palette aliases in `docs/acceptance/PART2_*.md`; density-aware physical UI sizing is recorded in `docs/acceptance/PART7_PHYSICAL_UI_SCALE.md`. Repeat this gate for future feature work. |
| A7 localization | Prerequisite documented in `06_TECH.md`. | Unity Localization package/tables, Urdu RTL shaping, Urdu and Devanagari fonts/fallbacks, and migration of existing literal UI copy. |
