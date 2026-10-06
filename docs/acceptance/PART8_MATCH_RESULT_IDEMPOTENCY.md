# Concurrent match result retry acceptance

## Goal

Keep authoritative match result submissions idempotent when duplicate server requests overlap.

## Player-facing behavior

Players receive one match participation, progression update, ledger grant, and cosmetic unlock for their match. A repeated result submission succeeds once the original durable receipts are present; it does not apply rewards again.

## Configured values

No new balance or retry timing values are introduced. Existing unique keys on match participation, credit ledger entries, and inventory grants remain the persistence guards.

## States and edge cases

- A fresh submission follows the existing authoritative result path.
- An already recorded submission produces no new progression changes.
- A concurrent database update conflict is treated as idempotent success only after every participant in that request has a stored receipt.
- If any participant receipt is still absent, the original persistence error is rethrown.

## Acceptance

- Result submission remains restricted to the authenticated game-server service principal.
- The completion processor checks durable per-player participation records before applying rewards.
- After a persistence conflict, the processor clears failed tracked state and checks durable receipts before suppressing the conflict.
- Existing sequential duplicate-result coverage was read; automated tests and concurrent database integration checks were not run.

## Telemetry

When a concurrent conflict is reconciled to already-persisted receipts, the backend emits the structured `match_result_duplicate_reconciled` log with match id and participant count. Player identifiers and reward contents are not logged. Existing match result and completion telemetry remain unchanged.

## Unmet

- Concurrent Postgres and dedicated-server retry acceptance still requires integration and network verification.
- No new automated regression test was added or run in this implementation pass.
