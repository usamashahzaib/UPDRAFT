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
