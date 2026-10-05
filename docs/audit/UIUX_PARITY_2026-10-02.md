# HOTDROP UIUX reference parity — 2026-10-02

## Follow-up — 2026-10-04

The shared Unity menu frame now uses a compact left-aligned title and Back control, a shorter status rail, and left-aligned content. The Party entry and live room show a clearer invite-code hierarchy and three squad cards. Appearance has a proportional live runner preview beside an information card, plus functional category tabs for actual owned items and their equip state; Profile has a visible loading card and, when the backend responds, separate identity, progression, eligible-match and saved-identity sections. Failed backend pages now show a framed error and retry action. These changes are in `MenuLayout.cs` and `PlayerProductScreens.cs`.

Verification: `PlayerProductScreensNavigationTests` passed 14/14 in EditMode. The targeted PlayMode visual capture for Training/Appearance/Profile passed 1/1; the Home/Settings/Support capture passed 1/1; an additional PlayMode test that clicks the Appearance category tab passed 1/1. Current Appearance, Profile, Home, Settings and Support captures were regenerated in the existing `ProjectHotdrop/UIEvidence` folders. `15_appearance_owned_fixture_1920x1080.png` uses explicitly labeled test-only inventory to inspect the populated right column; it is not a live account capture. The Profile capture shows a truthful loading state because the capture harness does not supply a live profile response; it does not prove the populated profile layout. No connected Party, matchmaking, account recovery or online results state was visually accepted in this follow-up.

Exact parity with every image in `UIUX` is still open. The folder includes review sheets and concept-only content; the approved HOTDROP product language, real game state and disabled Shop gate take precedence over invented stats, purchases or military branding. Final runner, weapon and arena art and device captures are still needed for visual acceptance of the playable screens.

Audit session started 2026-10-02 and continued past midnight into 2026-10-03 (Pakistan time). The report/capture folder names identify the session start date, not a claim that all captures predate midnight.

## Verdict

**Not 100% accurate or complete.** This is a source/reference inspection plus a bounded UI implementation pass, not acceptance of all 18 screens, finished art, a live backend, or a production build.

The UIUX folder contains 29 `screen.png` files, including review sheets, alternate Home states and handoff/icon/art references rather than 29 distinct production screens. **Corrected 2026-10-03: an earlier version of this document claimed "the individual Screen 01–18 PNGs were visually inspected against the Unity screen implementations". That was not supported by evidence and is withdrawn.** A screen can only be compared against the reference if a runtime capture of it exists; as of 2026-10-02 only 7 of the 18 production screens had one, and a ledger pass on 2026-10-03 found **11 of 18 had never been rendered at all** - so they had never been compared. The current, evidence-backed count and per-screen verdicts are in *Parity verdicts* below. The repository's `docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md` explicitly prohibits a literal copy of the entire UIUX folder: the review strips/handoff sheets are not game screens; Screen 08–10 lower dashboards and Screen 11–17 military/navigation/currency content must be redesigned. Remote concept images are neither production meshes nor licensed assets. Screen 18 must remain disconnected.

## Reference-to-runtime assessment

