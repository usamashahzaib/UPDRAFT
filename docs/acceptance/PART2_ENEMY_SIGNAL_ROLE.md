# Enemy color role and destructive actions

## Rules applied

- Presentation profiles A, B, and C keep the enemy token for enemy identity and damage feedback. The Bible's enemy coral rule explicitly excludes buttons.
- Destructive actions use neutral selected/panel surfaces, primary text, and the profile's warning tone for their border or status emphasis.
- Connectivity, offline, invalid pad placement, and defeat states use warning semantics instead of enemy identity. Team and enemy cues keep their existing labels and shapes.
- Existing action behavior is unchanged, including the account purge's two confirmation stages and held-action gate.

## Source acceptance

- Shared destructive buttons now use a neutral selected fill, primary text, and warning border/accent; Cancel Search is neutral.
- The account purge and offline account status use the warning role. The online-match vulnerability notice, offline error/retry block, offline connection indicator, pad-placement error, disconnected roster label, and defeat title use warning semantics.
- Match HUD enemy score/team/zone cues, enemy ping, and damage/low-health cues retain the enemy profile role.
- The Results `DEFEAT` heading uses the active profile's warning token; `VICTORY` remains ally-colored and `DRAW` remains objective-colored.
- Existing variant-fill and retry-state coverage was read. Tests were not run in this implementation pass.
- Unity runtime build passed after the Results semantic-color correction with 0 errors and 41 compiler warnings, primarily existing deprecation and unassigned serialized-reference warnings.
- `git diff --check` passed for the source edits.

## Unmet

- Cross-profile contrast and destructive-action visual review in Unity remain open for A, B, and C.
- Static opaque sRGB calculation for Grounded Tactical Flight's enemy color `#D85D61` is 4.90:1 against its background and 4.08:1 against its surface. The design materials do not set a numeric threshold for this semantic cue; confirm the small-text and panel uses during reviewed visual acceptance.
- The Unity editor was unavailable for an in-game visual review during the preceding implementation check; no device or platform verification is claimed.
- Authored copy on these surfaces now uses the configured Unity Localization lookups; English seed/table consistency is statically audited. Unity Editor import/re-save, translated locales, locale switching, and device review remain open under `A7_LOCALIZATION_TABLE_SEED.md` and the relevant `A7_LOCALIZATION_*` acceptance notes. Backend-supplied free-form error messages remain a separately documented localization prerequisite (`A7_LOCALIZATION_BACKEND_CODES.md`).
