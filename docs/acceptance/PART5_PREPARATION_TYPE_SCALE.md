# Preparation typography acceptance

## Rules applied

- Use the Bible's exact typography scale and no more than three sizes on one screen.
- Keep visible player-facing text at or above the 12 dp floor.
- Display weapon facts from the configured `WeaponDefinition`; do not substitute illustrative or invented balance values.
- Preserve the existing weapon selection, auto-confirm countdown, teammate loadout, and spawn/damage/pad restrictions.

## Scope and implementation

The Preparation screen uses the 12/17/22 dp caption, value, and emphasis tokens. Captions, weapon roles/specs/tips, squad chips, actions, and safety text use 12 dp; the actual opening-objective line uses 17 dp; weapon names and countdown use 22 dp. Damage, cadence, range, ammunition, role descriptions, current zone, and countdown sources are unchanged. No phase, balance, telemetry, or selection behavior changes.

## Verified this pass

- Existing tests and captures were read, including the live-preparation journey fixture and weapon-card text containment check; no tests were run.
- Runtime C# build succeeds.
- Source review confirms all `UiDraw.Text` sizes are the three exact token roles.

## Unmet

- Runtime visual review of the three weapon cards and 16:9/20:9 layouts was not run in this pass.
- Cross-profile contrast and color-vision review remains open in `PART2_PREPARATION_PALETTE.md`.
