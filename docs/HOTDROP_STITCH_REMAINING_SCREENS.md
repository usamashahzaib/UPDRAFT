# HOTDROP — Stitch continuation prompts for every remaining screen

## How to use this document

Screen 3 Home is already approved as the visual source of truth. Generate the remaining product in the six batches below, in order, inside the same Stitch project. Do not ask Stitch to generate all remaining screens in one pass. After each batch, verify visual consistency before pasting the next prompt.

Do not place state switchers, annotation controls, component buttons, developer notes, safe-area guides, or frame labels inside player-facing screens. Put those items on separate review/handoff frames.

---

## Batch 1 — Entry, first-launch choice, and tutorial

```text
Continue the existing HOTDROP Stitch project. Treat the approved Screen 3 Home Lobby as the locked visual source of truth. Reuse its exact wordmark, semantic colors, typography, icon family, spacing rhythm, panel construction, character identity, ARC rifle, launch-pad design, Switchyard materials, lighting, and Aerospace Utility Sport tone. Do not redesign Home.

Create three connected production screens: 01 Startup & Connection, 02 First-Launch Appearance Choice, and 04 Guided First-Run Training. Create separate state frames beside each screen for design review. State selectors and annotations must never appear inside the player-facing UI.

GLOBAL PRODUCT TRUTH

HOTDROP is an original landscape Android third-person 3v3 objective sport shooter. Relay matches last three minutes. The active zone rotates every 30 seconds. The signature launch pad remains for 18 seconds, arms in 0.75 seconds, may be used by either team, and can be destroyed by enemies. It is a launch pad, never a ramp or ARC tool. Keep the tone premium, grown-up, optimistic, physical, readable, and non-militaristic. Do not imitate an existing shooter.

SCREEN 01 — STARTUP & CONNECTION

Create a restrained startup screen using the HOTDROP wordmark and one functional Air Route animation. The background should be an abstracted but believable dawn view of Switchyard, with enough contrast for status text. The loading animation should depict a cyan route segment moving toward one gold Relay ring; it must not resemble a generic spinner.

Normal state:
- HOTDROP wordmark.
- Status: PREPARING THE ROOFTOP
- A calm determinate loading line when progress is known; otherwise a clearly animated route.
- Small build/version text in a bottom safe corner only. Do not show FPS, environment names, server internals, or fake telemetry.

Create separate full-screen states:
1. No connection — title NO CONNECTION; text Online Relay is unavailable. Offline play is ready.; primary PLAY OFFLINE; secondary TRY AGAIN.
2. Service unavailable — title RELAY SERVICE UNAVAILABLE; text Practice offline or try again.; primary PLAY OFFLINE; secondary TRY AGAIN.
3. Update required — title UPDATE REQUIRED; text Install the latest version to play online.; primary UPDATE; secondary PLAY OFFLINE only if the local build remains compatible.
4. Maintenance — title ROOFTOP MAINTENANCE; show a plain server-provided message and RETRY. Offer Offline when available.
5. Save recovery — title RESTORING YOUR PROGRESS; show a bounded recovery status, RETRY, and SUPPORT after repeated failure. Never show an endless unexplained spinner.

Flow annotation on the review frame only: returning connected player → Home; first launch → Appearance Choice; offline choice → Offline Challenges.

SCREEN 02 — FIRST-LAUNCH APPEARANCE CHOICE

Title: CHOOSE YOUR LOOK
Supporting copy: Appearance only. Same movement, abilities, animations, and hitbox.

Show two equal-quality grounded adult body presentations side by side on the same neutral Switchyard platform, in the same starter outfit, under identical lighting, with the same ARC rifle held safely at low-ready. Both use the same height, gameplay silhouette, skeleton, animation coverage, and hitbox footprint. Avoid sexualized posing, different power language, military armor, photoreal beauty treatment, or one option receiving better framing.

Interaction:
- One option is safely selected by default.
- Large selection cards have visible labels, selection outline, and check icon.
- Allow drag to rotate and a FACE / FULL BODY preview toggle.
- Primary CONTINUE.
- Quiet DECIDE LATER, which uses the safe default.
- Back is unnecessary on true first launch, but app interruption must restore the choice safely.
- Do not request a name, age, gender identity, account link, notifications, or payment here.

Create selected-left, selected-right, loading-preview, and asset-recovery-error states. Add a review annotation that the choice remains freely changeable later in Appearance.

SCREEN 04 — GUIDED FIRST-RUN TRAINING

Design tutorial overlays on real Switchyard gameplay rather than a slideshow. Keep the character, crosshair, active target, and thumb zones clear. Present exactly one instruction at a time with a visible target, short verb, progress, optional SHOW ME after inactivity, and SKIP TRAINING. The tutorial has no forced countdown and should take roughly 90 seconds for a comfortable new player.

Create representative frames for these nine real steps:
1. MOVE TO THE MARKER — highlight the movement joystick and ground marker.
2. FIRE AT THE TARGET — show right-side aim drag and Fire without blocking the target.
3. RELOAD — highlight Reload after the magazine is reduced.
4. PLACE A LAUNCH PAD — show tap placement on a legal flat surface.
5. LAUNCH TO THE PLATFORM — when standing on an armed pad, change Jump to the labeled LAUNCH action; never auto-launch.
6. TRY THE OTHER TEAM'S PAD — explicit copy: Both teams can use every armed pad.
7. DESTROY THE ENEMY PAD — show pad health/readability without teaching friendly fire.
8. HOLD THE RELAY — show that standing inside the active zone scores only while uncontested; flying over does not count.
9. COMPLETE THE PRACTICE FIGHT — short bot encounter using the real HUD.

Use progress such as 5 OF 9. A completed step gets one brief cyan route segment and check, without confetti or a full-screen interruption. If the player is inactive, SHOW ME demonstrates the input and returns control. The Skip confirmation says: Leave training? You can replay it anytime from More → Training. Actions: KEEP TRAINING and LEAVE TRAINING.

Final state:
- Title: READY FOR THE ROOFTOP
- Primary PLAY PUBLIC RELAY
- Secondary PRACTICE OFFLINE
- Quiet RETURN HOME
- No account demand, shop promotion, rating, rank, reward wheel, or notification prompt.

DELIVERABLE FOR THIS BATCH

Produce separate, clearly named 2400 × 1080 top-level frames for every player screen and its important states. Also produce one review-only flow strip. Preserve 48dp touch targets, 16:9–20:9 safe-area behavior, color-plus-shape meaning, larger-text resilience, and Reduce Motion alternatives. Do not put review controls inside the game UI.
```

