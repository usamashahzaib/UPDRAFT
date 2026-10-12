# Part 2 match search palette acceptance

## Acceptance criteria

- Match Search surfaces, structural rails, primary action signals, destructive action signals, and text use the selected A/B/C profile's semantic UI tokens.
- Queue state, region, party readiness, search animation, and actions are unchanged by profile selection.
- The blueprint image and transparency masks retain their image-specific neutral tinting rather than adopting semantic text colors.
- Selecting another profile updates an already open Match Search screen on its next render/rebuild.
- Queue-seat state labels, the live squad occupancy heading, and the fairness/latency note use the secondary-text role so live status and queue guidance do not use the intentionally muted metadata role. Empty-seat placeholders and decorative venue metadata remain muted.

## Review still required

Review contrast and signal distinction for all three profiles, including color vision differences, in the game UI. This source change does not claim that review.

## 9 October source follow-up

`YOUR SQUAD (N/3)` reports live party occupancy, so it now uses the same
secondary-text role as the queue-seat state and fairness note. Empty-seat
placeholders and the decorative venue footer retain the muted role. Queue
membership, readiness, layout, and copy are unchanged. Runtime build succeeded
with 0 errors and 41 existing warnings. In-game contrast review remains open;
no tests or visual/device review were run.
