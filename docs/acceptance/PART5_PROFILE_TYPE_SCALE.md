# Operative Profile typography acceptance

## Rules applied

- Use the Bible typography scale, no more than three sizes per screen, and a 12 dp minimum.
- Preserve the level-first profile hierarchy, stream-safe masking, account-backed values, and explicit unavailable/loading states.
- Keep the summary/details toggle and its existing telemetry event.

## Scope and implementation

The profile uses only 12/17/30 dp caption, UI/body, and title roles in summary and expanded states. Inline font-size markup was removed from the level label. The permanent progression guarantee now uses the readable body role and a taller container; profile cards grew to contain it. No account values, privacy behavior, data sources, or toggle behavior changed.

## Verified this pass

- Existing profile navigation, capture, and raycast coverage was read; tests and captures were not run.
- Source scan finds no numeric font-size arguments or inline font-size overrides in `OperativeProfileView`.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:minimal` succeeds with 0 errors and 41 existing warnings.
- `git diff --check` passes for the files changed in this slice.

## Unmet

- Review profile summary and expanded card fit at 16:9 and 20:9, including long aliases and credit values.
- Verify A/B/C contrast and stream-safe rendering in Unity.
- Localization remains an A7 prerequisite because Unity Localization is not configured.
