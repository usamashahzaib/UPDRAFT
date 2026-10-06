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

## Unmet

- Recheck chapter tiles, long challenge descriptions, and practice-panel fit in Unity at 16:9 and 20:9.
- Review selected-state, medal, and text contrast in all A/B/C profiles; the existing palette acceptance still requires in-game review.
- Localization remains an A7 prerequisite because Unity Localization is not configured.
