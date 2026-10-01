# MERIDIAN QA — Project UPDRAFT audit

## 1. EXECUTIVE VERDICT: NOT-READY

The checkout does not establish a shippable online mobile shooter. Required online resource assets are absent, product screens can remove every exit, party members lack a complete joining journey, and online combat omits feedback essential to understanding a fight. The available evidence supports portions of an offline prototype, not certification of the current source, six-player networking, retention, performance across the required device matrix, or commercial launch compliance. These are separate deficiencies: accepting an offline gate does not accept an online release. Most findings below are source-proven contract failures or explicitly bounded static risks; none is represented as a newly executed gameplay reproduction.

### Audit boundary and evidence rules

- Audited on 2026-09-29, repository `E:\Usama\Game`, base HEAD `2596e9274c2004ceb03784c4dea50292ed43ec14`, including existing uncommitted changes. This is a working-tree audit, not certification of that commit or an APK.
- Initial process check found no running `Unity` or `UnityHub` processes. No Unity launch, build, test execution, gameplay session, deployment, or implementation edit was performed. This report is the only file written.
- Read order: master specification; requested acceptance files; milestone status; license recovery/known issues; Unity and backend sources. `docs/acceptance/STEP_A.md` is absent. Historical `STEP_1.md` exists but is not silently substituted for the requested accepted gate. `STEP_B.md` exists. `docs/known-issues.md` is absent. `docs/UNITY_LICENSE_RECOVERY.md` exists.
- The supplied master is labeled **v1.0**. No v1.1 amendment was found in the repository's filenames or Markdown search. Amendment compliance is **UNVERIFIABLE**, not presumed satisfied. U01 specifies the required reconciliation.
- Source citations below use `S/` = `E:\Usama\Game\ProjectUpdraft\Assets\Scripts\`, `B/` = `E:\Usama\Game\Backend\UpdraftBackend\`, and `R/` = `E:\Usama\Game\`. Line numbers refer to the reviewed working tree. Asset/source absence searches included hidden and ignored files beneath `ProjectUpdraft/Assets`.
- **PASS (static)** means a named source rule was traced; it does not mean a device passed. **FAIL (static)** refers to a numbered finding. **UNVERIFIABLE** requires the numbered human procedure. Every severity is a prioritization of the described consequence, not a claim of runtime reproduction.
- No independently reproduced runtime P0 is asserted. The two P0s are static release blockers. The prediction finding concerns visual feedback, not demonstrated divergent authoritative damage: calling that a proven P0 simulation desynchronization would exceed the evidence.

### Evidence available, and its limits

`R/docs/acceptance/STEP_B.md` claims 142 EditMode and 6 PlayMode passes. `R/ProjectUpdraft/TestResults/PlayMode.xml` records 6/6 passes, 2026-09-29 06:09:10–06:10:59 UTC. The requested current `EditMode.xml` is absent; `EditMode2.xml` records 41/41 passes on 2026-09-28. These are read artifacts, not reruns or proof of coverage for the current dirty tree. Older failing touch XML files are not treated as current regressions. An older `EditMode.log` compilation failure in `BuildLinuxServer` is also not reported as a present compile failure: the current source has the cast and later Linux build evidence exists.

The accepted B APK is reported as 87,428,019 bytes, SHA-256 `1b1c6c6ed1acf9cd69ca451b5bd98861ab45a4445c137dd5f7f3b0d849b60dbf`. Historical STEP_1 evidence concerns a different APK. Phone screenshots `M01_buttons.png` and `M05_pause2.png` under `R/ProjectUpdraft/Builds/AcceptanceStep1/` were visually inspected; they establish historical appearance only. No current APK/source mapping was established.

`R/docs/acceptance/STEP_C.md` records a Linux server build, not a completed six-client runtime gate. `R/ProjectUpdraft/MILESTONE_STATUS.md` leaves device repairs, online verification, validation study, product/backend integration, commercial and release gates unfinished. Those limitations govern this verdict even where individual source rules pass.

## 2. Severity table

Counts are unique findings, not failed coverage rows; cross-cutting issues are counted once in their primary area. Unverifiable items and design proposals are not counted as defects.

| Primary area | P0 | P1 | P2 | P3 | Total |
|---|---:|---:|---:|---:|---:|
| A — FTUE | 0 | 1 | 0 | 0 | 1 |
| B — Controls | 0 | 2 | 1 | 0 | 3 |
| C — Movement/pads | 0 | 2 | 0 | 0 | 2 |
| D — Combat | 0 | 6 | 2 | 0 | 8 |
| E — Match flow | 1 | 5 | 0 | 0 | 6 |
| F — UI/UX | 1 | 4 | 0 | 0 | 5 |
| G — Bots | 0 | 1 | 0 | 0 | 1 |
| H — Performance/evidence | 0 | 1 | 2 | 0 | 3 |
| I — Accessibility | 0 | 2 | 0 | 0 | 2 |
| J — Retention | 0 | 1 | 0 | 0 | 1 |
| K — Safety/compliance | 0 | 3 | 0 | 0 | 3 |
| **Total** | **2** | **28** | **5** | **0** | **35** |

## 3. Findings, P0 → P3

### P0

**[QA-001] [P0] [E — Online entry] [S/Network/NetSession.cs:135–148,175; S/Network/NetMatch.cs:293; S/Config/BackendConfig.cs, Load]**

**Expected:** a configured checkout supplies the network match/player/pad prefabs and backend configuration needed to enter and play an online match. **Observed:** the resource lookups `NetPrefabs/NetMatch`, `NetPrefabs/NetPlayer`, and `NetPrefabs/NetPad` have no corresponding assets anywhere under Assets. `Resources/Config/BackendConfig.asset` is also absent. The existing online-availability check may explain unavailability to the user; it cannot supply the missing game objects. **Evidence: STATIC-ANALYSIS-ONLY.** Resource names, repository absence search and spawn callsites establish the assembly gap; no client was launched. Human reproduction: U04, including documenting any externally supplied assets before testing. Do not interpret this as proof that no other developer has a separately configured build.

**[QA-002] [P0] [F — Navigation dead end] [S/Core/PlayerProductScreens.cs:33,82–106,192–205,220–225,255–260]**

**Expected:** Party and account-deletion outcomes retain Home/Back or another usable exit. **Observed:** every button, including BACK, is named `Row_Button`; `ClearRows` destroys every `Row_` child except status. Rendering a party removes BACK. Successfully leaving that party clears its remaining actions. Successful deletion does the same. The full-screen panel remains, and `Close` is reachable through a button already destroyed. **Evidence: STATIC-ANALYSIS-ONLY, direct object-name/deletion trace.** Repro: open Party → create/join → leave; separately Support → delete → confirm success. Attempt to return Home without restarting. Verify U05. A user should never have to kill the application to leave an ordinary product screen.

### P1

**[QA-003] [P1] [B — Touch ownership] [S/Input/TouchInputManager.cs:198–230]**

**Expected:** touch ownership persists by finger ID until its Ended/Cancelled event, regardless of its current screen half. **Observed:** routing between movement and aim depends on the touch's current position. A movement finger that crosses into the aim half and releases is routed to the aim handler, which does not own it. The movement owner/input can remain latched. The converse can strand an aim owner. **Evidence: STATIC-ANALYSIS-ONLY.** Repro U03: hold forward on the left, drag across the midpoint, release on the right; repeat mirrored and with aim. This is different from, and does not prove, UI touches rotating the camera.

**[QA-004] [P1] [B — Camera collision] [S/Camera/ThirdPersonCamera.cs:136–150]**

**Expected:** the camera remains on the safe side of an obstruction after a sphere cast. **Observed:** on collision the desired position is `hit.point + direction.normalized * sphereCastRadius`, offsetting in the direction into/beyond the wall rather than locating the swept sphere on its approach side. Follow smoothing and shake occur without a final corrective sweep; initial overlap and lateral shoulder relocation also need coverage. **Evidence: STATIC-ANALYSIS-ONLY, geometry risk with explicit erroneous offset direction.** U06 must establish actual clipping on thin walls, corners and moving blockers; no through-wall exploit was executed.

**[QA-005] [P1] [C — Pad placement envelope] [S/Pads/PadPlacementRules.cs:58–69]**

**Expected:** the entire pad footprint is supported and the player's immediate launch path has adequate clearance. **Observed:** center/eight perimeter rays cannot establish continuous support between samples; a notch or hole between rays can pass. The overhead probe sweeps a radius-0.35 sphere starting only 0.5 m above the pad along a straight initial launch vector, not the full 1.8 m player envelope. A head-height obstruction can lie outside that probe. **Evidence: STATIC-ANALYSIS-ONLY, bounded validation gaps; not a claim that a particular shipped map location currently exploits them.** U07 supplies exact adversarial geometry tests.

**[QA-006] [P1] [C — Online pad feedback] [S/Player/PlayerController.cs:769–800; S/Network/NetClientPresenter.cs; S/Network/NetPlayer.cs:302–315]**

**Expected:** holding placement online previews the pad and explains invalid placement. **Observed:** preview implementation is on the offline PlayerController path. The online presenter does not reproduce it, and the server placement rejection returns without a player-facing rejection reason. **Evidence: STATIC-ANALYSIS-ONLY, offline/online call-path comparison.** U07: hold Pad, aim into a spawn exclusion, slope and zone, then release; record preview, reason and charge state independently.

**[QA-007] [P1] [D — Spawn protection geometry] [S/Player/PlayerController.cs:400–405; S/Config/MapConfig.cs]**

**Expected:** the full rectangular spawn room is recognized for firing/protection restrictions. **Observed:** the offline check converts the Vector2 room size to Vector3 and tests its `z` half-extent. Vector2→Vector3 supplies zero z, so only the room center plane is recognized on that axis. The online MapConfig check uses the second Vector2 component correctly. **Evidence: STATIC-ANALYSIS-ONLY, type-conversion and comparison trace.** U08: fire from several positions inside the room, compare protection with the center plane and online. This does not claim bullets pass through the spawn barrier; barrier collision is a separate system.

**[QA-008] [P1] [D — Predicted shot presentation] [S/Network/NetPlayer.cs:264–279; S/Network/NetMatch.cs:183–216; S/Weapons/ShotResolver.cs:32–62]**

**Expected:** predicted bullet/pellet feedback uses the authoritative seed, spread, movement state and weapon values. **Observed:** the server queues the seeded spread shot; input authority instead raycasts the unspread aim direction and emits one endpoint. BRICK presents one straight ray while authority resolves eight spread pellets. A shared seed comment is not a shared execution path. **Evidence: STATIC-ANALYSIS-ONLY.** U09 compares logged directions and visible tracers. This is a definite source mismatch in presentation, but a demonstrated client/server damage simulation desynchronization would be a separate **P0**. Client code does not currently predict damage, so this audit does not invent that result. Recoil is applied in the presentation callback; U09 must also establish server enforcement of weapon recoil/input legality rather than assume camera movement is authority.

**[QA-009] [P1] [D — Weapon cadence parity] [S/Weapons/WeaponState.cs:56–90; S/Network/NetPlayer.cs:247–249]**

**Expected:** offline and network weapon rates preserve configured rounds per minute. **Observed:** offline firing carries elapsed-time remainder; online resets a whole-tick timer from each shot. At a 30 Hz simulation rate, ARC's 0.125 s interval is 3.75 ticks and ZIP's 0.076923 s is 2.3077 ticks. A fixed rounded tick interval cannot preserve either mean rate without fractional accumulation. **Evidence: STATIC-ANALYSIS-ONLY.** Exact Fusion timer rounding was not established: do not treat a specific resulting RPM as measured. For illustration, ARC at four ticks is 450 RPM; ZIP at two/three ticks is 900/600 RPM. U09 measures actual cadence and TTK on the configured runner.

**[QA-010] [P1] [D — Online hit confirmation] [S/Network/NetClientPresenter.cs:281–282; S/Network/NetMatch.cs:183–216; S/Player/PlayerController.cs:638–640]**

**Expected:** an ordinary landed hit produces readable immediate confirmation. **Observed:** the online HUD calls `ShowHitConfirmed` when elimination count increases, not when a nonlethal shot lands. The authoritative damage path has no corresponding owner hit-feedback notification. Offline hits have a direct marker/sound call. **Evidence: STATIC-ANALYSIS-ONLY.** U10: land one ARC body shot on a healthy enemy, stop firing, and measure marker/sound latency; then compare an elimination. No claim of sub-100 ms latency is supported by existing evidence.

**[QA-011] [P1] [D — Online threat presentation] [S/Network/NetClientPresenter.cs:82–85; S/Network/NetVisuals.cs; S/Network/NetPlayer.cs; S/Network/NetPad.cs]**

**Expected:** remote/local fire, reload, footsteps, pad launch and death are legible, spatialized events. **Observed:** the local shot callback supplies recoil/tracer presentation without the complete fire-sound path. The reviewed network presentation lacks the equivalent offline remote shot/step/reload/pad cue wiring and a complete dead-avatar presentation transition. NetVisuals attaches primitive avatars; replicated state alone does not play an effect. **Evidence: STATIC-ANALYSIS-ONLY, missing presentation callsites.** U10 records every cue from both ends of a two-client fight. Do not use offline audio/video as online evidence.

**[QA-012] [P1] [D — Blocked-shot explanation] [S/Network/NetClientPresenter.cs:246–247; S/Player/PlayerController.cs:570–577,622]**

**Expected:** firing with an obstructed muzzle clearly explains why the shot fails. **Observed:** online warning requires a transition from unblocked to blocked while fire is already held. Aiming at cover first sets `shotBlocked`; firing afterward does not show the warning. Offline indicator collision also does not apply the shot resolver's ally-skipping rule, allowing a teammate to produce an inconsistent warning. **Evidence: STATIC-ANALYSIS-ONLY.** U10: aim at a wall, wait, fire; repeat through an ally and with muzzle touching thin cover. Initial-overlap/thin-cover behavior remains unverified.

**[QA-013] [P1] [A — Tutorial death explanation] [S/Tutorial/TutorialController.cs:101–108; S/UI/MatchHUD.cs:394–395]**

**Expected:** a new player dying in the tutorial bot fight receives a death explanation and respawn countdown. **Observed:** tutorial initialization lacks the regular match elimination/death HUD binding; the death panel also requires a match phase even though the tutorial HUD is initialized without a match. **Evidence: STATIC-ANALYSIS-ONLY.** U02: deliberately lose the final bot fight; record the entire four-second interval and controls until recovery.

**[QA-014] [P1] [E — Party joining] [S/Core/PlayerProductScreens.cs:46–113; S/Network/OnlinePlay.cs:70–114; B/Endpoints/MatchmakingEndpoints.cs:27–37]**

**Expected:** a ready party joins together after the leader queues. **Observed:** the backend creates member tickets when the leader enqueues, but nonleader clients have no party-screen subscription/poll-to-join path. Their ordinary Play path attempts enqueue, which is leader-only. Party rendering also removes the Refresh action along with the other rows. **Evidence: STATIC-ANALYSIS-ONLY.** U04: three accounts create/join/ready; only leader presses queue; observe each member's ticket consumption and transition. A backend party record is not evidence of a usable party flow.

**[QA-015] [P1] [E — Queue cancellation race] [S/Network/OnlinePlay.cs:70–83,231–246]**

**Expected:** Cancel removes any in-flight queue intent and cannot leave a ticket after returning Home. **Observed:** `searching` becomes true only after enqueue returns; Cancel sends backend cancellation only while this flag is true. Cancel during an awaited enqueue can return locally, then leave the newly created server ticket. The generic failure path also clears local state without a corresponding cancellation. **Evidence: STATIC-ANALYSIS-ONLY, asynchronous ordering analysis.** U11 delays the enqueue response and inspects tickets before/after cancellation; no phantom match was executed here.

**[QA-016] [P1] [E — Results lifetime] [S/Network/DedicatedServer.cs:100–102; S/Network/NetClientPresenter.cs:46–51; S/Network/NetSession.cs, Leave; S/UI/MatchHUD.cs, results callbacks]**

**Expected:** results and Home/Play Again remain usable for as long as the player reads them. **Observed:** the server leaves after a fixed 12-second results delay; presenter shutdown clears results state, while results actions are associated with the session presenter. This is a lifetime mismatch with a persistent results screen. **Evidence: STATIC-ANALYSIS-ONLY, teardown risk; the precise resulting screen/exception is not established.** U12 waits 20 and 60 seconds before pressing each action, including a party rematch.

**[QA-017] [P1] [E — Reward delivery and receipt] [S/Network/ServerBackend.cs:70–84; S/Network/DedicatedServer.cs:93–95; S/UI/MatchHUD.cs, results construction]**

**Expected:** eligible results show XP/credits/unlocks and retain a visible retry-pending state until authoritative delivery succeeds. **Observed:** server submission has bounded retries and then logs failure without a durable retry outbox. Results show match statistics without a complete authoritative reward receipt or pending-retry journey. **Evidence: STATIC-ANALYSIS-ONLY.** U12 drops the result endpoint through all retries, restores it after server teardown, and checks exactly one eventual grant and a truthful player receipt. Idempotent backend grants do not by themselves guarantee delivery.

**[QA-018] [P1] [E — AFK input accounting] [S/Network/NetPlayer.cs:135,177; S/Network/NetMatch.cs:251]**

**Expected:** aiming counts as activity under spec §18. **Observed:** `IsActivity` checks movement and button bits, not yaw/pitch changes. A stationary player actively tracking opponents can receive the 30-second warning and 45-second substitution. **Evidence: STATIC-ANALYSIS-ONLY.** U13 keeps aim moving while all buttons and movement remain idle for 50 seconds; verify warning, replacement and recovery.

**[QA-019] [P1] [F — Touch target dimensions] [S/Core/SceneSetup.cs:107–110; S/Core/OfflinePauseMenu.cs:167; S/UI/MatchHUD.cs:251–255, results controls; S/Core/PlayerProductScreens.cs:255–258; S/UI/TutorialUI.cs]**

**Expected:** every target, not only gameplay circles, is at least 48 dp after scaling. **Observed:** menus use fixed reference-canvas heights, not a dp floor. With the recorded 1600×720, 300 dpi phone and the 1920×1080, 0.5-match scaler, scale is approximately 0.745; 66 canvas units become 49.2 px, **26.2 dp**. An 80-unit Home control is 31.8 dp; a 90-unit result/tutorial control is 35.8 dp; 54-unit product rows are 21.5 dp. **Evidence: STATIC-ANALYSIS-ONLY for current dimensions, corroborated by the small historical settings screenshot.** U14 measures actual hit rectangles, including keyboard and safe-area effects. Gameplay's separate 48 dp clamp does not cover these menus.

**[QA-020] [P1] [F — Text legibility] [S/Core/PlayerProductScreens.cs:229–236,248,263; S/UI/MatchHUD.cs, text factories; S/Core/SceneSetup.cs, text setup]**

**Expected:** readable menu labels and combat information on the minimum device, including older eyes and outdoor light. **Observed:** 16–24 reference-unit text becomes approximately 6.4–9.5 dp on the documented A06 scaling; some auto-size floors are smaller. No player text-scale control or robust world-background contrast guarantee was found. The historical gameplay image shows bright HUD elements against bright terrain. **Evidence: STATIC-ANALYSIS-ONLY plus historical visual evidence, not a current sunlight measurement.** U14 measures rendered pixels, clipping and outdoor reading accuracy. Font dp calculations describe scale, not an assertion that dp and font sp are interchangeable under OS scaling.

**[QA-021] [P1] [I — Shake preference restoration] [S/Camera/ThirdPersonCamera.cs:105–109; S/Core/OfflinePauseMenu.cs:104, settings]**

**Expected:** shake disabled in settings remains disabled after restart. **Observed:** the setter writes `CameraShakeEnabled`; settings read it for display, but the camera's default enabled state does not load that preference. The UI can show OFF while the camera remains enabled. **Evidence: STATIC-ANALYSIS-ONLY, preference read/write search.** U15 toggles off, restarts and compares firing/landing camera motion.

**[QA-022] [P1] [F — SFX mute completeness] [S/Player/PlayerController.cs:850, PlaySound; S/Core/OfflinePauseMenu.cs:108, volume settings; S/Core/OriginalGameAudio.cs]**

**Expected:** SFX volume affects all gameplay sound effects. **Observed:** PlayerController's direct AudioSource sound path does not use the GameAudio SFX multiplier used by the procedural cue path. Master listener volume is separate and does not make the SFX control complete. **Evidence: STATIC-ANALYSIS-ONLY.** U15 sets SFX to zero with Master nonzero and exercises fire, hurt, steps, reload and pads; record which paths remain audible.

**[QA-023] [P1] [I — Team recognition without color] [S/Player/PlayerController.cs:835–845; S/Network/NetVisuals.cs, Attach]**

**Expected:** critical ally/enemy identity is not encoded solely by color. **Observed:** the reviewed avatar team distinction changes material color on otherwise matching geometry without a consistent non-color team marker/outline on the combatants. Zone text and pad ring/cross distinctions do not solve avatar identification. **Evidence: STATIC-ANALYSIS-ONLY, corroborated by the historical gameplay capture.** U14 tests grayscale and common color-vision simulations at combat distances. Online team-relative color assignment must also be checked for both local team IDs after spawn.

**[QA-024] [P1] [J — Cosmetic identity delivery] [S/Core/PlayerProductScreens.cs:131–145; S/Network/NetVisuals.cs; S/Player/PlayerController.cs, visual setup]**

**Expected:** Appearance previews owned items and equipped identity visibly appears in play. **Observed:** the screen explicitly lacks a preview; equip reaches the backend, but the reviewed avatar presentation does not consume equipped appearance to render the selection. Inventory rows also lack a scrolling layout for growth. **Evidence: STATIC-ANALYSIS-ONLY.** U16 equips contrasting items, restarts, enters offline/online where applicable, and has another client inspect the result. Inventory persistence alone is not delivered identity.

**[QA-025] [P1] [K — Player reporting usability] [S/Core/PlayerProductScreens.cs:175–185; S/Core/BackendClient.cs, ReportAsync; B/Endpoints/SupportEndpoints.cs:16–35,129]**

**Expected:** players can report a participant they encountered, with useful match context. **Observed:** the screen requires a raw player GUID; ordinary gameplay exposes aliases rather than a selectable participant reporting flow. The client call does not populate the endpoint's optional match ID. **Evidence: STATIC-ANALYSIS-ONLY.** U16 reports an opponent using only information visible in the app; no copying from developer logs or a database is allowed for this acceptance test.

**[QA-026] [P1] [K — Account recovery journey] [B/Endpoints/AuthEndpoints.cs:46–85; S/Core/PlayerProductScreens.cs, account screen]**

**Expected:** a previously linked player can recover the same account after reinstall/device change. **Observed:** linking operates on an existing authenticated guest; linking an identity already owned by another account returns conflict. No platform-identity-to-existing-account sign-in exchange was found, and the client product flow is not a completed platform login journey. **Evidence: STATIC-ANALYSIS-ONLY.** U17 must prove recovery of the original account ID, inventory and balance after deleting local state. Do not interpret this as permission to merge wallets automatically.

**[QA-027] [P1] [H — Analytics evidence integrity] [S/Core/Telemetry.cs:79–109; S/Network/OnlinePlay.cs:84,112,246; S/Core/BackendClient.cs, UploadTelemetryAsync; B/Endpoints/AnalyticsEndpoints.cs]**

**Expected:** events reach ingestion with correct online match/region/party/cohort/bot dimensions and agreed names. **Observed:** the client envelope hardcodes region `offline`; party size defaults to one without the necessary session updates; online event names include names outside ingestion's allowlist; `UploadTelemetryAsync` has no calling path in the reviewed Unity source. Substitution/tester fields are not all represented by the intake DTO. **Evidence: STATIC-ANALYSIS-ONLY.** Local JSONL is useful offline study evidence, but not a functioning online KPI pipeline. U18 compares emitted files, HTTP requests, stored rows and derived cohorts; D1/D7 cannot be accepted from absent or mixed context.

**[QA-028] [P1] [K — Web deletion completion] [B/Endpoints/SupportEndpoints.cs:55–79; B/Program.cs; R/docs, legal/deletion drafts]**

**Expected:** the advertised external deletion route can verify and complete requests, and tells users what actually happens. **Observed:** submission stores `pending_verification` and says to check email, but no corresponding verification sender/worker/completion journey was found. Publisher identity/contact/deletion URL drafts remain unresolved. **Evidence: STATIC-ANALYSIS-ONLY; not a legal conclusion.** U17 follows a real controlled request end to end. Google's policy requires both in-app and external initiation for applicable apps: [Google Play account deletion requirements](https://support.google.com/googleplay/android-developer/answer/13327111?hl=en), checked 2026-09-29. A form accepting an email does not demonstrate completion.

**[QA-029] [P1] [F — Matchmaking escape options] [S/Network/OnlinePlay.cs:18,93–114; specification §17]**

**Expected:** matchmaking shows elapsed time and region and offers the specified Practice path during a long wait. **Observed:** the searching screen shows elapsed time and Cancel, without region or a direct Practice action; server reservation waits can add up to 60 seconds. Cancel/Home is a recovery path, so this is not described as an unexplained spinner or an observed 60-second hang. **Evidence: STATIC-ANALYSIS-ONLY.** U11 measures the actual waits and counts taps into Practice, including the 45-second bot-fill boundary.

**[QA-033] [P1] [G — Substitution transparency] [S/Network/NetSession.cs:185; S/Network/NetMatch.cs:246–251; S/UI/MatchHUD.cs, participant names]**

**Expected:** disconnect/AFK substitutes remain visibly labeled bots. **Observed:** initial bots receive a bot alias, but replacement changes control state without the equivalent alias change, and the HUD displays the existing name. **Evidence: STATIC-ANALYSIS-ONLY.** U13 replaces a named human and inspects every participant/kill-feed label from all clients. Undisclosed substitution undermines both player trust and playtest interpretation.

### P2

**[QA-030] [P2] [B — Gyro calibration usability] [S/Input/TouchInputManager.cs:234–245,648–680; S/Core/OfflinePauseMenu.cs:104, settings]**

**Expected:** optional gyro can be set up predictably on the player's device. **Observed:** gyro is off by default and adds rate-based aim to touch, but no explicit calibration/recenter guidance or drift deadband workflow was found. **Evidence: STATIC-ANALYSIS-ONLY, usability gap rather than a measured sensor defect.** U03 tests a stationary phone and touch-plus-gyro; a 30-second rest must not be assumed drift-free from code alone.

**[QA-031] [P2] [D — Reload state readability] [S/UI/MatchHUD.cs:447–459; S/Network/NetClientPresenter.cs:271]**

**Expected:** a player knows that a manual partial-magazine reload is in progress. **Observed:** the HUD's reload wording is inferred from zero ammunition, not the actual reload state; online ammo text is then overwritten by the presenter. Partial reload can appear unexplained. **Evidence: STATIC-ANALYSIS-ONLY.** U10 reloads at half capacity and repeats during a launch and at zero ammo.

**[QA-032] [P2] [D — Death direction] [S/UI/MatchHUD.cs, death panel; S/Player/PlayerController.cs, elimination payload]**

**Expected:** the user can learn where the lethal attack came from without requiring a killcam. **Observed:** regular-match death information includes killer/weapon and respawn handling, but no complete attacker-direction explanation was found. **Evidence: STATIC-ANALYSIS-ONLY, experiential gap, not a demand for the intentionally excluded killcam.** U10 uses an unseen flank and asks a new player to identify its direction before respawn.

**[QA-034] [P2] [H — Bot lifetime retention] [S/Network/NetBotDriver.cs:31,39–42,155; S/Network/NetPlayer.cs, Despawned]**

**Expected:** per-player bot brains are released when players/sessions end. **Observed:** a static dictionary retains NetPlayer/Brain entries; `Forget` exists but no caller was found. A long-lived process hosting repeated sessions can retain past objects. **Evidence: STATIC-ANALYSIS-ONLY.** U19 compares retained object counts across 100 sessions. No measured memory growth or crash is asserted.

**[QA-035] [P2] [H — Core-loop allocations] [S/Weapons/ShotResolver.cs:32–62; S/Player/PlayerController.cs:570; S/UI/MatchHUD.cs, per-frame text; S/Input/TouchInputManager.cs:747]**

**Expected:** spec §23's allocation-free core loops. **Observed:** pellet arrays/random instances per shot, `RaycastAll` allocations, repeated formatted HUD strings and control-label splitting allocate in active play/presentation. **Evidence: STATIC-ANALYSIS-ONLY, allocation-producing operations; not a measured frame-time regression.** U19 profiles allocation sites and GC pauses under the maximum fight load. Uncalled VFX factories are not treated as active runtime cost.

### Coverage ledger — all requested checks

| Check | Verdict and evidence | Remaining human evidence |
|---|---|---|
| A1 cold start → first shot | **UNVERIFIABLE** as elapsed time. Bootstrap selects first-run tutorial, skip returns Home, Practice enters prep/countdown. No measured >5 s dead-air event established. §7 gives the actual conditional timeline. | U02 |
| A2 permissions before value | **PASS (static)** for no forced account, shop or notification request in bootstrap/tutorial/practice; offline play needs no backend account. OS/runtime SDK prompts are **UNVERIFIABLE**. | U02, U17 |
| A3 tutorial | **PASS (static)** nine completion-driven steps, one displayed instruction, Skip, explicit enemy-colored-pad use; eight one-second inter-step waits. 90 s is a target, not a forced duration. `TutorialController:135–175,382–434`, TutorialUI. | U02 for comprehension/time and all completion conditions |
| A4 first death | **FAIL:** QA-013; regular-match direction gap QA-032. | U02, U10 |
| B5 ownership/concurrency | **FAIL:** QA-003. Menu-consumed finger IDs prevent ordinary UI drag-through from becoming aim; fire drag intentionally aims. No proven P0 UI→camera leak. Separate owners support move/aim/fire/jump, but two physical thumbs cannot independently press four separate surfaces simultaneously. Fire-drag reduces required simultaneous contacts. | U03, including four-finger claw and OS cancellation |
| B6 joystick | **PASS (static)** fixed acquisition origin, 60 dp radius, 0.15 dead zone, magnitude clamp. Slides beyond radius retain full input in the same half; crossing the half has QA-003. No automatic recenter until next acquisition. Dead-zone exit jumps to approximately 15% rather than a remapped zero. | U03 for drift, release and feel |
| B7 fire/targets | **FAIL:** QA-019. Fire begins on press, holds, drag adds aim, release clears it; no release-triggered shot path found. Online uses sampled FireHeld without a press latch, so a sub-tick tap may disappear; exact loss rate is **UNVERIFIABLE**. Fire/jump nominal gap is 10 dp. A second Fire touch can replace its owner and needs adversarial testing. | U03, U09 |
| B8 aim assist | **PASS (static)** `AimAssistRules` and offline `PlayerController:582–606`/online presenter `131–173`: ≤3°, ≤25 m, visibility check, sensitivity multiplier 0.70, no target rotation assignment. This means **30% reduction**, not 70% reduction. Crowded/nonalloc-buffer and cover behavior remain **UNVERIFIABLE**. | U06, U20 |
| B9 gyro | Default OFF **PASS (static)**; additive touch/gyro intent traced; QA-030. | U03 |
| B10 camera | **FAIL:** QA-004. Masks cover Ground/Cover/SpawnBarrier; layers, initial overlaps and moving objects require proof. | U06 |
| B11 editor | **PASS (static)** move/scale/opacity, save/load and reset at `TouchInputManager:400–408,482–492,648–680`; gameplay hit sizes clamped ≥48 dp after scale. Overlap avoidance and after-load/aspect safe-area fit are **UNVERIFIABLE**; saved offsets can create unusable layouts. | U03, U14 |
| B12 left handed | **PASS (static)** move/aim halves and default control anchors mirrored (`TouchInputManager:422–439`). Custom offsets are not reflected and camera shoulder remains right; therefore not a complete visual mirror. Impact is **UNVERIFIABLE**, not automatically a spec defect. | U03 |
| C13 movement feel | Configured 6 m/s, 35 m/s², gravity 20 **PASS (static)**; feel **UNVERIFIABLE**. See calculations below. | U07, U20 |
| C14 jump/obstacles | Ideal jump height 0.9 m **PASS (static)**; getting onto an exactly 0.9 m obstacle is **UNVERIFIABLE** because clearance, controller skin and discrete integration matter. Capsule height does not halve foot rise. | U07 |
| C15 pad placement | **FAIL:** QA-005/006; restriction-by-restriction ledger below. | U07 |
| C16 air control | 30°/s steering rule **PASS (static)**; platform landing success **UNVERIFIABLE**. Derived envelope below. | U07, U20 |
| C17 collisions | Offline step offset 0.35 and player/pad/player-layer ignore setup **PASS (static)** (`PlayerController:188–192`, GameBootstrap:80–81). Ramps exist; network KCC prefab/filtering cannot be certified with absent assets. | U04, U07 |
| D18 all weapon TTK/dominance | Config arithmetic **PASS**, shown below; real hit rates/balance **UNVERIFIABLE**, cadence QA-009. | U09, U20 |
| D19 spread/recoil authority | **FAIL:** QA-008/009. Seed helper tests do not establish both presentation paths invoke it. No demonstrated damage desync. | U09 |
| D20 hit feedback | **FAIL:** QA-010/011. Offline hitmarker/sound callsites exist; a complete damage-number display was not found. Damage numbers are a requested UX benchmark, not an explicit v1.0 mandatory element. | U10 for ≤100 ms |
| D21 headshots | 1.25 multiplier on all weapon configs **PASS (static)**. Offline top-0.36 m threshold traced; online HeadHitbox dimensions **UNVERIFIABLE** without prefab. BRICK maximum full headshot is 90 HP. | U09, U20 |
| D22 reload/protection | WeaponState blocks firing during reload and automatically reloads an attempted empty shot; launch does not cancel reload **PASS (static)**. Firing ends protection online (`NetPlayer:252`); offline spawn geometry **FAIL** QA-007; reload display QA-031. | U08–U10 |
| D23 muzzle/camera | Muzzle→camera-aim obstruction path exists **PASS (static)**. Indicator **FAIL** QA-012; thin-cover/spawn barriers **UNVERIFIABLE**. | U06, U10 |
| D24 death/respawn | 4 s configured and regular killer/weapon/countdown path **PASS (static)**. Direction QA-032 and tutorial QA-013. Four seconds may limit frustration, but camera/reorientation determines whether it feels fair. | U10, U20 |
| E25 full online flow | **FAIL:** QA-001,014–017. Queue, reservation, joining, prep, countdown, live and results code exist; existence does not certify transitions. | U04, U11–U13 |
| E26 Relay | **PASS (static)** scoring rules below; replication latency, wall-clock drift and boundary ordering **UNVERIFIABLE**. | U08 |
| E27 rematch | Offline one-button return-to-match flow exists **PASS (static)**. Online one-tap action calls requeue, but party preservation/late results **FAIL** QA-014/016; automatic requeue without a voluntary press is not required. | U12 |
| E28 quit | Source marks participant disconnected, later substitutes bot, and excludes ineligible rewards/rating cases **PASS (static)**. Exact quit timing, substitution/rating flags and player-facing consequence **UNVERIFIABLE**. | U13 |
| F29 thumb reach | **UNVERIFIABLE** on devices; map below. Critical Ping is top-right, outside the requested bottom-60% band; pause upper-left is spec-aligned. | U03, U14 |
| F30 text/contrast | Palette on opaque panel **PASS** numerical contrast; physical size **FAIL** QA-019/020. | U14 |
| F31 labels | Most major actions use words (Play/Practice/Party/Appearance/Settings/Reload/Jump/Pad/Ping). Ring/cross and contextual PAD meaning require learning; instant comprehension of every control is **UNVERIFIABLE**. | U02, U14 |
| F32 settings | **FAIL:** QA-021/022; editor persistence static pass. Defaults and missing expectations discussed below. | U15 |
| F33 every §17 error | **FAIL:** QA-002,015–017,028/029; per-state ledger below. | U05, U11, U12, U17 |
| F34 premium design | Recommendations in §6 are design proposals, not invented implementation defects or proof of market preference. | U20 |
| G bots | Reaction/retreat/LOS logic **PASS (static)**, labeling **FAIL** QA-033. Believability **UNVERIFIABLE**. | U13, U20 |
| H performance | Current gate **UNVERIFIABLE**. Historical measurements and gaps below; QA-027/034/035. | U01, U18, U19 |
| I accessibility | **FAIL:** QA-021/023. Text scaling absent. One-thumb competitive play not supported by this manual move/aim/fire scheme; not an explicit promised mode. No full-screen flash or gore-toggle path found; assets/device effects and rating still **UNVERIFIABLE**. | U14, U15, U17 |
| J retention | Five-trigger review in §6. QA-024 undermines identity; D1/D7 and return intent **UNVERIFIABLE**, especially with QA-027. | U18, U20 |
| K safety/store | **FAIL:** QA-025/026/028 for product journeys. Purchase grant gate remains closed **PASS (static)**. Full production policy/data/payment compliance **UNVERIFIABLE**. | U17, U18 |

### Pad restriction ledger — spec §6

| Restriction | Source verdict |
|---|---|
| Placement grounded, 1.6 m forward, facing from view | **PASS (static)** PlayerController placement/NetPlayer placement and PadConfig; exact network transform U07 |
| Flat ≤5° | **PASS (static)** PadPlacementRules center normal test; mesh seams U07 |
| Whole 1.4 m footprint fits/supports | **FAIL (static)** QA-005: sampled perimeter is not whole-disc proof |
| No spawn-room placement | **PASS (static)** PadPlacementRules:39–46, radius-expanded room test; distinct from QA-007's firing check |
| Not within 5 m of exits | **PASS (static)** same function tests exit distance; boundary/radius interpretation must be reconciled with v1.1 in U01 |
| No capture-circle placement | **PASS (static)** all zone centers use zone radius + pad radius |
| No pad overlap | **PASS (static)** overlap check; simultaneous authoritative placements U07 |
| No immediate overhead obstruction | **FAIL (static envelope gap)** QA-005 |
| One charge, restores in 16 s | **PASS (static)** PlayerPadManager and PadConfig; death does not reset the charge clock |
| One active pad per owner, six match cap | **PASS (static)** replacement/active count validation; concurrent ownership and disconnect U07 |
| Arm 0.75 s, lifetime 18 s, HP 60 | **PASS (static)** LaunchPad/NetPad and PadConfig; clocks U07 |
| Jump required, no automatic launch | **PASS (static)** jump/activation path; edge overlap U07 |
| Either team may use every pad | **PASS (static)** launch path does not exclude enemy team; tutorial explicitly uses enemy-colored pad |
| Horizontal 14, vertical 12, steering 30°/s | **PASS (static)** shared movement/config path; collisions and discrete integration U07 |
| Validation precedes charge consumption | **PASS (static)** offline placement:752–764 and online:302–315; invalid online explanation fails QA-006 |

### Movement and landing calculations

Under continuous ideal motion, 6/35 = **0.171 s** to full ground speed; braking from 6 m/s over 35 m/s² needs **0.514 m**. This is fast acceleration with a meaningful stop distance, not evidence of either satisfying weight or floatiness. Ground fire speed is 5.1 m/s. Jump v=6 and g=20 gives v²/(2g)=**0.9 m**, apex at 0.3 s and same-height flight 0.6 s. An exactly 0.9 m ledge has no ideal clearance margin; it requires a real controller test.

A pad launch reaches **3.6 m** above launch height and returns in **1.2 s**, covering **16.8 m** without steering/collision. A y=3 m platform is crossed upward at 0.355 s and downward at **0.845 s**, with 11.829 m horizontal travel at the descending crossing. At 30°/s the heading can change about **25.35°** before that landing. For continuous maximum-turn steering at constant horizontal speed, lateral displacement is `14/ω × (1−cos(ωt))`, ω=π/6: approximately **2.57 m** at 0.845 s and **5.11 m** at 1.2 s. This gives meaningful correction within part of a 10×8 m platform footprint; it does not make arbitrary poorly placed pads landable. Capsule radius, launch offset, orientation and platform edge clearance reduce usable space. Two 7 m ramps rising 3 m are about **23.2°**, a ground route rather than a 0.9 m jump requirement. U07 must test every required route and pad-edge approach.

### Weapon TTK — arithmetic, not invented player measurements

Body damage against 100 HP, first shot at t=0, no acquisition time, latency, protection, regeneration or missed recovery. Damage falls linearly between each weapon's full/falloff distances. `hits=ceil(100/damage)` and uninterrupted `TTK=(hits−1)×60/RPM`. BRICK damage below is the sum of all eight pellets. These are **configured-rate** calculations; QA-009 prevents presenting them as measured online TTK.

| Weapon | Distance | Damage per trigger, full body hit | Minimum triggers | Ideal TTK |
|---|---:|---:|---:|---:|
| ARC | 5 m | 18 | 6 | 0.625 s |
| ARC | 15 m | 18 | 6 | 0.625 s |
| ARC | 25 m | 16.4 | 7 | 0.750 s |
| ZIP | 5 m | 13 | 8 | 0.538 s |
| ZIP | 15 m | 11.056 | 10 | 0.692 s |
| ZIP | 25 m | 7.167 | 14 | 1.000 s |
| BRICK | 5 m | 72 | 2 | 0.800 s |
| BRICK | 15 m | 42.462 | 3 | 1.600 s |
| BRICK | exactly 25 m | 0 | impossible | ∞ |

The exact-25 m BRICK result follows `GetDamageAtDistance` returning zero at `distance >= maxRange`; immediately inside 25 m the full body volley approaches 24 damage, requiring five triggers / 3.2 s. This endpoint is reported explicitly rather than disguised as falloff damage at an excluded range.

Illustrative mobile hit-pattern model, **not measured realistic hit rates**: ARC 65%/50%/40%, ZIP 70%/50%/35%; round required trigger count up after dividing body hits by accuracy. BRICK assumes six pellets per trigger at 5 m and two at 15 m. Actual human randomness, spread, target motion and miss clustering require U20; this deterministic model is not a statistical expected time-to-kill.

| Weapon | 5 m modeled triggers / TTK | 15 m modeled triggers / TTK | 25 m modeled triggers / TTK |
|---|---|---|---|
| ARC, 24 rounds, 1.6 s reload | 10 / 1.125 s | 12 / 1.375 s | 18 / 2.125 s |
| ZIP, 30 rounds, 1.5 s reload | 12 / 0.846 s | 20 / 1.462 s | 40 / 4.423 s, including reload |
| BRICK, 6 shells, 2 s reload | 2 / 0.800 s | 10 / 8.400 s, including reload | impossible at range endpoint |

Reload model: add reload duration **in place of**, not in addition to, the normal interval at the magazine boundary: `(N−1)I + floor((N−1)/mag)×(reload−I)`. Example ZIP25: 39×0.076923 + 1.5−0.076923 = 4.423 s. BRICK15: 9×0.8 + 2−0.8 = 8.4 s.

No weapon is strictly dominant across range, accuracy and exposure assumptions. ZIP wins close ideal sustained TTK; ARC has the strongest midrange/full-hit figures and avoids ZIP's modeled distant reload. BRICK is worth considering for a close peek delivering up to 72 body damage in one exposure and a broad pellet pattern; it loses badly in prolonged medium-range exposure. That niche must be demonstrated in counterbalanced player trials before declaring it viable or demanding a buff. A complete headshot BRICK volley is `8×9×1.25=90`, not a one-shot 100 HP kill.

Offline head detection uses the top approximately 0.36 m of a 1.8 m capsule. At 55° vertical FOV and 720 vertical pixels, a 0.36 m feature is roughly **50/17/10 px** high at 5/15/25 m (perspective estimate, before pitch/occlusion). A six-inch diagonal alone cannot establish headshot attainability. Online named head hitbox geometry is absent with the prefab; do not equate the visible head cube with its hitbox.

### Relay and match-transition trace

`S/Match/RelayScoringController.cs` grants time only to sole-team occupancy; empty/contested time does not score. Team-control changes reset the accumulating fraction. `S/Match/MatchFlowController.cs` handles 8 s prep, 3 s countdown, 180 s regulation, score limit 120, and tied regulation entering 30 s overtime. Overtime needs three continuous seconds of sole control; losing sole control resets that hold; expiry draws. These rules are **PASS (static)** against available v1.0. Tests in `RelayScoringControllerTests.cs` are evidence of intended assertions, not this audit's execution.

Zone occupancy filters dead/protected players, uses radius six and feet-height tolerance. Verify elevated/edge cases and simultaneous death/tick boundaries in U08. Scoring uses simulation deltas; no measured drift against wall clock or replicated-score presentation latency is available. No zone-change fractional-score defect is asserted where the absent amendment could define the intended boundary. Network match code ticks flow and resolves shots in a specific order; U08 must demonstrate that a result-boundary shot cannot change the final result or grant a late kill.

### UI, settings and errors

**Thumb map:** movement bottom-left and fire/jump/pad/reload bottom-right fit the requested bottom 60% in their default layouts; mirrored layout swaps sides. HP is low-center and ammo by fire. Ping is upper-right: a frequently used team action outside the thumb band. Pause is upper-left, consistent with the spec and infrequent use; moving it is a preference, not an automatic defect. Scores/clock/objective are top-center read-only. Teammate status is upper-left read-only. Tutorial Skip is high on screen; menus/results use central rows. The editor permits changes but does not make every custom layout reachable. U14 must include hand size, cutouts and both handedness modes.

**Contrast:** calculated sRGB contrast against opaque `#1B2940`: `#BBC6D8` **8.47:1**, `#F4F7FB` **13.59:1**, friendly `#30D5E8` **8.21:1**, enemy `#FF6376` **5.08:1**, objective `#FFD166` **10.13:1**. These clear a 4.5:1 text heuristic on that panel. They say nothing about text over the world, transparency, glare or tiny glyphs. At 1920×1080 and 360 dpi, 24 canvas units are only **10.67 dp**; at 1600×720 and 360 dpi they become **7.95 dp**. DPI without resolution/scaler/OS font scale cannot determine legibility.