---

## Batch 2 — Party, matchmaking, and match preparation

```text
Continue the same HOTDROP project and preserve the approved Home and Batch 1 visual system exactly. Create Screens 05 Party, 06 Matchmaking, and 07 Loadout & Preparation as one connected online journey. Do not add public ranks, seasons, clans, chat, voice, friend feeds, events, battle passes, or extra queues.

SCREEN 05 — PARTY

Create a focused three-player party room. Preserve the selected player's real character, weapon wrap, and badge. The screen must be understandable for solo, leader, and member roles.

Solo state:
- Title: PARTY
- Supporting text: Play together with up to three players.
- Primary CREATE PARTY.
- Secondary JOIN WITH CODE.
- Join field accepts one 8-character party code, automatically formats uppercase, and clearly differs from the player's account ID.
- Back returns to Home.

Active party state:
- Exactly three member slots.
- Each occupied slot shows character portrait, generated alias, READY / NOT READY, measured connection quality, and leader icon where applicable.
- Empty slots say OPEN SLOT and never pretend an invite was sent.
- Show the party code in a readable group with COPY CODE and SHARE CODE.
- Members get READY / NOT READY and LEAVE PARTY.
- Only the leader gets PLAY RELAY after all required members are ready.
- Explain member behavior plainly: Ready — you will join when the leader starts the search.
- No text chat and no voice controls.

Create separate states: solo, joining, 2/3 party, 3/3 all ready, member not ready, invalid/expired code, party full, member dropped, leader transferred, leader disconnected, backend unavailable, and retry. Keep recovery actions visible. A full party must not be visually framed as a competitive rank advantage.

SCREEN 06 — MATCHMAKING

Create a calm search screen using an animated overhead route map of Switchyard with three cyan player markers joining three team slots. This is a real search state, not a fake countdown.

Show:
- Title: FINDING A RELAY MATCH
- Real elapsed search time.
- Region and measured latency, for example US East • 24 ms.
- Party member readiness and connection state.
- Clear CANCEL SEARCH.
- Do not show a fabricated queue position, opponent rank, guaranteed wait time, or silent region switch.

State behavior:
- At 20 seconds, plain text may say Expanding the search.
- At 45 seconds, bot-fill eligibility may appear according to the current product rule. Bots must be labeled [BOT] and never presented as humans.
- At 60 seconds, show Still searching — practice or wait. Offer PRACTICE WHILE WAITING without leaving the queue and KEEP WAITING.
- Show server capacity, party member dropped, reservation failed, update required, cancelled search, and connection-lost states with clear recovery.
- If a reserved player does not load within 30 seconds, do not show a partly empty public match. Explain that the reservation was released and return the group to search.

Show a short MATCH FOUND state with six honest slots and a server-connection step. Avoid flashing, fake rarity, chest-opening motion, or a dramatic versus screen that increases load time.

SCREEN 07 — LOADOUT & PREPARATION

Create an eight-second preparation screen that can also appear as an in-world overlay. The spawn exits remain locked, damage is disabled, and launch-pad placement is unavailable. Explain these conditions through calm interface state rather than developer language.

Top:
- RELAY • SWITCHYARD
- Active opening objective: CENTER
- Preparation countdown, 8 to 0, with a final 3-2-1 start sequence.

Weapon choice:
- ARC rifle — Balanced • Mid range; 24-round magazine; automatic precision profile.
- ZIP SMG — Fast • Close range; 30-round magazine; rapid fire.
- BRICK scattergun — Heavy • Very close range; 6 shells; eight-pellet spread.
- All three are unlocked.
- Use accurate weapon silhouettes, readable ideal-distance diagrams, fire behavior, magazine/reload values, and one learning tip.
- Do not use arbitrary power bars, rarity colors, locked weapons, attachments, purchasable stats, or claims that cosmetics improve performance.
- One weapon is equipped for the life; weapon selection becomes available again while waiting to respawn. Do not imply switching while alive.

Show the two teammate cards and party markers without obscuring the weapon decision. Primary CONFIRM LOADOUT; allow automatic confirmation when the countdown ends. Provide selected, confirmed, teammate-loading, low-network, and asset-loading states. At zero, the loadout interface clears cleanly and control transfers to the Gameplay HUD.

DELIVERABLE FOR THIS BATCH

Create separate 2400 × 1080 player frames, major state frames, and one review-only flow strip: Home → Party or Play → Matchmaking → Match Found → Loadout/Preparation → Gameplay. Use no developer buttons or state switchers in player UI.
```

