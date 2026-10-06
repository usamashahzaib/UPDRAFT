# Match Search typography acceptance

## Rules applied

- Use the Bible's exact typography scale and no more than three sizes on one screen.
- Keep visible player-facing text at or above the 12 dp floor.
- Preserve honest queue time, assigned region, squad readiness, Practice, and Cancel information and actions.

## Scope and implementation

Match Search uses the 12/17/22 dp caption, value, and emphasis tokens. Header, region, readiness, squad status, action, and footer labels use 12 dp; search explanation uses 17 dp; elapsed search time and the screen title use 22 dp. Party names remain ellipsized in their existing seats. This is a presentation-only change; queue state, region values, actions, copy, and telemetry are unchanged.

## Verified this pass

- The existing `MatchSearchLayoutFixture_CaptureBothLandscapeRatios` and `S06_Search` fixtures cover layout captures at 1920x1080 and 2400x1080; they were read but not run.
- Runtime C# build succeeded with 0 errors and 41 existing warnings.
- Source review confirms all Match Search text-size arguments are one of the three token roles.
- `git diff --check` passed for this slice.

## Unmet

- Visual capture review at 16:9 and 20:9 was not run in this pass.
- A/B/C contrast and color-vision review remains open in `PART2_MATCH_SEARCH_PALETTE.md`.
