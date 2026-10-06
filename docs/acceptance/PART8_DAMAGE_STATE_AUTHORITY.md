# Authoritative damage mutation acceptance

## Goal

Make server authority explicit at every player/pad damage and elimination mutation point.

## Player-facing behavior

Valid server-resolved shots and eliminations behave as before. Calls without state authority or with non-finite/non-positive damage leave health, pad state, and elimination state unchanged.

## Configured values

No damage or health values changed; weapon damage and health remain in the existing authoritative config assets.

## States and edge cases

- Player damage requires state authority, a live match, a living unprotected target, and a finite positive amount.
- Pad damage requires state authority, a live match, an active enemy pad, and a finite positive amount.
- Elimination, shot queueing, pad spawning, and pad-use/destruction facts require matching state authority and runner ownership.
- Bot substitution, respawn, weapon-time facts, and pad initialization also require state authority; invalid or non-positive weapon-time increments are ignored.
- Friendly pad damage and existing spawn-protection behavior remain unchanged.

## Acceptance

- Source review confirms `NetMatch.FixedUpdateNetwork` resolves shots only on state authority.
- Public mutation methods independently guard authority and runner ownership where needed; pad event collections are exposed read-only to result submission code.
- Existing combat/authority tests were read but not run; runtime compilation was used for this slice.

## Telemetry

No new event is emitted. Existing `pad_placed`, `pad_used`, and `pad_destroyed` facts are appended only by state-authority code.

## Unmet

- Automated negative/non-finite damage tests and multi-client authority verification were not run.
- Six-client and device combat acceptance remain open.
