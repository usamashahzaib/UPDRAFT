# Appearance account-data load state

## Rules applied

- Distinguish server-loaded inventory from profile equipment and level data that failed to load.
- Do not present the local equipment choice as freshly confirmed account state.
- Preserve the existing locally saved appearance fallback and inventory retry behavior.

## Implementation

When inventory loads but the profile request fails, the locker stays usable with its existing local appearance fallback. The status now says that equipped and level information could not be checked and identifies the displayed equipment as the last locally saved choice.

## Verified this pass

- Unity runtime C# compilation succeeded with 0 errors and 41 existing warnings.
- No tests, visual captures, or device checks were run.

## Unmet

- Confirm the fallback disclosure and layout in Unity across 16:9 and 20:9 and A/B/C profiles.
- Localization remains an A7 prerequisite because Unity Localization is not configured.
