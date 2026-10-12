# In-match HUD — structure and ownership (3 Oct 2026)

## Follow-up — 2026-10-07

The saved 6 October PlayMode report (26/34) failed both preparation
objective-marker captures because the marker overlapped a measured panel. The
current source hides a marker when panel and ping exclusions leave no legal
slot; the preparation header still identifies the opening objective. The
behind-camera capture was moved to active gameplay. These changes compile, but
the PlayMode captures have not been rerun. The older `07b`/`07c` measurements
below are historical evidence, not current acceptance results.

Written after the pass that found the split-ownership defect. Later passes should build on this
rather than rediscover it.

## The problem this structure fixes

The in-match HUD had **two owners and no authority**:

- `SceneSetup` (editor-only; `BuildOfflineSlice.cs` runs it to bake `Assets/Scenes/Main.unity`)
  wrote each panel's colour into the scene file.
- `MatchHUD.Awake` rebuilt and restyled the live HUD at runtime.

Consequence: editing the generator changed nothing on screen and **no test failed**. Editing the
token, regenerating the scene, or neither — all looked like success. That silent no-op is the
defect; the fix is to give each concern exactly one owner and make the other caller *ask*.

## Owners

| Concern | Owner | Consumers |
|---|---|---|
| What a floating match panel looks like | `UIDesignTokens.HudGlass` → `HudAppearance` | `SceneSetup` (bake) and `MatchHUD` (runtime) |
| Where a world marker may be drawn | `HudMarkerLayer` | `PingSystem` and the `MatchHUD` objective marker |
| What a marker caption looks like against the arena | `HudAppearance.MarkerPlateColor()` | `PingSystem` and the `MatchHUD` objective marker |
| Score-bar wording | `MatchHUD.ScoreLines` (pure) | `MatchHUD.UpdateScores` + tests |

## Rules for future work

1. **Never name a HUD colour in `SceneSetup` or `MatchHUD`.** Call
   `HudAppearance.FloatingPanelColor()` / `ApplyFloatingPanels()`. The baked value is
   authoritative-but-not-load-bearing: `MatchHUD` re-applies the same rule every run, so a stale
   scene file self-corrects instead of silently disagreeing.
2. **Never clamp a world-projected marker locally.** Pings and the objective marker must go through
   `HudMarkerLayer`. The reserved bands (24% top / 26% bottom) were *measured* from the 1920x1080
   capture — score board plus zone caption reach ~22% of screen height. Changing chrome height means
   changing `HudMarkerLayer`, not the callers.
3. **Coordinate spaces are not interchangeable, and screen geometry has one owner.**
   `ProjectToCanvas`/`ClampToReadableBand` work in canvas-local units. `ClampToEdgeBand` works in
   whatever space the rect it is handed is in, and the behind-camera objective marker now hands it
   `SafeAreaRoot.ContentRectFor(canvasRect.rect)` — canvas-local units, like every other marker.
   Nothing in `MatchHUD` reads `Screen.safeArea`, `Screen.width` or `Screen.height`; screen size,
   density and OS intrusions are read in exactly one place, `SafeAreaRoot.CurrentInsets`. A second
   copy of "where the screen is" is what made that branch wrong on a notched device and untestable.
   `Hotdrop.Camera` is a project namespace, so `Camera` must be written `UnityEngine.Camera` in UI files.
4. **A modal is not a floating panel.** Death / preparation / results / countdown stay opaque and
   must never be added to `HudAppearance.FloatingPanels`.
5. **A marker caption gets the shared plate.** Call `HudAppearance.MarkerPlateColor()`; neither
   marker may name its own alpha. A world-anchored label lands on arbitrary arena pixels, and the
   capture showed gold text unreadable on a bright floor.

## Score bar

`ScoreLines` returns `(Left, Right)` with one convention both sides follow: **name outside, score
adjacent to the clock**, and the local team explicitly marked (`TEAM A · YOU  12` /
`0  TEAM B`). This replaces `ALLY 0` / `0 ENEMY`, which flipped word order between sides and, in a
3v3, called the team you fight *alongside* "enemy".

## Marker vs marker: settled 3 Oct (see below)

