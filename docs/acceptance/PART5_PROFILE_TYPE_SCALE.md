# Operative Profile typography acceptance

## Rules applied

- Use the Bible typography scale, no more than three sizes per screen, and a 12 dp minimum.
- Preserve the level-first profile hierarchy, stream-safe masking, account-backed values, and explicit unavailable/loading states.
- Keep the summary/details toggle and its existing telemetry event.

## Scope and implementation

The profile uses only 12/17/30 dp caption, UI/body, and title roles in summary and expanded states. Inline font-size markup was removed from the level label. The permanent progression guarantee now uses the readable body role and sizes its container from TMP's preferred wrapped height; the card and following summary row expand to contain it. The accreditation heading and phase label are trimmed to their reserved widths. The expanded metric-card footer labels now occupy non-overlapping, side-by-side boxes; metadata, aliases, XP, and credit text are constrained to their own boxes using the current type role. No account values, privacy behavior, data sources, or toggle behavior changed.

A saved A/B/C profile capture showed the badge field repeating its slot name ("BADGE: BADGE: ..."), because the generic cosmetic display name included both a slot prefix and value. The profile now uses the value-only cosmetic name while other screens retain the descriptive display name. The accreditation header is shortened to "LEVEL PROGRESS" and the "NEVER RESETS" label receives a wider reserved box so both labels can fit without clipping.

## Verified this pass

- Existing profile navigation, capture, and raycast coverage was read; tests were not run. Inspection of the saved 7 October A/B/C captures found the guarantee text painting below its fixed card, the accreditation heading colliding with the phase label, and metric footer labels crossing card boundaries. The source now measures the wrapped guarantee and grows the progression card and following row; headings, profile fields, metric labels, and values are clipped to their boxes, and metric footer boxes no longer overlap.
- Source scan finds no numeric font-size arguments or inline font-size overrides in `OperativeProfileView`.
- `dotnet build ProjectHotdrop/Hotdrop.Editor.csproj --no-restore -m:1 -v:q` succeeds with 0 errors and 41 existing warnings after the profile layout repairs.
- `git diff --check` passes for `OperativeProfileView.cs` and this acceptance note.

## Unmet

- Review a fresh profile summary and expanded capture at 16:9 and 20:9 after the layout repair, including long aliases and credit values; no fresh capture or device check was run.
- Verify A/B/C contrast and stream-safe rendering in Unity.
- Profile strings use the configured Unity Localization lookups and English seed path. Editor table seeding, translated layout, Urdu RTL, and Urdu/Devanagari fonts remain open under A7.