| Screen | Runtime owner | Current evidence / remaining gap |
|---|---|---|
| 01 Startup | `Core/StartupConnectionScreen.cs` | Real connection-state copy and local exits exist. Generic geometric/text presentation, not the reference's complete visual composition. Fresh editor capture available. |
| 02 Runner choice | `Core/OfflineHomeScreen.cs` | Improved: two real in-game model cards, explicit selected state and Continue/save; cosmetic parity copy and Decide Later retained. Production runner meshes/materials remain missing. |
| 03 Home | `Core/OfflineHomeScreen.cs` | Improved: real equipped-look preview, dominant Relay action, framed honest next-goal card, Offline action, fixed Party/Appearance/More navigation. No Shop. Not literal concept-art parity. |
| 04 Training | `Tutorial/TutorialController.cs`, `Tutorial/TutorialUI.cs` | **2026-10-03: captured** `parity_2026-10-03/04_training_1920x1080.png` and compared. Guided gameplay is exercised by the smoke suite. **Open defect: the instruction and step counter are unreadable** - see verdicts. |
| 05 Party | `Core/PlayerProductScreens.cs` | Improved: three real member/open-place cards with actual ready/leader states. Create/join/back flows retained. Live party/device visual evidence still needed; no invented player portraits or stats. |
| 06 Matchmaking | `Network/OnlinePlay.cs` | Actual search/elapsed/region/cancel/recovery implementation exists. Not visually accepted against a real allocated match; requires configured staging/network acceptance. |
| 07 Preparation | `UI/MatchHUD.cs`, match controllers | **2026-10-03: captured** `parity_2026-10-03/07_preparation_1920x1080.png` and compared. Existing ARC/ZIP/BRICK config-backed preparation and spawn/damage/pad restrictions covered by smoke tests. |
| 08 HUD | `UI/MatchHUD.cs`, touch controls | Real gameplay/HUD exists and physical-size tests pass on the simulated reference phone. Not a copied review dashboard. Final map/character/weapon art and all contextual/device states remain unaccepted. |
| 09 Pad feedback | `Pads/*`, `UI/MatchHUD.cs` | **2026-10-03: captured** `parity_2026-10-03/09_pad_feedback_1920x1080.png` (rejection path, driven by two real `TryPlacePad()` calls). Runtime placement/launch behavior tested. The 3D trajectory preview is touch-gated (`TouchInputManager.PadPreviewActive` has a private setter) and is **not** reachable headlessly; arming/damaged/expiry states still need acceptance. |
| 10 Settings/Match Menu | `Core/OfflinePauseMenu.cs` | Improved: Controls, Graphics, Audio, Accessibility, Account/Support tabs; section cards; slider overlap fixed. Offline pause versus online live-menu semantics preserved. Fresh editor settings captures and physical checks; live-online/device screenshots still pending. |
| 11 Results | `UI/MatchHUD.cs`, match/result controllers | **2026-10-03: captured** `parity_2026-10-03/11_results_1920x1080.png` from a match actually played to its end, and compared. **Fixed this pass:** teammate cards rendered over the results table's PLAYER column. Do not substitute fake verified rewards. |
| 12 Contracts | `Core/PlayerProductScreens.cs`, `ContractsMasteryFeature.cs` | Disabled-by-default staging feature. Existing backend/UI work is not visual or end-to-end production acceptance. Exact reward art/catalog and approved staging evidence are outstanding. |
| 13 Mastery | Same feature and backend progress service | Disabled-by-default. Permanent progress/threshold code is not finished reward art or an accepted player journey. No fake season/rank/telemetry imported. |
| 14 Offline Challenges | `OfflineChallengeController`, `OfflineChallengeRuntime`, `OfflineChallengeScreen`, config/save | **2026-10-03 follow-up implemented:** actual Library/Detail/tracker/pause/local Result/Retry/Next route, all twelve real-world drills, atomic/contextual local save and explicit interruption recovery. Editor tests/captures are recorded in `OFFLINE_TRAVEL_IMPLEMENTATION_2026-10-03.md`. Airplane-mode, process-loss and touch acceptance on Android remain open; this is not final art or gate acceptance. |
| 15 Appearance | `Core/PlayerProductScreens.cs` | **2026-10-03: captured** `parity_2026-10-03/15_appearance_1920x1080.png` and compared. Real cached-look preview and free runner-change action rendered before inventory response; camera isolation/local light; owned-item equip still needs server confirmation. Not full locker composition or verified cross-device inventory evidence. |
| 16 Profile | `Core/PlayerProductScreens.cs` | **2026-10-03: captured** `parity_2026-10-03/16_profile_1920x1080.png` and compared. Identity/progress card and actual eligible/win/draw statistic cards. No made-up rank/percentile. Live profile and finished art/device evidence remain. |
| 17 Account/Support | `Core/PlayerProductScreens.cs` | Shared style applied; existing real report and two-step deletion/back behavior retained. Support editor captures available. Google Play link/recovery, published policy URLs and real deletion/account-state device evidence are external acceptance work. |
| 18 Shop | Gated catalog/commerce | **Not implemented or enabled in this pass.** Gate D has not passed; no publisher signing/Play Billing/lifecycle approval evidence. |

## Parity verdicts — 2026-10-03 evidence pass

Six screens that existed and were reachable had never been rendered. `Capture(Canvas, label, ...)`
reached all six from test code only — no backend, no art, no gate approval — via two new PlayMode
tests in `PlayerJourneyVisualEvidenceTests`. Captures are in `ProjectHotdrop/UIEvidence/parity_2026-10-03/`.

| Ref | Capture | Verdict |
|---|---|---|
| 04 Training | `04_training` | **Divergence, defect fixed 2026-10-04.** Structure is close (top-centre card, step counter, SKIP); the instruction now renders legibly after the duplicate-card fix. Reference separates SKIP to the top-right; ours embeds it in the card. |
| 07 Preparation | `07_preparation` | **Divergence.** We have the countdown, squad chips, permission strip and three config-backed weapon cards with real stats. Our objective caption no longer prints over the weapon cards - the placement rule now measures them and puts the marker in the 39-unit slot above the panel with 2.00 units of clearance, verified on the `07` and `07b` frames. |
| 09 Pad feedback | `09_pad_feedback` | **Compared with known gaps.** The rejection toast works and is driven by the real `TryPlacePad()` rule. Reference's dominant state is a *valid* placement with a dashed trajectory and a pad-spec panel; that 3D preview is touch-gated and unreachable headlessly. |
| 11 Results | `11_results` | **Fixed this pass.** Teammate cards rendered over the results table's PLAYER column, making the first name in each row unreadable. `ShowResults` now hides the teammate panel; `Bind(match: true)` restores it on the next match. Both rows now fully legible. |
| 15 Appearance | `15_appearance` | **Divergence.** Real runner preview and free selection, but far less dense than the reference locker (no category rail, no slot grid, no equip-state chips). |
| 16 Profile | `16_profile` | **Divergence.** Real identity and statistics cards, no invented rank. Much sparser than the reference dossier. |

### Resolved 2026-10-04: the training instruction was unreadable — two stacked cards

`04_training_1920x1080.png` showed "Move to the marker using the left joystick" at **luma 39 against
a plate of luma 20** (~1.5:1) while SKIP on the same card rendered at luma 247. That is the whole
purpose of the screen.