---

## Batch 3 — Gameplay HUD, launch-pad feedback, and match menu/settings

```text
Continue the approved HOTDROP visual system. Create Screens 08 Gameplay HUD, 09 Launch-Pad Placement & Feedback, and 10 Match Menu & Settings. These screens must prioritize playability on a phone over decorative presentation. Use the real Switchyard, character, weapons, and launch-pad model. Do not alter gameplay rules.

AUTHORITATIVE MATCH RULES FOR ALL FRAMES

Teams are 3v3. Regulation lasts 180 seconds. First to 120 wins; otherwise the higher score at 0:00 wins. The active zone schedule is Center 0–30, West 30–60, East 60–90, Center 90–120, East 120–150, West 150–180. Preview the next zone 10 seconds early. Contested, empty, inactive, airborne-over-zone, dead, and spawn-protected presence score zero. Multiple teammates do not accelerate scoring. Kills do not score. A regulation tie starts up to 30 seconds of overtime on the current zone; the first uncontested three-second hold wins, and empty/contested time resets the hold.

SCREEN 08 — GAMEPLAY HUD

Create a Classic preset over a representative live-combat scene. Keep the center, character silhouette, crosshair, target, and movement route open.

Required layout:
- Top center: cyan team score, tabular match timer, coral team score in one compact broadcast bar.
- Beneath it: active zone name and state—CAPTURING, HELD, CONTESTED, EMPTY—and next-zone name/countdown during the final 10 seconds.
- Upper left: exactly two teammate cards with alias, health, alive/respawning/disconnected/[BOT] state, and a small party marker.
- Upper right: compact arena diagram with Center/West/East, teammates, active objective, next objective, and valid temporary pings. Never reveal an unseen enemy or track an enemy through cover.
- Lower center: player health and temporary damage direction. Low health must remain readable without a large red screen obstruction.
- Lower left: floating movement joystick and optional left Fire in Classic.
- Lower right: large Fire; Focus Aim in comfortable reach; Jump that becomes LAUNCH only while standing on an armed pad; Reload near ammunition; Pad action left of the combat cluster; weapon/ammo beside Fire.
- Edge marker: active objective direction and distance when off-screen.
- Pause, Ping, and network warning stay reachable but never cover the crosshair.

Interaction truth:
- Right-side drag and Fire-drag both aim.
- Dynamic crosshair expands during movement/fire/airborne spread and tightens when stable.
- Focus Aim changes camera framing from about 55° to 40° and uses separate sensitivity. It does not show a fake scope or change authoritative damage.
- Show blocked muzzle when the camera can see a target but the weapon line is obstructed.
- Show reload progress, hit confirm, elimination confirm, pad cooldown, and concise invalid-action feedback.
- No crouch, sprint, slide, prone, grenades, healing, inventory, hero ability, vehicle, weapon wheel, or mid-life weapon switching.

Create separate representative HUD frames for: eight-second preparation, normal live play, capturing, contested, next-zone warning, overtime, four-second respawn with teammate spectate and weapon selection, reconnecting to the same avatar, labeled bot substitution after disconnect, low network quality, 30-second AFK warning, AFK removal, and match-server failure. Also create a Simple preset with fewer persistent buttons and stronger contextual actions. It must still support simultaneous move, aim, and fire.

SCREEN 09 — LAUNCH-PAD PLACEMENT & FEEDBACK

Design this as gameplay substates, not a separate menu. The pad has one charge, recharges in 16 seconds, arms in 0.75 seconds, lives for 18 seconds, has 60 HP, and only one pad per owner may exist. New placement removes the owner's old pad. Death does not refresh the cooldown and the pad may outlive its owner.

Create frames for:
1. Tap placement about 1.6m ahead on a legal surface.
2. Hold longer than 180ms to preview trajectory; camera direction steers the preview; drag away to cancel.
3. Valid preview using cyan path, landing marker, directional arrow, and plain PLACE label.
4. Invalid preview using coral plus an icon/pattern and one reason: TOO STEEP, OBJECTIVE BLOCKED, NO CLEARANCE, TOO CLOSE TO SPAWN, OVERLAPPING PAD, or PAD ALREADY ACTIVE.
5. Charging pad, Armed friendly pad, Armed enemy pad, Damaged, Destroyed, Expiring.
6. Standing on an armed pad changes Jump to LAUNCH. It never launches automatically.
7. Enemy uses your pad and friendly player uses an enemy pad. Ownership remains readable through cyan ring versus coral split-chevron/cross motifs, not color alone.

The trajectory and VFX must never hide the player or target. Launch replaces velocity and allows restrained airborne steering and shooting. Do not depict velocity stacking, a shockwave attack, fall damage, a teleport, or a launch that scores merely by passing over the Relay.

SCREEN 10 — MATCH MENU & SETTINGS

Online title: MATCH MENU. Keep the live match status and a small world view visible so the screen never implies online play is paused. Primary RETURN TO MATCH. Secondary SETTINGS. Quiet RETURN HOME must use confirmation that explains the live match continues and leaving may affect eligibility. Never offer paid revival or skip timers.

Offline title: PAUSED. Actions RESUME, RESTART CHALLENGE where relevant, SETTINGS, and RETURN HOME.

Settings tabs:
- Controls: Classic/Simple, Look sensitivity slider with value, Focus Aim sensitivity slider with value, gyro off/on and calibration, invert Y, left-handed layout, aim-assist off/on, CUSTOMIZE CONTROLS, RESET LAYOUT.
- Graphics: AUTO DETECT, LOW, MEDIUM, HIGH, FPS target, dynamic resolution, supported shadow setting. Unsupported features show truthful disabled states and reasons.
- Audio: Master, SFX, Music, Announcer sliders; Mute Team Pings.
- Accessibility: Larger HUD/Text, color-plus-shape cues, Reduce Motion, Camera Shake, Vibration, High-Contrast Objective Marker, Announcer Text.
- Account & Support: compact linked/guest status and links to the dedicated Account/Support screen. Do not crowd destructive account deletion into the live-match menu.

Use real sliders with values. Changes apply live when safe, save immediately, and show a small SAVED confirmation. Every tab has RESET TO DEFAULT and a confirmation only when it would discard customization.

Create a separate Customize Controls frame over a neutral gameplay preview: move, resize, and opacity controls; safe-zone guides; RESET, CANCEL, SAVE; collision/overlap warning; left-handed starting layout. UI touches must never rotate the camera.

DELIVERABLE FOR THIS BATCH

Provide separate 2400 × 1080 frames for each requested state plus a review-only HUD anatomy sheet. Demonstrate 16:9 and 20:9 safe areas, minimum 48dp targets, larger-text mode, color-independent cues, Reduce Motion behavior, and clear thumb reach. No review controls inside player UI.
```

