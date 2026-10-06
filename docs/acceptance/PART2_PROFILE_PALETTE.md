# Profile palette acceptance

## Rules applied

- Profile backgrounds, panels, selection, structure, action, objective/reward, and text use the active A/B/C semantic palette.
- Inline rich-text colors derive from the same tokens so the level, guarantee, and identity values follow the selected profile.
- Profile hierarchy, stream-safe masking, telemetry, account facts, and navigation behavior remain unchanged.

## Acceptance

- Build the Unity runtime after the palette edit.
- Inspect collapsed and expanded profile in all three profiles at 16:9 and 20:9 for contrast, wrap, and clipping. Existing journey screenshots document the profile surface but do not cover all presentation profiles.

## Unmet

- Cross-profile Unity visual/device review was not performed in this pass.
- Player-facing copy is not localized because the project has no localization framework configured; record and resolve this prerequisite before localization acceptance.