**Settings:** gyro/left-handed/invert default off; aim assist default on; saved sensitivity/layout options have read/write paths. Shake restoration and SFX control fail QA-021/022. Volume stepping is not the spec's continuous slider experience. No complete user-facing frame-rate cap/graphics preset/text-scale/gyro-calibration workflow was found. These are appropriate mobile expectations to evaluate; FPP/TPP switching, scope sensitivity and ADS settings are not automatically missing features in this scoped TPP, no-scope design. Do not build an unrelated PUBG settings menu just to mimic breadth.

| Spec §17 failure/state | Verdict from current source | Acceptance procedure |
|---|---|---|
| Startup loading/retry/offline | Offline bootstrap exists; current time/failure recovery **UNVERIFIABLE** | U02 |
| No internet | Backend error mapping and failure/Home routes exist **PASS (static)**; actual network loss **UNVERIFIABLE** | U11 |
| Auth down | Failed state offers recovery **PASS (static)**; timeout and retry timing **UNVERIFIABLE** | U11 |
| Server full | Backend error/display path exists; complete recovery without residual tickets **UNVERIFIABLE** | U11 |
| Queue cancelled | **FAIL** QA-015 for in-flight race | U11 |
| Party member dropped | Server party/readiness mechanisms exist; live UI propagation/join **FAIL** QA-014 | U04, U13 |
| Update required | No verified version-negotiation-to-clear-update-action journey; **UNVERIFIABLE** as deployed behavior | U11, U17 |
| Match server down | Reconnect/failure UI exists; recovery/results classification **UNVERIFIABLE** | U11, U12 |
| Purchase pending/failed | Purchases gated; no completed enabled-store lifecycle. **PASS (static)** for no pending grant; enabled journey **UNVERIFIABLE** | U17 |
| Reward retry pending | **FAIL** QA-017 | U12 |
| Appearance preview/equip | **FAIL** QA-024 | U16 |
| Report | **FAIL** QA-025 | U16 |
| Results/Home/rematch | **FAIL** QA-002/016 where applicable | U05, U12 |

