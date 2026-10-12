# Physical UI scale acceptance

## Rules applied

- Convert design dp and typography dp to canvas units using the active canvas pixel scale and reported display density.
- Keep interactive controls at least 48 dp and visible text at least 12 dp on the live canvas; enforce again after device-size/density changes and generated UI layout.
- Calculate each text/control floor against its local transform scale so scaled containers cannot shrink visible targets below the physical-size requirement.
- Preserve the current reference-frame layout and allow existing scroll views to handle vertical overflow.
- Where a floored control no longer fits its fixed header/footer bar, reflow the bar instead of
  lowering the floor: the bar grows to hold the 48 dp control with its authored inset, and the
  content beside it (scroll viewport, tagline, touch-cluster reservation) moves with it.

## A06 off-screen reflow (2026-10-07)

The latest full PlayMode run on the current working tree (36 tests, 7 October) reported no control
under 48 dp and no text under 12 dp on the Galaxy A06 reference (1600x720 px, 300 dpi). Every
reported problem was "off-screen". On the A06 one canvas unit is one pixel and 48 dp is 90 units.
`UiFloor` grows an undersized control around its centre. A control centred in a 65-80 unit bar
at a screen edge therefore overhangs that edge by 5-11 units.

Source changes (`UiMetrics.BarHeight(designBar, designControl)` returns the design height unless
the floored control needs more, then keeps the authored inset):

| Report entry | Cause | Change |
|---|---|---|
| Home `HomeLogo`, `Home_Strip/SettingsButton` (and its icon) | 65 px header, contents 34 px from top | Header uses `BarHeight(65, 44)`; logo and chip strip anchored to the header's centre (same position at design density); tagline sits 8 px below the header |
| `PlayerProductScreen/Nav_Bar/Nav_Back` (Support confirm, Party) | 80 px `MenuLayout` bar holding a 48 dp BACK | `MenuLayout.TopBarHeight` = max(80, 48 dp); the scroll area below follows it |
| `OfflinePause/.../Nav_Back`, `RESUME`, `RETURN HOME` | 72 px console footer, 48 px actions | Footer and scroll viewport use `BarHeight(72, 48)` |
| Party `HomeButton`, `CodeInput` (+ text), `JoinCode` (+ label), `CreateParty` | 72 px header, 38 px code pill, 77 px footer | Header `BarHeight(72, 44)`, join pill `BarHeight(38, 28)`, footer `BarHeight(77, 48)` |
| Challenges Library/Detail `Footer/Back`, `DeployChallenge` | 76 px footer, 52 px actions | Footer and viewport use `BarHeight(76, 52)` |
| Preparation `Footer/Confirm` (+ text) | 73 px footer, 48 px CONFIRM | Footer and touch-cluster reservation use `BarHeight(73, 48)` |
| Results `Actions/Home`, `Actions/Replay` | 80 px action bar, 52 px PLAY AGAIN | Bar and viewport use `BarHeight(80, 52)` |

No floor was lowered and no gameplay rule changed. On the A06, the Preparation touch-cluster
reservation grows with its footer (73 to 115 units), so touch controls sit higher during the
weapon-choice phase only.

Note on the harness: `UiPhysicalSizeTests.failures` is an instance list that is not cleared
between tests. The Tutorial/HUD report repeats the Menus entries. Its own entries were Preparation
Confirm, Pause RESUME/RETURN HOME and Results Home/Replay. The first pass did not edit that test (as instructed), and the second pass did not change it either.

## Acceptance evidence required

- Unity runtime C# build passed with 0 errors and 41 existing warnings after the transform-aware floor change, and again (`Hotdrop.Runtime.csproj`, 0 errors, 41 warnings) after the 2026-10-07 bar reflow.
- Source inspection confirms the floor recalculates when simulated/current screen dimensions or density change and uses each element's rendered scale.
- Run the existing A06 `UiPhysicalSizeTests` for menus and tutorial/live HUD/results, then inspect 16:9 and 20:9 layout in the Unity Editor and on a physical phone.

## Current conditional compilation (2026-10-10)

- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -m:1 -v:q '-p:DefineConstants=UNITY_ANDROID%3BDEVELOPMENT_BUILD'` succeeded with 0 errors and 41 existing warnings.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -m:1 -v:q '-p:DefineConstants=UNITY_IOS'` succeeded with 0 errors and 41 existing warnings.
- These commands compile the conditional C# branches; they do not build Android/iOS player artifacts or verify native plugins, device layout, haptics, performance, or runtime behavior. Tests were not run.
- Applied rule: current Runtime C# source compiles under default, Android Development Build, and iOS conditional symbols. Unmet: native platform builds and all physical-device acceptance remain open.

## Follow-up fixes (2026-10-07, second pass)