`HudMarkerLayer.SeparateFromPlaced` is the rule for this. `MarkerCaptionSize` is the shared caption
footprint both marker kinds lay out to; `MarkerRowHeight` (44) is the vertical pitch held between
them. It sweeps row by row down then up and takes the first free row. It must **not** be rewritten
as "nudge one step and re-test" — that oscillates between two occupied rows and never escapes; the
cluster test in `HudOwnershipTests` caught exactly that during this pass.

## Behind-camera markers had no clearance at all (settled 4 Oct)

`MatchHUD.UpdateObjectiveMarker` had two branches. The projected one clamped into the readable
band and then called `AvoidPanels`; the behind-camera one clamped to a screen edge, converted to
canvas-local, and **stopped**. No panels measured, no margin required. `ProjectToCanvas` returns
false whenever the objective leaves the frustum, which in a six-player match is most of the time,
so the state with no protection was the common one, not the corner case.

The placement call is now **outside the branch**. Both paths build a candidate — one clamps into
the readable band, the other clamps to the safe-area edge — and both then go through the same
`ActivePanelRects` measurement and the same `HudMarkerLayer.AvoidPanels` call with the same
`MarkerClearance`. There is exactly one placement call site in the HUD.

`HudMarkerLayer.OffScreenCandidate` used to be the behind-camera counterpart to `ProjectToCanvas`, and
it is **gone**. It existed to mirror the objective, clamp it to the screen edge and convert the
result back into canvas units — three steps that had to agree about coordinate space, which is
exactly what a second screen-space copy of the geometry produced. It also returned a stale zero on
a failed conversion, so a marker could be drawn at the canvas centre for no stated reason. The
behind-camera branch now needs none of that: it takes the direction from the camera's own axes
(`mainCamera.transform.InverseTransformPoint`), takes the edge from `SafeAreaRoot`, and stays in
canvas-local units throughout. `ProjectionRefusesToInventAPositionBehindTheCamera` still pins
`ProjectToCanvas`'s contract, and the capture now measures the real branch end to end.

### Measured over all 24 bearings, and the one place it runs out

`BehindCamera_TheEdgeMarkerGetsTheSameMeasuredClearanceAsTheProjectedOne` sweeps every bearing a
player can lose the objective in, against the measured preparation geometry: 17 of 24 reach the
required clearance, **7 have no legal slot**.

**The 7 are bearings 135 through 225 in 15-degree steps — every one of them on the left**, and the
arithmetic is worth keeping. The edge fixes the marker's X, so only Y is free to search. Clamped
against the safe content rect (x −565…565, y −335…322.5 at the parity screen, from the 75/25/37.5
unit dp reserves) with 64 units of edge padding, a straight-left bearing anchors at x −501 and the
diagonals at −458.56 and −264.75. A 240-wide footprint therefore spans `Teammates` (x −590…−370) at
every one of those anchors, and `Teammates` (y 20…180) together with `WeaponSelect` (y −190…70)
covers the whole band y −164.05…164.70 — no free span to aim at and no swept row that clears. Miss
the teammate list and the free span y 70…110 reappears, exactly 39 units, and the bearing places at
y 90 with 2.00 units on each side. That is the whole rule: **blocked exactly when the footprint
spans `Teammates` in x**, which the test asserts for all 24 bearings rather than restating a list.

**It used to be four bearings (135, 150, 210, 225), and the routing change is why.** The old edge
was built from raw `Screen.safeArea` and ignored the 48 dp edge margin, which put the left anchor at
x −576 — outside `Teammates` entirely. Reading the same edge through `SafeAreaRoot` moves it to
−501, inside the overlap window, and the arc widens at both ends to meet it. The edge is now the one
the rest of the HUD is laid out from, edge margin and cutout included, so seven is the honest number.
The finding itself is unchanged: it is the same no-slot residual, on a slightly wider arc, and it is
now reachable in ordinary play — see the captures below.

**No band or footprint change fixes it without costing something real.** Raising the ceiling past
`Teammates` needs exactly 39 units above y 180 — knife-edge again, and it lands inside the transient
feedback strip and under the zone caption. Lowering the floor past `WeaponSelect` puts markers in
the thumb zone that `BottomReservedFraction` exists to protect. Narrowing the footprint shrinks the
window but never closes it, because the anchor is pinned to the edge. So it is measured and bounded
rather than papered over with a special case.

