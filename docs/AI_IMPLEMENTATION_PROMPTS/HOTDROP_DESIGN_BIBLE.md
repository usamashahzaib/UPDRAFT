# HOTDROP — GAME DESIGN BIBLE v1.0
## "The Sky Is The Third Lane"
**Owner:** Creative Director / Product Owner
**Stack:** Unity (client) + .NET (backend)
**Status:** Replace-everything draft. This document is the single source of truth for AI coding agents (Codex / Claude Code) and human developers.

---

# PART 0 — EXECUTIVE POSITIONING (READ FIRST, EVERYONE)

## 0.1 The market truth nobody tells you
PUBG owns *slow tactical survival*. COD owns *arcade infantry gunplay*. Apex owns *hero movement*. Fortnite owns *building + culture*.
**Nobody owns VERTICALITY as the core identity.** Flight is always a gimmick (a jetpack pick-up, a helicopter killstreak). No mainstream mobile shooter has made **the vertical axis the primary skill expression.**

HOTDROP is not "a battle royale." HOTDROP is a **3v3 rooftop-flight objective shooter** where the map is vertical, jump pads are weapons, and the winning play is one nobody on the ground can see coming.

**One-line pitch:** *"A shooter where the roof is floor one."*

## 0.2 Why this fixes your design problem
Your current screens try to look "military sci-fi terminal" because PUBG/COD look military. But HOTDROP's fantasy is not war — it's **parkour pilots**. Think: Sunset Overdrive energy + Titanfall movement + Valorant clarity. Bright, kinetic, readable, alive. The UI must feel like a *flight suit HUD*, not a server dashboard.

## 0.3 Non-negotiable law (the anti-copy, anti-clutter charter)
1. **Every screen answers ONE question in 3 seconds.** If it answers two, it's two screens.
2. **One primary action color.** Everything else is neutral. (Current screens violate this: cyan is on headers, buttons, accents, AND body text.)
3. **No fake jargon.** "AVIONIC ID // PUBLIC IDENTITY" → "Pilot Name". Players don't read lore-labels on functional UI.
4. **Cosmetics never touch power.** Ever. This is a competitive promise, printed on the shop itself.
5. **No dark patterns.** No loot boxes, no fake timers, no "99% off" lies. Unicorn companies are built on trust curves, not whales.

---

# PART 1 — THE FULL PLAYER LIFECYCLE (START → END → BETWEEN → IDLE)

This is the deep-think you asked for. Every state a player can be in, what the game is doing, what the player feels, and what the business is doing.

## 1.0 State machine overview
```
APP LAUNCH → COLD START → AUTH/ONBOARD → HOME (the hub loop)
   ↓                                              ↓
MATCH FLOW: QUEUE → LOBBY/LOADOUT → INTRO → MATCH PHASES → RESULTS → HOME
   ↓                                              ↓
META LOOP: PROGRESSION → CONTRACTS → MASTERY → SHOP → SOCIAL → (repeat)
   ↓
CHURN STATES: IDLE → PUSH/PULL BACK (events, friends, season) → EXIT
```

## 1.1 App Launch (0.0s – 2.5s)
- **Boot:** studio logo (0.4s, skippable on tap), straight into engine. No 5-logo parade. Measure launch frequency and cold-start time; do not assume a daily launch count.
- **Cold start rule:** first-ever launch shows *one* cinematic question: "Do you want to fly?" — then straight into movement tutorial. No account wall, no permissions wall before value. Guest play first, account link later (with a real benefit: cross-device progress + skin).
- **What's happening technically:** Unity splash → addressable pre-load of Home scene only (never pre-load match content; lazy-load during queue).
- **Emotional beat:** *speed*. The app must feel like the game: instant, light, airborne.