---

## Batch 4 — Results, contracts, and mastery

```text
Continue the approved HOTDROP design system. Create Screens 11 Results, 12 Contracts, and 13 Mastery. These systems should create honest reasons to return through visible progress, choice, and improvement. Do not use streak punishment, random rewards, fake scarcity, battle-pass language, rank pressure, flashing claim loops, or casino presentation.

SCREEN 11 — RESULTS

Create Victory, Defeat, Draw, and Overtime Victory/Defeat frames with equal visual dignity. The screen must answer, in order: What happened? What did I contribute? What progressed? What can I do next?

Header:
- VICTORY, DEFEAT, or DRAW.
- Final score and concise result condition, for example SCORE LIMIT, TIME, OVERTIME HOLD, or OVERTIME DRAW.
- Preserve the player's equipped character, weapon wrap, and badge.

Verified contribution block:
- Objective Time.
- Eliminations / Deaths.
- Pad Launches.
- USEFUL TEAM ROUTE only if a server-verified human teammate used this player's pad.
- NEW PERSONAL BEST only when authoritative data confirms it.
- Do not invent MVP, support praise, comeback, or helpful-pad language without evidence.

Team scoreboard:
- Two teams of three with aliases, [BOT] labels, objective time, eliminations, deaths, pad launches, and connection/left state where relevant.
- Do not expose hidden rating or internal account IDs.

Progression:
- Eligible public completion: +120 XP and +20 credits.
- Win adds +30 XP and +5 credits; draw adds +15 XP.
- Level-up adds +20 credits when applicable.
- Show selected contract progress and applicable mastery movement.
- Explain an ineligible/AFK result plainly; do not show progress the player did not earn.
- Reward pending state: Reward pending — your progress is safe. Provide RETRY without duplicating grants.

Actions:
- Primary PLAY AGAIN, returning the preserved party to queue.
- Secondary PLAY TOGETHER only when post-match party formation is eligible and requires explicit acceptance.
- Quiet HOME.

Create states for normal result, level-up, contract completed with automatically granted Flight Seal, cosmetic choice pending, reward service retry, player ineligible/AFK, server-cancelled match with no invented winner, and party member disconnected.

SCREEN 12 — CONTRACTS

Contracts unlock after two completed public matches. Before unlock, show progress: Complete 2 public matches to choose contracts. No purchase or shortcut.

Unlocked screen:
- Title: CONTRACTS
- Show exactly three permanent choices.
- FINISH TOGETHER — Complete 2 eligible public matches.
- HOLD THE RELAY — Earn 45 seconds of valid uncontested objective occupation across eligible matches.
- BUILD A TEAM ROUTE — Two distinct human teammates use your pads in eligible matches.
- Each card shows exact progress, What Counts info, reward 1 Flight Seal, active/paused/completed state, and a preview of the next meaningful reward.
- Exactly one is active. Selecting another pauses the current contract without deleting progress. Use a clear confirmation showing both saved progress values.
- The route contract must explain that owner uses and bots do not count. If no qualifying teammate is present, show an honest contextual note.
- Offline practice never advances these contracts.

Flight Seal path:
- Six seals unlock one exact choice among three previewable cosmetic items.
- Show 7 Flight Seals as earned progression, not purchasable currency.
- No chest, odds, random reveal, duplicate item, countdown, daily reset, or expiring selection.
- If the initial reward catalog is complete, say COLLECTION COMPLETE and continue showing mastery honestly rather than promising unavailable rewards.

Create locked, choice, active, switching, completed, Flight Seal granted, cosmetic-choice pending, catalog-complete, offline/unavailable, and retry-safe states.

SCREEN 13 — MASTERY

Use a clean permanent flight-log metaphor, never a battle pass. Mastery has no season reset and grants cosmetic badges only.

Tracks:
- RELAY SPECIALIST — 60 / 300 / 1,200 cumulative valid objective seconds for Bronze / Silver / Gold.
- ROUTE BUILDER — 3 / 12 / 40 distinct qualifying human teammate pad uses.
- WEAPON FAMILIARITY — for ARC, ZIP, and BRICK separately: 5 / 20 / 60 eligible matches in which that weapon was equipped for the majority of play.

Each track shows current progress, what counts, completed tiers, next threshold, exact badge preview, and EQUIP when owned. Use Bronze/Silver/Gold as achievement material, not purchasable rarity. Mastery never changes damage, recoil, cooldown, movement, matchmaking, or access to weapons.

Show empty/new, in-progress, tier-earned, badge-equipped, all-current-tiers-complete, offline/unavailable, and retry states. The player's equipped badge must match Home, Profile, Appearance, gameplay identity, and Results.

DELIVERABLE FOR THIS BATCH

Create separate 2400 × 1080 production frames, major state frames, and a review-only progression flow: Results → contract/mastery update → Play Again or Home. Use calm earned motion lasting 600–900ms and make it skippable. No developer controls in player-facing frames.
```

