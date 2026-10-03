# HOTDROP Stitch master design prompt

Paste everything inside the code block into Google Stitch as one prompt. Attach the current HOTDROP home and gameplay screenshots as reference images if Stitch allows image input. Treat those images as evidence of existing layout and functionality, not as the desired visual quality.

```text
Design a complete, cohesive, production-grade mobile game UI/UX system for a landscape Android game named HOTDROP. Create a connected multi-screen product prototype, not isolated concept art and not a generic esports dashboard.

PRODUCT DEFINITION

HOTDROP is a fast third-person 3v3 objective shooter built for three-minute sessions. The signature mechanic is a player-deployed launch pad that creates a temporary traversal shortcut. Either team can use any armed pad, and enemies can destroy pads. The emotional promise is: “Build a shortcut. Take the objective. Watch who follows.” The core mode is Relay: one capture zone is active at a time and rotates between Center, West, and East. Kills help create space but do not score points. The first team to 120 points wins, or the higher score wins when the three-minute timer ends.

The experience must feel premium, grown-up, fast, readable, optimistic, and original. It must be easy for a new mobile player to understand and deep enough for experienced shooter players to respect. Design for adults and older teens across a wide range of familiarity and physical comfort. Do not make the visual style childish, chibi, toy-like, militaristic, dystopian, casino-like, or overloaded with neon. Do not copy PUBG, Free Fire, Call of Duty Mobile, Fortnite, Apex Legends, Valorant, or Counter-Strike assets, layouts, logos, icons, characters, typography, maps, or trade dress. Familiar mobile-shooter interaction patterns are acceptable, but every visual asset and branded expression must feel original to HOTDROP.

Create the UI for Android landscape at a reference frame of 2400 × 1080, flexible across 16:9, 18:9, 19.5:9, and 20:9 screens. Respect notches, rounded corners, camera cutouts, and gesture areas. All essential touch targets must be at least 48dp. Keep central gameplay sightlines clear. The result should be realistic to implement in Unity with TextMeshPro, images, nine-sliced panels, icon atlases, layout groups, safe-area containers, and lightweight shader effects. Avoid design elements that depend on expensive live blur, video backgrounds, dense particles, or desktop hover interactions.

CREATIVE DIRECTION — “AEROSPACE UTILITY SPORT”

The world is a believable near-future rooftop athletic competition staged above a living coastal megacity. It is not a battlefield. The visual language combines industrial rooftop architecture, wind engineering, transit infrastructure, athletic equipment, flight-path markings, and professional broadcast graphics. Think precision sports equipment and city infrastructure: powder-coated metal, weathered concrete, rubberized grip surfaces, woven technical fabric, brushed alloy, glass, safety markings, subtle wear, and directional wind graphics.

The signature graphic motif is the AIR ROUTE: clean curved trajectory lines, wind-vector ticks, altitude markers, split arrows, and circular landing indicators. Use it sparingly as a functional visual language for movement, progress, navigation, and transitions. Avoid decorative sci-fi circuits, random hexagons, excessive diagonal cuts, hologram clutter, lens flares, fake data, skulls, flames, military stencils, and meaningless charts.

The game world shown behind menus is Switchyard, a believable industrial rooftop complex with three recognizable landmarks:
- Center: a communications relay tower and open competition deck.
- West: a compact water-treatment structure with pipes, tanks, filters, and strong vertical routes.
- East: a loading gantry with cargo lifts, rails, rooftop cranes, and layered cover.

Preserve strong daylight readability. Use a pale blue sky, distant city depth, soft atmospheric perspective, warm sun, cool shadows, and restrained baked lighting. No heavy fog, glossy floors, crushed-black corners, permanent night scene, or visual effects that hide opponents. Competitive cover and routes should be visually clean. Environmental detail belongs outside silhouette-critical combat lanes.

CHARACTER DIRECTION

Show grounded, athletic human characters with realistic adult proportions and expressive but restrained faces. Create two selectable body presentations using the same height, gameplay silhouette, skeleton, animation coverage, and hitbox footprint. Present the choice as appearance, not a question about the player’s own gender. Costumes use functional rooftop-sport clothing: fitted technical jackets, utility belts, flexible armor panels, gloves, compact boots, and aerodynamic fabric accents. No exaggerated sexualization, oversized heads, superhero armor, military uniforms, fantasy robes, or bulky silhouettes that imply different hitboxes.

Characters must look credible at lobby distance and readable at 10–40 meters in gameplay. Friendly and enemy identity must use both color and shape: friendly players use cyan plus a circular/ring motif; enemy players use coral plus a split-chevron/cross motif. Never rely on color alone. Cosmetics may change fabric, trim, patterns, weapon wrap, and badge, but never silhouette size, animation readability, audible weapon identity, or gameplay power.

Include three visually distinct weapons that fit the same world:
- ARC rifle: precise, balanced, clean mid-range profile.
- ZIP SMG: compact, agile, close-range profile.
- BRICK scattergun: short, heavy, unmistakable close-range profile.

Include one compact deployable launch pad: approximately circular, low profile, directional arrow on top, visible arm/charge state, and team motif. It should look like professional portable athletic/industrial equipment rather than alien technology.

COLOR SYSTEM

Use this semantic palette consistently:
- Deep background: #101827
- Raised panel: #1B2940
- Elevated/selected surface: #263A59
- Primary text: #F4F7FB
- Secondary text: #BBC6D8
- Muted text: #7F8CA3
- Friendly/action cyan: #30D5E8
- Friendly deep: #138EA3
- Enemy/danger coral: #FF6376
- Enemy deep: #B93A50
- Objective/reward gold: #FFD166
- Success: #7ED68A
- Warning: #FFB45C
- Disabled: #536176
- Concrete/world neutral: #C3B59E
- Structural blue: #49627B

Cyan means friendly identity, primary interaction, or confirmed selection. Coral means enemy, damage, destructive warning, or destructive confirmation. Gold means objectives, mastery, medals, and earned rewards. Success green is reserved for completed setup, account recovery, purchase verification, and successful connection. Never use red for ordinary Back or Cancel actions. Maintain WCAG-conscious text contrast. Every color-coded state also needs an icon, label, pattern, or shape.

TYPOGRAPHY AND ICONS

Use one modern, highly readable sans-serif family with open counters and clear numerals. Headings may be slightly condensed and athletic; body and control labels must remain wide and readable. Use tabular numerals for scores, timers, ammunition, ping, and progress. Avoid futuristic techno fonts and all-caps paragraphs. Use uppercase only for short competitive labels such as PLAY, VICTORY, CENTER, READY, and RELAY.

Use a consistent original outline icon family with rounded joins and a 2–2.5px visual stroke at the reference scale. Pair unfamiliar icons with text. Icons required: Play, Offline, Party, Appearance, Profile, Contracts, Mastery, Training, Settings, Support, Back, Close, Info, Network, Ping, Objective, Launch Pad, Focus Aim, Fire, Jump, Reload, Weapon, Audio, Graphics, Accessibility, Account, Report, Delete, Lock, Check, Warning, Medal, Flight Seal, Body Style, Weapon Wrap, Badge, Friend/Party, Retry, Resume, and Home.

SURFACE AND COMPONENT LANGUAGE

Use solid and translucent navy surfaces with subtle 1px borders, low-radius corners between 10 and 16px, and modest elevation. Avoid pill-shaped containers everywhere; reserve pills for short statuses, filters, and tags. Primary buttons should be large rectangular controls with a clear icon, one verb, and strong cyan fill or cyan edge treatment. Secondary buttons use elevated navy. Destructive confirmation uses coral only inside the final confirmation step.

Cards must communicate one decision each. Progress bars use a clear start/end, numeric progress, and exact reward preview. Do not create mystery chests, spinning wheels, slot-machine effects, flashing claim buttons, fake scarcity, red notification-dot overload, or “come back or lose progress” messages. Retention must come from visible goals, mastery, useful offline play, and social teamwork—not anxiety or punishment.

MOTION LANGUAGE

Show motion specifications as concise annotations where helpful:
- Standard UI transition: 180–240ms, ease-out, small opacity and 8–16px directional movement.
- Home-to-matchmaking: the selected air-route line extends forward and becomes the loading path.
- Successful launch: short directional pulse following the pad arrow; never cover the player.
- Objective activation: one clean gold ring expansion plus label; no full-screen flash.
- Reward earned: 600–900ms calm assembly animation using route segments and a final seal stamp; skippable.
- Damage: directional edge cue and small readable health response; no violent screen obstruction.
- Reduce Motion setting replaces scale/parallax motion with fades and immediate state changes.

INFORMATION ARCHITECTURE

Create a connected prototype covering the complete player journey. Use consistent navigation, Back behavior, safe areas, loading, empty, offline, success, disabled, and recoverable-error states. Never strand the player in a screen. The primary navigation model is:
- Home: Play, Offline, Party, Appearance, one active goal card, compact player identity.
- More: Profile, Contracts, Mastery, Training, Settings, Account, Support.
- Match flow: Matchmaking, Loadout/Preparation, Gameplay, Results.

Create the following screens and states as one visual system. Use realistic sample copy exactly where provided, and generate short plain-language copy elsewhere.

SCREEN 1 — STARTUP AND CONNECTION

Landscape startup screen with the original HOTDROP wordmark, a restrained air-route animation, build/version in small text, and status such as “Preparing the rooftop.” Support these states: normal loading, no internet with `PLAY OFFLINE` and `TRY AGAIN`, update required, backend unavailable, and maintenance. Offline must remain actionable whenever local play is available. Do not use an unexplained endless spinner.

SCREEN 2 — FIRST-LAUNCH APPEARANCE CHOICE

Show both body presentations at equal visual quality on the same neutral platform, wearing the same starter outfit and holding the ARC rifle safely. Title: `CHOOSE YOUR LOOK`. Supporting text: `Appearance only. Same movement, abilities, and hitbox.` Buttons: `CONTINUE` and `DECIDE LATER`. Allow rotation and a close-up face/body view without sexualized poses. Continue must be available with a safe default selected.

SCREEN 3 — HOME

This is the hero screen. Show the player’s selected in-game character and equipped weapon in the Switchyard environment, framed with enough negative space for UI. Do not make the character a separate illustration that cannot exist in gameplay.

Layout:
- Top-left: compact HOTDROP wordmark.
- Top-center/right: player alias, level, connection/region status, settings shortcut.
- Left or lower-left: one dominant `PLAY 3-MINUTE RELAY` button with `3v3 • one active objective` beneath it.
- Directly below: clear secondary `PLAY OFFLINE` button with `Works without internet` beneath it.
- Bottom navigation: Party, Appearance, More. Keep it sparse.
- Right-side or lower card: exactly one selected contract with progress and exact reward. Example: `HOLD THE RELAY • 31/45 sec • Reward: 1 Flight Seal`.
- Small contextual card only when relevant: party invite, first-match reward, or reconnect. Do not show all simultaneously.

Tagline: `Three players. One team. Three minutes.` Secondary line: `Build shortcuts your enemies can steal.`

Create Home variants for new player, returning player with contract progress, offline state, party-ready state, and reward-ready state. The primary Play action must remain visually dominant in every variant.

SCREEN 4 — GUIDED FIRST-RUN ENCOUNTER

Design a lightweight tutorial overlay on real gameplay. Only one instruction appears at a time. Use anchored callouts and a clear visual target; do not cover the crosshair or character. Core guided steps: Move, aim/fire at target, deploy a pad, launch to platform, capture objective. Show progress as `2 of 5`, a visible `SKIP TRAINING` action, and optional `SHOW ME` assistance after inactivity. Additional mechanics—enemy-pad use, pad destruction, and reload—are taught contextually during the first bot match.

SCREEN 5 — PARTY

Design a three-slot party room. Each slot shows avatar, alias, ready state, connection quality, and leader marker. Include create/join flow with an 8-character code, Copy/Share Code, Ready, Leave Party, and leader-only Queue. Make the difference between party code and account/player ID obvious. Show full, member dropped, invite expired, not ready, leader disconnected, and retry states. Use no open chat or voice-chat UI.

SCREEN 6 — MATCHMAKING

Show elapsed search time, selected region and measured latency, party readiness, Cancel, and `PRACTICE WHILE WAITING` after an appropriate delay. Use a clean rooftop route map animation with three player markers joining three open slots. Show transparent messages for expanding skill search, server at capacity, cancelled queue, party member dropped, update required, and bot-fill eligibility after the configured wait. Bots must be labeled; never visually imply a human-filled match when it is not.

SCREEN 7 — LOADOUT AND PREPARATION

Show three weapon cards: ARC rifle (`Balanced • Mid range`), ZIP SMG (`Fast • Close range`), BRICK scattergun (`Heavy • Very close range`). All are unlocked. Cards show readable weapon silhouette, magazine size, fire behavior, ideal distance, and a short learning tip. Do not display misleading power bars. Show the active objective and eight-second preparation countdown. Spawn remains locked, damage disabled, and pad placement unavailable. Make those restrictions understandable without developer language.

SCREEN 8 — GAMEPLAY HUD: CLASSIC PRESET

Create a clear third-person mobile shooter HUD over an actual Switchyard combat scene. Keep the center visually open.

HUD layout:
- Top-center: Ally score, match timer, Enemy score in a compact broadcast bar.
- Directly below: active objective name/status and next-zone countdown; use gold for objective, cyan/coral for team status.
- Upper-left: two teammate cards with alias, health, alive/down/respawning/bot state, and a small party marker.
- Upper-right: compact tactical arena diagram showing objectives and teammates. Never reveal unseen enemies. A valid enemy ping may show a temporary location marker, not tracking through walls.
- Lower-left: floating movement joystick and small optional left-fire button in the Classic preset.
- Lower-center: health, temporary damage direction, and low-health state without blocking aim.
- Lower-right: large Fire button; Focus Aim button above/inside comfortable thumb reach; Jump/LAUNCH above Fire; Reload near ammunition; Pad button left of the combat cluster; weapon/ammo display adjacent to Fire.
- Edge: objective direction/distance marker when the active zone is off-screen.
- Network warning must never overlap the crosshair.
- Pause and Ping remain reachable but visually secondary.

Fire-drag must imply camera aiming. The dynamic crosshair expands while moving/firing and tightens when stable. Focus Aim changes framing from approximately 55-degree FOV to 40-degree FOV and has separate sensitivity. Do not draw a scope overlay unless the selected weapon actually has a sight. Show blocked muzzle, reload progress, pad charge/cooldown, invalid pad-placement reason, hit confirm, elimination confirm, and visible-target enemy identity states.

Create HUD variants for preparation, live combat, contested objective, overtime, respawn/spectate, reconnecting, bot substitution, low network quality, and AFK removal. Also create a Simple control preset with fewer buttons and stronger contextual actions. Do not add crouch, prone, slide, automatic leaning, grenades, inventory, vehicles, healing items, or weapon switching while alive.

SCREEN 9 — PAD PLACEMENT AND LAUNCH FEEDBACK

Show the hold-to-preview trajectory, landing zone, directional arrow, valid cyan/gold state, invalid coral state, and short reason labels: `TOO STEEP`, `OBJECTIVE BLOCKED`, `NO CLEARANCE`, `TOO CLOSE TO SPAWN`, `PAD ALREADY ACTIVE`. Show armed, charging, ready, damaged, destroyed, expiring, friendly, and enemy pad states. Anyone may use either team’s pad. Ensure team ownership remains clear through ring versus split-chevron/cross shapes.

SCREEN 10 — PAUSE AND SETTINGS

Use tabs: Controls, Graphics, Audio, Accessibility, Account/Support. Settings must be practical on a phone:
- Controls: Classic/Simple preset, separate Look and Focus Aim sensitivity sliders with numeric values, gyro toggle and calibration, invert Y, left-handed layout, aim assist, Customize Controls, Reset Layout.
- Graphics: Auto Detect, Low, Medium, High; FPS target; dynamic resolution; shadow quality where supported; truthful disabled states when a feature is unavailable.
- Audio: Master, SFX, Music, Announcer sliders; mute team pings.
- Accessibility: larger HUD/text, color-and-shape team cues, reduce motion, camera shake, vibration, high-contrast objective marker, subtitle/announcer text.
- Account/Support: linked/guest state, recover account, report player, privacy, deletion.

Use real sliders rather than buttons that cycle invisible values. Every tab gets Reset to Default. Settings apply live where safe, save immediately, and show a compact confirmation. In offline play show Resume and Return Home. In online play do not imply the match pauses; title it `MATCH MENU` and keep live-match status visible.

SCREEN 11 — RESULTS

Design victory, defeat, and draw with equal dignity. First show the result and verified personal contribution:
- Objective time.
- Eliminations and deaths.
- Pad launches.
- Useful teammate route, only when a server-verified teammate used the player’s pad.
- New personal best, only when true.

Then show team scoreboard. Then show authoritative XP, credits, contract progress, mastery progress, and exact unlocked cosmetic. Use one strong `PLAY AGAIN` action, secondary `PLAY TOGETHER` when eligible, and Home. Losses still show legitimate participation progress without fake praise. Show reward pending/retry and ineligible/AFK states honestly.

SCREEN 12 — CONTRACTS

Design a focused three-choice goal screen unlocked after two public matches. One contract is active; switching pauses rather than deletes progress. Cards:
- `FINISH TOGETHER` — Complete 2 eligible public matches.
- `HOLD THE RELAY` — Earn 45 seconds of valid objective occupation.
- `BUILD A TEAM ROUTE` — Two distinct human teammates use your pads.

Each card shows progress, plain eligibility rules, estimated natural completion context without false countdown pressure, and reward `1 Flight Seal`. Show Flight Seal collection progress toward a previewable cosmetic choice. No random boxes, login streak punishment, expiring purchase, mystery odds, or red urgency timer. Make completion satisfying but calm.

SCREEN 13 — MASTERY

Create three permanent tracks: Relay Specialist, Route Builder, and Weapon Familiarity for ARC/ZIP/BRICK. Show Bronze, Silver, and Gold milestones; current progress; next badge preview; and what counts. Use a clean flight-log visual metaphor, not a battle pass. Mastery rewards are cosmetic badges only. Display completed tracks proudly without implying the player must keep grinding forever.

SCREEN 14 — OFFLINE TRAVEL CHALLENGES

Design a mode that feels valuable even with airplane mode enabled. Three chapters, four challenges each:
- First Flight: basic route, own-pad route, enemy-pad route, safe landing.
- Rooftop Operator: stationary targets, moving targets, reload discipline, launch-and-hit.
- Relay Captain: capture one zone, rotate between zones, defend against bots, complete offline Relay.

Show downloaded/available status—all 12 launch challenges are included in the install—local Bronze/Silver/Gold medals, personal best, Resume when a local challenge was interrupted, Retry, and Offline Relay. Clearly label local-only records. Never imply local edits can earn online currency, inventory, competitive rank, or leaderboard placement. Use the same core UI and character identity as online mode so offline does not feel like a cheap minigame.

SCREEN 15 — APPEARANCE

Show a rotatable real in-game character on a neutral Switchyard platform with consistent lighting. Use four independent categories: Body Presentation, Body Style, Weapon Wrap, Badge. Show Owned and Locked filters, exact unlock source, equipped state, and side-by-side preview where useful. Body presentation is freely changeable and gameplay-neutral. Cosmetics never change power. Do not show unavailable store prices unless the shop is enabled and connected.

SCREEN 16 — PROFILE

Show alias, level 1–20 onboarding track, XP, credits, equipped badge, personal bests, mastery highlights, eligible matches, wins/draws, objective contribution, and completed travel chapters. Keep hidden matchmaking rating hidden. Add rename entry with validation and clear policy, but avoid public age, gender, location, or contact fields. Profile should communicate identity and improvement rather than shame through excessive loss statistics.

SCREEN 17 — ACCOUNT, RECOVERY, SUPPORT, AND REPORTING

Design guest and linked account states. Explain guest risk plainly. Actions: Link Google Play Games, Recover Linked Account, Revoke Link, optional deletion-recovery email, Privacy, Terms, Support, Report Player, Delete Account. Reporting selects a recent match participant by visible alias and slot; do not ask the user to paste an internal GUID. Deletion uses a two-step confirmation, describes deleted and retained data, and always preserves a usable Back/Cancel path until final confirmation.

SCREEN 18 — FUTURE SHOP CONCEPT, DISABLED FROM THE MAIN FLOW

Create a separate concept screen for later validation, not an active Home promotion. Show three permanent cosmetic bundles with exact contents, full preview, localized price placeholder, restore purchases, and cosmetic-only label. No loot boxes, premium currency, power, paid respawn, forced ad, fake sale, crossed-out invented price, countdown pressure, or mid-match purchase. Add an annotation: `Enable only after gameplay validation and complete purchase/recovery testing.` Do not place this shop in the primary Home flow.

SYSTEM STATES AND COMPONENT SHEET

Add one design-system canvas showing:
- Color tokens and intended semantics.
- Type scale and tabular number style.
- Primary, secondary, quiet, destructive, disabled, loading, and icon-only buttons.
- Cards, tabs, sliders, toggles, progress bars, badges, medals, tooltips, toasts, dialogs, empty states, skeleton loading, offline banner, latency indicator, and error/retry panel.
- Friendly/enemy/objective visual cues using both color and shape.
- Control buttons in default, pressed, held, cooldown, unavailable, and customize-layout states.
- Safe-area guides for 16:9 and 20:9.
- Text expansion examples at 30% longer copy.
- Reduce Motion and larger-text examples.

COPY STYLE

Use concise, calm, specific language. Prefer `Server is full. Practice offline or try again.` over error codes. Prefer `Hold the zone for 14 more seconds` over motivational filler. Prefer `Reward pending—your progress is safe` over `Something went wrong`. Avoid taunts, manipulative urgency, childish exclamations, “epic,” “legendary” everywhere, fake scarcity, and repeated congratulations.

OUTPUT REQUIREMENTS

1. Generate a cohesive multi-screen landscape Android prototype containing all 18 screens, their major states, and a design-system canvas.
2. Show at least three visually distinct but brand-consistent Home variants, then select one recommended direction and apply it to every screen.
3. Use the same characters, equipment, map landmarks, icon style, spacing system, typography, and semantic colors throughout.
4. Label conceptual or future-only features, especially the Shop. Do not present them as currently implemented.
5. Keep every screen feasible for Unity and low/mid-range Android performance.
6. Provide clear screen names and component names suitable for developer handoff.
7. Preserve gameplay clarity above decoration. The final product should look like a credible new mobile sports-shooter brand, not a reskin of an existing battle royale.

Before finalizing, audit every screen for: clear primary action, readable hierarchy in two seconds, no unreachable Back path, no hidden destructive action, minimum touch size, safe-area compliance, text contrast, color-independent meaning, low-end feasibility, consistent character identity, consistent weapon identity, and no copyrighted imitation.
```