**Cause: `GameBootstrap.StartTutorial` rebuilt the view on every entry and never removed the old
one.** Runtime instrumentation found **two `Training instruction` Graphics at the same rect** (Unity
draw depths -11 and -16). The upper card's 93%-opaque plate composited over the lower card's
caption, which is exactly the measured blend `0.93*plate + 0.07*white`. A player can reach this:
first run auto-starts the tutorial ([GameBootstrap.cs:133](ProjectHotdrop/Assets/Scripts/Core/GameBootstrap.cs#L133)),
and Home > More > TRAINING starts it again.

Fixed by reusing the view the controller already owns. Verified by an A/B capture with the plate
disabled (caption bright) and again on a clean single-card scene, then by two independent runs of
the parity test — both produce a frame where the instruction and step counter render white and
legible. `AssertSingleTrainingCard` in the parity capture test fails if a second card ever returns.

Hypotheses tested and eliminated along the way, so nobody repeats them: text colour (the component
is white; recolouring red showed the glyphs are drawn), sibling order (`SetAsLastSibling` — no
change), the plate's alpha (1.0 hid the captions; reparenting the plate to its own child at sibling 0
— no change), and the private `TutorialUI.Label` helper (routing it through the shared `Ui.Text`
owner — no change). Every one of those probes was reverted; **`TutorialUI.cs` is byte-identical to
its state before this investigation.**

**Not done: the rendered-contrast assertion that was asked for.** It was written, and it failed
for the wrong reason — mapping the caption's rect through the render camera put the sample region
outside the card (in the captures taken at the time, that was the black band below the gameplay
view), so it reported a 1.01:1 ratio on a frame where the text was plainly legible. Rather than ship
a guard that cannot distinguish good from bad, it was removed. `AssertSingleTrainingCard` is real
and passing; **a genuine luma assertion is still owed**, and getting the screen-space mapping right
under a ScreenSpaceCamera render is the prerequisite.

### Closed: world markers landed on the weapon-select cards

`07_preparation` and `09_pad_feedback` both showed the objective caption printed over the weapon card
text. `HudMarkerLayer` reserved the top and bottom bands measured from the *gameplay* HUD; the
weapon-select panel occupies the lower middle during preparation and was never measured. Same defect
class as the score-bar collision closed in `HUD_OWNERSHIP.md`, on a panel that was invisible until
this pass captured it.

**Fixed by measuring the real footprint at runtime, not by naming the screen.** `MatchHUD` now
publishes `ActivePanelRects()`, which converts each on-screen panel's world corners into
canvas-local units, and `HudMarkerLayer.AvoidPanels()` keeps a marker caption out of them. The panel
list is measured per frame from whatever the HUD actually has up, so a panel that appears in any
phase is honoured without the rule knowing that phase exists.

**Four separate causes had to be fixed; fixing only the first still failed the capture.**

1. *The rules used the anchor as if it were the text.* A marker's caption hangs **below** its
   anchor, so every overlap test was measuring a box above where the text is drawn.
2. *The two rules undid each other.* `AvoidPanels` lifted the caption off the panel and
   `SeparateFromPlaced`, knowing only about other markers, immediately put it back down. Both now
   test one combined `Blocked(...)` predicate, so a row must be free of panels *and* markers.
3. *No row fit at all.* At the harness's 1280x720 canvas the readable band is y −172.8…187.2,
   `WeaponSelect` covers y −190…70 and `Preparation` covers y 110…190, leaving only y 70…110 — forty
   units, enough for the thirty-unit caption, but between two sampled rows of the 44-unit sweep.
   `NearestFreeSlot` centres the marker in the measured free gap nearest the preferred position.
4. *The caption was the wrong thing to protect.* The capture passed while the diamond still printed
   across `DAMAGE BLOCKED`, because only the caption box was asserted. The rule now protects
   `MarkerFootprintSize` — the **whole** marker — and `AssertNoMarkerOverlapsPanel` measures the
   real drawn bounds of every visible child, rotated geometry included, rather than trusting the
   declared box.

**Changing the marker's own layout was the only honest way out.** Icon above caption made the
objective 74 units tall and a ping 60; the only free gap is 40. No placement rule can fit a 74-unit
marker in a 40-unit gap, so the geometry had to change rather than the rule. Laying icon and caption
on one row makes both 36 units tall. A second correction came straight out of the new assertion: a
diamond rotated 45° has an axis-aligned box of `size × √2`, so a 34-unit icon measured **48.09**
tall against a declared 36 and put the marker straight back on the panel. Icons are now sized against
that diagonal (24 and 22), and an EditMode test pins `MarkerFootprintSize.y` to the measured gap so
a future restack cannot silently reintroduce it.

**The assertion is on the real frame and has teeth.** `AssertNoMarkerOverlapsPanel` joins the other
two checks in the capture harness. Neutering `AvoidPanels` makes the capture fail with concrete
numbers: `'ObjectiveMarker:CENTER 34m' (x:-100.00, y:-39.00, width:200.00, height:30.00) intersects
panel 0 (x:-450.00, y:-190.00, width:900.00, height:260.00)`.

Worth recording: this assertion was **vacuous on its first draft**. It compared the caption's
*world* rect against the panels' *canvas-local* rects, which under a `ScreenSpaceCamera` render do
not coincide, so it could never fire. It only became a real check once both sides were converted to
the same space. The same lesson as the earlier probe that "passed" without testing anything.

**Teeth.** Neutering `AvoidPanels` fails the capture on the full footprint, with the real measured
numbers rather than the declared box: `'ObjectiveMarker:CENTER 34m' footprint (x:-116.97, y:-16.97,
width:236.97, height:33.94) intersects panel 0 (x:-450.00, y:-190.00, width:900.00, height:260.00)`.

### Still open on these two screens

- **Weapon-card stats overflow the card's bottom edge** on `07` and `09` — the `18 DMG · 480 RPM` and
  `24 MAG · 1.6s RELOAD` lines sit below the card border. Visible in the regenerated captures; not
  addressed by this pass.
### Closed: the pad-feedback line was a real element the rule could not see

The line above was not a "report it and move on" item — it was the same defect class, and it is now
fixed properly.

**Measured, on the 720-unit canvas the 1920x1080 capture renders:**

| element | canvas-local rect |
|---|---|
| Marker footprint | y 71.03…104.97 (33.94 tall) |
| Feedback **ink** | y 97.71…122.29 (24.58 tall), x −249.93…249.92 |
| Feedback **container** | y 40…180 (140 tall, ~85% empty padding) |
| Free gap in the marker band | y 70…110 (40 units) |

The source is `combatFeedbackText`, the HUD's general transient-messaging channel — `PAD READY`,
`LAUNCH`, `CAN'T PLACE PAD — …`, `SHOT BLOCKED`, `ELIMINATED <name>`, `GO — CAPTURE THE ZONE`,
`MOVE OR YOU'LL BE REPLACED BY A BOT`. Nothing about it is pad-specific.

**Measuring it alone was not enough, and the geometry says so.** Publish its ink and the only free
space in the band becomes y 70…97.71 = **27.7 units**, against a 33.94-tall marker. There is no
placement that clears both, so the rule would keep its fallback and put the marker back on the
weapon panel. That is exactly what probe 1 demonstrated (see teeth below). Shrinking the marker to
27 units was the alternative, and it costs text size, which players read — so the contention was
removed instead: transient feedback now has **its own band** at `CombatFeedbackBandY = 205`, in the
strip between the marker band ceiling (187.2) and the zone caption (222). Its ink clears both.

Measurement follows the same mechanism as panels, no second policy: `ActivePanelRects()` publishes
the feedback element's **ink**, not its 700×140 container, because taking the box would report 140
units of occupied screen for 25 units of readable text and leave markers nowhere to go. It falls
back to the container if the mesh has not been generated, so the measurement is never silently empty.

**Teeth, two probes.** Undoing only the layout half (toast back at y=110, measurement intact) fails
with the marker on the weapon panel — `'ObjectiveMarker:CENTER 34m' footprint (x:-116.97, y:-16.97,
width:236.97, height:33.94) intersects panel 0 (x:-450.00, y:-190.00, width:900.00, height:260.00)`
— which is the "no clear placement exists" proof. Undoing only the measurement half fails on the new
assertion with the original defect caught: `'ObjectiveMarker:CENTER 34m' footprint (x:-116.97,
y:71.03, width:236.97, height:33.94) intersects 'CAN'T PLACE PAD — NOT DURING THIS PHASE' ink
(x:-249.93, y:97.71, width:499.85, height:24.58)`.

**07 stayed clear.** It carries no feedback, so it is unchanged by the move, and both captures were
re-opened at native resolution to confirm it: 07's marker still sits in its gap, and on 09 the toast
now reads cleanly above the `LOADOUT` plate.

`TheToastBandClearsTheMarkerBand` pins the invariant so the two bands cannot silently start
contending again.

### Closed: weapon-card stat lines rendered outside their card

On `07` and `09` the numbers a player picks a weapon by — `18 DMG · 480 RPM`, `24 MAG · 1.6s
RELOAD` — rendered below the card's bottom border, so they read as belonging to the gap between the
cards rather than to the card above them.

**Measured on the 720-unit canvas the 1920x1080 capture renders, before the fix:**

| element | rect |
|---|---|
| Card interior | y −160…+40 (270×200) |
| Role ink | y −133.88…−62.12 (71.76 tall, 3 lines) |
| Stats ink | y **−169.25…−126.74** (42.51 tall) — 9.25 below the card |
| Unused space above the title | ~35 units |

**It does fit — nothing had to shrink.** Title ink ~41 + role 71.76 + stats 42.51 = 155.27 inside a
200-unit card. The boxes were simply oversized and the stack sat too low, so ~35 units were wasted at
the top while the last block hung out the bottom. `MatchHUD` — the card's owner — now lays the stack
out from its own measured interior: title at y −28…13, role at −92.88…−21.12, stats at
−143.26…−100.75, leaving ~7 between blocks and ~17 below the last.

**A second overflow surfaced as soon as the first was fixed, and it was not vertical.** `BRICK
SCATTERGUN`'s `9 DMG · 8 PELLETS · 75 RPM` measured **279.07 wide inside a 270-wide card** — running
off both edges. Wrapping was disabled on the stat text, so a long line simply overflowed. It now
wraps, which keeps every number at the same size instead of shrinking it to fit. Because a wrapped
line needs a third row, the card went from 200 to **240 tall**, still inside the 900×260 panel
(−180…60), so the panel the marker rule measures is unchanged and the 40-unit marker gap is
untouched.

`Role`'s position is now applied whether or not the text was just created — a scene-authored child
must get the same layout as a code-built one, or the file would own only half of its own card.

**Teeth.** Reverting the card geometry fails with the original numbers: `'WeaponBtn_0' stats '18 DMG
· 480 RPM / 24 MAG · 1.6s RELOAD' ink (x:-396.83, y:-169.25, width:213.66, height:42.51) is not
inside card (x:-425.00, y:-160.00, width:270.00, height:200.00); it overflows by 9.25 units.`

`AssertWeaponStatsStayInsideTheirCard` compares each card's **real** rect against its stat text's
**real ink**, so it catches vertical and horizontal escape alike, and fails if no card carried a stat
line at all — an assertion that silently stops checking anything is worse than none.

**Then the fix traded a border overflow for a misreadable number.** With wrapping on, `BRICK
SCATTERGUN`'s row folded as `9 DMG · 8 PELLETS · 75` / `RPM` — a fire rate whose unit sat on the next
line, which is worse than text running long. Resolved by breaking at a real boundary instead of
letting the wrap choose: the pellet count belongs with the damage it modifies, so a pellet weapon
gets the rate on its own row. Non-pellet weapons keep the two-row layout, because a card that always
showed three rows would waste space on the weapons that do not need it. Nothing was deleted, no type
size changed, and the three rows fit the card's 68-unit stat box with the measured 63.8 ink.

`LoadoutStats_NeverSplitAValueFromItsUnit` guards the invariant directly: every row the copy emits
must not end on a bare digit, so a unit can never be stranded on another line however the text later
wraps. `LoadoutStats_GiveAPelletCountItsOwnLineSoTheRateStaysReadable` pins the row counts.
Reverting the break fails with `Expected: 3, But was: 2`.

**Visual sign-off.** The preview panel refused to attach tabs throughout, including for a
registered local HTML page, so the captures were decoded and rendered in the terminal as ASCII at
1:1 pixel scale and read directly. What the pixels actually show, in both `07` and `09`:

| card | stat rows (pixels) | read as |
|---|---|---|
| ARC RIFLE | y 683–703, 715–735 | `18 DMG · 480 RPM` / `24 MAG · 1.6s RELOAD` |
| ZIP SMG | y 683–702, 716–735 | `13 DMG · 780 RPM` / `30 MAG · 1.5s RELOAD` |
| BRICK SCATTERGUN | y 667–686, 700–718, 732–751 | `9 DMG · 8 PELLETS` / `75 RPM` / `6 MAG · 2.0s RELOAD` |

`75 RPM` is one row with the value and unit together — the split the previous fix introduced is gone,
and neither of the other two cards picked up a break (both stay at two rows). Every stat row ends by
y=751; the card plate ends at y=810 and the strip between (y 811–824) has **zero** ink, so nothing
renders below the border. The feedback line on `09` renders as one continuous row at y 214–251,
above the marker band (which starts at y=259).

The objective marker's diamond and `CENTER 34m` caption now occupy y **380–429** on both `07` and
`09`, against a weapon-panel top edge at y=435 — **five clear rows** of separation, measured by
scanning for glyph ink (luma > 120) inside the marker's own span rather than by eye.

### Resolved this pass

**The marker/panel clearance was knife-edge, and is now a deliberate margin.** The previous
finding recorded the marker clearing the weapon panel by ~1.5 px. That was genuinely clear and the
assertion proved it, which is exactly the problem: non-overlap cannot see a 1 px gap. `HudMarkerLayer`
now carries an explicit required clearance, and the placement rule reserves it.

- **Where the margin belongs: in the rule, not the footprint and not the band.** A larger footprint or
  a wider band buys clearance by taking room away from the marker's glyphs or from the space markers
  are placed in. A margin is a separate requirement the rule enforces, so it costs layout nothing and
  applies to every panel and every other marker through the single `Blocked` predicate.
- **`AvoidPanels` was rewritten from first-fit to largest-clearance-wins**, scoring every candidate
  with `ClearanceAt` and accepting only once it reaches the margin. First-fit spent all the slack on
  one side and left 1.03 units on the other.
- **`CandidateYs` gained the centre of each free vertical gap**, not just the swept rows, and now
  sorts the panels before its cursor scan. The scan assumes intervals arrive in ascending order;
  `ActivePanelRects` publishes them in field order, so the Teammates panel (y 20–180) arriving after
  the preparation plate (y 110–190) was skipped as "already covered" and the one gap the preparation
  screen has never became a candidate. That bug silently disabled the rule and left the marker on
  the weapon panel; the capture caught it.
- **The gap centre, not its nearest edge.** Clamping the preferred row into `[lo+half, hi-half]` snaps
  it to whichever edge is closer, and since the slot is only 40.0008 units against a 36-unit footprint
  every legal position is within half a unit of the margin. That landed the marker *exactly* on the
  margin — a rule that guarantees the minimum clearance is still guaranteeing a knife edge.

**`MarkerClearance = 1.5`, and the interesting number is why not 2.** The binding constraint is the
declared footprint: a slot must hold `MarkerFootprintSize.y + 2C`. The only free slot is forty units
and the footprint is thirty-six, so `(40 − 36) / 2 = 2` exactly — and 2 was tried. It fails on the
live frame: panel rects are laid out in floats, the real slot measures **39.9993**, and `36 + 2*2`
exceeds it by four thousandths of a unit. No candidate reaches the margin, the rule falls back to the
original placement, and the marker is left **on the weapon panel**. A requirement that consumes its own
slot is the same knife-edge in a different place, and it fails *silently* by disabling the rule rather
than by refusing. 1.5 needs 36.94 and leaves ~3 units of slack that float rounding cannot consume, and
because the slot is only 40 units tall a marker that clears the requirement in the middle of it ends
up ~2.0 units from each panel — **double** what first-fit gave. `TheClearanceLeavesSlackInTheSlotRatherThanConsumingIt`
pins the slack requirement directly, so the bound cannot be re-tightened to the arithmetic maximum.

**Non-vacuity, proven by neutering.** `AvoidPanels` was temporarily replaced with first-fit over a
1-unit grid. The capture then failed with the concrete numbers:

> Marker clearance is knife-edge at 1920x1080. 'ObjectiveMarker:CENTER 34m' reserved footprint
> (x:-120.00, y:71.00, width:240.00, height:36.00) keeps only **1.00 units** from panel 0
> (x:-450.00, y:-190.00, width:900.00, height:260.00), and the placement rule requires 1.5. Drawn ink
> (x:-116.97, y:72.03, width:236.97, height:33.94) is clear — which is exactly why the margin needs
> its own check: a non-overlap assertion alone reports this frame as good.

With the rule restored the frame measures footprint clearance **1.9987** units. The assertion
compares the marker's *declared footprint* (its root rect, built at `MarkerFootprintSize` and centred
on the anchor, so `CanvasLocalRect` recovers exactly what the rule placed) rather than its ink: the
footprint is a deliberate superset of the drawn glyphs — the diamond's 45° box, 24 × √2 = 33.94, fits
inside the declared 36 — so a margin measured on it is a margin the ink also has. `RectGap` was made
public so the assertion and the rule share one definition of "gap" and cannot disagree.

