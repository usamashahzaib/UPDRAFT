# Player lifecycle requirements

Status: target behavior for future slices. Existing build behavior remains live
where a capability below has not shipped. Numbers marked [ESTIMATE] require
telemetry or playtest before release tuning.

## Goal

Keep the path from launch to the next match short, while showing honest network,
download, maintenance, and account state.

## Required states and acceptance criteria

| State | Player-facing behavior | Acceptance |
|---|---|---|
| Cold launch | One short startup path; first session can enter practice before account linking. | No login or permission modal blocks first playable movement. Measure cold start on target phones. |
| Home | One clear Play action and visible practice, loadout, progression, and social entry points. | Return from match/results opens Home without duplicate panels. |
| Queue | Show practice while searching, elapsed time, honest region/latency, and retry/cancel. | Cancel stops queue polling; offline practice remains accessible. |
| Loading | Show map/mode information and addressable download progress when available. | Resumable downloads retain progress; never show a false match-found state. |
| Force update | Block stale client, show the required minimum protocol, and offer a configured HTTPS link to the matching platform store. If no release URL is configured, keep offline play and connection retry available. | Server minimum version gates requests; Android/iOS store URLs are supplied only from release configuration, never guessed by the client. |
| Optional patch | Show size, Wi-Fi-only preference, progress, pause/resume, and practice access. | Interrupted download resumes without restarting completed chunks. |
| Maintenance | Show server message and ETA when supplied; disable queue only. | Home and local practice stay usable. Never invent an ETA. |
| Offline/server down | Home keeps a visible availability state and refreshes it every 30 seconds. Queue-status checks retry transient network/server failures with capped exponential backoff while preserving Cancel and Practice actions. | No offline match facts are presented as server-verified rewards. Long-disconnect ticket recovery remains bounded by the server's ticket expiry. |
| Region | Auto-select a region and display measured ping; allow manual override. | Show current probe result and empty/error state; do not substitute a configured target for a measurement. |
| Match backgrounded | Keep reconnect window and bot substitution policy consistent with the server. | Rejoin does not create a second seat or duplicate rewards. Push is opt-in. |
| Season rollover | Present recap and new season introduction once, outside a match. | Reopening or reconnecting cannot repeat a claimed reward. |
| Account recovery | Explain guest-device risk; prompt to link at first purchase or level 5. | Account linking preserves guest progress and is idempotent. |
| Ban/penalty | Show reason, duration, and appeal path. | No silent matchmaking denial or shadow state. |
| Age gate | Ask age before social features; under-13 gets quick-chat only, no voice/free text. | Do not collect birth date beyond the approved age-check design. Legal review is required per launch region. |

## Decisions deliberately not inferred

The amendment does not define purchase compensation in maintenance, account
recovery proofing, the exact rejoin duration for the current product, or legal
age-check flow. Use existing backend contracts until each is specified.
Age-rating and local-law requirements are [VERIFY].
