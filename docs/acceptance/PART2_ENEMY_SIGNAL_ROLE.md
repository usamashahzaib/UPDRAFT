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
- Existing variant-fill and retry-state coverage was read. Tests were not run in this implementation pass.
- Unity runtime project build passed with 0 errors and 41 compiler warnings, primarily existing deprecation and unassigned serialized-reference warnings.
- `git diff --check` passed for the source edits.

## Unmet

- Cross-profile contrast and destructive-action visual review in Unity remain open for A, B, and C.
- The Unity editor was unavailable for an in-game visual review during the preceding implementation check; no device or platform verification is claimed.
- Localization remains a project prerequisite for these and other existing literal UI strings.
