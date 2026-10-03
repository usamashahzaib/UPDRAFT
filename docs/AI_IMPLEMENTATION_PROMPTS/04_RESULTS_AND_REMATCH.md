# Prompt 04 — Results, rewards, rematch, and Home return

```text
Work in E:\Usama\Game on ProjectHotdrop. Implement a production Results journey for HOTDROP after Prompt 03's gameplay UI is stable. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md first.

Visual reference:
- UIUX/stitch_project_continuation_hub (4)/stitch_project_continuation_hub/screen_11_match_results_verified_contribution/screen.png

Do not copy the reference literally. Remove its HOTDROP/HOTDROP duplicate branding, Hangar/Operatives/Loadouts/Deploy/Telemetry navigation, Tactical Net language, fake altitude/velocity charts, Match Quality Index, Pass/Leaderboard navigation, fake server-verification claims, and any value not supplied by runtime or backend. Preserve only the useful order: outcome → verified contribution → team scoreboard → real progression → next action.

Relevant code:
- UI/MatchHUD.cs, especially ShowResults
- Match/MatchController.cs and MatchFlowController.cs
- Network/NetMatch.cs and NetClientPresenter.cs
- Core/PendingResultStore.cs and RecentMatch.cs
- Config/ProgressionConfig.cs
- Backend client/result submission paths and corresponding backend endpoints/tests
- ResultsLifetimeTests and OfflineSliceSmokeTest

Requirements:
1. Support Victory, Defeat, Draw, Overtime Victory, Overtime Defeat, and server-cancelled states with equal visual dignity.
2. Header shows final score and truthful end condition: SCORE LIMIT, TIME, OVERTIME HOLD, OVERTIME DRAW, or MATCH CANCELLED.
3. Contribution block shows only authoritative/available facts: objective time, eliminations, deaths, pad launches, and a useful teammate route only when ownership and a qualifying human teammate use are verified. Personal Best appears only after a real comparison.
4. Team scoreboard shows two teams of three, aliases, [BOT] labels, objective time, eliminations, deaths, pad launches, and disconnected state. Never expose hidden rating or internal IDs.
5. Offline results clearly say LOCAL PRACTICE — NO ONLINE XP, CREDITS, CONTRACTS, OR MASTERY. Do not fabricate progression.
6. Online eligible results display XP/credits using ProgressionConfig and backend response. Contract/mastery sections stay hidden until those systems exist. Reward-pending says Reward pending — your progress is safe and retries idempotently.
7. Primary PLAY AGAIN routes directly to the correct offline restart or preserved-party queue. Secondary PLAY TOGETHER appears only when the real opt-in flow exists. HOME always works.
8. Match end stops damage/scoring immediately while the results UI remains stable. Avoid duplicate result panels, duplicate submissions, and actions firing twice.
9. Preserve the player's equipped character, wrap, and badge only when real data/assets exist.

Do not implement contracts/mastery in this task. Build extension points without fake cards or placeholder rewards.

Validation:
- Test offline win/loss/draw, score-limit/time/overtime outcomes, server-cancelled result, repeated ShowResults calls, pending result retry, Play Again, and Home.
- Existing ResultsLifetimeTests and PendingResultStoreTests pass; add behavior tests for action idempotency and offline/online reward separation.
- Run relevant EditMode and PlayMode tests.
- Capture Unity screenshots for Victory, Defeat, Draw, Offline result, and Reward Pending at 16:9 and 20:9.
- Report exact changed files, real fields displayed, tests, and unimplemented progression sections.
```

