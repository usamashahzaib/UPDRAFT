# Native profile palette aliases

## Rules applied

- Literal colors that match an A, B, or C semantic palette value resolve to the active profile role when constructed through `UiDraw.Hex`.
- Already-open UI remaps semantic palette surfaces and six-digit TMP rich-text color tags when the player changes presentation profile; newly created or refreshed shared UI text normalizes legacy semantic tags against the active profile.
- Background, panel, primary text, secondary text, action, ally, enemy, and objective roles remain separate.
- Derived semantic tones (pressed action, pressed enemy, action/objective mix, dimmed warning) are palette values too, so an already-open graphic built from one of them remaps on a profile switch instead of keeping the previous profile's hue.
- Legacy UI code keeps its current layout and flow; only colors matching these documented profile roles are remapped.
- New generated UI uses the active profile tokens at creation time, rather than relying on a later profile-switch remap to correct a hard-coded palette color.

## Acceptance

- Unity runtime build passed with 0 errors and 41 existing warnings after adding semantic rich-text remapping.
- Focused `git diff --check` passed for the tracked profile implementation.
- Inspect existing rich-text labels through A → B → C profile changes and confirm semantic colors follow the selected palette without changing non-palette markup.
- Check color-role outputs for all three profile configurations and review cross-profile contrast in game before visual acceptance.

## 9 October source follow-up

The touch-layout editor's scrim used Hybrid's `#101827` background literal.
Existing-graphic remapping occurs on later profile changes, so a first build
under A or B could retain Hybrid's tint. The scrim now reads
`UIDesignTokens.Background` at creation time and preserves its existing 55%
opacity. Touch geometry and editing behavior are unchanged. Runtime
compilation passed; visual profile-switch review remains open.

## Unmet

- No in-game rich-text or cross-profile contrast review was run in this pass.