### Bots, performance, accessibility and release evidence

**Bots:** BotConfig specifies easy practice reaction 0.6 s, standard 0.35 s, retreat at 30% health. BotDecisionMaker gates response and retreat; NetBotDriver selects visible enemies through `BotPerception.CanSee`, adds aim error and focus-fire delay, and routes retreat toward its spawn. Hearing uses nearby active gunfire; it is not a license to aim through cover. No general hidden-target tracking path was found. Network bots do not generate Pad or Jump input in `NetBotDriver.Decide`, so public substitutes do not demonstrate mastery of the signature traversal mechanic. Treat believable pressure, sound-localization precision, path failures, cornering and pad competence as U20, not as proven player churn. Substitution labels fail QA-033. A player should recognize a bot when disclosed; “would believe them” means plausible play, not disguising AI as humans.

**Performance:** historical `R/ProjectUpdraft/Builds/AcceptanceStep1/logcat_v6.txt:912,6580,7203` records three approximately 30 s samples: 897/901/901 frames, 29.9/30.0/30.0 average FPS, 2/1/0 frames above 50 ms, and about 99 MB Unity allocation reading. This is not PSS, a percentile frame-time report, or a 20-minute thermal run. The historical document's 281.7 MB PSS needs a matched capture. Android activity start times of 679–831 ms and separate roughly 2.23–2.27 s bootstrap measurements do not prove cold-start-to-interactive ≤8 s. A reported 1–2 ms local match-load instrument does not prove post-reservation online arena load ≤10 s.