**The residual is preparation-only.** Take the weapon panel down — what any ordinary fight looks
like — and all 24 bearings place, the worst of them with 8.25 units to spare. It needs the
weapon-select panel specifically *and* a player facing away from the zone while it is up.
`BehindCamera_TheOnlyBearingsWithNoSlotAreLeftAndOnlyWhileTheWeaponPanelIsUp`
asserts both halves: that the fight case clears at every bearing, that the blocked set is exactly
seven, that it stays on the left, and that it is exactly the set of bearings whose footprint spans
the teammate list.

### Proved by neutering, and what the pixels say

`AvoidPanels` was temporarily replaced with first-fit over a 1-unit grid. Driving the behind-camera
placement through it produced:

> Marker clearance is knife-edge at 1920x1080. 'ObjectiveMarker:CENTER 34m' reserved footprint
> (x:-290.90, y:73.20, width:240.00, height:36.00) keeps only **0.80 units** from panel 1
> (x:-310.00, y:110.00, width:620.00, height:80.00), and the placement rule requires 1.5.

Restored, the same edge candidate — raw (−170.90, 187.20) at 0.00 clearance — places at
(−170.90, 90.00) with **1.9987**. Pixel-read, the projected marker occupies y 380…429 against a panel
edge at y=435 on both `07` and `09`, five clear rows, and the edge-case frame the same band at
380…430. Stat lines unchanged: 683–703 / 715–735, 683–702 / 716–735, and 667–686 / 700–719 /
732–751 for the pellet weapon.

**Closed: the edge branch had a second copy of the screen, and it is gone.** `MatchHUD`'s edge branch
read raw `Screen.safeArea` and `Screen.width`/`Screen.height`, unlike every other HUD element, which
goes through `SafeAreaRoot` and its `SimulatedScreen`. Two things followed from that. The harness
**could not drive the branch**: the capture pins the simulated screen but does not simulate
`Screen`, and in a capture run `Screen` is 640×480 while the frame is rendered at 1920×1080 — so
turning the camera off the objective put the marker at local (−56652, 405889), thousands of units
off-canvas, where a clearance check passes for the wrong reason. The earlier edge proof was a
synthetic frame built in canvas-local units, i.e. a probe. And on a notched device the marker was
laid out against a screen nobody else in the HUD was laid out against.

Both are fixed at the source rather than in the harness:

- `SafeAreaRoot.CurrentInsets(edgeMarginDp, gestureInsetDp, topCutoutDp)` is now the one place screen
  size, density and OS intrusions are read, and `SafeAreaRoot.ContentRectFor(canvasRect.rect)` turns
  that into the safe content rect in the canvas's own centred units. `Current()` delegates to it, so
  a layout pass and the marker cannot disagree.
- The edge branch takes its direction from `mainCamera.transform.InverseTransformPoint(world)` and
  its edge from `ContentRectFor`, and stays in canvas-local units. No `Screen` is named in
  `MatchHUD` at all now.
- **A third source turned up while doing this:** the edge padding was a serialized field of the HUD
  (`markerScreenEdgePadding`, 60 in code and **50 baked into `Main.unity`**, so the scene file won)
  while `HudMarkerLayer.EdgePaddingReference` is 96. Same quantity, two values, and the rules were
  being measured against a number the player never saw. The field is deleted; `EdgePaddingReference`
  is the only copy, and the real frame now lands where the sweep predicts.

**What the real capture shows.** `PreparationWithTheObjectiveBehindThePlayer_CapturesTheEdgeMarker`
plays a real offline match, waits for preparation, and photographs two states with the objective
behind the player, both through `MatchHUD`'s own branch:

- `07b_preparation_objective_behind` — objective straight behind. The edge branch places the marker
  and the full panel gate passes, so it clears every published panel by at least 1.49 units.
  Neutering `AvoidPanels` fails **this same frame** for real: the caption's reserved footprint sits at
  y −181.02…−147.08 inside `WeaponSelect` (y −190…70). Restored, it passes. That is the teeth.
