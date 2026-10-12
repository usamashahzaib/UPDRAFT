# HOTDROP reference-parity pass — 2026-10-04

Follow-up to `UIUX_PARITY_2026-10-02.md`. This pass built a measurement harness, re-captured the
screens that have runtime evidence, and fixed what the measurements showed. It starts at Screen 01
and works upward, as asked.

Everything below is measured on files in the repository. Captures live in
`ProjectHotdrop/UIEvidence/`, comparison pages and the tools that produce them in
`ProjectHotdrop/UIEvidence/compare/`.

## What the reference images actually are — measured, not assumed

| property | reference (`UIUX/**/screen.png`) | Unity capture |
|---|---|---|
| size | 1600×1280 (5:4) | 1920×1080 (16:9) and 2400×1080 (20:9) |
| origin | Stitch HTML comps rendered in a browser | real game frames via `PlayerJourneyVisualEvidenceTests` |
| safe insets | **zero** — content is flush to the frame edge (top-left dot starts at x=0) | 2% lateral margin (`UIDesignTokens.EdgeMarginDp`) |

Two consequences that govern the whole parity question:

1. **The comps are not a phone frame.** `env(safe-area-inset-*)` resolves to `0px` in the renderer,
   so the reference draws flush to the bezel, which no device can do under a cutout or gesture area.
   The game keeps its margin on purpose. This is a deliberate, documented difference of ~2% per side.
2. **The reference's type is proportionally about half the smallest text this project is allowed to
   draw.** Measured on Screen 01, as a fraction of frame height:

   | element | reference ink | capture before | capture now | comment |
   |---|---|---|---|---|
   | brand wordmark | 2.34% | 3.24% | 2.87% | width now matches exactly (12.7% of frame width both) |
   | "PREPARING THE ROOFTOP" | 0.94% | 1.85% | 1.85% | at the 12 dp floor |
   | route motif block | 6.02% | 9.81% | 9.17% | flattened arc |

   The floor is `UiMetrics.MinTextDp = 12` dp on a 720-unit-tall canvas ≈ 2.6% of screen height. The
   reference's heading is 13 px on a 1280-tall comp = 1.0%, which on a 1080-tall phone is ~4.4 dp —
   half the minimum and not readable. **Text at exactly the reference's size is therefore impossible
   without breaking the project's own legibility gate.** The composition, copy, colour, order and
   horizontal extents can and do match; the type is one step larger and the vertical rhythm follows.

## Harness

- `compare/build_compare.py` — pairs each reference `screen.png` with its runtime capture and writes
  one inline page per screen (the Preview tab serves a single file, so images are data URIs).
- `compare/parity.py` — measurement: `montage`, `crop` (both sides scaled to equal **height**),
  `blocks` (drawn-element boxes as % of frame), `probe` (ink box of one named element in both),
  `rows`, `colors`.
- `compare/board.py` — `compare/board.html`, every screen as [reference | capture] at equal height.
- `compare/view.py` — inline one or more PNGs into `compare/viewer.html` for inspection.

## Screen 01 — startup / connection

Fixed in `Core/StartupConnectionScreen.cs`:

- wordmark 34 → 30 units; it now occupies 12.71% of frame width, the reference wordmark 12.70%;
- heading tracking 3 → 1.2, which brings the line from 22.45% to 21.93% of frame width (reference
  18.25%; the rest is the floor);
