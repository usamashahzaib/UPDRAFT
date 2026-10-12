# Contracts and Mastery typography acceptance

## Rules applied

- Use no more than three Bible typography sizes on each page and keep all visible text at or above 12 dp.
- Continue to show only backend-provided progression and reward-catalog state; never invent progress or rewards.
- Keep gated Contracts and Mastery read-only when `ContractsMasteryFeature.Enabled` is false.

## Scope and implementation

Both pages now use 12/17/30 dp caption, UI/body, and title roles. Inline TMP font-size overrides were removed so the rendered values stay within those roles. Rules and directive-condition cards gained vertical room for body copy at the larger readable size; no goals, thresholds, reward values, unlock rules, or button actions changed.

The saved Contracts layout fixture showed directive progress captions painting beneath the right-aligned count, and condition copy continuing beyond its short card into the reward row. The progress caption now truncates within the space reserved before the count, the condition panel gives its wrapped copy a taller text area, and the three directive cards plus their following content reserve the added height.

## Verified this pass

- Existing feature-gate, read-only navigation, and 16:9/20:9 capture coverage was read; tests and captures were not run.
- Source scan finds no numeric font-size arguments or rich-text size overrides in `GoalsPagesView`.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:minimal` succeeds with 0 errors and 41 existing warnings.
- Runtime rebuilt after reserving progress-label width and increasing the directive condition area: 0 errors, 41 existing warnings. `git diff --check` passed for `GoalsPagesView.cs` and this acceptance note.
- `git diff --check` passes for the files changed in this slice.

## Unmet

- Review long body copy and card/footer fit in the existing 16:9/20:9 Unity fixtures.
- Verify cross-profile contrast and color-vision distinction in A/B/C.
- The verified contract reward catalog is still disabled/incomplete; the existing honest unavailable state remains in place.
