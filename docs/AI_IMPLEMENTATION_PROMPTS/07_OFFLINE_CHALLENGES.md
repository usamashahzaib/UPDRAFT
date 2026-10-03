# Prompt 07 — Offline travel challenges

```text
Work in E:\Usama\Game on ProjectHotdrop. Implement the offline travel-challenge collection only after the core first-run, HUD, results, and pause/resume journey are stable. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md and the offline-challenge section of docs/HOTDROP_PRODUCT_AMENDMENT_2026-10-01.md.

Visual reference:
- UIUX/stitch_project_continuation_hub (5)/stitch_project_continuation_hub/screen_14_offline_travel_challenges/screen.png

Do not copy its HOTDROP military/recon terminology, fake 384 MB cache, fake device-sync status, latency/build telemetry, invented K-Core currency, Mission Control navigation, Deploy HOTdrop action, or locked values presented as finished. Preserve only the chapter/library/detail information structure.

Implement:
1. A versioned OfflineChallengeConfig model and 12 authored entries using existing movement, weapons, pads, bots, Switchyard, HUD, pause, and results systems.
2. Chapters:
   - First Flight: Basic Route, Own-Pad Route, Enemy-Pad Route, Safe Landing.
   - Rooftop Operator: Stationary Targets, Moving Targets, Reload Discipline, Launch and Hit.
   - Relay Captain: Capture One Zone, Rotate Between Zones, Defend Against Bots, Complete Offline Relay.
3. A versioned atomic local save for completion, medal, personal best, challenge version, control preset, and difficulty where relevant. Corrupt/missing data recovers safely.
4. Challenge Library, Challenge Detail, gameplay objective tracker, result, Retry, Next Challenge, Library, interruption Resume, and clean Restart when snapshot recovery fails.
5. All 12 challenges ship installed. No account, internet, ad, streamed download, online reward, credit, Flight Seal, contract, mastery, rating, or leaderboard.
6. Label results LOCAL RECORD. Offline Relay remains a clear three-minute practice option.
7. Do not invent Bronze/Silver/Gold thresholds. Put thresholds in config and leave unvalidated values disabled or explicitly marked for tuning in development builds only. Player builds must not show developer placeholders.
8. Save safely on step completion, pause, background/interruption, and result. Never imply an online match can pause.

Suggested locations after inspection:
- Config/OfflineChallengeConfig.cs
- Core/OfflineChallengeController.cs
- Core/LocalChallengeSave.cs
- focused UI views under UI/
- Resources/Config for authored assets only after values are reviewed

Do not modify online authority, backend wallets, inventory, matchmaking, contracts, or mastery.

Validation:
- All 12 entries load and route to valid gameplay.
- Airplane mode completes and saves challenges.
- Restart/interruption/resume works.
- Corrupt save recovers without granting medals.
- Local edits cannot grant online state.
- Relevant EditMode save/config tests and PlayMode challenge smoke tests pass.
- Capture Library, Detail, gameplay, Result, Resume, empty-progress, and completed-chapter screenshots.
- Report exact files, authored versus untuned thresholds, tests, and device verification status.
```