---

## Batch 5 — Offline challenges, Appearance, and Profile

```text
Continue the approved HOTDROP project. Create Screen 14 Offline Travel Challenges, Screen 15 Appearance, and Screen 16 Profile. Preserve the same world, characters, equipment, colors, typography, and interaction language as online play. Offline must feel like a valuable part of HOTDROP, not a cheap minigame.

SCREEN 14 — OFFLINE TRAVEL CHALLENGES

Create a challenge library with three chapters and four authored challenges each. All 12 are included in the installed game and require no internet, account, ad, or download.

Chapter 1 — FIRST FLIGHT:
- Basic Route
- Own-Pad Route
- Enemy-Pad Route
- Safe Landing

Chapter 2 — ROOFTOP OPERATOR:
- Stationary Targets
- Moving Targets
- Reload Discipline
- Launch and Hit

Chapter 3 — RELAY CAPTAIN:
- Capture One Zone
- Rotate Between Zones
- Defend Against Bots
- Complete Offline Relay

Library behavior:
- Show chapter progress, locally earned Bronze/Silver/Gold medals, personal best, and last-played state.
- Medal thresholds must appear as authored values only after validation; do not invent impossible numbers to decorate the mockup. Use placeholders marked TUNE AFTER DEVICE TEST where numbers are not approved.
- Label records LOCAL RECORD. Explain once: Offline medals stay on this device and do not grant online currency or competitive standing.
- Do not show download buttons because all content ships installed.
- Provide OFFLINE RELAY as a clear three-minute practice option.

Create challenge-detail frames showing objective, controls/mechanics used, best result, medal conditions, START, and Back. Create gameplay-complete frames with RETRY, NEXT CHALLENGE, and CHALLENGE LIBRARY. Create interruption recovery with RESUME and RESTART. If snapshot recovery fails, explain it and offer a clean restart. Online matches must never show Pause/Resume language.

Create airplane-mode, no-local-progress, chapter-complete, all-challenges-complete, corrupt-save-recovery, and local-save-unavailable states. Never imply local save edits can mint online rewards.

SCREEN 15 — APPEARANCE

Show the real rotatable in-game character on a neutral Switchyard platform using the same lighting and equipment as Home. Changes must preview on the actual character and weapon.

Four independent categories:
- BODY PRESENTATION — freely changeable; same skeleton, animations, capsule, hitbox, and gameplay.
- BODY STYLE.
- WEAPON WRAP for ARC, ZIP, and BRICK.
- BADGE.

Show OWNED and LOCKED filters, equipped state, exact unlock source, and side-by-side preview where useful. Locked examples say Complete Relay Specialist Bronze or Choose after earning 6 Flight Seals; do not use vague mystery locks. Cosmetics never alter silhouette readability, team cues, weapon audio, power, recoil, movement, or matchmaking.

Actions: EQUIP, PREVIEW, BACK. Show SAVED after successful equip. Include asset-loading, equip-pending, retry, item-revoked/unavailable, and offline-cached states. Do not show a price unless the future shop is enabled and a verified localized price is available.

SCREEN 16 — PROFILE

Create a personal improvement profile, not a public status contest.

Show:
- Generated alias with validated rename entry.
- Level 1–20 onboarding track, current XP, and next level requirement. The track never resets.
- Credits and equipped badge.
- Eligible matches, wins, draws, objective contribution, selected personal bests, mastery highlights, and completed offline chapters.
- Equipped body style and weapon wrap preview.
- Keep hidden matchmaking rating hidden.
- Avoid public age, gender, real location, contacts, loss-shaming ratios, global rank, or fabricated percentile.

Alias rename uses allowed-character guidance, length feedback, availability check, and clear error recovery. Do not expose internal account IDs. Create new-profile, established-profile, level-20-complete, data-loading, offline-cached, rename-error, and retry states.

DELIVERABLE FOR THIS BATCH

Create separate 2400 × 1080 production frames, major state frames, and one review-only navigation map: Home → Offline Library / Appearance / More → Profile. Maintain Back paths and never place design controls inside player UI.
```