- `07c_preparation_objective_behind_left` — objective 45° behind the left shoulder, which is the
  residual arc. The marker anchors at **(−501.00, −143.61)**: the safe content edge x −565 plus the
  64 units of padding, i.e. the geometry now demonstrably comes from `SafeAreaRoot`. It has zero
  clearance to its nearest panel and its footprint spans the teammate list, so it is held to
  `AssertResidualFrame`, which demands both — and fails a knife-edge, so it cannot go quietly green.

So the two proofs are complementary, not duplicate: the EditMode sweep is the rule across all
24 bearings with no camera, and the capture is one served frame plus one residual frame rendered by
the real HUD, canvas and harness. `OffScreenCandidate`'s contract test is deleted — the branch it
guarded no longer exists.

Two things this does **not** claim: the captures are layout evidence from the editor, not Android
device acceptance, and pixel-level reading of the caption in these two frames was inconclusive —
the HUD is translucent glass over a lit arena and a luma threshold could not isolate the glyphs from
static HUD content, so every number above comes from rects measured on the frame that was rendered.

## Marker vs panel: the clearance is now explicit (settled 4 Oct)

Non-overlap is not the contract. The objective marker cleared the weapon panel by **1.03 canvas units
— 1.5 px at 1080p** — which every "do they touch" check reports as good and which the next small
change to the marker, the card, the band or the panel would erase. `HudMarkerLayer.MarkerClearance`
is now an explicit required clearance, enforced in one place.

- **In the rule, not the footprint or the band.** A bigger footprint or wider band buys clearance by
  taking room from the marker's glyphs or from the space markers are placed in. The margin is a
  separate requirement, so it costs layout nothing and reaches every panel and every other marker
  through the single `Blocked(...)` predicate. Nothing here names a screen.
- **Largest clearance wins, not first fit.** `AvoidPanels` scores every candidate with `ClearanceAt`
  and accepts only once it reaches the margin. First fit spent the slot's whole 6-unit slack on one
  side and left 1.03 on the other.
- **The gap centre, not its nearest edge.** `Mathf.Clamp(preferred, lo+half, hi-half)` snaps to the
  closer edge; since the slot is 40.0008 units against a 36-unit footprint, every legal position is
  within half a unit of the margin. Clamping landed the marker *exactly* on the required margin —
  a rule that guarantees the minimum is still guaranteeing a knife edge.
- **Panels are sorted before the span scan.** The scan keeps a running cursor and only raises it,
  which is correct for ascending intervals and silently wrong otherwise. `ActivePanelRects` publishes
  in field order, so the Teammates panel (y 20…180) arriving after the preparation plate (110…190)
  was skipped as "already covered" and the only gap on the preparation screen never became a
  candidate. That silently disabled the rule and left the marker on the weapon panel.

### `MarkerClearance = 1.5`, and why not 2

A slot must hold `MarkerFootprintSize.y + 2C`. The only free slot is 40 units and the footprint is
36, so `(40 − 36) / 2 = 2` **exactly** — and 2 was tried and failed on the live frame. Panel rects
are laid out in floats; the real slot measures **39.9993**, so `36 + 2*2` exceeds it by four
thousandths of a unit. No candidate reaches the margin, the rule falls back to the original
placement, and the marker is left on the weapon panel. **A requirement that consumes its own slot
fails silently by disabling the rule rather than by refusing**, which is worse than a visible
failure. 1.5 needs 36.94 and leaves ~3 units that float rounding cannot consume; because the slot is
only 40 tall, a marker placed in its middle ends up ~2.0 units from each panel, double first fit.

`TheClearanceLeavesSlackInTheSlotRatherThanConsumingIt` asserts `footprint + 2C ≤ gap − 1`, so the
bound cannot be quietly re-tightened to the arithmetic maximum, and asserts `C > 1.03` so it cannot
be lowered until it stops doing any work. `TheMeasuredPreparationSlotIsNotSpentToZeroSlack` asserts
the outcome on the same measured geometry: `ClearanceAt(result) ≥ MarkerClearance`, and that the
result is the slot's middle rather than merely any legal position.

