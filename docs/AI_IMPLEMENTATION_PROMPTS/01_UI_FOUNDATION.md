# Prompt 01 — Unity UI foundation

```text
Work in E:\Usama\Game on the existing ProjectHotdrop Unity project. Implement the reusable HOTDROP UI foundation only. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md, ProjectHotdrop/PROJECT_UPDRAFT_SPEC.md, docs/HOTDROP_PRODUCT_AMENDMENT_2026-10-01.md, ProjectHotdrop/MILESTONE_STATUS.md, current Git status, and the relevant UI/tests before editing.

Use these references:
- UIUX/stitch_project_continuation_hub (6)/stitch_project_continuation_hub/master_design_system_component_handoff_sheet/screen.png
- UIUX/stitch_project_continuation_hub/a_technical_unity_game_ui_asset_and_component_handoff_sheet_titled_unity/screen.png
- UIUX/**/aerospace_utility_sport/DESIGN.md
- ProjectHotdrop/Assets/Scripts/UI/MenuLayout.cs
- ProjectHotdrop/Assets/Scripts/UI/UiMetrics.cs
- ProjectHotdrop/Assets/Scripts/UI/MatchHUD.cs

Treat the Stitch sheets as visual references, not runtime code or gameplay truth. Do not import their remote imagery, HTML libraries, fonts, developer labels, fake telemetry, or review controls.

Deliver a reusable Unity UI system that later screens can share:
1. Central semantic design tokens for colors, typography roles, spacing, radii, border weights, opacity, and motion durations. Do not create a second competing palette beside MatchHUD/MenuLayout; consolidate safely and retain compatibility.
2. A safe-area root that adapts to landscape 16:9, 18:9, 19.5:9, and 20:9 without placing controls under cutouts or gesture areas.
3. Reusable components for primary, secondary, quiet, destructive, disabled, loading, and icon-only buttons; panel/card; header; navigation item; progress bar; slider; toggle; input field; status badge; toast; modal; error/retry state; offline banner; network indicator.
4. Button states for normal, pressed, held, disabled, cooldown, and loading. Minimum interactive size is 48dp.
5. TextMeshPro styles with tabular numerals for timers/scores/ammo, readable body text, larger-text support, and 30% text expansion tolerance.
6. Friendly cyan ring, enemy coral split-chevron/cross, and objective gold ring cues so color is never the only signal.
7. Lightweight motion helpers using 180–240ms ease-out; Reduce Motion replaces translation/scale with fades.
8. A development-only component gallery reachable only from the Unity Editor or development build, never from production navigation.

Prefer focused new files under ProjectHotdrop/Assets/Scripts/UI and incremental changes to MenuLayout/UiMetrics. Do not rewrite MatchHUD or existing screens during this task. Do not regenerate Main.unity or prefabs wholesale.

Add meaningful EditMode tests for semantic token consistency, 48dp floors, safe-area calculations, and component state behavior. Preserve existing tests. Run Unity compile, relevant EditMode tests, and UiPhysicalSizeTests if supported.

Completion evidence:
- List exact files changed.
- Show the component gallery at 16:9 and 20:9.
- Report tests with pass/fail counts.
- Identify any temporary font/icon assets and their license status.
- Do not claim that player screens are implemented yet.
```