---

## Batch 6 — Account/recovery/support/reporting, future shop, and final system audit

```text
Continue the approved HOTDROP design system. Create Screen 17 Account, Recovery, Support & Reporting and Screen 18 Future Shop Concept. Then create one separate final design-system and journey-audit canvas. Keep destructive, legal, and future-only actions out of the main Home hierarchy.

SCREEN 17 — ACCOUNT, RECOVERY, SUPPORT & REPORTING

Account overview states:
- Guest: explain plainly that progress may be lost if the app is removed or the device is unavailable. Primary LINK GOOGLE PLAY GAMES; secondary CONTINUE AS GUEST.
- Linked: show linked status, last successful sync, RECOVER LINKED ACCOUNT where relevant, and REVOKE LINK.
- Sync pending/retry: Progress is saved on this device. Online sync will retry. Never imply success before confirmation.

Recovery:
- Explain which profile is about to be restored using alias and safe metadata.
- If local and cloud progress conflict, present clear choices only when the backend supports them; do not invent a merge.
- Use progress-safe retry language and a Support route.

Support and legal:
- PRIVACY
- TERMS
- SUPPORT
- RESTORE PURCHASES only on supported builds and only for verified purchases.
- Show external-link confirmation where appropriate and always preserve Back.

Report Player:
- Start from a recent-match list.
- Select a visible alias and team slot; never ask the player to paste an internal GUID.
- Select one concise category, optional bounded details, SUBMIT REPORT, and CANCEL.
- Provide block/mute only if those systems actually exist; do not invent them.
- Confirmation says REPORT RECEIVED without promising a punishment or outcome.
- Create no-recent-match, offline, submission-failed, and submitted states.

Delete Account:
- Keep deletion behind Account, never in the main Settings landing view.
- Step 1 explains what will be deleted and what may be retained for legal/security obligations.
- Step 2 requires an explicit final confirmation with a deliberate hold or typed confirmation suitable for mobile.
- Back and Cancel remain available until the final confirmed action.
- After submission, show real pending/completed/recovery-window status from the service. Never fake immediate deletion.

SCREEN 18 — FUTURE SHOP CONCEPT — DISCONNECTED FROM HOME

Create this as a separate concept frame with a large review-only annotation outside the UI: ENABLE ONLY AFTER GAMEPLAY VALIDATION AND COMPLETE PURCHASE/RECOVERY TESTING. Do not add a Shop destination, banner, badge, popup, or price to Home or the primary journey.

If enabled later, show exactly three permanent cosmetic bundles with original contents:
- Signal bundle — localized price returned by Google Play.
- Foundry bundle — localized price returned by Google Play.
- Skyline bundle — localized price returned by Google Play.

Each bundle shows exact body styles, weapon wraps, and badges; real in-game preview; COSMETIC ONLY; permanent ownership; BUY; and verified purchase state. No premium currency, random contents, loot boxes, power, paid respawn, forced ad, fake discount, crossed-out invented price, countdown, limited-stock claim, or mid-match purchase.

Create concept states for price loading, purchase pending, verified, failed, already owned, restore purchases, refund/revocation, and service unavailable. Never show a successful grant before backend verification.

FINAL DESIGN-SYSTEM CANVAS — REVIEW/HANDOFF ONLY

Create one separate canvas outside the player flow containing:
- Semantic color tokens and color-plus-shape rules.
- Type scale, tabular numerals, icon grid, spacing, panel radii, borders, and elevation.
- Primary, secondary, quiet, destructive, disabled, loading, and icon-only controls.
- Tabs, sliders, toggles, fields, progress bars, contract progress, mastery badges, medals, toasts, dialogs, skeletons, offline banner, network warning, and error/retry panel.
- HUD controls in idle, pressed, held, cooldown, unavailable, LAUNCH, and customize-layout states.
- Friendly cyan ring, enemy coral split-chevron/cross, and objective gold ring motifs.
- Safe-area examples for 16:9 and 20:9.
- Larger-text and 30%-longer-copy examples.
- Reduce Motion examples.
- Unity handoff names for reusable components. Use implementation-neutral names such as Button/Primary, Card/Contract, HUD/ObjectiveStatus, HUD/PadAction, Dialog/DestructiveConfirmation.

FINAL CONNECTED-JOURNEY AUDIT

On a separate review canvas, map these flows with arrows outside the UI:
1. First run: Startup → Appearance Choice → Training → Home.
2. Online: Home → Matchmaking or Party → Matchmaking → Loadout/Preparation → Gameplay → Results → Play Again/Home.
3. Offline: Home → Challenge Library → Challenge Detail → Offline Gameplay → Challenge Result → Retry/Next/Home.
4. Progression: Home/More → Contracts/Mastery/Profile → Back to the originating screen.
5. Recovery: any recoverable failure → retry, offline alternative where valid, Support, or safe Back.

Audit every player screen for a clear primary action, readable two-second hierarchy, reachable Back path, protected destructive action, 48dp touch targets, safe areas, contrast, color-independent meaning, low/mid-range Android feasibility, consistent character/equipment identity, honest system state, and no copyrighted imitation.

DELIVERABLE FOR THIS BATCH

Return separate 2400 × 1080 production frames, major state frames, the future-only Shop concept, one developer design-system canvas, and one complete journey-audit canvas. Do not place review annotations, frame selectors, or developer controls inside player-facing screens.
```

---

## Acceptance gate after all six batches

Do not begin broad Unity UI implementation until the generated project passes these checks:

1. The approved Home remains visually dominant and every screen feels like the same product.
2. The launch pad, Relay rules, timing, scoring, weapons, progression, offline boundaries, and account states contain no invented behavior.
3. Every normal, loading, empty, offline, disabled, error, retry, and destructive state has an honest next action.
4. Player screens contain no prototype switchers, component modals, safe-area guides, or implementation notes.
5. The full first-run, online match, offline challenge, results, rematch, recovery, and Home-return journeys are connected without dead ends.