The capture assertion measures the marker's **declared footprint** (its root rect is built at
`MarkerFootprintSize` and centred on the anchor, so `CanvasLocalRect` recovers exactly what the rule
placed) rather than its ink — the footprint is a deliberate superset of the drawn glyphs, the
diamond's 45° box of 24 × √2 = 33.94 fitting inside the declared 36, so a margin measured on it is
a margin the ink also has. `RectGap` is public so the assertion and the rule share one definition of
"gap" and cannot disagree. Neutered to first fit over a 1-unit grid, it fails with:

> reserved footprint (x:-120.00, y:71.00, width:240.00, height:36.00) keeps only **1.00 units** from
> panel 0 (x:-450.00, y:-190.00, width:900.00, height:260.00), and the placement rule requires 1.5.
> Drawn ink (x:-116.97, y:72.03, width:236.97, height:33.94) is clear.

Restored, the frame measures **1.9987**. In pixels the marker glyph occupies y 380–429 against a
panel edge at y=435 on both `07` and `09` — five clear rows.

## Resolved: the score-bar overlap (was open, now closed with evidence)

The 3 Oct finding *"world markers still overlap the score bar in the capture harness"* is **closed**.
It was a harness artefact, but it hid a real gap behind it, so both are recorded here.

**It was a harness artefact.** Markers were clamped in `LateUpdate` against the canvas rect of that
moment; the capture harness then re-scaled the canvas and switched it to `ScreenSpaceCamera` and
rendered without another frame boundary. Instrumented in `PlayerJourneyVisualEvidenceTests`, the
failing frame reported `markerLocal.y = 292.35` against a ceiling of `360 - 720*0.24 = 187.2` —
markers carrying coordinates that were valid for the *old*, larger canvas. `HudMarkerLayer` was never
wrong. `MatchHUD.RefreshHudMarkers()` now re-places every marker on demand, which the harness calls
after mutating the canvas; on a device the next `LateUpdate` already did this.

**What the artefact was hiding.** With the capture finally honest, the same frame showed a second,
genuine collision: `ENEMY HERE 52m` and `CENTER 34m` printed through one another, because the
objective (34 m out) and an enemy ping (52 m out) projected ~20 px apart. The reserved bands stop a
marker landing on HUD *chrome*; nothing stopped two markers landing on each other. That is fixed by
`SeparateFromPlaced`, and the objective caption gained the same translucent plate a ping caption has
(it was gold text straight onto a bright greybox floor).

**Both are asserted on the real frame**, in the same frame the capture is taken, after the canvas
mutation — so a harness artefact cannot masquerade as a runtime defect and vice versa:

- `AssertNoMarkerOverlapsHud` — objective marker vs the score bar and zone caption.
- `AssertNoMarkerOverlapsMarker` — every pair of visible marker *captions*.
- `AssertNoMarkerOverlapsPanel` — every visible marker *caption* vs every HUD panel on screen, using
  `MatchHUD.ActivePanelRects()`. Closed the preparation-screen defect where the objective caption
  printed across the weapon-select cards.

All three have teeth, verified by neutering the owning rule and re-running. `SeparateFromPlaced`
neutered fails with `'Ping:ENEMY HERE 53m' ... intersects 'Ping:ENEMY HERE 52m'`; `AvoidPanels`
neutered fails with `'ObjectiveMarker:CENTER 34m' (x:-100.00, y:-39.00, width:200.00, height:30.00)
intersects panel 0 (x:-450.00, y:-190.00, width:900.00, height:260.00)`. Note the first of my own
teeth probes was written with a condition that never fired and "passed" without testing anything, and
`AssertNoMarkerOverlapsPanel`'s first draft compared world rects against canvas-local rects so it
could never fire at all — if a probe passes, confirm it can fail before believing it.

### Panels are an input to placement, not a special case

The reserved bands are two fixed fractions measured from the gameplay HUD, so they cannot describe
panels that only appear in one phase. `MatchHUD.ActivePanelRects()` measures whatever is on screen
from the panels' real rects and hands it to `HudMarkerLayer.AvoidPanels`; no caller names a screen
and no rule knows what a "preparation panel" is.