**Card 0 vs the teammate panel: measured, and deliberately left unguarded.** The rects do overlap —
Teammates occupies canvas x −590…−370, y 20…180; card 0 occupies x −425…−155, y −180…60, sharing a
55 × 40 unit corner (x 322–405, y 450–510 at 1920×1080), which grew from 30 px to 60 px when the card
went 200 → 240 tall. Decision: **no guard**, for two measured reasons.

1. *Nothing readable is lost.* The teammates' names are laid out at y 80–174, above the overlap, so the
   shared corner holds only empty panel margin plus the leading `>` and `A` of the weapon title — and in
   the capture those pixels render at **full luma 255**, because the card draws in front of the glass
   panel rather than under it.
2. *The marker path cannot be affected either way.* `ActivePanelRects` is a marker exclusion set, not a
   packing system. Overlapping rects make placement harder, never wrong, because every rect is tested
   independently; with the Teammates panel in the list the objective still finds its slot and the
   capture asserts the margin it lands with.

A guard was rejected on purpose: panel-versus-panel packing would be a second layout policy that does
not generalise, and it would fire on plate margins that cost a player nothing. Recorded in `MatchHUD`
beside the `teammatePanel` field, along with the one thing that *would* change the verdict — the
Teammates panel drawing above the weapon card, which would dim that corner of the weapon name. Nothing
asserts against that today; it would surface in the next capture. A known shape beats an unknown
surprise, but it is not a closed risk.

