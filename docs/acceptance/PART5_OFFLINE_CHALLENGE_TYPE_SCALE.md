# Offline Challenges typography acceptance

## Rules applied

- Use the Bible typography scale, no more than three sizes per screen, and a 12 dp text floor.
- Keep offline challenge records local and keep thresholds sourced from the installed challenge config.
- Preserve chapter selection, resume/start actions, practice match behavior, medal distinctions, and truthful no-network status.

## Scope and implementation

The challenge library, chapter cards, detail view, personal record, practice panel, and footer now use 12/17/30 dp caption, UI/body, and title roles. Save/record/body copy uses readable UI text, statuses use the caption role, and screen titles use the title role. Objective descriptions continue to size their container from preferred text height. No local save fields, medal thresholds, challenge rules, or online reward behavior changed.

## Verified this pass

- Existing local-save integrity, challenge-flow, and runtime navigation coverage was read; tests were not run.
- Source scan finds no numeric font-size arguments in `OfflineChallengeLibraryView`.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:minimal` succeeds with 0 errors and 41 existing warnings.
- `git diff --check` passes for the files changed in this slice.

## Responsive-width follow-up (9 October)

`OfflineChallengeLibraryView` previously calculated its column widths and card
layout only when the screen was first shown. A screen/canvas width change after
initialization could leave the challenge library laid out for the old width.
The density watcher now rebuilds the view when the screen metrics or root width
changes, preserving the current vertical scroll position. No challenge state,
local records, rules, or reward behavior changed. Runtime compilation passed
with 0 errors and 41 existing warnings; no Unity resize capture or device
verification was run.

## Part 5 state-honesty audit (7 October, same day)

`OfflineChallengeController.CompleteChallenge` computed the result screen's
`saveSucceeded` flag by reading `LocalChallengeSave.LastSaveSucceeded` twice —
once right after the conditional `SaveRecord` call, and again after the
unconditional `ClearSnapshot` call that follows it. Since `LastSaveSucceeded`
is a single mutable property overwritten by every `Save()`, the final flag
reported the snapshot-clear outcome, not the record save: a completed attempt
whose medal record saved successfully but whose snapshot clear happened to
fail would show `OfflineChallengeScreen`'s "NOT SAVED — storage unavailable"
banner over a record that was in fact saved; a failed attempt (which never
calls `SaveRecord`) read a stale `LastSaveSucceeded` left over from an
unrelated earlier save. Fixed by capturing the record-save outcome
immediately (vacuously true when no record save was attempted) before
`ClearSnapshot` runs, and combining it with the snapshot-clear outcome
captured separately. No local-save fields, medal thresholds, or save file
format changed. `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore`
succeeds with 0 errors and 41 existing warnings after the fix; the existing
`OfflineChallengeRuntimeTests` success-path assertion on `LastResult.saveSucceeded`
was read and still holds (both saves succeed on a normal filesystem) but was
not re-run.

## Unmet

- Recheck chapter tiles, long challenge descriptions, and practice-panel fit in Unity at 16:9 and 20:9.
- Review selected-state, medal, and text contrast in all A/B/C profiles; the existing palette acceptance still requires in-game review.
- Offline Challenge copy has since been migrated under A7; locale and device
  review remain open in `docs/acceptance/A7_LOCALIZATION_OFFLINE_RUNTIME.md`.
- Run `OfflineChallengeRuntimeTests` and `OfflineChallengeSaveTests` to confirm the corrected `saveSucceeded` flag under a simulated save failure (e.g. a read-only save path); no PlayMode run was performed this pass.