| Budget | Verdict |
|---|---|
| Sustained Low 30 FPS; tested higher tiers 60 | Historical short Low averages available; current sustained result **UNVERIFIABLE**, U19 |
| Download ≤250 MB / installed ≤600 MB | Reported B APK ~83.4 MiB is below download budget as a file; Play delivery/splits and installed footprint **UNVERIFIABLE**, U19 |
| Steady memory ≤750 MB | Unity allocated memory is not process PSS; current steady PSS **UNVERIFIABLE**, U19 |
| Startup ≤8 s / reservation load ≤10 s | **UNVERIFIABLE**, U02/U04 |
| Network ≤100 KB/s combined | No matched six-player/all-pads protocol-overhead capture; **UNVERIFIABLE**, U19 |
| ≤150 draw calls / ≤250k visible triangles | No matching measured scene report; **UNVERIFIABLE**, U19 |
| No core-loop allocations | **FAIL (static)** QA-035; actual frame harm **UNVERIFIABLE** |
| 3 GB old phone, 4 GB different GPU, midrange, 16 KB pages, aspects/notches, 20+ min | Complete matrix absent; **UNVERIFIABLE**, U19 |

`DevicePerformanceProbe` source does not substantiate all broader documentation claims about PSS, thermal or percentile instrumentation. `OriginalGameVfx.Create` has no callsite in the reviewed source; its existence does not establish integration or runtime quality.

