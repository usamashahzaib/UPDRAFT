# Native profile palette aliases

## Rules applied

- Literal colors that match an A, B, or C semantic palette value resolve to the active profile role when constructed through `UiDraw.Hex`.
- Already-open UI remaps semantic palette surfaces and six-digit TMP rich-text color tags when the player changes presentation profile; newly created or refreshed shared UI text normalizes legacy semantic tags against the active profile.
- Background, panel, primary text, secondary text, action, ally, enemy, and objective roles remain separate.
- Legacy UI code keeps its current layout and flow; only colors matching these documented profile roles are remapped.

## Acceptance

- Unity runtime build passed with 0 errors and 41 existing warnings after adding semantic rich-text remapping.
- Focused `git diff --check` passed for the tracked profile implementation.
- Inspect existing rich-text labels through A → B → C profile changes and confirm semantic colors follow the selected palette without changing non-palette markup.
- Check color-role outputs for all three profile configurations and review cross-profile contrast in game before visual acceptance.

## Unmet

- No in-game rich-text or cross-profile contrast review was run in this pass.