- In-party code pill: CopyCode/Refresh icon buttons are sized to max(24 px, 48 dp) and spaced by
  their real width; the pill widens and uses `BarHeight(38, 24)`, so the floored buttons no longer
  overlap each other.
- Settings tab bar: `BarHeight(54, 50)`; the settings scroll viewport starts below it, so floored
  tabs no longer rise into the console header.
- Home logo (editor/development builds): the study tap is an invisible centred `StudyTap` child, so
  the floor enlarges the tap target, not the logo art.
- Density changes on reused roots: Home re-fits its header and tagline in `LateUpdate`; the Pause
  console re-fits footer, tab bar and scroll viewport each time it refreshes while open.
- `UiScreenEnterMotion` re-reads each layer's resting position on every enter/exit again (instead
  of keeping the first one ever seen), so a layer that re-fits, like the Home tagline, is not snapped
  back to a stale position by the next exit.

Verification: a targeted PlayMode run on 7 October passed both `UiPhysicalSizeTests` (Menus and
TutorialAndMatchHud) on the A06 profile. A full PlayMode run (36) then passed 35. The remaining
failure was a Challenges touch-target check that hit the now-taller footer on the batch screen.
The test now scrolls the card into view before checking it.

## Follow-up source changes (2026-10-07, third pass)

- Party entry and active-room screens now watch screen size/density and text-scale changes while
  open. They re-fit the header/footer and recalculate the code pill, join field, and icon buttons
  before reapplying the physical-size floor.
- Settings tabs now sit in a horizontally scrollable, clipped viewport. Tab widths are measured
  after the active text scale is applied, and the tab strip is rebuilt when screen metrics or text
  scale changes. The pause console also refreshes its footer, tab bar, and content viewport while
  open.
- Challenge, Preparation, and Results roots now watch display metrics while open. Their footer
  heights and scroll viewports reflow; Preparation also updates the reserved touch-cluster space.
- Results rebuilds its measured page when display metrics, root width, or text scale changes,
  restoring the vertical scroll position and current trust/receipt/reward state. An in-progress
  reward reveal settles to its final receipt during that rebuild instead of restarting.
- `Hotdrop.Runtime.csproj` compiled after the Results width reflow with 0 errors and 44 existing
  warnings. No tests or new captures were run for this pass.

## Unmet

- Unity captures were not reviewed by eye, and no physical phone was used.
- On denser 16:9 phones (e.g. 1920x1080 at 400 dpi, where 48 dp ≈ 80 units), Home, Party, Pause, Challenges, Preparation and Results bars grow to keep floored controls inside the bars. Their visual balance against UIUX references has not been reviewed on those devices.
- Results width reflow has not been inspected in Unity at narrow and wide landscape ratios or on a physical phone. Confirm card fit, text wrapping, report-action hit targets, preserved scroll position, and reward state after rotation.

## 12 October 2026 Results card width allocation

The Results page now calculates the contribution/roster split in physical dp.
It reserves 360 dp for three contribution tiles and 360 dp for the compact
roster; if the live content width cannot hold both plus the gap, it stacks the
cards in the existing vertical ScrollRect. Runtime, Network, and Editor builds
passed with 0 errors. No tests or captures were run. Applied: dp-aware width
reflow and the existing vertical-overflow behavior. Unmet: inspect 16:9 and
20:9 captures, safe areas, text expansion, report hit targets, scroll
restoration, and rotation on Unity/device.

At the review matrix's 400 dpi captures, `UiMetrics.ScaleFor` yields 1.5 px per
canvas unit for both 1920×1080 and 2400×1080. Their design-canvas widths are
1280 and 1600 units respectively, producing 739.2 dp and 931.2 dp of Results
content after the authored 24-unit side margins. The first lays out a 360/372 dp
contribution/compact-roster pair; the second yields 360/564 dp and crosses the
existing 560 dp compact/wide roster threshold. The 16:9 content width is 12 dp
above the stacked-card minimum; the 20:9 roster width is 4 dp above the wide-
roster cutoff, so a safe-area inset could change the selected layout. At the
400 dpi capture scale, a 360 dp reportable roster row gives about 90 dp per
compact metric cell: the card is converted to canvas units first, while
padding and gaps remain authored canvas units. These are calculations, not
render captures; check the safe-area source width and both breakpoint
transitions in Unity.

## Match Search width source review (2026-10-07)

- `MatchSearchView` stretches its root, header, and footer to the live canvas; its search stack stays centered at the authored 576-unit maximum, and the region/venue chips stay anchored to the right edge. Width changes therefore do not leave those elements at stale screen coordinates.
- The existing `MatchSearchLayoutFixture_CaptureBothLandscapeRatios` captures 1920x1080 and 2400x1080, but those captures were not run or visually inspected in this pass. This source review is not evidence of safe-area fit, text-scale fit, or rendered non-overlap; retain those visual checks in the Match Search acceptance.
