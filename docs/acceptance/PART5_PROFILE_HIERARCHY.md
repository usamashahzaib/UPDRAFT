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

## Verified this pass

- The existing 1920×1080 profile capture showed the two identity-card footer labels overlapping. Their widths now divide the available row evenly with a 20 px gap at the captured card width; no post-fix Unity capture was made.
- Profile labels now use plain language in runtime fallbacks and the English seed: “BADGE,” “NEVER RESETS,” “ELIGIBLE MATCHES,” “MORE STATS,” “EQUIPPED ITEMS,” “BODY STYLE,” “WEAPON WRAP,” and “PROFILE BADGE,” alongside the private-rating, verified-record, and offline-achievement labels. The guarantee still explains that starter level never resets between seasons and only verified match results add progress. Profile and offline-challenge chapter names now consistently say “ROOFTOP RUNNER.” This applies Bible Part 0 §0.3 (no fake jargon) while preserving the same data, privacy, and progression rules.
- Runtime fallbacks, seed values, and serialized English rows match for all 20 Profile/shared chapter changes and four Support navigation changes; their existing localization IDs are unique and preserved. The Editor project build succeeded with 0 errors and 41 existing warnings. No tests or new captures were run.
- Runtime C# compilation succeeds with 0 errors and 41 existing warnings. A static comparison found all 1,368 compiled English seed entries equal their serialized Unity table values, with 1,368 unique IDs and no mismatches. No tests or new captures were run.

## Review still required

Inspect the hierarchy, revised labels and guarantee copy, text wrapping, and 48 px target in Unity at 16:9 and 20:9 layouts. Verify stream-safe capture behavior and physical-device presentation before marking the profile work fully accepted. Unity Editor import/re-save, translated copy, locale switching, and device acceptance remain open in `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md` and `docs/acceptance/A7_LOCALIZATION_PRODUCT_SCREENS.md`.
