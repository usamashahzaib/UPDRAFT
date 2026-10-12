# Results typography acceptance

## Rules applied

- Use the Bible's exact typography scale and no more than three sizes on one screen.
- Keep visible player-facing text at or above the 12 dp floor.
- Use only match facts and the persisted server receipt for results and rewards.
- Keep the next-match action available throughout the reward reveal.

## Scope and implementation

Results uses the 12/17/30 dp caption, value, and outcome-display tokens. Labels, table headers, subtitles, callouts, receipt, and readiness status use 12 dp; scores, player names, table values, and actions use 17 dp; victory/draw/defeat uses 30 dp. No match facts, server receipt fields, reveal timing, skip behavior, scoreboard ordering, scroll layout, or sticky action behavior changes.

## Verified this pass

- Existing rendered-results journey capture and result-screen session-lifetime coverage were read; tests were not run.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:minimal` succeeds with 0 errors and 41 existing warnings.
- Source review confirms all Results `UiDraw.Text` calls resolve to the three token roles.

## Unmet

- Visual review of long names, unlocked-item text, score rows, and the sticky actions at 16:9 and 20:9 was not run in this pass.
- A/B/C contrast review remains open in `PART2_RESULTS_PALETTE.md`.

## 12 October compact scoreboard follow-up

Below a 560 dp Results scoreboard column, measured after converting canvas
units to dp, the table header yields to compact team headers and player rows.
Team points move below the team heading. Names and the report control share the
upper row; labeled hold, K/D, and pad values sit below it in 12 dp line boxes.
The report action reserves its 60 × 48 dp target from the compact name and
metric regions. Wide rows grow to contain the 48 dp target, which uses the role
cell on reportable rows so role text cannot overprint it. All text keeps the
existing 12/17/30 dp token roles. **Unmet:** narrow and wide render captures,
localization expansion, and device touch-overlap review remain unverified; no
tests were run.

## 12 October narrow outcome-banner follow-up

The outcome title and score pill now reserve the right-side player-card column
before placement. When the combined title and score do not fit, the score moves
below the title; if its one-line content still exceeds that column, FINAL SCORE
uses a second line. The banner and following Results content grow from the
chosen layout. The existing 12/17/30 dp text roles and score/player facts are
preserved. **Applied:** prevent source-defined title/score/player-card overlap
while retaining responsive Results content flow. **Unmet:** Unity rendering,
locale expansion, and 16:9/20:9 device acceptance remain open. No tests were
added or run.

## 12 October Results callout line-height follow-up

Personal-best and next-goal callouts now size their caption title/tag rows and
wrapped body from the active density and larger-text setting. Their card height
and the next card's position follow the wrapped body height. The existing
player facts, copy, colors, and typography roles are unchanged. **Applied:**
the Results 12/17/30 dp scale and reflow for wrapped content. **Unmet:** Unity
localization expansion, rendered aspect/profile review, and device acceptance
remain open; no tests were run.

## 12 October progression receipt and save-note reflow

The progression receipt and save-note now wrap at their measured widths. Their
12 dp line heights follow active density and larger text; the XP meter, reward
burst, note row, card, and page height are recomputed when receipt or note
content changes. The sticky actions and receipt authority are unchanged.
**Applied:** Results type scale, truthful receipt state, and scroll-content
reflow. **Unmet:** runtime animation/layout, locale expansion, and device
acceptance remain unverified; no tests were run.

## 12 October compact header line-height follow-up

Compact name, metric, team-heading, and points-chip boxes now use 1.32 times
the 17 dp or 12 dp font size plus padding. This covers the largest line extent
among the bundled Results fonts while preserving horizontal ellipsis. Compact
row heights include the enlarged name and metric line boxes; the gap between
team heading and points also grows. Wide player names and subtitles use a
measured vertical stack, so names remain visible in both scoreboard modes,
including Practice. The Runtime sidecar build passed with 0 errors and 44
existing warnings. No tests or Unity/device captures were run. **Applied:**
Results 12/17/30 dp typography, larger-text scaling, and physical UI
conversion. **Unmet:** localized text expansion, rendered narrow/wide layout,
and device acceptance remain open.

## 12 October live text-scale reflow

Results now rebuilds its measured page when display metrics, root width, or the
larger-text setting changes. It restores scroll position and carries current
trust, receipt, and reward state through the existing snapshot path. Runtime
build passed with 0 errors and 44 existing warnings; no tests or Unity/device
captures were run. **Unmet:** verify the live comfort-setting transition and
rotation visually in Unity and on device.

## 12 October contribution-card line-height follow-up

Contribution heading and match-mode labels, plus all three metric-tile lines,
now size their text boxes from the live 12/17 dp text sizes including the
larger-text comfort scale. Tile height, spacing before later callouts, and the
match-mode row height grow with those line boxes. Match facts, copy, colors, and
the existing three typography roles are unchanged. Build and remaining visual
acceptance are recorded in the implementation ledger; no tests were run.

## 12 October outcome-banner line-height follow-up

The outcome banner now sizes its eyebrow, outcome title, score pill, and player
name/weapon lines from the existing 12/17/30 dp text tokens through the active
density and larger-text setting. The banner grows to contain the resulting
layout, and following sections use its returned height. Match result, score,
player/weapon facts, colors, and copy are unchanged. **Applied:** the Results
type scale and physical text sizing. **Unmet:** dynamic text expansion and
narrow/wide profile layout still need Unity visual and device acceptance; no
tests were run.

## 12 October sticky-action label sizing

The Home, Play Again, and Ready Status labels size their line boxes from the
existing 12/17 dp typography through current density and larger-text settings.
Ready Status uses its measured text width; action hit targets and sticky-bar
behavior are unchanged. Runtime sidecar build passed with 0 errors and 44
existing warnings. No tests or Unity/device captures were run. Localization
expansion, narrow-layout spacing, and physical-device acceptance remain open.

## 12 October progression-heading reflow

The progression heading now uses the same density/text-scale-aware caption
line height as the receipt layout. The receipt starts below that measured
heading, including when `SetTrustState` replaces the initial heading text.
**Applied:** Results 12 dp caption sizing and dynamic card reflow. **Unmet:**
localized line expansion and Unity/device layout acceptance remain open; no
tests were run.

## 12 October trust-state header reflow

When Results changes from its initial trust label to pending, verified,
cancelled, or another authoritative state, the eyebrow plate and following
mode/arena metadata now remeasure and reposition from the updated localized
label width. This preserves the 12 dp caption role and does not change trust
state or result data. **Applied:** dynamic text-width layout for changing
player-facing status. **Unmet:** expanded translations and rendered profile /
aspect acceptance remain open; no tests were run.

## 12 October Results reflow and horizontal-fit follow-up

The PERSONAL BEST result is recorded once by `MatchHUD` at match end, supplied
to the Results page, and retained in its reflow snapshot. Resizing or changing
text scale therefore does not repeat the record-and-save routine or remove
that match's notice. The
outcome metadata line now reserves the player card's width and ellipsizes to
the remaining space; the player card caps its width and truncates long names.
Contribution headings, impact labels, tile labels/values/subtitles, and match
mode text are bounded to their rectangles. The callout title and record tag
share the measured header width, its icon and text align within a taller header
row, and its body/next sections follow that row. Tile value and subtitle lines
now retain their intended 4-unit gap. **Applied:** preserve match result state
through reflow, fit labels to measured cards, and maintain the 12/17/30 dp type
scale. Runtime build passed with 0 errors and 44 existing warnings; Editor build
passed with 0 warnings and errors. No tests were run. **Unmet:** Unity captures,
locale expansion, and device rendering remain open.

## 12 October outcome metadata/player-card fit

The outcome eyebrow and combined mode/arena metadata now share the same
measured left-column limit as the title and score, reserving the player card's
width before placement. Both labels use the existing 12 dp caption role and
ellipsize within the remaining space, including when localized copy or larger
text expands. **Applied:** prevent status, metadata, and score text from
overlapping the player card while preserving result facts. **Unmet:** Unity
rendering at narrow/wide ratios, translated expansion, and device acceptance
remain open. Runtime, Network, and Editor builds passed with 0 errors and 0
warnings in the incremental build; no tests were run.

## 12 October Stream-Safe preference follow-up

The Results view now rebuilds when Stream-Safe Display changes and suppresses
the cached PERSONAL BEST callout while redaction is enabled. This closes the
cached-result disclosure path during later reflows. **Applied:** preserve the
privacy preference across Results rebuilds. **Unmet:** Unity toggle/capture
behavior and device rendering remain open; no tests were run.

## 12 October final source review and fix pass

Confirmed by source reading and fixed in `ResultsView.cs`/`MatchHUD.cs`:

- **Wide line heights:** the wide team header (26), points chip (18), note (14),
  column labels (22) and row cells (18) used fixed boxes below the 1.32 em
  line extent. They now derive from `ResultsTextLineHeight`, keeping the
  existing minimums; header geometry grows with the larger-text setting.
- **Wide header overlap:** "MATCH RECORD / EQUAL COMPETITION ROSTER" was a fixed
  260-unit right-aligned box that could overprint the points chip near the
  560 dp threshold. It now takes only the space the chip leaves, with ellipsis.
- **Outcome header refit:** `FitOutcomeHeader` only shrank text, so a trust-state
  change to a shorter label stayed at the earlier shrunken size. It resets the
  eyebrow/meta to the authored size before refitting.
- **Contribution header:** the DIRECT IMPACT label ended 12 units inside the
  tile edge; it now aligns with the tile row's right edge. The MATCH MODE row's
  fixed 160/180 widths are now bounded to the card and fitted.
- **Callout layout:** the body was fixed at x = 36 while title/tag/icon track the
  scaled icon; body x/width now follow the title column, and the body minimum
  uses the 1.32 em line box.
- **Personal best:** `MatchHUD.ShowResults` skipped `PersonalBests.Record` when
  Stream-Safe was on, so that match's record was lost permanently. It now records
  once at match end regardless; display remains gated by the live preference in
  `ResultsView` and the legacy table text. Reflows reuse the captured string, so
  resize/text-scale changes do not re-record. `ResultsView.Show`'s fallback
  record path got the same change.

**Verified:** Runtime build 0 errors / 44 existing warnings; Network 0/0;
Editor 0/0. No tests were added or run.
**Unmet / unverified:** no Unity render, locale-expansion, or device captures;
the ellipsis-blank behavior noted in `UiDraw` batch captures could still hide
truncated labels. Narrow and wide responsive layouts still need visual review.
The title/score-pill overlap concern recorded in this earlier review was
addressed by the 12 October narrow outcome-banner follow-up above; the new
layout still needs rendered acceptance.

## 12 October narrow Results card reflow

The contribution card and compact roster now each keep a 360 dp width
allocation when the viewport can fit both plus their gap. The roster minimum
also preserves width for a 60 dp report action and three readable compact
metric columns. On narrower viewports the cards stack at full available width above
the existing scrollable page; on wider viewports the roster receives the width
left after the contribution minimum. Scoreboard report eligibility, data,
typography roles, and sticky actions are unchanged. **Applied:** responsive
physical-width allocation using the existing scroll layout. **Unmet:** Unity
rendering, translated-content expansion, and narrow/wide device acceptance
remain open; no tests were added or run.

For the existing layout-review capture sizes at 400 dpi, the source sizing
math gives a 739.2 dp content width at 1920×1080 (360 dp contribution, 372 dp
compact roster) and 931.2 dp at 2400×1080 (360 dp contribution, 564 dp wide
roster). The compact-to-wide scoreboard change is therefore near the wide
capture size: the 2400×1080 roster is only 4 dp over the existing 560 dp wide-
roster cutoff, while the 1920×1080 layout has 12 dp above the stacked cards'
combined minimum. A viewport-width loss from a safe-area/cutout inset could
switch either layout. These are source-derived bounds; root safe-area behavior
and visual continuity at both breakpoints remain unverified.

## 12 October roster compact/wide hysteresis margin

The wide roster layout now starts at 600 dp (was 560 dp). At 2400×1080 and
400 dpi the side-by-side roster is 564 dp, so a few units of safe-area inset
flipped it between layouts; it now stays compact with a 36 dp margin. Stacked
full-width rosters between 560 and 600 dp also use the compact rows. Not built
or tested in this pass; Unity/device captures remain open.