### Resolved this pass (follow-up): the behind-camera marker had no clearance at all

The audit found that the clearance work only covered half the objective marker. `UpdateObjectiveMarker`
had two branches; the projected one called `AvoidPanels`, the behind-camera one clamped to a screen
edge and stopped - no panels measured, no margin required. Since `ProjectToCanvas` returns false
whenever the player looks away from the zone, the unprotected path was the common one.

**Both paths now go through one placement call.** The `ActivePanelRects` measurement and the
`AvoidPanels` call were hoisted out of the branch, so there is exactly one placement call site in the
HUD and both candidates get the same measured panels and the same `MarkerClearance`.
The behind-camera branch used to have its own mirror/clamp/convert helper in the same class. That
helper is deleted: the branch now takes its direction from the camera's own axes and its edge from
`SafeAreaRoot.ContentRectFor`, and never leaves canvas-local units, so there is nothing left to mix
up by a caller and no stale-zero fallback to reason about.

**Measured over all 24 bearings: 17 clear, 7 with no legal slot** - bearings 135 through 225 in
15-degree steps, all on the left. The edge fixes the marker's X, so only Y is free to search: with
the safe content rect (x -565..565, y -335..322.5) and 64 units of edge padding the left anchor sits
at -501, and a 240-wide footprint from there spans the teammate list, whose y range together with
the weapon panel's covers the whole band. Miss the teammate list and the 39-unit slot at y 70..110
reappears and the bearing places with 2.00 units on each side - so a bearing has a slot exactly when
its footprint misses `Teammates` in x, which is what the test now asserts for all 24. It read as
four bearings until this pass, because the old raw-screen edge sat at -576, outside the teammate
panel; the safe-area edge is 75 units further in and the arc widens to meet it. The blocked set is
contiguous in the middle of the left arc and empty at both ends of it.

