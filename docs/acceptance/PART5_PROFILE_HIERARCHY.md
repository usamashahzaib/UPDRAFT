# Part 5 profile hierarchy acceptance

## Acceptance criteria

- The collapsed profile presents the level as its single hero progression value.
- The collapsed career summary contains exactly two secondary metrics: eligible matches and win rate.
- Draws, credits, private rating, offline accomplishments, equipped loadout, and the personal growth record are available behind a clearly labeled details toggle.
- The toggle has a minimum 48 px hit target and can return the user to the summary.
- The persistent stream-safe setting continues to mask account aliases, progression, currency, and other protected values in both states.
- Return to Home and Edit Appearance remain available in the sticky footer in both states.

## Telemetry

Record `profile_details_toggled` with one boolean field, `expanded`, only when the user changes the view. Use the existing local telemetry pipeline and its pseudonymous metadata. Do not include account identifiers, aliases, stat values, or cosmetic selections.

## Review still required

Inspect the hierarchy and 48 px target in Unity at 16:9 and 20:9 layouts. Verify stream-safe capture behavior and physical-device presentation before marking the profile work fully accepted. No localization framework is configured; localized player-facing copy remains a project prerequisite.
