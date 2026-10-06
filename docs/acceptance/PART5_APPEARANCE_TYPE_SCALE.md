# Appearance Locker typography acceptance

## Rules applied

- Use the Bible typography roles, a maximum of three sizes per screen, and a 12 dp minimum.
- Keep the A/B/C palette role mapping and cosmetic-only / server-confirmed inventory claims intact.
- Preserve existing category, preview, equip, retry, loading, unavailable, empty, and technical-disclosure behavior.

## Scope and implementation

All Appearance Locker text now uses the 12/17/30 dp caption, UI, and title roles. This includes category/filter controls, inventory status and item rows, the technical-details disclosure, and footer actions. Canvas positions, hit areas, inventory rules, server claims, and item behavior are unchanged. Copy remains inline because Unity Localization is not configured; migration is tracked as the A7 prerequisite.

## Verified this pass

- Existing navigation, inventory-category, and visual-capture coverage was read; tests were not run.
- Source review confirms the screen's `UiDraw.Text` calls use only the three token roles.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:minimal` succeeds with 0 errors and 41 existing warnings.
- `git diff --check` passes for the files changed in this slice.

## Unmet

- Review long cosmetic names, expanded technical details, and the 20:9 composition in game; visual captures were not run.
- Review A/B/C contrast and color-vision distinction using `PART2_APPEARANCE_PALETTE.md`.