**This is the geometry, and it is reported rather than patched.** Raising the band ceiling past
`Teammates` needs exactly 39 units above y 180 - knife-edge again, and inside the transient feedback
strip and under the zone caption. Lowering the floor past `WeaponSelect` puts markers in the thumb
zone the bottom band exists to protect. Narrowing the footprint shrinks the window but never closes
it, because the anchor is pinned to the edge. **With the weapon panel down - any ordinary fight - all
24 bearings place**, the worst with 8.25 units to spare, so the residual needs the preparation panel
and a player facing away from the zone at the same time. It is pinned in EditMode, both as a count
and as the predicate behind it, so it cannot grow silently.

**The capture now renders it.** `PreparationWithTheObjectiveBehindThePlayer_CapturesTheEdgeMarker`
photographs two real behind-camera frames: `07b` with the objective straight behind, where the rule
finds the slot and the panel gate passes (neutering `AvoidPanels` fails that frame for real, with
the caption's footprint at y -181.02..-147.08 inside the weapon panel), and `07c` with it 45°
behind the left shoulder, where the marker anchors at (-501.00, -143.61) - the safe content edge
plus its padding - with no clearance and the teammate list inside its footprint. `07c` is held to
`AssertResidualFrame`, which demands the documented cause and rejects a knife-edge, so the residual
is visible in the evidence folder without being reported as a clean frame.

**One consequence to know:** the capture assertion is strict, so a capture that ever rendered one of
those four bearings would fail with no legitimate way to pass. That is the intended signal, but those
bearings must not be captured until the geometry changes.

Verified by neutering the rule to first-fit over a 1-unit grid, which put the edge marker at
y 91.20 keeping **0.80 units** from the preparation panel - the assertion fired with those numbers
while the drawn ink was still clear. Restored, the same candidate (0.00 clearance on arrival) places
at y 90.00 with **1.9987**. Pixel-read on the regenerated captures, the projected marker occupies
y 380..429 against a panel edge at y=435 on both `07` and `09` - five clear rows - and the edge-case
frame the same band at 380..430. Stat lines unchanged.

Also this pass: `ClearBox` dropped to private (no external users), and the 17-line `teammatePanel`
comment reduced to four lines pointing at this document.

### Closed in this pass

- **`MatchHUD`'s behind-camera branch read raw `Screen`, not `SafeAreaRoot`.** It now takes its edge
  from `SafeAreaRoot.ContentRectFor` and its direction from the camera's own axes, stays in
  canvas-local units, and names no `Screen` at all. The parity capture drives it: `07b` and `07c` are
  behind-camera frames rendered through the real branch, and the panel assertion runs on `07b` for
  real - it fails with numbers when `AvoidPanels` is neutered. The separate edge padding on the HUD
  (60 in code, 50 baked into `Main.unity`, against `HudMarkerLayer.EdgePaddingReference`'s 96) is
  deleted, so the edge the player sees is the edge the rules are measured against.

### Still open

- **The hit marker (`X`) is a transient HUD element still unmeasured.** It sits at the canvas centre
  and is deliberately excluded: it appears on every shot, so measuring it would make markers jump on
  each hit, which is worse than the overlap it would prevent. It needs its own decision — either its
  own band or an explicit accept — not a silent exception.
- **Seven edge bearings on the left have no legal placement during preparation** (135 through 225 in
  15-degree steps). Fully measured and bounded above, and now rendered: `07c` is the real frame. Not
  fixed, because every available fix costs something real — it needs a layout decision, not another
  rule. It grew from four when the edge moved onto `SafeAreaRoot`, which is the honest cost of that
  correction, and the count is pinned so it cannot move again unnoticed.
  Nothing about this is a test-only concern: a player facing away from the zone during preparation,
  on the left arc, sees the objective caption sitting on the weapon panel and the teammate list.
  The decision owed is whether the loadout screen's panels may yield, or whether the edge marker may
  travel inboard along the edge rather than being pinned to it.

## Implemented changes in this pass

- Shared menu controls now use the existing `UIKit`/`HotdropButton` stateful design system rather than independent flat buttons. Added panel cards, structured header/status surfaces and rounded input fields.
- Fixed four-edge border geometry: the frame now fills its parent and each rail sits on the correct edge; inset/weight changes keep left/right rails inside.
- Home uses the real equipped appearance, a more prominent Play action and persistent bottom navigation. Goal copy uses actual local training state, not fake account progress.
- First-run selection previews both actual rigs, changes selection without starting gameplay, and persists only on Continue or the explicit default/Decide Later path. Preview cameras/stages are isolated and disposed.
- Party/Profile presentation now groups actual backend values into cards without adding fake player data.
- Appearance retains local model/runner selection if inventory loading fails; it does not let cached client state grant ownership or authorize an equip.
- Settings are separated into five tabs using the existing saved settings/actions. The Slider-driven handle no longer extends over its neighboring rows; reopening/resetting refreshes the actual saved slider values and graphics note. Account links are disabled in a match; they do not bypass match exit flow.
- Added targeted border and offline-local Appearance/retry-cleanup tests, explicit Continue smoke assertions, settings-tab physical-size coverage, and an editor-only real-screen capture test. The capture test now also asserts that both Home Play actions are fully inside the viewport without scrolling at both ratios; this catches the observed 20:9 Offline-button clipping.

## Changed files attributable to this UI pass

All paths below are relative to `E:\Usama\Game`. The worktree already contains extensive other staged/uncommitted work; this list is not a claim that every dirty file was changed here.

- `ProjectHotdrop/Assets/Scripts/UI/UIKit.cs`
- `ProjectHotdrop/Assets/Scripts/UI/UiBorder.cs`
- `ProjectHotdrop/Assets/Scripts/UI/MenuLayout.cs`
- `ProjectHotdrop/Assets/Scripts/UI/HomeCharacterPreview.cs`
- `ProjectHotdrop/Assets/Scripts/Core/OfflineHomeScreen.cs`
- `ProjectHotdrop/Assets/Scripts/Core/OfflinePauseMenu.cs`
- `ProjectHotdrop/Assets/Scripts/Core/PlayerProductScreens.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/UIDesignFoundationTests.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/PlayerProductScreensNavigationTests.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/PlayMode/OfflineSliceSmokeTest.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/PlayMode/UiPhysicalSizeTests.cs`
- `ProjectHotdrop/Assets/Scripts/Tests/PlayMode/PlayerJourneyVisualEvidenceTests.cs` and Unity-generated `.meta`
- `ProjectHotdrop/ASSET_LICENSE_REGISTER.md`
- This report and generated captures in `ProjectHotdrop/UIEvidence/journey_2026-10-02/`

No backend contract, migration, ownership grant, billing behavior or combat balance changed in this pass. No new paid bundle/catalog item or third-party graphic was produced/imported. Existing original UI geometry and graybox model provenance are recorded in the asset register; production-model/cosmetic provenance remains outstanding.

## Validation

- Fresh Unity 6000.3.25f1 EditMode: **283 passed, 0 failed** (`ProjectHotdrop/TestResults/EditMode.xml`).
- Full PlayMode suite after the Home visibility/settings/retry fixes: **10 passed, 0 failed**, duration 132.3408828s, completed 2026-10-03 00:14:17 PKT. After the final narrow-tab caption adjustment, focused `PlayerJourneyVisualEvidenceTests;UiPhysicalSizeTests`: **3 passed, 0 failed**, duration 18.6952318s; fresh compilation and PNG regeneration completed. `TestResults/PlayMode.xml` contains this most recent focused run (the runner overwrites its previous XML/log), not a preserved XML of the preceding ten-test run.
- The PlayMode suite exercises the actual Main scene, explicit runner Continue, guided training, input, bots/pads, preparation restrictions, results/rematch cleanup, and simulated-phone physical minimums. It does not prove pixel-perfect matching or human usability.
- Generated PNGs are **Unity editor renders of actual locally reachable screens**, captured at 1920×1080 and 2400×1080 (400-dpi simulated layout), not Android screenshots, mocked backend responses or Play purchase evidence.
- Current capture set: 33 PNGs across three folders — `journey_2026-10-02/` (17: Startup at 16:9; Choice, Home, Controls, Graphics, Audio, Accessibility, Account/Support settings tab and Support at both ratios), `challenges_2026-10-03/` (16: eight offline-challenge states at both ratios), and `parity_2026-10-03/` (6: Training, Preparation, Pad feedback, Results, Appearance, Profile at 16:9). **13 of the 18 production reference screens now have a runtime capture.** Still uncaptured: 05 Party, 06 Matchmaking (needs a real allocated match), 12 Contracts and 13 Mastery (gated off), 18 Shop (Gate D, must stay disconnected). Final screenshots include explicit Home action-visibility and settings-tab caption-bounds assertions.
- No Android device was attached when `adb devices -l` was checked. Actual cutouts, gesture areas, thermals, GPU rendering, left-hand ergonomics and wider/larger-text device visual acceptance remain open.
- Unity initially hit sandbox licensing/UPM startup failures; refreshing Unity Hub licensing allowed successful compilation and test execution. Do not interpret that startup failure as a source-code test failure.
- The full-run log contains an Input System assertion on exiting editor Play Mode, after test results were saved (`InputManager.ApplySettings`, package source line 2997, `m_Settings != null`). It is outside the saved test failures, but the full log was **not completely clean**; test/editor teardown still needs investigation. No package code was modified or warning hidden to manufacture a clean result.

## Shop prerequisites and unchanged flags

Read-only acceptance/config check:

- `docs/acceptance/STEP_D.md`: external playtest open, Gate D go/kill decision pending; **not a pass**.
- No explicit owner authorization for `CatalogConfig.shopEnabled` found in acceptance records.
- Publisher contact/desired brand and final package ID are recorded, but `AndroidKeystoreName` and `AndroidKeyaliasName` are empty. These are not publisher-owned signing/Play Console evidence.
- `Assets/Resources/BillingMode.json` says `GooglePlay`; selecting a store name is not evidence of configured products, credentials, real purchase verification, acknowledgment or restore/refund/revocation approval.
- Exact produced/licensed bundle contents and approved full purchase-lifecycle evidence are absent.
- `CatalogConfig.asset`: `shopEnabled: 0`.
- Backend `PurchasesEnabled: false`; `AdConfig.asset`: `rewardedAdsEnabled: 0`.
- Contracts/mastery: no enabled feature asset; disabled by default. This pass did not turn them on.

**No purchase evidence exists from this UI pass.** Shop remains absent from Home. Sandbox/backend tests cannot substitute for Gate D, publisher approval, verified billing lifecycle and production release acceptance.

## Remaining work before claiming completion

1. The 2026-10-03 follow-up connects offline challenges to real gameplay and local views. Finish its **Android device** airplane-mode, process-loss, storage/recovery and all-twelve human-play acceptance; see `OFFLINE_TRAVEL_IMPLEMENTATION_2026-10-03.md` for current automated evidence and limitations.
2. Produce/license final same-skeleton runner models, the three weapons, pad, modular map art and every real cosmetic; keep remote Stitch artwork out of shipped assets.
3. Finish corrected per-screen composition and all loading/error/empty/context states, with current-build screenshots for every reachable screen; current capture coverage is explicitly limited.
4. Run real backend/Photon staging journeys and real-device UI acceptance across supported aspect ratios, cutouts, larger text, left-handed controls and Reduce Motion. Collect actual multiplayer/result/recovery evidence.
5. Resolve publisher legal address/identity, public policy/support/deletion URLs, Play Games/signing configuration and Gate D human gameplay validation. Keep monetization and gated rewards off until their independent evidence/authorizations pass.
6. Investigate the Input System editor-exit assertion noted above; keep automated pass counts separate from log cleanliness and device acceptance.

No percentage estimate or production-readiness claim is inferred from the automated pass counts.
