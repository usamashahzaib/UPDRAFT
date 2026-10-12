# Appearance Locker typography acceptance

## Rules applied

- Use the Bible typography roles, a maximum of three sizes per screen, and a 12 dp minimum.
- Keep the A/B/C palette role mapping and cosmetic-only / server-confirmed inventory claims intact.
- Preserve existing category, preview, equip, retry, loading, unavailable, empty, and technical-disclosure behavior.

## Scope and implementation

All Appearance Locker text now uses the 12/17/30 dp caption, UI, and title roles. This includes category/filter controls, inventory status and item rows, the technical-details disclosure, and footer actions. The filter label now has a gap before its chips, and its earned-only notice moves to a second row when the card is too narrow to keep the notice beside the chips. The stage title and cosmetic-only chip occupy separate bounded regions; stage weapon and wrap values use two columns and are shortened to fit. Hit areas, inventory rules, server claims, and item behavior are unchanged. Screen copy uses the configured Unity Localization string table through `UiDraw.L`; locale seeding and translated wrapping are tracked under A7.

## Verified this pass

- Existing navigation, inventory-category, and visual-capture coverage was read; tests were not run.
- The existing 1920×1080 Appearance capture showed the filter label, chips, and earned-only notice colliding. Source layout now separates the label and wraps the notice when needed; no post-fix Unity capture was made.
- Saved 7 October A/B/C Appearance captures showed the title colliding with the cosmetic-only chip and stage equipment labels colliding with their values. Source now reserves header regions and uses bounded stage columns with measured truncation for both labels and values; no post-fix Unity capture was made.
- Source review confirms the screen's `UiDraw.Text` calls use only the three token roles.
- `dotnet build ProjectHotdrop/Hotdrop.Editor.csproj --no-restore -m:1 -v:q` succeeds with 0 errors and 41 existing warnings.
- `git diff --check` passes for the files changed in this slice.

## Unmet

- Review long cosmetic names, expanded technical details, the filter row, and the 20:9 composition in game; no post-fix visual capture was run.
- Review A/B/C contrast and color-vision distinction using `PART2_APPEARANCE_PALETTE.md`.
