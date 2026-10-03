# Prompt 02 — Startup, appearance choice, Home, and tutorial

```text
Work in E:\Usama\Game on ProjectHotdrop. Implement the complete first-run and returning-player shell: Startup → first-launch Appearance Choice → nine-step Tutorial → Home, plus returning launch → Home. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md and require Prompt 01's UI foundation to exist before editing.

Visual references:
- UIUX/stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_01_startup_connection_normal_state/screen.png
- UIUX/stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_01_startup_states_matrix_design_review/screen.png (review states only; never ship the matrix)
- UIUX/stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_02_first_launch_appearance_choice/screen.png
- UIUX/stitch_project_continuation_hub/screen_3_home_lobby_production_clean/screen.png
- UIUX/stitch_project_continuation_hub/screen_3_home_lobby_production_final_states/screen.png (state reference only)
- UIUX/stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_04_guided_first_run_training_step_5_of_9/screen.png
- UIUX/stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_04_guided_training_9_step_review_strip/screen.png (review only)

Relevant implementation:
- Core/GameBootstrap.cs
- Core/OfflineHomeScreen.cs
- UI/HomeCharacterPreview.cs
- Player/BodyRig.cs and Player/AvatarRigVisual.cs
- Tutorial/TutorialController.cs
- UI/TutorialUI.cs
- Core/OnlineHooks.cs
- existing body-rig, home, tutorial, smoke, and physical-size tests

Requirements:
1. Add a real startup/connection presentation using HOTDROP branding and a restrained Air Route loading motif. Support normal, no internet, service unavailable, update required, maintenance, and recoverable save-loading states. Offline must be actionable whenever local play is available. No endless spinner or fake status.
2. First launch shows two equal body presentations with identical gameplay profile. Continue and Decide Later must both work. The selection persists and remains changeable later. Do not ask for account, age, gender, notifications, or payment.
3. Implement all nine real tutorial steps already defined by TutorialController: move, fire, reload, place pad, launch from own pad, use enemy-colored pad, destroy enemy pad, capture the active zone, complete the bot fight. Show one instruction at a time, step N of 9, Show Me after inactivity, and Skip Training with confirmation. Never obscure crosshair, target, or thumb zones.
4. Rebuild Home around one dominant PLAY 3-MINUTE RELAY action, one PLAY OFFLINE action, the real selected character, exactly one honest goal card, and Party / Appearance / More navigation. Do not ship Season 1, rank, fake latency, fake Flight Seals, or contract progress unless backed by real state. Hide unavailable backend-backed values or label them honestly.
5. Preserve the facilitator-only study entry but keep it undiscoverable in ordinary production presentation and exclude it from screenshots.
6. Home visual order: character/launch route → Play → goal → Offline → navigation. Use the real in-game character and launch-pad identity, not a remote Stitch image as a shipped background.
7. Connect every Back path and app-resume state. Interruption during first-launch selection or tutorial must recover safely.

Do not implement Party, Contracts, Mastery, Shop, or broad account work in this task. Do not change match balance.

Validation:
- Fresh profile completes Startup → Choice → all nine tutorial steps → Home.
- Decide Later works and Appearance can change the choice afterward.
- Returning profile skips first-launch choice and reaches Home.
- No-internet startup can enter Offline.
- Tutorial Skip confirmation works and training remains replayable.
- Home Play/Offline/Party/Appearance/More actions route correctly or show truthful unavailable states.
- Run relevant EditMode and PlayMode tests, including BodyRigParityTests, OfflineSliceSmokeTest, Tutorial-related coverage, and UiPhysicalSizeTests.
- Capture Unity screenshots at 16:9 and 20:9 for Startup, Choice, Home, and one tutorial step.

Report exact changed files, test results, screenshots, and remaining limitations. Do not claim the online journey is complete.
```

