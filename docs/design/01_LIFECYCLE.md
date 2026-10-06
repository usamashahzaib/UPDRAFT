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
| Maintenance | Show the operator-supplied message and ETA when supplied; show an optional operator-supplied compensation note without inferring an amount or entitlement; disable queue only. | Home and local practice stay usable. Never invent an ETA or compensation offer. |
| Offline/server down | Home keeps a visible availability state and refreshes it every 30 seconds. Queue-status checks retry transient network/server failures with capped exponential backoff while preserving Cancel and Practice actions. | No offline match facts are presented as server-verified rewards. During a live match, the client repeats the authenticated reconnect check and requests fresh short-lived join tickets while the server reservation remains eligible. The 90-second ticket expiry bounds each credential, not the reconnect window. |
| Region | Auto-select a region and display measured ping; allow manual override. | Show current probe result and empty/error state; do not substitute a configured target for a measurement. |
| Match backgrounded | Keep reconnect and bot substitution policy consistent with the server. The current server uses the reserved match slot, substitutes a labeled bot after the configured grace (currently 5 seconds), and allows that account to reclaim the same avatar until the match ends, unless server-side inactivity removal is final. | Rejoin does not create a second seat or duplicate rewards. Each join ticket expires after 90 seconds and is refreshed by the reconnect loop; this is not a fixed 90-second reconnect window. Mobile OS background socket behavior and this flow still require device/network acceptance. Push is opt-in. |
| Season rollover | Present recap and new season introduction once, outside a match. | Reopening or reconnecting cannot repeat a claimed reward. |
| Account recovery | Explain guest-device risk; prompt to link at first purchase or level 5. | Account linking preserves guest progress and is idempotent. |
| Ban/penalty | Show reason, duration, and appeal path. | No silent matchmaking denial or shadow state. |
| Age gate | Ask age before social features; under-13 gets quick-chat only, no voice/free text. | Do not collect birth date beyond the approved age-check design. Legal review is required per launch region. |

## Decisions deliberately not inferred

The amendment does not define the amount or entitlement for purchase compensation
in maintenance; the UI may display only an operator-supplied note. It also does
not define account recovery proofing, a separate fixed rejoin duration for the current product,
or the legal age-check flow. For reconnects, use the current backend reservation
contract through match completion and the server's existing inactivity-removal
rule; do not treat the 90-second join-ticket lifetime as that window.
Age-rating and local-law requirements are [VERIFY].