## 1.2 Home / Hub (the 80% screen — where players live between everything)
Home is NOT a menu. Home is a **rooftop at golden hour with your pilot standing on it.** Camera slowly orbits. This is your identity stage.
- **Left rail (max 6):** Play · Loadout · Pilots (appearance) · Contracts · Pass · Social.
- **Center:** the pilot + season countdown + one featured card (event or contract).
- **Right:** party squad stack (3 slots) + quick-chat from friends.
- **Ambient:** distant city audio, wind, a billboard that rotates real content (events, patch notes, your friends' achievements).
- **No red badges.** Badge fatigue is real. Only show a dot on things that *expire within 24h*.

## 1.3 Queue → Match flow (the funnel that decides retention)
```
TAP PLAY → (party ready-check 3s) → QUEUE with honest ETA
→ FOUND: map fly-through camera (4s) → LOADOUT (10s; weapon choice may also be changed while dead)
→ TEAM INTRO (3s, name banners + pilot) → DROP
```
**Status:** this is a proposed lifecycle, not the shipped Relay sequence. Relay currently uses Preparation → Countdown → InProgress → Overtime (when triggered) → Results; do not add the ready-check, fly-through, timed loadout, or team intro until those queue and presentation states are specified and implemented together.
- **Queue screen is dead time — kill it.** Show a live "flight school" drill (practice pad) while queueing, exactly like your current "Practice While Waiting" (keep it, it's your best idea).
- **Matchmaking honesty:** never fake "found match!" then load forever. Honest latency display, and the *reason* you matched these players ("similar flight-time, same region").
- **Loading is content:** show your next map tip, the last match's "play of the game" clip, or contract progress. Never a bare spinner.

## 1.4 The Match — internal phases (what PUBG/COD do that you must copy in *principle*, not content)
Every shooter match is a tension curve. Design yours explicitly:

| Phase | Time | Tension | What the game does |
|---|---|---|---|
| **Drop** | 0:00–0:20 | Curiosity | Both squads launch from opposite roofs. Pads on cooldown. Music: sparse pulse. |
| **Scramble** | 0:20–0:60 | Rising | First pads armed, first rotations. Announcer minimal: "Relay live." |
| **Contest** | 1:00–2:00 | Peak #1 | Objective (Center Relay) unlocks. Overtime stakes revealed. |
| **Mid-game reset** | on wipe | Dip | Respawn window, loadout re-pick (you already have this — "changeable on respawn" is correct, keep). |
| **Endgame** | last 60s | Peak #2 | Music layers full, HUD trims to essentials (auto-hide secondary panels — "no distractions" mode, see 9.0). |
| **Resolve** | final 5s | Release | Slow-mo final kill cam, winner banner, immediate rematch button (your "PLAY AGAIN" is correct). |

**Status:** the table above is a pacing concept, not the current Relay state machine. Current Relay has Preparation, Countdown, InProgress, Overtime, and Results; its configured three-minute schedule and score/hold end conditions govern live play. No wipe-triggered reset phase or slow-motion kill cam is implemented. Preserve individual respawns unless a reviewed mode spec changes them.

**Current Relay respawn contract:** eliminations respawn individually after the configured 4 seconds at a team spawn exit. Weapon choice can be made during preparation or while dead and takes effect at the next spawn. Respawn protection is configured to 1.5 seconds maximum and ends early when the player fires or leaves the protected spawn room. These values come from the current runtime configs; they are Relay behavior, not rules for another mode.

**Rule:** the map, timers, and music should support a deliberate tension curve. Treat the phase timings above as [ESTIMATE] until measured in playtests; do not infer that the map or players are at fault from a flat curve alone.

## 1.5 Results → the 30-second window (where unicorns are won)
Players decide "one more match" HERE, not at Home.
- **Instant:** scoreboard (you have it), then ONE personal beat: a 6-second highlight of *your* best moment (auto-generated: longest flight chain, a clutch pad save).
- **Progress, always:** even on defeat, something fills (contract ticks, mastery, pass XP). Never a screen that only says DEFEAT. *(Your current defeat screen shows stats — good — but add one earned reward line minimum.)*
- **One-tap rematch** + "squad stayed together" auto-prompt.

## 1.6 Between matches — the meta game (what happens when NOT playing)
Between-match activity share is [ESTIMATE] and must be measured with telemetry. Design this time as deliberately as gunplay:
- **Daily loop (5–15 min):** 1 featured contract + login cache. Respect the player's time; HOTDROP never demands 40-minute chores.
- **Weekly loop:** contract rotation, map/mode rotation, a "Flight School" challenge (movement trials with leaderboards).
- **Season loop (8 weeks):** new pilot, new map zone, new pad type, new weapon wrap set. **Zero power resets** — your "ZERO SEASON WIPES" instinct is 100% correct, print it bigger.
- **Social pull:** see a friend's highlight clip on Home → one tap → party up. Clips are your viral loop.
- **When idle 48h:** ONE push max, with a real hook ("Your contract is 90% done — one match finishes it"), never "we miss you."

## 1.7 Exit & return
- Exit mid-match: bot takeover (your bots exist), rejoin window 90s, no rank penalty if rejoined.
- Session recap on next launch: "Yesterday: 7 matches, 4 wins, best flight chain 22s" → immediate PLAY button.

---

# PART 2 — ART DIRECTION (COLOR, LIGHT, TYPE, VFX)

## 2.1 The identity: "Neo-Aviation"
World: a dense vertical megacity at permanent golden hour (sun low = long dramatic shadows = readable silhouettes). Materials: concrete, glass, steel, neon signage, fabric flight suits. Stylized-realistic (Valorant-style PBR-stylized hybrid): realistic lighting on slightly simplified geometry. Device performance is a target to measure, not a guarantee.

## 2.2 Color system (replace your current palette)
Your current navy+cyan+gold is *technically fine* and *emotionally dead* because it's applied to everything. New system:

| Role | Hex | Usage |
|---|---|---|
| **Base dark** | `#0C1220` | Backgrounds (not pure black — pure black kills perceived depth) |
| **Surface** | `#17202E` | Cards, panels |
| **Text primary** | `#EDF2F7` | Headlines |
| **Text secondary** | `#8A94A6` | Body, captions |
| **Primary action** | `#3DE0C0` (mint) | Clickable action only |
| **Ally** | `#4DA3FF` (blue) | Team; shape cue required |
| **Enemy** | `#FF5A66` | Threat — used ONLY for enemies and damage. (Never for buttons.) |
| **Objective / gold** | `#FFB84D` | Relay, rewards, currency |
| **Danger zone / storm** | Deferred | Define a zone mechanic before assigning this role |

**Law:** any new UI element must declare its role. If it doesn't fit a role, it doesn't ship.

## 2.3 Typography (kill the all-mono look)
- **Display (headers, big numbers):** a condensed geometric sans — e.g., *Rajdhani*, *Chakra Petch*, or *Saira Condensed*. Uppercase only for true labels.
- **Body/UI:** clean humanist sans — *Inter* or *Noto Sans*.
- **Mono (accents ONLY):** *JetBrains Mono* or *Share Tech Mono* — reserved for telemetry numbers, killfeed, countdowns. Mono = flavor, not foundation.
- **Type scale (exact):** 12 caption / 14 body / 17 UI / 22 sub-head / 30 title / 44 display. Never more than 3 sizes on one screen.

## 2.4 Lighting & mood
- **Time-of-day per map** is a brand asset. Golden hour is the current concept baseline; night-market variants remain art-direction candidates. Do not create a storm variant or danger-zone palette until the zone mechanic is defined. World-art execution remains gated by the reviewed world-art/rating specification.
- **Rim light on pilots:** every character gets a subtle rim light so silhouettes read against any roof — competitive clarity is a lighting feature.
- **Sky sells flight:** volumetric-ish gradient skybox (cheap: layered gradient + sun disc + animated cloud billboards), birds, distant air traffic. The sky must look *inviting*, because it's 50% of your play space.

## 2.5 VFX direction
- **Jump pad:** anticipation ring → compression burst → launch trail. Player must *feel* the launch in their stomach: 3-frame FOV kick + slight chromatic edge + haptic.
- **Flight/glide:** speed lines at periphery, wind audio as the speedometer (no speedometer UI — audio IS the UI).
- **Relay capture:** concentric rings filling toward center; team-colored; readable from 100m away (this is your A-site smoke — it must be a beacon, see 5.6).
- **Damage:** directional hit indicator arc (modern standard), brief desaturate vignette at low HP, no screen-shake spam (max 2 events/sec).

---

# PART 3 — GUNPLAY & COMBAT REALISM ("same looking guns in reality, firing reality")

You can't copy COD's guns — but you CAN study the *engineering of gun-feel*. Gun-feel is a formula, not an asset:

## 3.1 The response chain (measure perceived timing in playtests)
1. **Input** (touch) → 2. **Audio, muzzle flash, and tracer** (same-frame target; perceived order is [ESTIMATE], test it) → 3. **Recoil impulse** (camera response, tune in playtest) → 4. **Server-confirmed hit feedback** (impact VFX + hitmarker) → 5. **Kill confirmation** (distinct tone and killfeed entry; slow-motion duration is [ESTIMATE] and needs platform testing).

## 3.2 Realism model (stylized-physical, not mil-sim)
Your three weapons map to real archetypes — make each *feel* like its real counterpart without licensing anything:
- **ARC RIFLE = precision auto (AR archetype):** tight first-shot spread, mild vertical climb you must pull down, 20m drop-off. Model language: long barrel, carry handle, straight magazine — *AR-15 silhouette family* (the most readable rifle shape on Earth, generic enough to be yours).
- **ZIP SMG = high-RoF (MP archetype):** wild close spread, fast reload, strafe-speed bonus. Model: compact, top-mounted mag, collapsible stock (P90/MP5 flavor).
- **BRICK SCATTERGUN = pump shotgun archetype:** 8-pellet cone, brutal 0–7m, feather 8m+. Model: boxy receiver, exposed tube, heavy stance.
- **Ballistics:** hitscan under 25m, then mild projectile drop — mobile-friendly but honest. **Headshot bonus 1.5x** [ESTIMATE], subject to balance configuration and playtest.
- **Reload realism:** magazine-out sound → pause → mag-in *click* → bolt release. The CLICK is the satisfaction; never cut it.

## 3.3 Blood & impact policy (rating-safe, feedback-rich)
- **Realism without gore:** surface-specific impact decals (concrete chips, metal sparks, suit scuffs), a *brief* stylized red hit-puff on players (particle, not texture gore), plus suit-damage state (cracked visor, torn wing) as persistent damage telegraphy.
- **Why:** Verify age ratings and regional content rules for each launch market. Maximum shippable content determines rating; see amendment A3. The PUBG Mobile regional blood-color claim is [VERIFY] and is not a design premise. Feedback comes from sound, hitmarker, and impulse.

## 3.4 Damage & TTK (time-to-kill) doctrine
- **TTK: 0.9–1.4s** [ESTIMATE], subject to balance configuration and playtest.
- **Armor Integrity** (your current system) is good — rename to **Suit Integrity** (fits pilot fantasy). Segmented bar is good; make segments = shield cells that break with audio cracks.
- **Pad counter-play:** launching mid-fight is the HOTDROP outplay — use the configured 0.3 s weapon lockout [ESTIMATE] to create counter-play; tune in playtest.

---

# PART 4 — MAP DESIGN FRAMEWORK (every map, ever)

## 4.1 The vertical map grammar (your unique moat)
Ground shooters think in lanes. HOTDROP thinks in **layers**:
- **Layer 0 (street):** traversal only, no combat value. The consequence for falling is **UNDECIDED**; do not implement instant death, damage, a slow return, or another punishment until the owner selects one.
- **Layer 1 (roofs):** primary combat. 6–10 roof plates per half, connected.
- **Layer 2 (mid-structures):** bridges, cranes, billboards — contest space.
- **Layer 3 (sky):** the route. Open, fast, exposed.
**Rule:** any roof must be reachable by ≥2 methods (pad + glide) or it's a trap, not a position.

## 4.2 Timing budget (the invisible ruler)
- Spawn-to-center (Relay): **12–15s** [ESTIMATE] via optimal flight. Longer = boring opener; shorter = spawn chaos.
- Rotation between any two roofs: **4–8s** [ESTIMATE]. Measure time-between-contacts; the proposed 12s ceiling is also [ESTIMATE].
- Cover: every sightline >25m must be breakable (AC unit, antenna, glass panel). No free sniper alleys (you have no sniper — good — but keep the law anyway).

## 4.3 Switchyard (current map) — redesign notes
Keep the construction-rooftop identity; fix three things:
1. **Landmarks:** currently everything is gray boxes. Add 3 unique readable landmarks: a rotating crane, a neon "SWITCHYARD" sign, a glass skybridge. Players navigate by landmark, not minimap.
2. **Pad placement:** pads are your power positions — place them so each enables a *decision* (high risk roof vs safe relay touch), never just convenience.
3. **Color-coding per zone:** subtle roof-edge trim in team-neutral but zone-distinct hues so callouts work ("they're on the gold roof").

## 4.4 Future map archetypes (roadmap)
- **Night Market:** dense neon, short sightlines, vertical clutter.
- **Dam/Spillway:** long horizontal + vertical extremes; wind zones as hazard.
- **Storm Tower:** single mega-structure spiraling up — the "one-lane" experimental map.

---

# PART 5 — UI/UX SYSTEM (THIS IS THE "DESIGN IS FUCKED" FIX)

## 5.1 The audit of your current screens (be ruthless to win)
| Your screen | Problem | Fix |
|---|---|---|
| Home/Lockers | ~30 ALL-CAPS micro-labels; 0 visual hierarchy | One hero element per screen; labels in body font, sentence case |
| Contracts | Jargon ("DIRECTIVE // 02 [ENGAGED]"), fake-terminal chrome | Plain verbs: "Hold the Relay — 45/45s". Cards breathe: 24px padding minimum |
| Appearance | Telemetry block ("HITBOX DELTA 0.00%") front and center | One line: "Cosmetic only. Zero gameplay effect." Move tech specs to a collapsible |
| Profile | 5 identical stat cards | Hero number (Level) + 2 secondary; rest behind a tap |
| Settings | Tabs good, but toggle rows are walls of text | Setting name + one-line plain description, toggle right-aligned, 56px row height |
| Results | Actually your best screen. Keep structure, add highlight clip + one reward line | — |
| Shop | "PROD GUARD // CONCEPT REVIEW ONLY" red banner = fear, not trust | Banner → small tag "Preview". Trust design: "Own it forever. No duplicates." |

**Universal fixes:**
- Kill `//` separators in player-facing text. They're code, not communication. ("FLIGHT DOSSIER // PERMANENT ONBOARDING TRACK" → "Your pilot record")
- Max 1 badge/pill per element. Stacking "READY" + "SET NOT READY" + role + borders = noise.
- Rounded radius 12px system-wide; 1px borders at 10% white, not glows everywhere.
- Every interactive element: 44px minimum touch target **[ESTIMATE; validate against device layouts and accessibility review]**, state change on press within 80ms **[ESTIMATE; measure on target devices]**, optional haptic tick subject to the player's intensity setting.

## 5.2 HUD anatomy (in-match, the no-distraction cockpit)
```
TOP-CENTER  : Score strip (ally | timer | enemy) + Relay bar. Nothing else.
TOP-LEFT    : Squad (3 chips: name, suit integrity bar, pad status). Collapses after 5s idle.
TOP-RIGHT   : Minimap (rotating, north-up toggle in settings) + compass strip.
BOTTOM-LEFT : Move stick (floating — appears where thumb lands).
BOTTOM-RIGHT: FIRE (biggest), JUMP, PAD, weapon swap — sized by frequency.
CENTER      : Crosshair (dynamic: expands with spread). Nothing else, ever.
```
**Combat clarity law:** during any enemy within 30m or player firing, all non-essential HUD fades to 15% opacity in 200ms. The screen shows *the fight*, not the interface. This is your "NO DISTRACTIONS" feature — make it a settings toggle called **"Cockpit Mode"** and market it.

## 5.3 Information architecture (app-wide)
- **3-tier rule:** any screen = 1 hero, ≤3 secondary, everything else behind interaction.
- **Navigation:** persistent left rail (5 items max) + back = up. No hamburger mazes, no depth >2 from Home.
- **Empty states are content:** no owned wraps → show how to earn one (contract card), not "OWNED 1".

## 5.4 Motion design
- Transitions 180–250ms, ease-out. Screen changes = *slide with parallax*, never fade-to-black (breaks energy).
- Buttons: press-scale 0.96 + haptic tick. Current shared button applies the press scale; haptic feedback remains on the mobile-control path. Rewards: count-up numbers + meter fill + one celebratory VFX burst (not confetti spam).
- Match intro: camera flies the map (4s) — your loading IS your trailer.

---

# PART 6 — AUDIO DESIGN (sound is 50% of "realism")

## 6.1 Weapon audio (layered, the AAA method)
Each gunshot = 4 layers mixed live:
1. **Body** (thump, low-mid) — gives weight.
2. **Mechanism** (bolt, shell drop) — gives realism.
3. **Crack/transient** (the attack players react to) — gives danger.
4. **Tail** (rooftop echo vs open-sky air) — gives space.
- Distinct signature per weapon so players identify guns by ear across the map (your "audio radar").
- **Distance model:** full mix <15m → filtered body 15–40m → distant popcorn 40m+. Shot heard far away must sound *scary and directional*.

## 6.2 Movement & flight audio (your identity sound)
- Glide: wind = layered noise tied to velocity + cloth flutter + faint city rush below. Diving pitch-shifts up. **Players should be able to close their eyes and feel their trajectory** — that's the flight fantasy.
- Jump pad: pneumatic *whoomp* + rising charge tone that tells enemies "someone is launching" (counter-play information encoded in sound — fair and skill-based).

## 6.3 Adaptive music (the tension engine)
- **Hub:** warm lo-fi pulse, 1 layer, ignorable.
- **Match:** 3 stems (pulse / rhythm / lead) mixed by phase (1.4). Endgame layers all + tempo +4 BPM. Defeat: music *resolves* (a full cadence — a gentle let-down, not an audio slap).
- **Silence as a tool:** 1.5s of near-silence before Relay unlock makes it feel like an event. Sound absence = attention.

## 6.4 UI & communication audio
- Every action has a sonic tick (positive = soft rising, negative = low flat). Kill = distinct pentatonic blip; your own death = short low drone (never harsh — deaths are frequent; harshness breeds quit).
- Voice: quick-chat wheel + contextual auto-lines ("Pad placed!", "Relay contested") with regional voice packs. Push-to-talk voice for parties, opt-in.

## 6.5 Chat & social system
- **3 channels:** Squad (auto-join), Party (cross-match), Quick-chat (wheel of 8: Attack/Defend/Pad here/On my way/Retreat/Nice/Thanks/Sorry — localizes automatically).
- **Ping system (copy the *principle* of Apex pings):** look + tap = context ping (enemy/pad/loot/rotate). Pings have audio+visual, decay 8s, no spam lockout needed at 3v3 scale.
- **Safety:** profanity filter by region, report-from-scoreboard (2 taps), mute in 1 tap. Under-13 players use the quick-chat wheel only, with no free-text chat or voice. Legal requirements are [VERIFY]. Moderation is a *growth feature* — parents talk, markets listen.

---

# PART 7 — SETTINGS, ACCESSIBILITY, PERFORMANCE ("reality" includes the device)

## 7.1 Settings that matter (your current list is fine — reframe by player intent)
- **3 master presets:** Performance (60fps locked, low fx) / Balanced / Cinematic (30fps+, full VFX). Auto-detect on first launch; player can override.
- **Controls:** floating stick, sensitivity per-zoom, gyro aim (where hardware exists — your current "not available" handling is correct, keep), left-handed layout, **full button re-map with drag-and-place HUD editor** (you have "HUD Workspace" — ship it, it's a competitive moat).
- **Sensitivity:** 0.5–2.5 range is right; add 10-step presets named by feel (Low/Standard/Rapid — you have this) + per-weapon ADS multiplier.
- **Audio:** per-mix sliders (Master/Music/Effects/Voice), mono toggle, haptic intensity.

## 7.2 Accessibility (non-negotiable, cheap, huge market)
Colorblind-safe enemy/ally palettes (never rely on hue alone — add shape difference to hitmarkers), subtitle-style captions for gameplay sounds ("[pad launch nearby]"), reduced-motion mode (kills FOV kicks/screen shake), one-handed layout, text scaling, aim-assist transparency slider (always visible value — trust).

## 7.3 Performance budget (mobile reality)
- Low tier: ~1.5GB client memory [ESTIMATE] on representative 3–4GB devices. Mid-tier: <2.5GB memory [ESTIMATE], with frame-rate, startup, match-load, and download-size targets measured per release.
- Budget: ≤120 draw calls combat, ≤1.5M tris visible, audio ≤24 voices. Profile per release on a *real mid-tier phone*, not the dev machine.

---

# PART 8 — SYSTEMS & TECH ARCHITECTURE (what to tell Unity & .NET)

## 8.1 Unity client architecture
- **URP** (not Built-in, not HDRP for mobile) with Shader Graph VFX. Texture atlasing + GPU instancing for roofs/props.
- **Netcode:** Photon Fusion is the current implementation. Evaluate Fusion, NGO, Fish-Net, and Mirror in a 3v3 spike before any future architecture lock — authoritative server, client prediction for movement (flight *demands* prediction or it feels like swimming), server reconciliation, lag-compensation cap 200 ms [ESTIMATE], configurable and tuned against measured RTT. Tick and interpolation targets require profiling.
- **Movement:** CharacterController custom (not Rigidbody) for glide — you need deterministic arcs for pad design; freeze rotation, custom gravity curve: fall speed caps at terminal, glide = forward velocity trades with lift.
- **Bots:** your backfill bots must obey the same movement laws (players can spot fake physics instantly) — utility AI: objective-score + threat + flight-stamina evaluation, 3 difficulty tiers, clearly labeled [BOT] (your current labeling is honest — keep).
- **Replay/highlight system:** record at event level (positions @ 10Hz + inputs) — powers kill-cam, highlights, and anti-cheat review. Build it early or never.

## 8.2 .NET backend (the meta backbone)
- **ASP.NET Core** services: Auth (guest → Play Games/Game Center link), Profile, Matchmaking (ELO + latency buckets), Relay-authoritative match service, Progression/Contracts, Inventory/Cosmetics, Store (server-side prices, receipt validation), Social (friends, parties, chat relay), Telemetry (every event: match result, pad usage heatmaps, drop-off funnels).
- **DB:** PostgreSQL + Redis (sessions, matchmaking queues, leaderboards).
- **Live-ops:** config-driven everything (contracts, shop, events ship as JSON, no client update) — this is how you run a live game with a small team.
- **Anti-cheat:** server-authoritative damage/inventory + anomaly detection on telemetry (impossible flight speeds, accuracy outliers). Client obfuscation is secondary; trust the server.

## 8.3 Data you must measure from day 1
FTUE completion %, queue→match abandon, time-between-contacts, pad usage heatmap per map, defeat-quit rate, D1/D7/D30, session length distribution. Thresholds are [ESTIMATE] until a soft-launch baseline exists; a high defeat-quit rate alone does not prove that TTK is broken. **Every design argument in this doc is falsifiable by these numbers.**

---

# PART 9 — "NO DISTRACTIONS" DOCTRINE (explicit, because you asked)

1. **Cockpit Mode** (auto or toggle): HUD fades to essentials during combat.
2. **[DECISION] No ads.** If retained, plan revenue around cosmetics and the pass; do not present this business choice as a technical law.
3. **No social popups mid-match** — invites queue silently until results.
4. **One notification language:** only time-limited things get badges (≤2 visible app-wide).
5. **Kill the parade:** every celebration ≤2.5s, skippable. Respect the next-match button — it's the heartbeat of the game.
6. **Stream-safe overlay option** (hides personal data) — your viral clip loop depends on it.

---

# PART 10 — MONETIZATION & ETHICS (unicorn economics)

- **Sell:** pilot outfits, weapon wraps, pad FX, finishers, intro banners, battle pass (cosmetic-only, 8-week, *finishable by casual play* — a pass that requires grinding breeds resentment, not revenue).
- **Never sell:** power, pad types, matchmaking priority, map access.
- **Price reality for target markets (PK/IN/SEA):** regional pricing, small denominations, permanent items ("no rentals, no expiry" — print it on every store card).
- **Store design = trust design:** show full price, show odds if randomized (you have ZERO loot boxes — good, it's literally on your shop screen: make it "ZERO RANDOMNESS" and put it in marketing).
- **Earned currency flows:** your Flight Seals (earned-only) are the spine. Every cosmetic should be earnable *or* purchasable — one economy, two doors in.

---

# PART 11 — 90-DAY REBUILD PLAN (design-first, not feature-first)

**Days 1–14 — Foundation:** adopt Part 2 palette + type system as a Unity UI kit (design tokens: colors, spacing, type scale, radius). Re-skin Home, Results, HUD only. Lock the kit — no screen ships outside it.
**Days 15–35 — Core feel:** gun-feel pass (Part 3 response chain), flight audio layers, Cockpit Mode. Playtest metric: time-between-contacts <12s on Switchyard.
**Days 36–60 — Map & clarity:** Switchyard landmark/color pass (4.3), ping system, quick-chat, settings presets.
**Days 61–75 — Meta dignity:** rewrite ALL contracts/profile copy in plain language (the table in 5.1), highlight clips from replay system, reward-on-defeat line.
**Days 76–90 — Polish & soft launch prep:** accessibility suite, performance budget pass on mid-tier device, analytics dashboard, store trust copy.

**Gate rule:** at each phase, if the playtest tension curve (1.4) isn't felt by 70% of testers, STOP — the map/feel is wrong, do not add content on a broken curve.

---

# PART 12 — HOW TO FEED THIS DOCUMENT TO YOUR AI (Codex / Claude Code)

1. Place this file at repo root as `HOTDROP_DESIGN_BIBLE.md` and add a short `CLAUDE.md` / `AGENTS.md`: "All UI, gameplay, and systems code MUST follow HOTDROP_DESIGN_BIBLE.md. When unsure, prefer: clarity > realism > decoration. Never introduce a new color, font, or HUD element not defined in Part 2/5."
2. Work in vertical slices per Part 11 phases; prompt pattern: *"Implement [feature] per Part X of the design bible. First list which design rules apply, then write the code, then self-review against those rules."*
3. Give the agent the **playtest metrics** (8.3) as tests where possible — e.g., HUD fade timing, TTK constants, rotation timings as configurable `ScriptableObject` game-balance assets, not hardcoded magic numbers.
4. Screens: have the agent generate UI from the design-token kit first, then content. If a screen needs a 4th label, push back — the doc caps you at 3.

---

*End of Bible v1.0. The teams that win are not the ones with the most features — they're the ones where every screen, sound, and second serves one fantasy. Ours: the moment you launch off a roof and the whole map opens up below you. Everything in this document exists to protect that moment.*