**Accessibility:** pads use ring/cross and objective status text; avatar identity still fails QA-023. No complete text-scale feature is present. A single thumb cannot continuously move, aim and manually fire with the current layout; this is an inclusion limitation, not a promised one-thumb mode defect. Consider a separately validated accessibility control option rather than silently changing core combat rules. No full-screen-flash implementation or gore-toggle implementation was found. Absence of a toggle is not a defect when v1.0 specifies no gore feature; visual assets/effects and any v1.1 change need U14/U17. Age 16+ in a design document is not an issued store content rating.

**Safety/compliance:** backend ownership checks derive from authenticated sessions; ledger/inventory/purchase-token uniqueness and result processing provide relevant idempotency controls. Tests exist, but were not run. Commerce is disabled and pending verification does not grant items; disabled commerce is compliant with the project's pre-validation intent, not a missing-store defect. There is no demonstrated production verification/acknowledgment/refund/restore/RTDN lifecycle. Do not enable payments on the strength of endpoint names.

In-app deletion removes multiple account-linked tables, but raw-event deletion does not establish removal/justified retention of pseudonymous daily-presence aggregates, local telemetry identifiers/files, matchmaking records and external copies. `ForgetLocalSession` is not a verified complete telemetry reset. U17/U18 must trace these records and publish a truthful retention policy; this is an evidence gap, not an unsupported declaration of illegality. No contacts/free-chat collection was found. Error messages and event payloads still need a real data inventory/redaction review.

