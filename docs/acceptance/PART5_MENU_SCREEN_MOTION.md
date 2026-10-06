# Shared menu screen motion acceptance

## Rules applied

- Shared menu screens enter with the Bible's slide and parallax treatment, using the established 180–240 ms ease-out motion tokens.
- Reduce Motion removes translation and uses a plain opacity transition.
- The menu backdrop remains opaque during normal motion; no fade-to-black transition is introduced.
- Product, home, startup, settings, runner-choice, online-status, offline-challenge, Preparation, Results, and Match Search screens use outgoing motion when closing; reusable roots can reverse an in-flight close without losing their resting layout.
- Navigation, input availability, screen contents, and gameplay behavior remain unchanged.

## Scope

The shared `MenuLayout.Create` factory covers Home, startup, account/product pages, offline challenge pages, runner choice, More, and the online status panel. Each direct visual layer enters at a different travel depth so the header, content, and footer move at parallax rates. Product-page replacement/back, startup progression/retry, Home/runner-choice dismissal, settings close, online panel dismissal, offline-challenge page changes, Preparation/Results dismissal, and Match Search dismissal also slide outgoing layers before hiding or disposal. Reused panels restore their captured resting positions after closing and can cancel a close if reopened quickly. Results is built once per match receipt and uses the same entry/exit component; replacing a result card destroys its predecessor immediately so two cards cannot overlap. Match Search is created once per queue attempt, updates its timer, region, party labels, and actions in place during polling, and exits when the queue flow leaves the search screen. Preparation enters once per phase, updates the countdown in place, and does not replay entry when its weapon cards rebuild after a selection.

## Verification

- Unity runtime project build passed after Results, Match Search, and Preparation transitions with 0 errors and 41 compiler warnings; warnings are existing deprecation and unassigned serialized-reference diagnostics.
- Focused `git diff --check` passed for Results, Match Search, Preparation, queue update, token, tracker, and acceptance edits.
- Existing motion and Reduce Motion coverage was read; tests are not run in this implementation pass.

## Unmet

- Verify page entry and rapid navigation in Unity at 16:9 and 20:9, including the active Reduce Motion preference.
- Preparation, Results, and Match Search entry/dismissal still need runtime inspection for countdown updates, scrolling, reward reveal, receipt updates, party changes, and immediate next-match actions. Tests were read and not run in this implementation pass.
- No device or platform verification is claimed.