Four things the first attempts got wrong, all found by the capture rather than by reading the code:

- The caption hung **below** its anchor, so testing a box centred on the anchor measured above the
  text. Footprints are now measured from the parts that actually draw.
- `AvoidPanels` and `SeparateFromPlaced` ran as separate passes and **undid each other** — the first
  lifted the marker off the panel, the second put it straight back. Both now test one combined
  `Blocked(...)` predicate, so a row is free only if it is clear of panels *and* markers.
- A row sweep steps by `MarkerRowHeight`, which **cannot reach every free slot**. On the preparation
  screen the readable band is y −172.8…187.2, `WeaponSelect` covers −190…70 and `Preparation` covers
  110…190, leaving only y 70…110 — a 40-unit gap sitting between two sampled rows. `CandidateYs` now
  offers the **centre of each free vertical span** as well as the swept rows.
- **The protected box was the caption, not the marker.** The capture passed its assertion while the
  diamond still printed across `DAMAGE BLOCKED`, because the caption cleared and the icon did not.
  `MarkerFootprintSize` is now the protected box, and both marker kinds are laid out inside it.

**The marker's layout had to change, because no placement rule could have fixed it.** Icon above
caption made the objective 74 units tall and a ping 60, against a 40-unit gap. Laying icon and
caption on one row makes both 36. A rotated icon's axis-aligned box is `size × √2`, so the first
one-row attempt still measured **48.09** tall and failed; icons are now sized against that diagonal,
and `TheProtectedFootprintIsTheWholeMarkerNotJustItsCaption` pins the declared footprint to the
measured gap so a restack cannot reintroduce it silently.

The lesson worth keeping: **the assertion must measure the drawn result, not the declared
constant.** Both of my first attempts here protected or checked a box that the marker did not fill,
and both reported a clean frame while a real overlap was on screen.

### Readable text is not a panel, and panels are not the only thing on screen

`ActivePanelRects()` publishes what markers must avoid. For a long time that was panels only, which
left a hole of the same kind: the HUD's transient feedback line is readable, on screen, and in no
panel's rect. On screen 09 its ink measured y 97.71…122.29 — inside the 40-unit gap markers occupy
— so the frame was clear by about three pixels of luck.

It is now measured through the same mechanism: `AppendTextInkRect` publishes the **ink**, not the
container. That distinction is the whole fix on the measurement side — the element is authored in a
700×140 box to hold a single 24.6-unit line, and taking the box would report 140 units of occupied
screen for 25 units of text. Falls back to the container when the mesh is not generated, so the
measurement is never silently empty.

**Measuring it exposed a second problem the measurement could not solve on its own.** With the ink
published the only free space in the band becomes 27.7 units against a 33.94-tall marker, so no
placement cleared both and the rule kept its fallback — the marker back on the weapon panel. That is
a real conflict between two readable things, and the options are only: shrink one, move one, or give
them separate bands. Shrinking the marker would cost text size; giving the feedback its own band
(`CombatFeedbackBandY`, in the strip between the marker band ceiling and the zone caption) costs
nothing a player reads. `TheToastBandClearsTheMarkerBand` pins that invariant.

Two neutering probes, because the fix has two halves and each was verified separately: undoing the
band change fails with the marker on the weapon panel (proving the geometry forces the change), and
undoing the measurement fails with the original defect caught by name
(`footprint (x:-116.97, y:71.03, …) intersects 'CAN'T PLACE PAD — NOT DURING THIS PHASE' ink
(x:-249.93, y:97.71, …)`).

**Known exclusion:** the hit marker (`X`) is a transient HUD element at the canvas centre that is
deliberately not measured, because it appears on every shot and measuring it would make markers jump
on each hit. It is written down as an accepted exception rather than left to be rediscovered.

### Still open (not a defect, a limit of this environment)

Markers sweep rows within the readable band and **keep their original placement if the band has no
free row** — a caption pushed into the score bar to cure a smear trades a legibility bug for a worse
one. With 9 markers inside one pitch-sized neighbourhood the smear is still possible; that is
accepted, not solved. (Panel avoidance additionally falls back to the nearest measured free gap, but
marker-vs-marker still has no such fallback.)