For spec §30 and adjacent release obligations, no live store listing, completed publisher identity/privacy/support/deletion pages, signed-release provenance, content-rating questionnaire, Data Safety declaration, tested acquisition clips/permissions or operational launch evidence was established. As of the audit date, Google lists **API 36** for ordinary new app/update submissions from 2026-08-31: [target API requirements](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en-gb). `AndroidTargetSdkVersion: 0` means the setting alone does not prove the final manifest target. If the publisher is a personal account created after 2023-11-13, the applicable production-access process includes **12 opted-in testers continuously for 14 days**: [Google Play testing requirements](https://support.google.com/googleplay/android-developer/answer/14151465?hl=en). Applicability depends on the actual publisher account. U17 verifies these facts; no store approval is claimed.

## 4. TOP 10 FIXES — ranked by player impact ÷ implementation effort

These are recommendations only; no implementation was performed. Effort is a rough relative estimate, not a schedule. Release dependency QA-001 must be resolved before meaningful online end-to-end acceptance even though a smaller high-impact fix can rank above it.

| Rank | Concrete result required | Findings | Impact / estimated effort |
|---:|---|---|---|
| 1 | Keep persistent Back/Home outside row cleanup; verify leave/delete never strand a player | QA-002 | Blocks navigation / small |
| 2 | Route all continuing/ending touches by acquired finger ID | QA-003 | Prevents stuck movement/aim / small |
| 3 | Correct spawn-room dimension check and test interior positions | QA-007 | Restores protection rules / small |
| 4 | Correct camera collision-side placement and final safe positioning | QA-004 | Preserves visibility and fairness / small–medium |
| 5 | Count aim activity; label replacement bots | QA-018,033 | Prevents unjust removal and misleading identity / small |
| 6 | Restore shake preference and apply SFX volume consistently | QA-021,022 | Makes promised accessibility/settings work / small |
| 7 | Supply/configure reviewed online resources and establish a reproducible six-client session | QA-001 | Makes the main game accessible / medium, infrastructure-dependent |
| 8 | Add a complete member ticket-follow/join journey and reliable cancellation | QA-014,015 | Makes team play viable / medium |
| 9 | Unify seeded shot presentation; deliver ordinary hit/threat feedback and correct cadence | QA-008–012 | Makes combat understandable and trustworthy / medium–large |
| 10 | Apply device-scale target/text floors and non-color team cues | QA-019,020,023 | Improves every first session and reduces misidentification / medium |

Rewards, recovery, deletion and analytics remain release requirements despite falling outside this top-ten ratio list. Do not use a ranking to waive them. Validate results lifetime and durable reward delivery before exposing public progression.

## 5. UNVERIFIABLE — exact human acceptance procedures

All procedures below are **future human work**, not actions performed by this auditor. Execute on an explicitly identified build/commit and retain raw captures. An expected result in a procedure is an acceptance criterion, not existing evidence.

**U01 — Authority and provenance.** (1) Obtain the actual v1.1 amendment and the accepted STEP_A or a written declaration that neither exists. (2) Record their hashes/approval dates and reconcile changed rules against v1.0. (3) Map the current source snapshot to each APK/server binary and test XML using build manifests. (4) Supply the claimed 142-test XML, list test names and compare timestamps/source hashes. (5) Resolve conflicting gate claims without substituting an older successful build. This closes document/test provenance only, not runtime coverage.

**U02 — First three minutes and tutorial.** (1) On each minimum/representative phone, clear local state and install the identified build; enable 60+ fps screen recording and a synchronized external input video. (2) Cold launch from a stopped process, recording launch tap, every loading/permission/account screen, first interactive frame and any no-action/no-explanation interval >5 s. (3) Complete the nine tutorial tasks without developer help; timestamp every instruction, completion, inter-step wait and first fired shot. (4) Deliberately die in the bot fight and record all four seconds plus recovery. (5) Reset and repeat Skip → Practice → first legal shot; count taps/holds separately, including movement out of spawn. (6) Reset and repeat tutorial → Home → public Play after U04 is available; measure the complete first-three-minute trace. (7) Repeat warm launch to verify tutorial persistence and access to replay. Accept ≥80% completion only with the specified real-user study/sample, not one tester. Log all >5 s dead air as newly reproduced findings.

**U03 — Touch/gyro/handedness.** (1) On a real multitouch phone show input diagnostics and record touch IDs, camera yaw/pitch and movement. (2) Hold move+aim+fire+jump with four fingers; repeat three-contact move/fire-drag/jump, both hands and large thumbs. (3) Cross the screen midpoint with each owning finger; release and cancel outside its acquisition zone. (4) Drag every menu/gameplay control across the aim region and lift; UI-only touches must never rotate the camera, except documented fire/pad gestures. (5) Add a second fire finger; release owners in both orders; test rapid taps shorter than one network tick, long holds and aim drags. (6) Interrupt by notification shade, app focus loss and orientation/safe-area change; verify no latched movement/fire. (7) Test gyro initially off, enable on a stationary phone for 30 s, rotate/recenter, combine touch and gyro; quantify drift and sensitivity. (8) Save custom controls, restart, mirror, resize to minimum, overlap edges, reset and restart again; compare all positions/sizes/opacities. Any reproduced UI→camera leakage is P0 under this engagement.

**U04 — Online/party lifecycle.** (1) Document required supplied resources/configuration and build provenance without hiding missing checkout dependencies. (2) Launch one server and six authorized clients in the intended region. (3) Time queue, reservation, arena-ready, prep and countdown; verify no live match starts with an empty reserved slot. (4) Have one client fail loading for >30 s and verify others visibly return to queue. (5) Repeat with a three-person party: create, join by code, ready, leader-only queue; every member must join the same team/session without independent enqueue. (6) Drop a member at each transition; verify live UI and cancellation/requeue behavior. (7) Verify both teams can leave all three spawn exits and cannot enter enemy spawn; verify network KCC player collision filtering. (8) Repeat results → rematch preserving party. Capture client/server state and UI together.

**U05 — Product navigation.** (1) Open each Home destination and use Back. (2) Create/join a party, refresh, change Ready, leave; after each action return Home. (3) On a disposable account open deletion confirmation, cancel, reopen and confirm successful deletion. (4) Verify an operable exit remains after each response, including errors and delayed responses. (5) Switch screens while requests are pending; old replies must not redraw the new screen. Test system Back separately; do not count force-stop as recovery.

**U06 — Camera/assist/occlusion.** (1) Walk and aim against every map wall/corner, ramp edge and narrow gap; include a test fixture with thin walls and moving blockers on each intended obstruction layer. (2) Place the shoulder pivot inside/next to geometry; rotate 180°, launch past it, and trigger recoil/shake. (3) Record camera position and rendered frames; it must not cross the blocking surface or expose hidden targets. (4) Put enemies just inside/outside 3° and 25 m, then behind cover, allies and a pad. (5) Record equal-size input deltas with assist off/on; inside visible region should produce 0.70 sensitivity, with no autonomous tracking or rotation. (6) Repeat with more candidates/colliders than the nonalloc buffer capacities to check false positives/negatives. (7) Test muzzle inside/touching thin cover and verify authoritative shots as well as the indicator.

**U07 — Movement/pads/map routes.** (1) Measure ground/fire speeds, acceleration, stopping distance, jump foot apex and pad apex/range using a frame/tick trace. (2) Attempt 0.85/0.90/0.95 m ledges from multiple speeds/angles; complete every authored required route and both ramps on each platform. (3) Test pad placement at 0°, 5°, >5°, edges, holes between sampled support rays, spawn boundaries, 5 m exit boundaries, capture-circle boundaries and overlapping pads. (4) Test a low ceiling across the character's head/chest launch envelope and walls immediately ahead. (5) Verify rejected placements do not spend charge and always explain why, offline and online. (6) Place concurrently up to six pads; replace one owner pad, die/disconnect and test ownership cleanup. (7) Time arm .75 s, life 18 s, recharge 16 s, HP60 destruction; walk across without Jump, then jump from center/edge using both teams. (8) Repeatedly correct left/right onto every 10×8 platform; record success rate and edge collisions rather than merely one successful landing.

**U08 — Protection/Relay boundaries.** (1) Fire from center and off-center spawn positions during protection; leave each exit and test expiry/damage rules. (2) Run scripted single-team, empty, contested, rotating-zone and elevated occupancy while logging authoritative occupants, fractional score, zone clock and client displays. (3) Cross score120 and time180 on adjacent ticks; submit simultaneous lethal shots on the result boundary. (4) Force a tie: hold 2.9 s then contest/empty, then hold 3 s; verify reset, victory and 30 s draw. (5) Compare simulation timers and six-client display updates to a common clock under packet delay/loss. Record maximum drift/score display latency; no match may score or damage after result lock.

**U09 — Weapons/authority.** (1) Against stationary 100 HP targets at 5/15/25 m, record body/head damage and shot timestamps for all weapons and movement states. (2) Fire full magazines plus reloads at the actual network tick rate; calculate observed RPM and TTK. (3) Capture seed, pellet directions, state and weapon values on predicting client and authority; compare all BRICK pellets and every tracer. (4) Repeat under rollback/150 ms RTT/packet loss; distinguish cosmetic duplicates from authoritative ammo/damage divergence. (5) Use an authorized test client supplying constant aim while firing and check whether server recoil/input legality meets the intended authority contract. (6) Measure actual head hitboxes, especially the missing network prefab definition. (7) Test firing/reload during launch, respawn protection and death. A reproduced authoritative prediction desync is P0; attach tick-level evidence.

**U10 — Combat understanding.** (1) Record touch, shot, server hit, marker, sound, HP and death on two clients at 60+ fps with synchronized timestamps. (2) Land one nonlethal hit per weapon; measure touch/shot-to-feedback separately from confirmed-hit latency and require the chosen feedback path within 100 ms. (3) Verify local/remote fire/reload/empty/steps/landing/pad arm-launch-destroy/damage/low-HP/elimination cues with stereo headphones. (4) Fire after already aiming into cover, through a teammate, and at a thin edge. (5) Reload partial/empty magazines on ground and in flight; verify understandable state. (6) Die to visible and unseen attackers; verify who/weapon/direction, exact four-second respawn, spectator transition and restored control. (7) Ask an unbriefed player what hit them and why their blocked shot failed; record errors rather than coach them.

**U11 — Error and cancellation matrix.** (1) For sign-in, enqueue, reservation and joining independently inject offline network, timeout, 401, 409/full, 429 and 5xx. (2) Delay enqueue until after Cancel is tapped; then release response and inspect tickets/server allocation. (3) Cancel while searching, reserving, loading and reconnecting; assert no stale ticket or later surprise transition. (4) Observe waits through 45 and 60 s, recording elapsed text, region, Cancel and Practice access. (5) Present an incompatible client/version and inspect update instructions. (6) Terminate the match server and restore connectivity within/outside the reconnect window. (7) Every outcome must have truthful explanation and a functioning retry/Home/Practice/update action, not merely a dismissed overlay. Verify backend state after every local exit.

**U12 — Results/rewards/rematch.** (1) Finish a normal six-human match; photograph stats, authoritative XP/credits/unlocks and party membership. (2) Wait 20 s and 60 s at results before pressing Home/Play Again, including after server teardown. (3) Repeat win/loss/draw/ineligible/AFK/disconnect/server-failed outcomes and compare wallet/XP/rating to eligibility rules. (4) Block result delivery for all retries, then restore after server exit; demand eventual exactly-once receipt and a visible pending state until resolved. (5) Resubmit identical results; balances must not duplicate. (6) Shut down the runner during live play and verify it cannot be classified as an ordinary completed loss or award. The source's `Cancelled`/BuildResult branch alone does not settle this failure classification.

**U13 — Quit/AFK/bot substitution.** (1) Keep only yaw/pitch changing for 50 s; verify no AFK warning/removal. (2) Stay truly idle and timestamp 30 s warning and 45 s substitution. (3) Disconnect at prep/live/results and inspect five-second replacement behavior, names, team slots and reconnect eligibility. (4) Attempt recovery before/after removal; verify there is no duplicate avatar or permanent control lock. (5) Check reward/rating records for quitters and affected teammates, including bot-substitution flags. (6) Observe replacement labels in HUD, scoreboard and kill feed on all clients; strangers must not be silently inserted mid-match.

**U14 — Accessibility/UI physical validation.** (1) On the required aspect ratios/notches at reported DPI, measure every clickable rectangle in pixels and convert using actual density; require ≥48 dp after all customization and keyboard states. (2) Overlay the bottom-60% reach band and test all critical controls with small/large hands, left/right layouts and one-thumb limitations documented. (3) Test minimum/default/maximum OS font/display scale; read all labels at normal distance in indoor and bright outdoor light, checking truncation. (4) Test grayscale/protan/deutan/tritan views; ally/enemy, pads, zone, damage and disabled states must remain distinguishable without hue. (5) Capture every effect during a six-player maximum-pad firefight and inspect full-screen luminance transitions, camera motion and reduced-motion settings; review actual assets for gore. (6) Have unbriefed players identify each label/icon within one second. Attach misses and physical dimensions, not aesthetic opinions alone.

**U15 — Settings persistence/audio.** (1) Start with cleared preferences and record every default. (2) Change each setting individually, leave/reenter menus, restart the process and verify actual behavior as well as displayed value. (3) Test aim sensitivity, invert, assist, gyro, handedness, layout size/position/opacity, reset, shake and ping mute. (4) Independently mute Master/SFX/Music/Announcer and exercise every cue category; inspect unauthorized survivors. (5) Reset and restart; compare exact defaults. A UI checkmark alone does not pass.

**U16 — Identity/reporting.** (1) Earn/unlock and equip each representative cosmetic with two accounts; compare preview, own avatar, remote avatar and restart persistence. (2) Populate inventory beyond one screen and verify navigation/accessibility to the last item. (3) From a finished match select an opponent to report using only app-visible information; verify selected identity, reason, match context, receipt and moderation record. (4) Exercise invalid/rate-limited submissions and recovery without exposing internal identifiers as a usability requirement.

**U17 — Account/store/release.** (1) With a controlled test identity link an account, record IDs/inventory/balance, remove local state and recover on another device; verify the same account without wallet auto-merge. (2) Delete another disposable account in-app and through the public web route; receive verification, complete it and inspect every retained/deleted record and external copy against the published policy. (3) Verify publisher identity, contact, live privacy/support/deletion URLs, store Data Safety answers and content-rating questionnaire against actual SDKs/assets/network capture. (4) Inspect the built manifest, signing and Play pre-launch report for target API/permissions/16 KB compatibility; confirm the publisher-account-specific closed-testing obligations. (5) Keep commerce disabled until authorized test-track purchase scenarios pass: pending→paid, cancel, duplicate token, acknowledgment, refund/revocation, reinstall restore, linked-account ownership and network interruption. (6) Verify localized Play prices, exact permanent contents and zero paid-stat/respawn/random-reward grants. (7) Review actual launch assets/gameplay recording consent and support operations. These are release checks, not a request for this auditor to enable payments or publish anything.

**U18 — Telemetry/privacy/KPIs.** (1) Use consented test accounts in tutorial, practice, solo public, party public and substituted-bot sessions; record first-open and each event path. (2) Compare local event, outgoing request, accepted row and reporting dimension, including true event time, match/region/party/cohort/bot/tester flags. (3) Retry/drop/duplicate batches and verify retention/cohort math; count skipped tutorial separately from completed tasks. (4) Remove identifiers/account and verify local queue reset, raw deletion and a documented aggregate-retention decision; inspect error payloads for personal data. (5) Validate D1 24–48 h and D7 cohorts/sample sizes, voluntary rematch and queue medians from source rows before accepting a dashboard. (6) Reconcile server-owned facts with authoritative match results rather than client claims.

**U19 — Performance matrix.** (1) Use the 3 GB old device, a 4 GB different-GPU phone, current midrange, a 16 KB-page device and representative aspect/notch variants; record OS/GPU/build/ambient temperature/graphics cap. (2) Cold-start ten times, then play ≥20 minutes with six clients, all six pads live, frequent respawns and maximum visible effects. (3) Capture frame-time distribution, sustained FPS, thermal/frequency behavior, process PSS, GC allocations, draw calls and visible triangles, not just average FPS/Unity heap. (4) Measure combined per-client bandwidth with transport overhead during that fight and arena readiness after reservation. (5) Measure delivered download and installed footprint from the actual release artifacts. (6) Host 100 sequential sessions in a long-lived server and compare retained bot/player counts and PSS. (7) Report each spec budget per device with raw traces and worst sustained intervals, including any dynamic-resolution floor breach.

**U20 — Feel, balance, bots and return intent.** (1) Recruit the spec's 30–50 non-friend adult target mobile-shooter players, including casual older players and experienced competitive players; record device and handedness. (2) Counterbalance pad/no-pad study conditions and weapons, without coaching the enemy-pad lesson. (3) Collect per-distance hit/pellet rates, acquisition/reorientation times, landing success and deaths understood; compare assist off/on and steering success. (4) Test bots' reaction timing, retreat survival, line-of-sight loss, hearing, path failures and inability to use pads; ask whether pressure feels fair and purposeful, not whether AI was secretly human. (5) Record voluntary rematch and ask unaided why they would return; then measure actual D1/D7 cohorts after U18 passes. (6) Evaluate the three proposed retention fixes in §6 individually. Do not convert stated intention, rewarded participation or one reviewer's preference into retention evidence.

## 6. DESIGN-INTENT review and premium-design recommendations

### Deliberate omissions — not defects

| Omission | Does it hurt the target experience? |
|---|---|
| Sprint | Not inherently: one ground speed removes a mobile input burden and gives pads a clear traversal role; route pacing must still pass U20. |
| Prone | Not inherently: it favors readable short arena fights and avoids another small touch control; cover/vertical routes must supply enough tactical choice. |
| Attachments | Not inherently: three fixed weapon roles reduce loadout friction and preserve immediate fairness, provided each role is viable. |
| Killcam | Some deaths are harder to learn from, but killer/weapon/direction and clear hit feedback can address that within scope; do not add a killcam to close QA-032. |

No FPP mode, scopes/ADS, battle royale, voice chat, hero roster or ranked-season system is demanded by this audit. Shop/ads being disabled before validation is deliberate and should remain so.

### Five return triggers

| Trigger | Evidence-based assessment |
|---|---|
| Immediate satisfaction | Offline movement/shooting call paths exist; stuck input and online hit/threat feedback gaps directly undermine it. No measured subjective/latency pass. |
| Learning | Nine-step tutorial and shared pads create a teachable distinction; missing online preview, tutorial death explanation and directional death context weaken the learning loop. |
| Unpredictability | Both teams can use a player's pads and six active pads are allowed; actual player-created route diversity needs the prescribed study. Bots do not demonstrate the same traversal input. |
| Team connection | Ready/party/backend records exist, but member join and reliable rematch are incomplete; a social promise that stops at the leader is not delivered. |
| Identity | Progression/inventory exist as data; missing preview/visible equipped appearance makes earned identity difficult to perceive. |

**Three weakest hooks and specific fixes:** (1) **Immediate satisfaction:** finish ordinary hit confirmation, spatial threat cues and trustworthy seeded shot feedback; require measured feedback ≤100 ms and new players correctly explaining their last fight. (2) **Team connection:** make one leader queue transport the entire ready party, preserve it through a delayed results screen and show useful objective/pad-assisted contributions; require repeated three-person rematches without manual regrouping. (3) **Identity:** render the actual equipped cosmetic in a live preview and for teammates/opponents, then show the authoritative earned reward on results; require players to recognize what they earned and see it in the next match. These are concrete product corrections, not a prescription for manipulative streaks or a larger content treadmill.

**Would a PUBG player return on day 2?** There is no reliable evidence to answer yes. The current online joining/feedback/party failures give identifiable reasons to leave; day-2 behavior remains U18/U20. This report does not invent a retention percentage.

### F34 — make the existing design feel premium

The inspected historical captures show a graybox prototype: primitive avatars/cover, broad flat surfaces, small utility text and a utilitarian settings grid. Graybox art is explicitly allowed for the accepted early scope, so its existence is not a hidden P1 art defect. It is also not evidence of a launch-quality premium presentation. Proposed improvements, within the existing scope:

1. **Readability before decoration:** enforce physical target/text sizes, stable dark backplates behind critical world-overlaid text, consistent spacing and a clear primary action per screen. Keep combat sightlines open; dark glossy panels and tiny “luxury” type would make this shooter worse.
2. **Recognizable silhouettes:** finish one original, readable character language and three immediately distinguishable weapon silhouettes. Give pad team-neutral usability a shared shape, with clear owner/team markings and arm/ready/broken states. Avoid copying PUBG/Call of Duty assets or piling on scope-excluded systems.
3. **Material and lighting hierarchy:** use a restrained industrial palette, authored roughness/material breakup and high-contrast route/cover edges. Separate playable surfaces from background clutter; maintain the Low performance budget before adding expensive effects.
4. **Responsive interaction finish:** consistent press/hold/disabled/error feedback, short restrained transitions, accurate progress/retry wording and predictable Back behavior. Optional reduced motion must affect actual camera/effects. Never mask loading with an unmeasured animation.
5. **Weapon and traversal sound identity:** distinct fire/reload/empty signatures, spatial threat priority, readable launch/landing cues and restrained UI sound. This contributes more to perceived quality than extra menu ornament, and must exist online as well as offline.
6. **Results that close the loop:** legible outcome, objective/pad contribution, trustworthy earned rewards and one stable Play Again action; retain party context and make pending delivery explicit.

Treat these as testable design proposals. A premium feeling cannot be certified from source or a reference to competing games.

## 7. FTUE timeline — first three minutes as the code actually defines them

An absolute cold-start second-by-second transcript cannot honestly be manufactured from source: device loading, player actions, network waits and tutorial completion are not fixed durations. The historical startup numbers do not measure the complete current journey. Therefore the absolute 0–180 s FTUE is **UNVERIFIABLE (U02)**. The following is the exact conditional schedule recoverable from code, including every fixed wait and every gate; it deliberately does not pretend a new player finishes the tutorial at second 90.

### Screens, actions and friction gates

| From launch | Existing behavior | Actions/taps and friction |
|---|---|---|
| t=0 | OS launch requested | One launch tap; engine/scene startup duration unknown |
| 0≤t<S, where S is first interactive tutorial frame | Bootstrap/scene initialization | Number/duration of visible loading frames unknown; inspect permissions and flag any actual >5 s dead air in U02 |
| t=S | First-run preference selects tutorial directly | No mandatory account/shop/notification gate in the inspected source; one instruction begins |
| Tutorial step 1 | Move to marker | Movement hold; duration depends on discovery/skill |
| Completed step 1 → next | One-second coroutine wait | Fixed 1 s; not a >5 s dead-air finding |
| Step 2 | Kill stationary target | Aim + fire hold/taps; the first tutorial shot occurs here, time unbounded by code |
| Step 3, after another 1 s | Reload | One Reload action, weapon-duration wait; default ARC 1.6 s |
| Step 4, after another 1 s | Place pad | Pad hold/aim/release gesture; invalid-placement retries vary |
| Step 5, after another 1 s | Ride own pad to platform | Move + Jump; pad arm/trajectory and landing matter |
| Step 6, after another 1 s | Use enemy-colored pad | Move + Jump; this is the explicit lesson that both teams use every pad |
| Step 7, after another 1 s | Destroy enemy pad | Aim + fire; no fixed player completion time |
| Step 8, after another 1 s | Capture zone | Move into zone and meet the tutorial hold condition (3 s) |
| Step 9, after another 1 s | Short bot fight | Aim/move/fire; death can add four seconds without the proper tutorial panel (QA-013) |
| Tutorial completion or Skip | Completion preference saved, Home shown | Skip is one tap at any step; completing all steps has eight fixed 1 s transitions, not an automatic 90 s cutoff |
| Home → Practice | Practice starts local match setup | One menu tap; no account required; initialization duration not measured |
| Practice prep | Loadout/prep | 8 s available for weapon selection; ARC default. This is purposeful prep, not automatically “dead air,” but early-ready players can feel the wait |
| Countdown | Match countdown | 3 s fixed; shot legality then depends on live phase and spawn position |
| Live | Relay play | Movement out of spawn + aim/fire; first legal shot is not guaranteed exactly at live start |
| Home → public Play alternative | Sign-in → enqueue → reservation → join | One Play tap initiates it, but online dependencies fail QA-001; party QA-014; actual queue/loading unmeasured |

**Minimum menu-tap accounting for the skip/practice route:** launch + Skip + Practice = **three discrete taps** before movement/aim/fire gestures. Weapon selection is optional; firing adds at least one press, and spawn exit needs a hold. The full tutorial route has no truthful fixed total tap count because aim/fire may be held or tapped, movement is analog, and retries/deaths vary. Public play cannot be assigned a successful first-shot timestamp in this checkout.

### Every elapsed second of a local match, relative to a known event

Let `P` be the instant practice match initialization enters Prep (not OS launch). Ignoring frame-boundary quantization, `k=floor(t−P)` gives the following exhaustive per-second rule. Thus each second of the first 180 s is accounted for without fabricating user action durations:

| k (each integer second) | Existing scheduled state |
|---|---|
| 0 | Prep, about 8 s remaining |
| 1 | Prep, about 7 s remaining |
| 2 | Prep, about 6 s remaining |
| 3 | Prep, about 5 s remaining |
| 4 | Prep, about 4 s remaining |
| 5 | Prep, about 3 s remaining |
| 6 | Prep, about 2 s remaining |
| 7 | Prep, about 1 s remaining |
| 8 | Countdown, about 3 s remaining |
| 9 | Countdown, about 2 s remaining |
| 10 | Countdown, about 1 s remaining |
| Every k=11,12,…,179 | Live elapsed approximately k−11 seconds, **unless** score120 already ended the match, the player exited, or a failure interrupted it. Health, deaths, pad charge and actions are event-dependent. Regulation remaining approximately 191−k seconds. |

At P+180 s, a non-ended match has approximately 169 s of live play and 11 s of regulation left; the three-minute regulation does **not** include prep/countdown. Regulation expiry is approximately P+191 s, with tied overtime afterward. Frame-step handling may add small drift, which U08 must measure.

For the actual first 180 s after OS launch, substitute `P = S + time-to-Skip-or-complete + time-at-Home + practice-initialization`. At any absolute second before P, the tutorial remains at its last unmet completion condition or Home remains waiting for player input. If a player has not passed that condition, there is no code event forcing progress at second 90 or 180. For public play, add measured auth/queue/reservation/load intervals instead, and do not claim success until QA-001 and U04 are resolved. This conditional trace is the limit of defensible source evidence; U02 must provide the requested absolute recording and flag every real friction interval.

## 8. Independent re-verification pass (continuation session, 2026-09-29)

Process check before this session: no `Unity`, `Unity Hub`, or `Unity.Licensing.Client` process running (`tasklist` scan returned none). Read-only work throughout: no Unity launch, no builds, no test execution, no source edits. This report file is the only write.

To sustain the no-cry-wolf standard, the findings below were independently re-traced against the working tree by direct line-level reads during this session, with the following results:

- **QA-001 re-traced and hardened:** `NetSession.cs:137,175` and `NetMatch.cs:293` load `NetPrefabs/NetMatch|NetPlayer|NetPad` from Resources; a repository-wide asset search found no such files (only `.cs`/`.cs.meta`), and no editor or build script generates that path (0 matches across `Assets/Scripts/Editor`). `Resources/Config/BackendConfig.asset` is absent while `BackendClient.cs:56,69–91` degrades null config to `NotConfigured` (graceful offline messaging — confirmed as described); the Net prefab gap has no such degradation because a null prefab flows straight into `Runner.Spawn`. P0 stands.
- **QA-002 re-traced:** `PlayerProductScreens.cs:34` creates BACK through `Button()` → every instance is named `Row_Button` (`:257`); `ClearRows()` (`:220–225`) destroys all `Row_*` children except the status label; `RenderParty` (`:84`), leave-success (`:106`), and delete-success (`:202`) all call it; `Close()` (`:213`) is reachable only from the destroyed BACK button. P0 stands.
- **QA-003, QA-004, QA-007, QA-008, QA-009, QA-010, QA-013, QA-015, QA-016, QA-018, QA-021, QA-033, QA-034 re-traced:** each observed mechanism confirmed at the cited lines — midpoint-routed touch ownership latch (`TouchInputManager.cs:221–230` with `HandleMoveTouch`/`HandleAimFireTouch` ownership gates); sphere-cast offset `hit.point + direction.normalized * radius` with `direction` pointing toward the obstruction (`ThirdPersonCamera.cs:136–143`); `Vector2*0.5f → Vector3` zeroing the z half-extent in `IsInsideOwnSpawnRoom` (`PlayerController.cs:400–405`); predicted tracer = single unspread ray vs server-seeded 8-pellet queue (`NetPlayer.cs:264–279`); whole-tick `FireCooldown` restart per shot vs offline remainder-carrying `WeaponState` (`NetPlayer.cs:247–249` vs `WeaponState.cs:56–90`); `ShowHitConfirmed` gated on elimination-count delta (`NetClientPresenter.cs:281`); tutorial `OnDeath` handler empty (`TutorialController.cs:290`); `searching` set only after awaited enqueue while `Cancel` checks it (`OnlinePlay.cs:75–83,231–242`); `await Task.Delay(12000)` server leave vs results lifetime (`DedicatedServer.cs:101`, `NetClientPresenter.cs:46–51`); `IsActivity` = movement/bits only (`NetPlayer.cs:177`); `CameraShakeEnabled` written but never read on camera start (`ThirdPersonCamera.cs:105–109`); substitution flips `BotControlled` without relabeling `Alias` (`NetMatch.cs:246–251`); `NetBotDriver.Forget` has zero callers (repo grep exit 1).
- **Quantitative re-computation:** TTK table reproduced from config (ARC 25 m: 18−8·5/25=16.4 → 7 hits → 0.750 s; ZIP 15 m: 11.056 → 10 hits → 0.692 s; BRICK 15 m: 42.462 → 3 hits → 1.600 s; BRICK all-head ceiling 8×9×1.25=90<100). QA-019 dp math reproduced (scale 0.745 → 26.2/31.8/35.8/21.5 dp). QA-020 text sizes reproduced (6.4–9.5 dp). All agree with §3.
- **Config spot-check:** `Weapon_ARC.asset` matches spec §7 exactly (18 body, 480 rpm, 24/1.6 s, 1.25×, 0.45/0.75/1.2° spread, 0.55° recoil, 20/45/55 m falloff envelope).
- **PASS verdicts re-traced:** `AimAssistRules` = 3°/25 m/0.70 with LOS raycast and no snap/magnetism path (B8); `RelayScoringController` implements §3 sole-occupancy 1 pt/s, contested/empty zero, fractional-interval reset, 120 cap, tie→overtime, 3 s hold with reset, and draw expiry (E26).

That pass is historical. The shared working tree changed afterward. The reassessment below is authoritative for the current source and supersedes conflicting dispositions, counts, and rankings in Sections 2–4 and the coverage ledger above. No Unity process was running at the start of this continuation. No Unity, build, or test command was run, and no source file was changed.

## 9. Current working-tree reassessment (2026-09-29)

The current tree contains broad corrective edits after the earlier snapshot. Static reading finds that the fixes are present in source and matching regression tests have been added for many of them; tests were not executed in this audit. A source fix is therefore a static disposition only. It does not close device, network, backend deployment, or product acceptance.

### Current severity counts

Counts below cover current source findings only. Release evidence gaps are listed separately as UNVERIFIABLE and do not inflate defect counts.

| Primary area | P0 | P1 | P2 | P3 |
|---|---:|---:|---:|---:|
| E — Online entry and queue | 1 | 0 | 1 | 0 |
| G — Bots | 0 | 1 | 0 | 0 |
| B — Controls | 0 | 0 | 1 | 0 |
| D — Combat | 0 | 0 | 2 | 0 |
| H — Performance | 0 | 0 | 1 | 0 |
| **Total** | **1** | **1** | **5** | **0** |

### Current findings

**[QA-001] [P0] [E — Online entry] [ProjectUpdraft/Assets/Resources/Config/BackendConfig.asset:15; ProjectUpdraft/Assets/Scripts/Network/OnlinePlay.cs:44–49]**

**Expected:** a release client can reach its authenticated backend and join a configured match service. **Observed:** the formerly missing network prefabs and backend config resource now exist, but the checked-in `BackendConfig.asset` sets `environmentName: dev` and `baseUrl: http://localhost:5000`; on an Android player's device, localhost is that device. The resource-presence test cannot establish a reachable production backend, and the milestone record still says the six-client service journey is unaccepted. **Evidence: STATIC-ANALYSIS-ONLY.** This is a release-blocking online-entry configuration, not the earlier missing-prefab claim. Human reproduction: install the exact release-candidate build on two separate Android devices, verify the backend URL/config embedded in each build, then sign in, queue, reserve and join the same live session; capture HTTP and Fusion connection results. Do not use a development loopback endpoint as evidence of a production route.

**[QA-029] [P2] [E — Matchmaking fallback] [ProjectUpdraft/Assets/Scripts/Network/OnlinePlay.cs:132–153,295–311; Backend/UpdraftBackend/Endpoints/MatchmakingEndpoints.cs:67–81]**

**Expected:** choosing “PRACTICE INSTEAD” during search or server reservation releases the pending queue/match reservation before starting practice. **Observed:** the option appears during reservation after 20 seconds and `PracticeInstead` calls `Cancel`, but the backend cancellation endpoint changes only tickets whose status is `searching`. At that point the ticket is `matched`; the client exits its local flow and starts practice while the reserved match allocation can remain active. The elapsed/region text and search-phase fallback are present. **Evidence: STATIC-ANALYSIS-ONLY.** Repro: U11 step 4, continue until “MATCH FOUND” shows “PRACTICE INSTEAD,” tap it, then inspect ticket and allocation state. Expected result: practice starts and the reservation is withdrawn or explicitly expires with a visible recovery status; no orphan allocation remains.

**[QA-030] [P2] [B — Gyro usability] [ProjectUpdraft/Assets/Scripts/Input/TouchInputManager.cs:196–207,492–498,672–695; ProjectUpdraft/Assets/Scripts/Core/OfflinePauseMenu.cs:100–104]**

**Expected:** an optional gyro user can enable the control and establish a usable neutral orientation. **Observed:** gyro defaults off, persists, and adds rate-based input to touch, but settings expose only an on/off toggle; no calibration, recenter action, drift deadband, or instruction is present. The absence is a usability gap requested by this audit, not a sensor malfunction proven by source. **Evidence: STATIC-ANALYSIS-ONLY.** Human reproduction: on a supported phone, enable gyro, hold the device still for 30 seconds, measure camera drift; rotate and return to the intended neutral position; combine gyro with touch aim, background/resume, and restart. Record whether the player can establish a stable neutral aim without disabling gyro.

**[QA-031] [P2] [D — Reload state readability] [ProjectUpdraft/Assets/Scripts/UI/MatchHUD.cs:471–484; ProjectUpdraft/Assets/Scripts/Network/NetClientPresenter.cs:309–314; ProjectUpdraft/Assets/Scripts/Player/PlayerController.cs:161–164]**

**Expected:** the HUD distinguishes a partial-magazine reload, an empty magazine, and ordinary ammunition, and communicates reload progress. **Observed:** `PlayerController` now exposes `IsReloading` and `ReloadProgress`, but no HUD callsite reads either property. `MatchHUD.UpdateAmmo` still labels zero rounds “RELOADING” and turns on the reload indicator solely when current ammo is zero; a manual partial reload continues to show the old count. Online presenter updates only when ammo changes and then overwrites the touch label with `current/max`. The progress property is not rendered. **Evidence: STATIC-ANALYSIS-ONLY.** Human reproduction: in practice fire approximately half a magazine, manually reload, and observe text/progress until completion; repeat with zero ammo and while airborne/launched. Verify status returns to the ammo count and never indicates reload after interruption or completion.

**[QA-032] [P2] [D — Death direction] [ProjectUpdraft/Assets/Scripts/UI/MatchHUD.cs:416–430,503–508; ProjectUpdraft/Assets/Scripts/Player/PlayerController.cs:701–723; ProjectUpdraft/Assets/Scripts/Network/NetPlayer.cs:464–478]**

**Expected:** after death the player can identify who killed them, with what weapon, and the direction of the attack, without a killcam. **Observed:** the HUD receives killer name and weapon and shows both, but the elimination payload has no attack direction or last-hit origin and the HUD displays none. **Evidence: STATIC-ANALYSIS-ONLY.** Human reproduction: in a two-player match, die to an attacker outside the current camera view; before respawn ask an unbriefed player to identify the attack direction from the death screen. Record whether it is conveyed without a killcam.

**[QA-033] [P1] [G — Bot substitution transparency] [ProjectUpdraft/Assets/Scripts/Network/NetMatch.cs:241–257; ProjectUpdraft/Assets/Scripts/Network/NetPlayer.cs:128–150; ProjectUpdraft/Assets/Scripts/UI/MatchHUD.cs participant display]**

**Expected:** disconnect and AFK replacements are visibly labeled as bots. **Observed:** substitution sets `BotControlled` and `HadBotSubstitution`; the observed substitution branch does not change the player's alias or add a HUD bot label. Initial bot naming does not prove that a replaced human is identified as a bot. **Evidence: STATIC-ANALYSIS-ONLY.** Human reproduction: show a named human on all six clients, disconnect them, wait through the 5-second substitution grace, then inspect teammate status, overhead name, scoreboard and kill feed on every client. Repeat for AFK substitution.

**[QA-035] [P2] [H — Core-loop allocations] [ProjectUpdraft/Assets/Scripts/Weapons/ShotResolver.cs:32–62; ProjectUpdraft/Assets/Scripts/Player/PlayerController.cs shot resolution; ProjectUpdraft/Assets/Scripts/Input/TouchInputManager.cs:338,796]**

**Expected:** gameplay core loops avoid per-frame/per-shot allocations under the performance contract. **Observed:** seeded shot resolution allocates a pellet array and `System.Random` per shot; touch UI hit testing calls `GraphicRaycaster.RaycastAll`; layout editor text splits strings. These are allocation sites, not evidence of a measured frame-time or GC failure. **Evidence: STATIC-ANALYSIS-ONLY.** Human/device reproduction: profile a release build on the 3 GB reference device for a 20-minute fight with all six players firing and pads active; capture per-frame GC allocation, collection pauses, frame-time percentiles, and allocation call stacks. Compare against the stated performance budget.

### Dispositions of the earlier findings

These statuses supersede the earlier finding text. “Static correction present” means the cited defect mechanism was re-traced in the current source; it does not mean its regression test passed in this audit.

| Earlier ID(s) | Current disposition |
|---|---|
| QA-001 | **Still FAIL, revised evidence.** Required Resources now exist; checked-in online backend URL is development loopback. See current finding above. |
| QA-002–028 | **Static correction present; runtime/integration acceptance UNVERIFIABLE.** Current tree contains corresponding navigation, input, camera, pad, combat, party/queue, results/reward, AFK, sizing, settings/audio/cue/cosmetic, reporting/recovery, telemetry and deletion changes plus regression tests. No test was run; retain their exact procedures in Section 5 before claiming accepted behavior. |
| QA-029 | **Partial correction; current P2 FAIL above.** Search text includes elapsed time and the returned region when supplied; at 45 seconds it offers “PRACTICE INSTEAD”; reservation offers it after 20 seconds. The action starts offline practice, but the server cancellation route only withdraws `searching` tickets, so matched reservation cleanup is statically incomplete. |
| QA-030–032 | **Still FAIL**, with current source evidence and procedures above. |
| QA-033 | **Still FAIL**, with current source evidence and procedure above. |
| QA-034 | **Static correction present.** `NetPlayer.Despawned` calls `NetBotDriver.Forget`, and `Decide` prunes destroyed keys. Long-session memory behavior remains UNVERIFIABLE (U19). |
| QA-035 | **Still FAIL (static allocation sites)**, with current source evidence above; performance impact remains UNVERIFIABLE. |

QA-028 also has changed since its original finding: the backend now contains token-based email confirmation and a completion route. The production sender/configuration, externally reachable URL, and end-to-end deletion completion remain **UNVERIFIABLE**; the older assertion that no completion route exists is superseded. The online product screens and resources now exist, but `STEP_E.md` still records Gate E as NOT ACCEPTED and identifies publisher credentials/client identity and deployed integration gaps.

### Current coverage verdict changes

| Coverage item | Current verdict |
|---|---|
| A4 tutorial death feedback | **PASS (static correction present)**; run U02 before acceptance. |
| B5–B12 touch/camera/gyro/editor/handedness | **UNVERIFIABLE** on device. Source corrections address earlier ownership and camera findings; QA-030 remains the gyro usability finding. No UI-to-camera leak was reproduced because no runtime was launched. |
| C15 pad restrictions and preview | **PASS (static correction present)** for inspected footprint/clearance/rejection-reason paths; concurrent placement and real geometry remain U07. |
| D19–D24 combat presentation/authority | **UNVERIFIABLE** for network behavior; static changes add shared deterministic spread inputs, hit confirmation and audio cues. QA-031 and QA-032 remain FAIL. Do not infer client/server parity from comments or unrun tests. |
| E25–E28 online flow | **FAIL:** QA-001 remains P0 and QA-029 leaves matched reservations alive when practice is selected. Other queue/join/rematch behavior is **UNVERIFIABLE** end to end. Gate C and Gate E are not accepted. |
| F29–F33 UI/error coverage | **UNVERIFIABLE** on device/backend; source changes address prior layout/navigation issues. QA-033 remains a bot-label FAIL. |
| G bots | **FAIL:** QA-033. Reaction, retreat, LOS and no-hidden-position logic remain static evidence only; believability requires U20. |
| H performance | **UNVERIFIABLE** on current build/device matrix; QA-035 records allocation sites only. |
| K deletion/report/purchases | **UNVERIFIABLE** in deployed environment. In-app/backend source flows are present; production email, publisher identity, external URL and Google Play purchase verification/restore/refund evidence are absent. Shop and ads remain correctly gated off. |

The design-intent review and omission-by-omission answers in Section 6 remain applicable. No v1.1 amendment has been located, so any amendment-only requirements remain UNVERIFIABLE. STEP_A is absent; STEP_1 is not substituted silently. `docs/known-issues.md` remains absent. Existing test XML and acceptance claims were read only and do not verify this modified tree.

### Updated top 10 actions by player impact per effort

| Rank | Action | Player impact / effort |
|---:|---|---|
| 1 | Replace the loopback backend configuration with publisher-controlled deployment settings and prove the six-client join path. | Online mode blocked / medium plus infrastructure |
| 2 | Make the reservation-time practice action withdraw or safely expire its matched ticket/allocation. | Prevents hidden server work / small–medium |
| 3 | Label a human slot immediately when bot substitution takes control. | Trust and fairness / small |
| 4 | Render actual reload state and progress in both offline and online HUD paths. | Fight readability / small |
| 5 | Show a clear death-direction cue alongside killer and weapon. | Learn from deaths / small–medium |
| 6 | Add a gyro neutral/recenter flow and test sensor drift on supported devices. | Optional-control usability / small |
| 7 | Profile and remove high-frequency allocations that breach the measured device budget. | Stability on low-end devices / medium |
| 8 | Run touch ownership, target-size, custom-layout and one-handed reach checks on physical phones. | First-match control reliability / test effort |
| 9 | Run the six-client latency/loss, disconnect, rematch and server-failure matrix. | Main online loop / infrastructure and test effort |
| 10 | Complete account recovery/deletion deployment and collect pad, retention and thermal evidence before store work. | Account safety and launch risk / operations plus external study |

### Remaining hands-on work

The detailed procedures U01–U20 in Section 5 remain the required worklist. In particular, first-match elapsed timing (U02), four-finger/two-thumb touch and gyro (U03), six-client matchmaking (U04), camera geometry (U06), pad validation and landing (U07), Relay/tick boundaries (U08), authoritative TTK and shot parity (U09), 100 ms hit feedback/death comprehension (U10), error injection and QA-029 practice fallback (U11), results/reward persistence (U12), bot substitution/behavior (U13/U20), settings and accessibility (U14/U15), support/deletion/purchase deployment (U16/U17), telemetry integrity and retention cohorts (U18), the current device/performance matrix (U19), and external return-intent study (U20) have not been performed in this audit.

The executive verdict remains **NOT-READY**. Current static fixes reduce the code-level defect list, but the online entry still points to localhost, bot substitution is not labeled, and the required Gate C/D/E and production evidence are not accepted. No player-facing release can be certified from this tree.

This report certifies nothing. It is evidence for the publisher's decision.
