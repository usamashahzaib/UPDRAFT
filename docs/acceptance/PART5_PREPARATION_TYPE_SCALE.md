# Preparation typography acceptance

## Rules applied

- Use the Bible's exact typography scale and no more than three sizes on one screen.
- Keep visible player-facing text at or above the 12 dp floor.
- Display weapon facts from the configured `WeaponDefinition`; do not substitute illustrative or invented balance values.
- Preserve the existing weapon selection, auto-confirm countdown, teammate loadout, and spawn/damage/pad restrictions.

## Scope and implementation

The Preparation screen uses the 12/17/22 dp caption, value, and emphasis tokens. Captions, weapon roles/specs/tips, squad chips, actions, and safety text use 12 dp; the actual opening-objective line uses 17 dp; weapon names and countdown use 22 dp. The legacy weapon-select cards now use the same 22 dp name and 12 dp role/spec sizes, and their safety/countdown copy is kept at the caption size. The full-screen Preparation view continues to render the opening objective at 17 dp and its countdown at 22 dp. Damage, cadence, range, ammunition, role descriptions, current zone, and countdown sources are unchanged. No phase, balance, telemetry, or selection behavior changes.

## Verified this pass

- Existing tests and captures were read, including the live-preparation journey fixture and weapon-card text containment check; no tests were run.
- `Hotdrop.Network.csproj` and its runtime dependencies build successfully: 0 errors, 121 existing warnings. Build output used a temporary `BuildTemp` directory.
- Source review confirms both the full-screen and legacy weapon-selection Preparation surfaces use the exact 12/17/22 dp roles. The legacy roles/specs and its combined safety/countdown copy use the 12 dp caption token; name and countdown use 22 dp; the opening objective remains 17 dp.

## Unmet

- Runtime visual review of the three weapon cards, text wrapping at the smaller token size, and 16:9/20:9 layouts was not run in this pass.
- Cross-profile contrast and color-vision review remains open in `PART2_PREPARATION_PALETTE.md`.

## 12 October larger-text and display reflow follow-up

The open Preparation view now rebuilds when the current display metrics, root
width, or larger-text scale changes. The rebuild preserves the selected weapon
and latest countdown value, and replacing an existing view does not replay the
phase-entry animation. This lets measured labels and width-dependent placement
be recalculated from the active typography and canvas instead of only growing
the footer and physical-size floor.

**Spec rules applied:** Bible Part 7's larger-text preference, live display
reflow, and 48 dp touch-floor behavior; Part 5's 12/17/22 dp Preparation type
roles and preservation of weapon selection/countdown behavior.
**Verified:** Runtime, Network, and Editor project builds succeeded with 0
errors; the Runtime dependency reported 44 existing warnings. No tests were
run.
**Unmet:** Unity rendering, weapon-card wrapping, and 16:9/20:9 physical-device
acceptance remain open. The change recompiles the existing authored layout; it
does not claim that the fixed reference-width card row has been visually
accepted on every aspect ratio.

The same rebuild watcher responds to the Stream-Safe preference event, so
teammate chips do not retain aliases captured before a live privacy toggle. See
`docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.
