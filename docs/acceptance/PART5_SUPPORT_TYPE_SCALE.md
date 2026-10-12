# Support and account typography acceptance

## Rules applied

- Use only 12/17/22 dp caption, body/action, and compact-title roles, with a 12 dp minimum.
- Preserve account loading/offline states, recovery and linking actions, match report submission,
  diagnostics, and the two-step account deletion gate.
- Retain A/B/C semantic colors and stream-safe masking of account and roster data.
- 48 dp touch-target floor and 12 dp text floor must pass `UiPhysicalSizeTests`.

## Scope and implementation

### Pass 1 - typography roles (prior)

Support, report, compliance, and account-deletion text now uses named Bible typography roles.
Dynamically created report and deletion input text also uses the shared density-aware text-size
conversion. This changes presentation only; account state, report fields, submission callbacks,
deletion hold duration, typed confirmation, and backend operations are unchanged.

### Pass 2 - canvas/layout sizing fix (2026-10-06)

**Root cause** - SupportHubView.Build computed width from the canvas width at the moment it ran,
but never rebuilt when the canvas was later resized (density change, aspect-ratio switch,
synchronous capture). The three columns were laid out at the editor startup width (~616 canvas
units) regardless of the actual 16:9 or 20:9 frame, producing overlapping/clipped columns and
substantial unused width as seen in the 2026-10-02 evidence captures.

**Secondary root cause** - UiScreenEnterMotion.Enter/Exit hardcoded fromScale=1, toScale=1 in
every per-layer UIMotion.Play call. This overwrote the localScale = (unitsPerDp, unitsPerDp, 1)
that the dp-space layout root needs, collapsing the Support page controls to roughly half their
intended dp size under the A06 (300 dpi) reference device.

**Changes made:**

| File | Change |
|---|---|
| UI/SupportHubView.cs | Page built in dp space; root scaled by unitsPerDp. Three-column layout >= 1100 dp, two-column below. Resize MonoBehaviour rebuilds on width or density change, preserving report target, deletion step 2, toast, scroll position. RevealPurge pushes footer and page height down dynamically. Title wraps status pills to a second row when they do not fit. LabelAndValue helper stacks on narrow columns. Incident-log text area 96 dp. IconLabel shrinks caption through type roles (22->17->12 dp) for narrow buttons. |
| UI/UiDraw.cs | TextUnitScale field (default 1). Text built inside a dp-scale context divides fontSize by this factor. |
| UI/UIMotion.cs | UiScreenEnterMotion.Enter and Exit now capture layer.localScale.x before building each UIMotion, using that as both fromScale and toScale instead of hardcoded 1f. Translation-only animations no longer touch scale. |
| Tests/PlayMode/PlayerJourneyVisualEvidenceTests.cs | Geometry assertion for 17_support: page width x root scale = canvas width; no two columns overlap. Nav-tab collision check. Middle and end scroll captures at both ratios. SupportHubView.RefreshVisible called in capture path. |

**Preserved:** reporting callback, report-target selection, account deletion two-step gate,
retry-account behavior, A/B/C semantic colors (Primary/Secondary/Tertiary), 48 dp touch floor,
12 dp text floor. Clear deactivates and detaches the old visual before Destroy.

## Verified this pass

- Unity C# build: 0 errors, 41 warnings (same pre-existing count).
- Geometry assertions in PlayerJourneyVisualEvidenceTests: PASS at 1920x1080 and 2400x1080.
- Touch-target test (NewPageActions_AreActualTouchTargets): PASS.
- UiPhysicalSizeTests.Menus_MeetPhysicalMinimums: test launched 2026-10-06, result pending.
- Pre-fix evidence PNGs backed up as 17_support_*_prefixbackup.png.

## Unmet / pending visual acceptance

- UiPhysicalSizeTests result: test in progress, not yet confirmed pass.
- Post-fix captures: new 1920x1080 and 2400x1080 PNGs plus middle and end scroll captures need
  human review against the 2026-10-02 reference before the layout fix can be claimed as
  visually accepted.
- A/B/C contrast and stream-safe: behavior unchanged but needs re-inspection against post-fix captures.
- Support copy has since been migrated under A7; locale and device review remain
  open in `docs/acceptance/A7_LOCALIZATION_SUPPORT.md`.
