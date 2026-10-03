# HOTDROP Home refinement — selected direction

Use `screen_3_home_lobby_production_direction (1).html` as the structural base. Paste the prompt below into Stitch while that design is selected.

```text
Refine the current Screen 3 Home Lobby into the final production direction for HOTDROP. Keep the current broad composition—full-screen Switchyard world, a centered playable character, primary actions on the left, one contract on the right, compact identity at the top, and navigation along the bottom—but redesign the content and visual storytelling according to every requirement below.

This is a landscape Android home screen at a 2400 × 1080 reference frame. It must scale safely from 16:9 through 20:9, respect cutouts and gesture areas, and remain realistic to implement in Unity using TextMeshPro, sprites, nine-sliced panels, layout groups, and lightweight shaders. Use touch targets of at least 48dp.

CORE PRODUCT TRUTH

HOTDROP is an original third-person 3v3 objective sport shooter played in three-minute matches. One Relay capture zone is active at a time and rotates every 30 seconds among Center, West, and East. Kills create space but do not score. The signature mechanic is a compact player-deployed launch pad that creates a temporary shortcut, remains active for 18 seconds, can be used by either team, and can be destroyed by enemies. Never call it a ramp, jump ramp, ARC tool, ability charge, or permanent structure.

MAKE THE SIGNATURE MECHANIC VISIBLE

The current image feels like a generic military shooter lobby. Replace it with a believable near-future rooftop athletic venue above a bright coastal megacity. Show the selected playable character in the real Switchyard environment with realistic adult proportions and grounded athletic posture. Their clothing should feel like premium rooftop sport equipment: technical fabric, flexible utility panels, compact boots, gloves, and a cyan ring motif. Avoid military uniforms, tactical-operator posing, camouflage, skulls, rank insignia, or bulky armor.

Place one compact HOTDROP launch pad in the near foreground. It is circular, low profile, readable, and made from powder-coated metal and rubber grip material. Give it a cyan directional arrow and visible armed state. From the pad, draw one restrained curved AIR ROUTE through the environment toward a distant gold Relay ring. The route should use wind ticks and a landing marker and should explain the game in one glance. The character and weapon must still look like assets that can exist in gameplay, not separate marketing illustrations.

Use bright daylight, pale blue sky, warm sun, cool shadows, distant city depth, weathered concrete, transit structures, rooftop pipes, and a recognizable communications relay tower. Keep strong negative space behind text. Avoid permanent night, smoke, explosions, lens flare, heavy fog, hologram clutter, fake charts, or noisy particles.

EXACT HOME HIERARCHY AND COPY

Top left:
- Original HOTDROP wordmark only.

Top right:
- Player alias: VividSwift
- Level 14
- 7 Flight Seals with a small gold seal icon
- Connection: US East • 24 ms, visually quiet and shown only as useful status
- Settings icon

Do not show a production build label, version, FPS counter, server-health claim, rank, tier, season, cycle, "competitive operations," "tactical doctrine," "deployment engine," or invented progression terminology.

Left primary action group:
- Eyebrow: RELAY
- Main button: PLAY 3-MINUTE RELAY
- Supporting line: 3v3 • One active objective • Zone rotates every 30 sec
- A small state label may say SOLO • READY

The Play button is the largest and highest-contrast interactive element. Use one clear cyan action treatment. Do not add both ENGAGE and START QUEUE; the button itself starts matchmaking.

Left secondary action directly below:
- Button: PLAY OFFLINE
- Supporting line: 12 Launch Challenges • Works without internet

The Offline action must remain clearly actionable but subordinate to Play. Do not call the challenges speed trials or travel missions.

Right contract card—show exactly one goal:
- Eyebrow: ACTIVE CONTRACT
- Title: HOLD THE RELAY
- Description: Earn 45 seconds of valid objective occupation.
- Progress: 31 / 45 sec
- Reward: 1 Flight Seal

Show numeric progress and a gold reward preview. Do not add a CLAIM button until progress reaches 45/45. Do not describe contested control as progress because a contested Relay scores zero.

Bottom navigation—show exactly three destinations:
- Party
- Appearance
- More

More contains Profile, Contracts, Mastery, Training, Settings, Account, and Support on the next screen. Do not expose all of them as Home navigation. Do not show a duplicate squad roster, replay vault, loadout, shop, mastery, or inbox panel on Home.

Brand lines, placed with restraint:
- Three players. One team. Three minutes.
- Build shortcuts your enemies can steal.

VISUAL SYSTEM

Use the “Aerospace Utility Sport” direction: premium, grown-up, optimistic, readable, physical, and original. Do not imitate PUBG, Free Fire, Call of Duty Mobile, Fortnite, Apex Legends, Valorant, or Counter-Strike.

Use this semantic palette:
- Deep background #101827
- Raised panel #1B2940
- Selected surface #263A59
- Primary text #F4F7FB
- Secondary text #BBC6D8
- Muted text #7F8CA3
- Friendly/action cyan #30D5E8
- Friendly deep #138EA3
- Enemy/danger coral #FF6376
- Objective/reward gold #FFD166
- Success #7ED68A
- Disabled #536176

Reserve cyan for primary action and friendly identity, coral for enemies or destructive warnings, and gold for objectives and earned rewards. Every color-coded state also needs a label, icon, pattern, or shape. Use solid or lightly translucent navy panels, subtle 1px borders, 10–16px corner radii, restrained elevation, and a readable modern sans-serif. Avoid glassmorphism, excessive pills, techno fonts, all-caps paragraphs, notification-dot clutter, fake urgency, daily streak pressure, loot boxes, and casino effects.

Create a clear information hierarchy at normal phone viewing distance. The eye order must be: character and launch route, PLAY, active contract, Offline, then navigation. Reduce the number of outlines, micro-labels, dividers, and tiny telemetry elements. The interface should feel calm before the player chooses action.

STATE VARIANTS

After completing the main Home screen, create four matched states without changing its layout:
1. New player: contract becomes FIRST FLIGHT • Complete one Relay match • 0/1 • Reward: 1 Flight Seal.
2. Offline: PLAY becomes unavailable with plain status NO CONNECTION; PLAY OFFLINE remains active and prominent. Do not hide the character or world.
3. Party ready: compact contextual card shows 3/3 READY and the main Play action remains dominant.
4. Reward ready: contract shows 45/45 and one gold CLAIM REWARD action; keep the reward animation calm and skippable.

MOTION ANNOTATIONS

Use 180–240ms ease-out transitions. On Play, extend the cyan air-route line forward and transform it into the matchmaking route. Give the launch pad a slow, subtle armed pulse. Give the gold Relay ring one restrained expansion. In Reduce Motion mode, replace scale and parallax with fades.

DELIVERABLE

Return one polished production-ready Home design and the four state variants. Also provide a small component strip containing the Play button, Offline button, contract card, navigation item, Flight Seal indicator, launch-pad icon, and network status. Prioritize believable gameplay identity, hierarchy, readability, accessibility, and Unity feasibility over decorative detail.
```
