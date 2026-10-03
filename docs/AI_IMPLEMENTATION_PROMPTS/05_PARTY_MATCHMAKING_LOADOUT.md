# Prompt 05 — Party and matchmaking journey

```text
Work in E:\Usama\Game on ProjectHotdrop. Implement the honest online shell Home → Party/Play → Matchmaking → Reservation → Loadout/Preparation → Match, using existing backend and Fusion systems. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md, current milestone status, network acceptance documents, backend contracts, and existing tests first.

Visual references:
- UIUX/stitch_project_continuation_hub (2)/stitch_project_continuation_hub/screen_05_party_room_production_state/screen.png
- UIUX/stitch_project_continuation_hub (2)/stitch_project_continuation_hub/screen_06_matchmaking_search_state/screen.png
- UIUX/stitch_project_continuation_hub (2)/stitch_project_continuation_hub/screen_07_loadout_preparation_8s_countdown/screen.png
- UIUX/stitch_project_continuation_hub (2)/stitch_project_continuation_hub/batch_2_flow_review_strip_design_review_only/screen.png (flow reference only)

Relevant code:
- Core/PlayerProductScreens.cs party behavior
- Core/BackendClient.cs and OnlineHooks.cs
- Network/OnlinePlay.cs, NetSession.cs, NetClientPresenter.cs, NetMatch.cs
- Config/MatchmakingConfig.cs
- backend Auth/Party/Matchmaking endpoints and tests
- PlayerProductScreensNavigationTests, NetAuthorityTests, network/resource tests

Requirements:
1. Party entry supports Create Party and Join with one 8-character code. Make party code visibly different from account/player ID.
2. Active party has exactly three slots with alias, Ready/Not Ready, connection quality when measured, and leader indicator. Copy/Share Code, Ready, Leave Party, refresh/reconnect, and leader-only start must use real backend behavior.
3. Do not implement Kick unless the backend already authorizes it. Do not implement chat, voice, friend feed, clans, ranks, or full-party advantage language.
4. Handle solo, joining, 2/3, 3/3 ready, invalid code, expired/not-found code, full party, member dropped, leader transfer, leader disconnected, auth/backend failure, and retry.
5. Matchmaking shows real elapsed time, selected region and measured latency, party status, and Cancel. Never show fake queue position, fake opponents, fabricated wait estimate, or silent region changes.
6. Search expansion messaging follows MatchmakingConfig. Public bot fill appears only after the configured threshold and bots are labeled [BOT]. Practice While Waiting appears only when implemented without losing the queue.
7. Reservation/load failure returns the party safely to search. Never begin a public match with an empty reserved slot or invent a winner on server failure.
8. Match Found and server connection state transition cleanly into the real eight-second preparation/loadout UI from Prompt 03.
9. Back, cancellation, app interruption, duplicate taps, stale async responses, and reconnect must leave one coherent current screen.

Do not restyle the entire game, change matchmaker rules, create a second queue, or implement contracts/shop in this task.

Validation:
- Backend party/matchmaking tests pass.
- Unity navigation and stale-async-response tests pass.
- Exercise create/join/ready/leave/leader transfer, cancel search, search failure, party drop, and reservation timeout.
- Run the existing six-client/dedicated-server acceptance workflow where the environment supports it; if external credentials/services block it, document the exact blocker and do not claim acceptance.
- Capture Unity screenshots of solo Party, 2/3, 3/3 ready, searching, 60-second wait choice, Match Found, and one recoverable failure.
- Report changed files, tests, network evidence, and remaining external blockers.
```