- route-motif arc flattened: the block was 9.81% of frame height, now 9.17%, reference 6.02%;
- footer chrome added: the centred gesture-zone pill and the bottom-right version chip
  (`BUILD v1.0`, from `Application.version`, not the comp's placeholder);
- top-right annotation now reads the composed frame (`SafeAreaRoot.ScreenSize`) instead of
  `Screen.width`. In a capture those differ — the first harness run printed "640×480" on a 1920-wide
  frame, which is exactly the label a reviewer would have caught. Two further rounds were needed to
  make it true: the label was first built from the frame before the harness pinned it, then the
  harness's refresh looked for the component under the canvas while the screen lives on the
  bootstrap object that owns the canvas. `RefreshFrameLabel()` is public and the exporter calls it
  after pinning the frame — a synchronous exporter renders in the same frame it pins the size, so
  no `Update` runs in between;
- caption copy is the reference's "Synchronizing Relay protocol nodes…" for the normal state;
- backdrop grid 5% → 7% cyan, matching the comp's overlay.

**Evidence defect found and fixed.** The first re-capture of Screen 01 was not Screen 01: it was the
runner choice, filed as `01_startup`. The screen advances itself after 1.1 s of unscaled time, and in
a batch-mode PlayMode run the scene load plus test setup can outlast that, so the capture
photographed whatever the game had already moved on to. `StartupConnectionScreen.HoldForCapture` now
holds the frame (and re-shows it, raising it above the screen the auto-advance opened) and the
evidence test applies the hold in `[UnitySetUp]`. The capture is deterministic; player behaviour is
unchanged.

## Screen 02 — first-launch appearance choice

Reference copy recovered from the comp: `CHOOSE YOUR LOOK` + `STARTER OUTFIT`, "Appearance only. Same
movement, abilities, and hitbox.", `BODY PRESENTATION 01/02`, `SELECTED` / `TAP TO SELECT`,
`DRAG TO ROTATE`, `ARC Rifle (Equipped)`, `Standard Hitbox Footprint`, "You can freely change your
appearance anytime from Appearance.", `DECIDE LATER` / `CONTINUE`.

Fixed in `Core/OfflineHomeScreen.cs`: the `STARTER OUTFIT` chip on first run; card footer copy; the
status line copy. The two card facts stack on two lines instead of sitting side by side — at the
12 dp floor the two strings are wider than the card, and a single row collided them
(`ARC RIFLE (EQUIPPED)STANDARD HITBOX FOOTPRINT` was visible in the capture).

Not copied: the comp's `FULL BODY / FACE` toggle and its `360` badge. The game has no face/body
split and no turntable, so those controls would do nothing; they are omitted rather than faked.

## Screen 03 — home

Fixed in `Core/OfflineHomeScreen.cs` and `UI/UIKit.cs`:

- **the settings icon was a square.** `UiGlyph.Settings` drew a ring out of four bars aligned to the
  square's edges, which at button size reads as an empty box — visible in the capture next to the
  player name. It now draws a cog: a hub disc plus eight radial teeth. Two intermediate versions
  were rejected on the rendered frame — a wide-tooth cog merged into a solid octagon, and long thin
  teeth at a wider radius read as an asterisk — so the teeth are short wide stubs on the hub's edge.
- **a missing-glyph box on the goal card.** `⚑` (U+2691) is not in the UI font; TMP logged it 15
  times and drew `□`. The label uses `●`, the same marker as the RELAY chip.
- bottom navigation is the reference's centred floating pill rather than a full-width bar, so the
  arena stays visible either side of it;
- offline detail copy and its two-line fit; `•` separators in the play detail.

Not copied: the comp's right-hand card is an `ACTIVE CONTRACT / SEASON 1 / HOLD THE RELAY` panel with
a reward claim. Home keeps the real local training goal, which is the same hierarchy with true data.

## Verified on the final capture set (15:52 run, PlayMode 1/1 passed)

- Screen 01 top-right reads **`ANDROID LANDSCAPE • 1920×1080`** on the 1920 frame — the label now
  follows the composed frame, checked by cropping the rendered PNG rather than by reading the code.
- Screen 02 card footers render as two clean lines (`ARC RIFLE (EQUIPPED)` /
  `STANDARD HITBOX FOOTPRINT`) with the `DRAG TO ROTATE` hint above and no collision.
- Screen 01 wordmark occupies 12.71% of frame width against the reference's 12.70%.
- Screen 03 navigation is the centred, bottom-hugging pill; checked at 16:9 and 20:9.

## Validation

- `Run-UnityTests.ps1 -Platform EditMode`: **374 passed, 0 failed** (3.81 s, run after the last
  source edit). The suite compiles the whole project, so it also confirms the `UIKit` glyph change
  (the four-bar `Ring` and `AddRingBar` were deleted once `Gear` replaced them) and the new
  `SafeAreaRoot.ScreenSize`.
- `Run-UnityTests.ps1 -Platform PlayMode -testFilter ...FirstRunHomeSettingsAndSupport_...`:
  **1 passed, 0 failed**, which regenerated the Screen 01/02/03/10/17 captures. No source file was
  modified after that run, so the captures on disk match the code that produced them.
- Captures are Unity editor renders at 1920×1080 and 2400×1080, not Android device screenshots.
  Airplane mode, cutouts, gesture areas, thermals and left-hand ergonomics are still unverified on a
  device, and are reported as such.

**Known-imperfect, not fixed:** at 3x zoom the cog still reads as a rounded rosette rather than a
crisp gear — the hub and the eight stubs dissolve into one silhouette at ~37 px. It is no longer the
unrecognisable square it was, but a real gear needs a stroked ring (a sprite with an inner hole)
rather than overlapping bars. Next pass.

## Method note: what a fix has to survive here

Every change in this pass went through the same loop — change, re-capture from the real game,
re-measure the frame, and only then call it done. Three of the fixes above passed a first capture and
were still wrong on the second (the frame label twice, the cog twice). A capture that has not been
opened and read is not evidence.

## Decision taken (owner, 2026-10-04)

For screens 07-18 the reference's **layout, copy, colour and information hierarchy are to be matched,
but the invented values are not to be shipped**: no `SEALS 48,250`, no `K-CORE 1,420`, no `LVL 14`,
no Pass/Leaderboard navigation, no fabricated telemetry. Screens 12, 13 and 18 stay disabled until
their gates pass, so their comps are references for composition only. This settles the conflict
noted above: parity means the same screen with true data, not a fabricated one that matches the
mockup's numbers.

## Screen 04 / 07 / 08 / 09 — the touch controls were not in the evidence at all

The largest parity defect in the project, found by measuring screen 04 rather than looking at it.

**What was wrong.** The entire lower control cluster — movement joystick, FIRE, JUMP/LAUNCH, PAD,
RELOAD, PING, PAUSE, the centre crosshair and the pad-placement caption — was drawn from
`TouchInputManager.OnGUI` with immediate-mode `GUI.DrawTexture`/`GUI.Label`. Immediate-mode drawing
happens in the game view's own pass, so it never appears in a frame captured by rendering the HUD
canvas into a RenderTexture, which is how `PlayerJourneyVisualEvidenceTests.Capture` produces every
screenshot in `UIEvidence/`. Measured on the screen 04 capture before the fix: **zero** control
discs in the lower half, against four in the reference. Screens 04, 07, 08, 09 and the 14 gameplay
frames had all been "compared" against references whose dominant element was simply missing from the
capture. It was also a standing-rules violation: the brief asks for TextMeshPro + Unity UI only.

**What changed.** New `UI/TouchControlsView.cs` draws the controls as real uGUI (disc + ring sprite +
TMP caption), and `TouchInputManager.OnGUI`, `DrawButton` and `DrawPadPreviewCaption` are deleted.
Input is untouched: every rectangle still comes from `TouchInputManager` and every touch is still
routed by `ProcessTouch` against those same rects. Nothing in the view hit-tests (`raycastTarget`
false throughout), so moving the pixels onto a canvas cannot change what a tap does.

Fixing it exposed **three further faults of the same shape**, each caught only because the pixels
were checked afterwards rather than trusting the passing test:

| fault | symptom measured | fix |
|---|---|---|
| geometry laid out from `Screen.safeArea`, which is 640×480 in batch mode | controls occupying one eighth of a 1920-wide frame | `ComposedSafeRect()` reads `SafeAreaRoot.ScreenSize`; used by the button layout, the editor strip and `KeepOnScreen` |
| placement converted screen points through `RectTransformUtility` | same, one step later | `Place()` maps a rect's *share of the composed frame* onto the canvas rect — no `Screen`, no branch |
| `dpiScale` read `Screen.dpi` (96) while the frame claims 400 dpi | PAD disc 70 px where a 400 dpi frame needs 175 px; captions could not fit at the 12 dp floor | `ComposedDpiScale()` reads the composed frame's dpi |

**And the same lesson as the frame label, twice.** A synchronous exporter renders in the frame it
pins the size, so no `Update`/`LateUpdate` runs in between. `RefreshForComposedFrame()` is public and
the exporter calls it — and it must be called *after* the canvas is switched to camera mode and laid
out. Called earlier it measured a canvas with no rect, sized every control to zero, and the
controls disappeared from the capture entirely: a passing test and a screen with no controls on it.

**Verified on the rendered capture, not on the code.** RELOAD measures **x 90.6–93.8 %** of a
1920-wide frame against its computed rect of 1740–1800 px (90.63–93.75 %) — exact. The PAD disc
measures **175×175 px**, which is 70 dp × 2.5 dpi as intended. Captions no longer wrap: the old
immediate-mode path shrank its caption to the disc's inner 80 %, and `SetCaption` reproduces that,
floored at the project's ~12 dp gate.

## Validation

- `Run-UnityTests.ps1 -Platform EditMode`: **374 passed, 0 failed**, run after the last source edit.
- `Run-UnityTests.ps1 -Platform PlayMode -Filter ...TrainingAppearanceAndProfile_...`: **1 passed,
  0 failed**, which regenerated the screen 04 capture the numbers above were measured from.
- Two temporary diagnostic tests were written to read the real rects and canvas mapping, used once,
  and deleted; neither is in the 374.

## Still open

- Screens 07, 09, 10, 11, 14, 15, 16, 17 still need the element-by-element measurement and fix that
  screen 04 has now had. Screens 07, 09, 14 and the gameplay frames inherit the touch-control fix and
  should be re-captured before they are judged.
- The controls are sized from the game's own dp values at the frame's dpi (an 80 dp FIRE button on a
  400 dpi frame), which is larger relative to the frame than the comps' fixed-px buttons. That is the
  project's chosen touch ergonomics, not a styling error, and it has not been retuned to the comps.
- Screens 05, 06, 12, 13 and 18 have no runtime capture, so they have never been compared. 05 Party
  is reachable; 06 needs a real allocated match; 12/13 are gated off; 18 must stay disconnected.
- Screens 04, 07, 08, 09, 10, 11, 14, 15, 16, 17 have captures and have been compared by eye in this
  pass but not yet measured and fixed element by element; 05, 06, 12, 13 and 18 have no runtime
  capture at all, so they have still never been compared.
- The reference comps for 07–17 carry invented telemetry, currencies, ranks and navigation
  (Telemetry/Operatives/Sector Map tabs, `SEALS 48,250`, `K-CORE 1,420`, `LVL 14`, `PASS`,
  `Leaderboard`, military branding). `docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md` forbids
  copying those into player UI and Screen 18 must stay disconnected until Gate D. A literal copy of
  those screens is a product decision, not a styling one — it would display fabricated player data.
- Final art: the comps use licensed/remote photography; the game renders its own rooftop and rigs.

## Screen 17 - Support (layout fix 2026-10-06)

**Issue reproduced and fixed.** Pre-fix captures (backed up as `17_support_*_prefixbackup.png`)
confirmed overlapping columns at 1920x1080 and 2400x1080; columns were approximately 120 canvas
units wide on a 427-unit wide page with the remaining width unused.

**Root causes found:**

1. `SupportHubView.Build` baked the canvas width at open time and never rebuilt on resize.
   All geometry was expressed in canvas units but dp-sized text had no matching box sizing.

2. `UiScreenEnterMotion.Enter/Exit` hardcoded `fromScale=1f, toScale=1f` in every translation
   `UIMotion.Play` call. The dp-space layout root sets `localScale = (unitsPerDp, unitsPerDp, 1)`;
   the motion overwrote this with `(1,1,1)` within the same or following frame, shrinking all
   controls to approximately 0.53x at the A06 (300 dpi) reference device.

**Fixes:**

- `SupportHubView`: full dp-space layout (all boxes, icons, hit areas authored at dp values; root
  scaled by `unitsPerDp`). Three columns >= 1100 dp, two columns below. `Resize` component
  rebuilds on width or density change, preserving selection/deletion/toast/scroll state.
  `RevealPurge` grows page on demand instead of reserving space. `Title` wraps pills.
  `LabelAndValue` helper stacks on narrow widths. Incident-log 96 dp. `IconLabel` shrinks
  captions through type roles. Clear deactivates+detaches before Destroy (same-frame ghost fix).

- `UiDraw.TextUnitScale`: per-build scale factor so TMP `fontSize` is in dp-space units.

- `UIMotion.cs`: Enter and Exit now capture `layer.localScale.x` and pass it as both
  `fromScale` and `toScale`. Translation-only animations no longer modify scale.

**Geometry test assertions added** to `PlayerJourneyVisualEvidenceTests`: page width x root
scale = canvas width; columns do not overlap in 2D and stay inside the page; nav tabs do not
collide with SystemChip or Credits chip in the top bar.

**Verification status:**

| Check | Result |
|---|---|
| 0 compile errors | PASS |
| Geometry assertions at 1920x1080 | PASS |
| Geometry assertions at 2400x1080 | PASS |
| Touch-target raycast test | PASS |
| UiPhysicalSizeTests.Menus_MeetPhysicalMinimums | IN PROGRESS (launched 2026-10-06) |
| Human review of post-fix 16:9 and 20:9 captures | NOT YET DONE |
| Human review of scroll-middle and scroll-end captures | NOT YET DONE |
