PROJECT UPDRAFT — FINAL MASTER SPECIFICATION (v1.0)
Merge of both plans: Astra's design/tech discipline + bot-filled lobbies + rewarded ads + BR growth path. Hand this entire document to Claude Code or your developer as-is.
Working title: UPDRAFT. Verify name availability before publishing.
Pitch: Three teammates. Three minutes. Build your own shortcuts — and watch who follows you.
Decision: Third-person 3v3 team arena shooter → grows into a 12-player battle royale mode (Phase 2) on the same stack.
Platform: Android first, landscape, 16+.
Monetization (corrected): Rewarded ads post-validation + permanent cosmetics. No pay-to-win, ever.
Success gate: Milestone D playtests. Not opinions. Not hype.
1. RETURN TRIGGERS (every feature must serve at least one)
Table
Reason	Source
Immediate satisfaction	Responsive movement, readable hits, strong sound
Learning	Aim, routes, smarter pad placement
Unpredictability	6 players rewiring movement mid-match
Team connection	Saves, coordinated pushes, party codes
Identity	Earned cosmetics, mastery badges
Forbidden: streak punishments, energy systems, expiring purchases, frustrating progression.
2. FIRST-RELEASE SCOPE (production contract)
Include: 3v3 public mode (Relay), one arena (Switchyard), three weapons, one shared pad ability, tutorial, offline practice, parties of 3, contextual pings, cosmetic progression, settings/account recovery/reporting, small cosmetic shop (post-validation), rewarded ads (post-validation), bots in public lobbies when queue times exceed 45s, performance + gameplay analytics.
Exclude v1: battle royale (Phase 2), vehicles, attachments, hero classes, ranked seasons, clans, battle pass, loot boxes, voice/free chat, destructibles, trading, killcam, user maps, extra modes.
3. MODE: RELAY
Zones: Center (0,0), West (−22,0), East (+22,0). Radius 6m. One active zone at a time.
Uncontested team earns 1 point/second of valid occupation. Contested = 0. Empty = 0. Multiple teammates don't speed scoring. Kills don't score.
Valid occupation: alive, feet inside 6m radius, feet ≤1.5m above zone floor, not spawn-protected. Flying over = no capture.
Schedule: 0–30 Center, 30–60 West, 60–90 East, 90–120 Center, 120–150 East, 150–180 West. Show next zone 10s early. No early capture on inactive zone.
Win: first to 120 points, or higher score at 180s. Tie → 30s overtime on current zone; first uncontested 3s hold wins; empty/contested resets; 30s without a hold = draw.
No hidden comeback bonuses. Comebacks come from respawns, movement, objective rotation.
4. MATCH FLOW
Home → Play/Party → Matchmaking → Server reservation → Load → 8s prep (weapon select, objective shown, spawn-locked, no damage, no pads) → 3s countdown → Match → Results → Play again/Home.
Match start: full HP/ammo, 1 pad charge, spawn exits open, authoritative timer starts.
Match end: scoring/damage stop instantly; result + stats + rewards on one screen; "Play again" immediate; party preserved; return to queue.
Reserved player fails to load in 30s → cancel start, return others to queue. Never start a public match with an empty slot.
5. MOVEMENT
Table
Property	Value
Health	100
Ground speed / firing speed	6 / 5.1 m/s
Acceleration / Gravity	35 m/s² / 20 m/s²
Jump velocity / height	6 m/s / ~0.9 m
Capsule	1.8m h × 0.35m r, step 0.35m
Respawn delay	4s
No stamina, sprint, crouch, slide, prone, climbing, or fall damage. Falling off map = instant elimination. Players don't block each other. Weapons don't change speed. Cosmetics never alter hitboxes.
Regen: starts 5s after last damage, 25 HP/s, interrupted by new damage. No healing items.
6. LAUNCH PADS (signature mechanic)
Tap: place ~1.6m ahead facing camera horizontal. Hold >180ms: preview trajectory; camera steers; drag to cancel; invalid placement costs nothing.
Restrictions: grounded; surface ≤5° tilt; fits fully; never in spawn rooms or within 5m of exits; never inside a capture circle; no overlap; reject if launch path immediately hits overhead geometry.
| Property | Value |
|---|---|
| Charges / recharge | 1 / 16s |
| Lifetime / arm time | 18s / 0.75s |
| Pad HP / diameter | 60 / ~1.4m |
| Launch velocity | 14 m/s horizontal, 12 m/s up |
| Max per owner / per match | 1 / 6 |
| Activation lockout | 1s per player |
Standing on an armed pad turns jump into a labeled LAUNCH action. No auto-launch. Anyone can use any pad — enemies included. Enemies can destroy pads (friendly fire can't). Pads don't block movement.
Launch replaces velocity (never stacks). 30°/s air steering preserving horizontal speed. Can shoot mid-air (airborne spread applies). Landing = normal movement. No shockwave.
Death doesn't refresh cooldown; pad outlives owner; new placement removes old one.
Readability: directional arrow always; team color + symbol; charging sound; launch/landing sounds have gameplay priority; VFX never hide the launched player.
7. WEAPONS (all unlocked, one equipped, no switching while alive)
Table
ARC rifle	ZIP SMG	BRICK scattergun
Body damage	18	13	9/pellet
Fire rate	480 rpm	780 rpm	75 spm
Mag / reload	24 / 1.6s	30 / 1.5s	6 / 2.0s
Pellets	1	1	8
Full dmg to	20m	10m	7m
Falloff end / dmg	45m / 10	28m / 6	20m / 3/pellet
Max range	55m	35m	25m
Head multiplier	1.25× all
Linear falloff between thresholds; fractional damage internally, rounded for UI only. BRICK cannot one-shot a full-health player, even all-head. Server-seeded spread; scattergun pellets evenly distributed (seeded rotation, no clustering).
Spread cone half-angle (stationary/moving/airborne): ARC 0.45/0.75/1.2°, ZIP 1.0/1.3/2.0°, BRICK 4.0/4.0/4.5°.
Recoil per shot: ARC 0.55° up, ZIP 0.30° up, BRICK 2.0° up. No horizontal recoil. Client and server share the model. Unlimited reserve ammo; auto-reload default; reload blocks firing; no friendly damage; friendly bodies don't block bullets; weapon skins never change identifying sounds.
8. CONTROLS & CAMERA
Floating left joystick; right-side drag to aim; fire button drag also aims; jump/launch above fire; pad button left of fire; reload near ammo; ping upper-right; pause upper corner.
Simultaneous move+aim+fire mandatory. One touch = one owner. UI touches never rotate camera. ≥48dp targets. Notch-safe. Left-handed layout. Full button move/size/opacity/reset. Settings save locally. Sensitivity, invert-Y, vibration, gyro aim (off by default).
Third-person shoulder cam: 55° FOV, 3.2m follow, 0.45m shoulder offset, upper-torso pivot, sphere-cast wall clip prevention, subtle shake (disableable), no combat cinematics.
Aim assist: slowdown only, ≤3° cone, ≤25m, sensitivity drops to 70% inside region, no snap, no tracking through cover, no magnetism, no auto-fire, off switch.
TPP shooting correctness: aim point from camera, shot traced muzzle→point; walls can block even when camera sees over; show blocked-shot indicator.
9. MAP: SWITCHYARD
Bright industrial rooftop training arena. No real military branding. 1 Unity unit = 1m. Origin = Center zone.
Footprint 96×80m (X −48..+48, Z −40..+40). Ground Y=0. Center (0,0), West (−22,0), East (+22,0), radius 6m.
North spawn (X0, Z+35, ~20×8m), South spawn (X0, Z−35). Three exits each, mirrored layout.
Raised platforms 10×8m at 3m height, centers X±20/Z±14 — each with two walkable ramps. Main routes ≥4m wide.
Cover: central sightline blocker per spawn (Z±20), short segments around each zone perimeter, zone interiors mostly open, outer-lane breakers, no single vantage sees all three zones.
Spawn protection: 3 exits; enemies can't enter; barriers block shots both ways; can't fire out from inside; respawn grants ≤1.5s protection ending on fire or exit; protected players can't score.
Acceptance: every objective ≥3 approaches; every platform ≥2 walkable approaches; no spawn-to-spawn direct shot; combat within 8–12s of leaving spawn; pads never create unreachable firing spots; no map escape via camera collision/pads/ramps/speed.
10. DEATH & DISCONNECT
Elimination: short feedback anim, killer + weapon shown, 4s respawn countdown, weapon select while waiting, teammate camera view. Full HP + mag on respawn. No killcam, no paid revives, no paid respawn timers.
Disconnect: avatar stays; after 5s a labeled bot drives it; player can reconnect to the same avatar until match end; no refreshed HP/ammo/charge; never refill with a stranger; bot-substituted matches flagged in analytics and excluded from balance stats.
Server failure cancels match. No invented winners, no hidden skill loss.
11. TUTORIAL (~90s, no forced timer, skippable/replayable)
Move to marker → 2) kill stationary target → 3) reload → 4) place pad → 5) ride own pad to platform → 6) use enemy-colored pad → 7) destroy enemy pad → 8) capture a zone → 9) short bot fight. One instruction at a time; advance on completion. Explicitly teach: both teams can use every pad. After tutorial: offer practice or public play. Default weapon ARC. No forced account, no shop popup, no notification permission.
12. BOTS
Exist in: tutorial, offline practice, disconnect substitution, and public lobby fill when queue exceeds 45s (labeled [BOT], never hidden from telemetry).
Same movement/HP/ammo/cooldowns/weapons as humans, driven through the same input interface. LOS + audible-event perception only — never hidden position reads. Authored routes and supported pad transitions. Rotate to active objective. Retreat when hurt. No synchronized focus fire. Reaction delay: easy ~600ms, standard ~350ms. Imperfect tracking, never secretly reduced damage, never tuned to force wins.
Offline practice: no rating, no online currency.
13. MATCHMAKING
One public queue, one region (pick by measured latency from your concentrated test market, not geography). Max party 3 via 8-char code. Compositions: 3v3, duo+solo vs duo+solo, solos vs solos. Never silently match a full party vs beginners.
Hidden rating: start 1000, team = average, Elo K=24, win/loss/draw only, no public rank v1, skip bot-substituted matches.
Search: evaluate every 2s; target Δ150; after 20s expand +50 per 10s; max Δ350; prefer RTT <120ms; no silent region shuffling. After 60s: "Still searching — practice or wait" (stay queued in practice).
Early testing: scheduled play windows so testers meet each other. Never advertise an always-on community before one exists.
14. COMMUNICATION
Contextual pings only: "Go here," "Enemy here," "Group up," "Pad ready." Enemy pings require a visible target; mark the location, not a wall-hacked player; 3s expiry; one ping per 2s per player; mute available. No free text, no voice. Generated aliases (AmberFox-4821). Post-match opt-in party formation only.
15. VISUAL IDENTITY
Stylized utility sport: clean shapes, strong silhouettes, movement-ready gear, one body rig with material variants.
Table
Role	Hex
Menu bg	#101827
Panel bg	#1B2940
Friendly	#30D5E8
Enemy	#FF6376
Objective	#FFD166
Main text	#F4F7FB
Secondary	#BBC6D8
Concrete	#C3B59E
Structural blue	#49627B
Color never the only signal — pair with icons + outline patterns. Enemy outlines only when actually visible. Emissive reserved for gameplay info. No glossy floors, heavy fog, dark corners. Cosmetics can't be smaller/quileter/darker or harder to ID.
Asset budget: 1 character mesh/rig, 3 weapon meshes, 1 pad mesh, ~15–20 modular env pieces, ~12 core animations, small reusable VFX. Original assets or verified commercial licenses — maintain a license register. "Free download" ≠ free commercial use.
16. AUDIO
Per weapon: fire/reload/empty. Footsteps, landings, pad deploy/arm/launch/destroy, hit + elimination confirm, damage taken + low HP, zone activate/contest/milestone, countdown, result, UI.
Priority: threats > launch/landing > objective > cosmetic/menu. Stereo positioning, distance attenuation, simple obstruction. Enemy footsteps and pad sounds stay audible in firefights. Separate volume sliders (master/SFX/music/announcer). Quiet combat music. No full-screen flashes.
17. SCREENS
Startup (loading/retry/offline access) · Home (Play, Practice, Party, Appearance, Settings) · Party (members, code, ready, leader) · Matchmaking (elapsed, region, cancel, practice) · Loadout (3 weapons, plain roles) · Gameplay HUD (scores+clock top-center; active zone + rotation countdown under; teammate status upper-left; HP low-center; ammo by fire; pad state on button; objective edge marker; network warning not over crosshair) · Results (outcome, stats, rewards, Play again, Home) · Appearance (preview/equip) · Shop (exact contents, localized price, purchase/restore status) · Settings · Report.
Explicit failure states, never an unexplained spinner: no internet, auth down, server full, queue cancelled, party member dropped, update required, match server down, purchase pending/failed, reward retry pending.
18. PROGRESSION (no power, ever)
Profile XP: +120 eligible public match, +30 win, +15 draw. Level 1 at 0; each next level +400 XP; track ends at 20; no resets. Eligible = present ≥75% of match, no AFK removal, stayed through results; server-failed matches ineligible for rating (participation comp logged separately).
AFK: 30s idle → warning; +15s → removal + bot substitution. Movement/aim/fire/interaction count as activity; app open doesn't.
Credits: +20 eligible completion, +5 win, +20 level-up. No credit purchases v1, no transfers, no offline earning.
Unlocks: starter set immediately; next after 3 eligible public matches; then levels 3/6/10/15/20. Credit shop: body style 300, weapon wrap 200, badge 100. All weapons/mechanics unlocked from the start.
Track personal bests (objective time, pad-assisted plays) — don't XP-incentivize every stat or players farm stats instead of winning.
19. MONETIZATION (corrected from source plan)
Pre-validation: no shop prompts, no ads.
Post-validation: rewarded ads (AdMob) — rewarded-only, never interstitial, never forced: extra crate roll (1/day), small credit bonus (capped/day), cosmetic trial (24h). All skippable, all capped.
IAP: three permanent cosmetic bundles — Signal $1.99, Foundry $3.99, Skyline $5.99 (test prices; show Play-returned localized prices). Preview everything; label as cosmetic; permanent; no random rewards, no premium currency, no paid stats, no paid respawns, no mid-match shop, no forced interstitials.
Rules: backend-verified purchases, exactly-once grants, pending handling, restore after reinstall, refund/revocation handling, acknowledge within Google window (~3 days). Never enable payments until the full lifecycle passes testing.
20. TECH STACK
Table
Layer	Choice
Engine	Unity 6.3 LTS + URP (small mobile config)
Language	C#, Unity Input System
Netcode	Photon Fusion 2, Server mode
Match authority	Dedicated Linux headless Unity server
Movement	Fusion-compatible KCC (start: Simple KCC)
Backend	ASP.NET Core, single modular service
DB	PostgreSQL 16
Deploy	Docker (backend + DB) + controlled game-server allocator
Android	IL2CPP, ARM64, AAB
VCS	Git + LFS; pinned versions
Architecture: client → HTTPS (accounts/parties/matchmaking/inventory/purchases) + Fusion (match server); match server → authenticated backend (results/telemetry); backend → Postgres + allocator + purchase verification. No Firebase, no second account system. Offline practice runs the same rules via local simulation, no credentials needed.
21. SERVER AUTHORITY
Server owns: position/movement legality, health/damage, weapon state, cadence, pads, occupancy, scores, results, rewards. Client sends input + shows prediction. Never trusted: damage, scores, wallet, entitlements.
30 ticks/s; local prediction + reconciliation; remote interpolation; hitscan lag comp (Server mode), max rewind 150ms initial, bounded and validated.
Tick order: validate inputs → movement/abilities → shots/damage → eliminations/respawns → zone transitions → occupancy/scoring → match end. Same-tick ties resolved deterministically.
Security: validate bounds/cadence/ammo/placement/cooldowns; rate-limit APIs; authenticated result submission; short-lived account-bound join tickets; reject duplicate rewards; secrets off-client; log suspicious patterns; no auto-bans from one latency anomaly.
22. BACKEND & PERSISTENCE
Ledger for all currency: player_id, change, reason, unique source_id, timestamp; unique constraint prevents double-grant.
Records: account, linked platform identity, XP, credit ledger, inventory, equipped appearance, hidden rating, participation/results, purchase transactions/entitlements, reports, deletion requests.
APIs: Auth (guest session, Google Play Games link/refresh/revoke) · Profile (read, equip, inventory) · Party (create/join/leave/ready) · Matchmaking (enqueue/cancel/status/ticket) · Matches (reconnect, server-only result submit) · Commerce (catalog, verify, restore) · Support (report, account deletion).
Ownership from validated session only — never from request-body IDs. Offline practice = no account. Guest play allowed. Require linked recoverable identity before purchasing. Never auto-merge wallets.
23. PERFORMANCE BUDGETS
Min Android 9, 3GB RAM, ARM64. Low preset: sustained 30 FPS; mid/high 60 where tested. Download ≤250MB, installed ≤600MB, steady memory ≤750MB on Low. Startup ≤8s, arena load ≤10s post-reservation. Netcode ≤100KB/s combined per client (measured in 6-player fights, all pads live, real protocol overhead). Low preset: ≤150 draw calls, ≤250k visible tris, dynamic res with floor, baked lighting, minimal shadows, no bloom/motion blur/DoF/SSAO, pooled effects, no per-frame allocations in core loops.
Test matrix: 3GB old device, 4GB different-GPU device, current midrange, 16KB-page device, multiple aspect ratios/notches. 20+ min thermal runs. Never claim "works on every cheap phone" from one test.
24. ANALYTICS & KPI GATES
Schema-versioned events: first_open, tutorial_started/step/completed, queue_started/cancelled, match_reserved/started/completed, disconnect/reconnect, pad_placed/used/destroyed, weapon_selected, results_play_again_clicked, purchase_started/verified, client_error. Server owns match facts; client owns UI actions. Always: build/balance version, pseudonymous ID, match ID, region, device tier, party size, human/bot flag, cohort, timestamp. Never collect contacts/chat.
KPIs (soft-launch decision thresholds):
Tutorial completion ≥80% · D1 ≥35% · D7 ≥15% (later D30 ≥5%) · crash-free ≥99.5% · voluntary instant rematch ≥50% · median public queue <45s during windows.
Cohorts from first open; D1 = match 24–48h after; separate tutorial/practice/public users; separate incentivized testers; report sample sizes.
Kill/pivot triggers, set in stone: two failed KPI passes → fix core loop or pivot (Phase 2 re-skin), no sunk-cost spending.
25. PAD VALIDATION STUDY (before any store work)
30–50 non-friend target players, real phones, two counterbalanced versions: with pads vs without. Measure: comprehension without coaching, purposeful placement, whether pads create opportunity or confusion, mid-air aim comfort, ground-route viability, which version players voluntarily replay. No rewards for choosing the pad version. Pass = most explain/use it unprompted + clear preference + voluntary rematches + readable touchscreen combat. Two failed focused revisions → simplify or cut the mechanic before monetizing anything.
26. MILESTONES (gated, sequential — no parallel feature creep)
Table
Gate	Deliverable	Exit condition
A	Foundation: project, Android build, graybox, movement, camera	Playable on reference phone
B	Offline combat slice: 3 weapons, HP, respawn, pads, bots, scoring, full loop	Complete offline match
C	Network slice: dedicated server, 6 clients, scoring, reconnect	Correct under 30/80/150ms RTT + loss/jitter
D	Gameplay validation: tutorial, sound, placeholder art, telemetry + pad A/B study + external playtests	KPI table reviewed; go/kill decision
E	Product shell: accounts, parties, progression, settings, reports, bot-filled public lobbies	Complete reliable player journey
F	Monetization: rewarded ads + catalog + purchase lifecycle	Billing tests pass end-to-end
G	Closed launch: optimized assets, ops, store materials	Retention + reliability gates met
H	Controlled one-market release	Economics (CPI vs LTV) understood
Timeline allowance: 4–6 weeks for serious prototype (A–B); 6–9 months to G. Estimates, not promises. Pay devs per milestone acceptance — working build + source + tests, never screenshots.
Pre-D freeze: no clans, maps, or battle pass while D is unresolved.
27. PHASE 2 — GROWTH INTO ARENA ROYALE (only after D7 ≥15% sustained)
Same stack, same guns, same art kit (~60% reuse):
New mode: 12-player FFA/squad BR, 4-min cap, shrinking zone, bot-filled lobbies early.
Reconnect + spectator voting retained; one additional stylized map.
Monetization widens: starter packs ($0.99–2.99), second battle-pass-style track only if DAU >10k.
KPIs identical. Same kill-gates. Phase 2 is a bet placed with evidence, not hope.
28. PRE-RELEASE TEST LIST (must pass, no TODOs)
Gameplay: exact damage/falloff/cadence, head multipliers, BRICK one-shot ceiling, pad validation, cooldown-through-death, replacement + 6-pad cap, no velocity stacking, contested scoring, every zone transition, score-limit/timer/overtime/draw wins, spawn barriers, weapon-change timing.
Network: 6 clients @ 30/80/150ms, loss + jitter, disconnect during fire/launch/capture/respawn, reconnect same avatar, duplicate join/result, wrong client clocks, server crash, backend outage, no resource growth over repeated matches.
Economy/accounts: no duplicate rewards from duplicate results, offline save-edit can't mint currency, double-buy can't duplicate entitlement, pending purchase ≠ ownership, restore after reinstall, refund/revocation handled, request-body IDs can't change ownership, deletion path works.
Devices: thermal, background/foreground, phone calls, Wi-Fi↔mobile switch, low storage, multi-touch, aspect ratios, accessibility, 16KB pages.
29. OPERATING COSTS (real, planned)
Unity Personal (eligibility-checked) · Photon launch tier (100 CCU free, one game — verify at signup) · dedicated game-server compute · backend/DB hosting · backups/monitoring · store deductions · support · acquisition.
Capacity: ACU = DAU × avg daily min ÷ 1440 (e.g., 1000 DAU × 18min ≈ 12.5 ACU; peak differs — measure). Benchmark CPU/RAM/bandwidth per 6-player match before buying capacity.
Controls: max simultaneous servers, clean rejection at cap, keep matches running, billing alerts (not a hard limit), no uncapped autoscaling, load tests inside a traffic budget.
If cash = dev payment + $25 Play fee only: stop after Milestone D and run limited scheduled tests. A live commercial launch needs an operating budget.
30. LAUNCH & ACQUISITION
Recruit adult mobile-shooter testers in one concentrated market. 2) Scheduled sessions. 3) Record real gameplay (permission). 4) Short clips of understandable pad plays. 5) Small creator seeding with honest stage description. 6) Track which messages bring back retainers, not views.
Test three messages: "Three-minute team battles" / "Build shortcuts your enemies can steal" / "A movement shooter made for touch." Scale spend only when net LTV per acquired player > CPI with margin — net of refunds, platform cut, service costs. Never compare ad spend to gross sales.
Publishing checklist: publisher-owned Play Console, publisher-controlled signing, final app ID, license register, privacy policy matching actual collection, Data safety, rating questionnaire, support email, in-app + web deletion path, real screenshots/video, tested catalog, monitoring + rollback. Target API per Google at submission (currently 36 / Aug 31, 2026 — recheck). New personal accounts: 12 testers × 14 days closed test before production access.
31. OPS & MAINTENANCE
Separate dev/stage/prod configs; client-server compat checks; versioned balance config (fixed per match); daily encrypted backups + tested restore; health/capacity monitoring; alerts for match + purchase failures; strongly-authed admin tools; independent matchmaking/purchase kill switches; config rollback. Retention: raw analytics 30d, aggregated 180d, security logs per need, purchases per accounting/law — documented in privacy policy. Someone reads reports before launch. Content priority: 1) controls/combat clarity 2) perf/net 3) routes/balance 4) tutorial 5) cosmetics 6) second map only after mode 1 retains.
32. PUBLISHER DELIVERABLES (from devs, every milestone)
Full source repo from day one · editable Unity project · backend source · DB migrations · build scripts · deploy config · lockfiles · asset-license register · balance config files · test results · device perf report · setup docs · known-issues list · account/key inventory.
Config assets (never hardcode): MovementConfig, WeaponConfig, PadConfig, MatchRulesConfig, MapConfig, BotConfig, MatchmakingConfig, ProgressionConfig, GraphicsConfig, CatalogConfig. Every milestone = working build + repro instructions.
33. HANDOFF INSTRUCTION (paste this with the document)
Implement Project UPDRAFT per this complete specification. Begin with Milestones A and B: an Android-playable offline vertical slice — graybox arena, movement, camera, three weapons, launch pads, bots, scoring, respawns, complete match loop.
Treat all gameplay values as the initial authoritative configuration in named config assets. Do not add out-of-scope features. Placeholder art with clear silhouettes and commercial-compatible licenses; log every asset/dependency. Keep presentation separate from authoritative rules so the same rules run locally and on the dedicated server.
For unspecified details, choose the simplest consistent solution and record it in the repo. Don't stop for cosmetic or routine architecture questions.
If credentials/publisher identity/paid infra are missing, document the dependency and continue all independent work. Never fabricate credentials, enable paid services silently, or present mocks as production-ready.
Deliver working increments with reproducible builds and tests. A feature isn't done because its UI exists — match flow, error states, device behavior, and persistence must work.
After the offline prototype, build the six-client dedicated-server slice (Milestone C). Do not build the shop, ads, or Phase 2 before Milestone D passes.
The first thing to buy is six people finishing a match and independently wanting another. Everything larger — shop, ads, Phase 2 BR — is bought with that evidence.
