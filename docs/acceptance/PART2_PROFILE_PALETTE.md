# Profile palette acceptance

## Rules applied

- Profile backgrounds, panels, selection, structure, action, objective/reward, and text use the active A/B/C semantic palette.
- Stylized Flight text secondary is `#8A94A6` as specified by the Bible and Art Tokens; its muted role is derived from secondary text and the background so it remains visually distinct.
- `UIDesignTokens.TextMuted` now reads the active palette's muted role. Stylized Flight keeps the derived role (its authored tokens provide no separate muted value); Grounded Tactical Flight and Hybrid use their configured muted values. Existing-label profile recoloring and newly generated labels now use the same role value.
- Inline rich-text colors derive from the same tokens so the level, guarantee, and identity values follow the selected profile.
- Derived semantic tones are owned by the palette (`ActionPressed`, `HazardPressed`, `ActionObjectiveMix`, `WarningDim`) and are part of the profile-switch remap, so a live A/B/C switch cannot leave the previous profile's pressed or mixed hue on an already-open graphic.
- Settled two-line copy wraps inside its column instead of keeping the `UiDraw.Text` NoWrap+Overflow default, which the masked page then clips.
- Profile hierarchy, stream-safe masking, telemetry, account facts, and navigation behavior remain unchanged.
- The in-match pad charge fill uses neutral primary text in every team state; ally/enemy identity remains in the localized `ALLY PAD` / `ENEMY PAD` status label, so enemy red is not used as a button/control fill.

## Acceptance

- Unity runtime build succeeds after the profile token correction.
- Inspect collapsed and expanded profile in all three profiles at 16:9 and 20:9 for contrast, wrap, and clipping. Existing journey screenshots document the profile surface but do not cover all presentation profiles.

## Verified this pass

- Source review confirms the pad charge ring no longer maps enemy identity to an enemy-red control fill. The existing localized status text continues to identify the pad team; Unity contrast/readability review remains open.
- Source comparison confirms all 8 UI roles and 13 Switchyard world roles in
  each code profile match the corresponding values in `02_ART_TOKENS.json`.
- Source review found the previous global muted-text formula could diverge from `UiPresentationPalette.TextMuted`, leaving existing labels unmatched during a profile switch. The shared token now uses the active palette role directly.
- Source review found four derived semantic tones that were re-derived at their call sites instead of on the palette, so `RemapRichTextPalette`/`TryRemap` could not match them and an already-open graphic kept the previous profile's hue after a switch. `UIDesignTokens.ActionCyanPressed`/`HazardCoralPressed`, `GoalsPagesView.SecondaryAccent`, and `SupportHubView.SecondaryC` now read palette-derived values, and `TryRemap` covers them. The derived values are unchanged at build time; only live-switch remapping changes.
- Settled two-line copy in the settings presentation card and the Contracts/Mastery "Permanence Pact" note was placed with the NoWrap+Overflow default in narrow columns (roughly 420 and 354 units; the permanence note is 51 characters at 17 dp), so it ran past its column edge into the masked page. The permanence note now wraps with `TextWrappingModes.Normal` and TopLeft alignment in a two-line box. The settings card's copy was shortened in the working tree during this pass and now fits, so it needed no height change; it is recorded here as reviewed rather than fixed.
- Opaque-color sRGB contrast calculation is recorded for visual review. On the opaque background / panel / selected panel: A `#677080` is 3.75:1 / 3.28:1 / 2.54:1; B `#858D8C` is 5.36:1 / 4.46:1 / 3.44:1; C `#7F8CA3` is 5.23:1 / 4.30:1 / 3.37:1. The QA report applies a 4.5:1 text heuristic to primary and secondary roles on the opaque panel, but it does not cover the muted/inactive-metadata role and no threshold is stated for it; B and C authored muted values also sit just below 4.5:1 on the panel, so these figures stay review evidence and no colour was changed.
- The same opaque sRGB calculation gives primary/secondary text contrast on each profile's normal panel as A `14.54:1 / 5.35:1`, B `13.24:1 / 6.69:1`, and C `13.59:1 / 8.47:1`; all clear the recorded 4.5:1 heuristic on that surface. On the selected surface, secondary text is A `4.14:1`, B `5.16:1`, and C `6.65:1`. A's secondary token is explicitly authored as `#8A94A6`; this static result is retained for selected-row visual review rather than changing the required token. These calculations do not include translucent overlays or prove rendered/device contrast.
- Source review found queue-seat readiness (`WAITING`, `READY`, `NOT READY`), party readiness (`OPEN`, `NOT IN PARTY`, `NOT READY`), party-seat instructions/roles, room status, and the match-search fairness line were using the low-contrast muted role despite conveying live state or instructions. They now use the Bible's secondary-text role (body/captions); empty-seat placeholders, disabled-button labels, and decorative metadata remain muted.
- Follow-up source review found three live HUD facts still using the muted structural role: the next-objective rotation countdown, the player's current zone label, and teammate distance from the objective. They now use the active profile's secondary-text role. Timer, zone, roster wording, match rules, and A/B/C palette values are unchanged; no color-only cue was introduced. Runtime build passed with 0 errors and 41 existing warnings. Unity overlay/device contrast review remains open.
- 11 October follow-up audit found muted or structural colors on meaningful current-state, action, and product-copy labels: startup connectivity/recovery status and Home/startup secondary taglines, the Tutorial `SHOW ME` action, offline runner-choice reassurance/instructions and selectable framing tabs, the passive-HUD touch-zone instruction, Preparation's `UNLOCKED` badge, Mastery's `LOCKED` state, Profile's `HIDDEN` value, and the Results receipt/save note. These now use the active profile's secondary-text role. Remaining muted call sites are de-emphasized Preparation metadata, Match Search venue/caption and open-seat placeholders, Party Room labels and empty-seat/input placeholders, disabled button labels, Support form placeholders, inactive-row metadata, decorative outlines, and a runner hitbox note. Profile-choice labels and their inactive badge now use secondary text; their selectable state remains indicated without color alone by the card outline. No palette or copy changed.
- A Runtime C# sidecar build after these role changes succeeded with 0 errors and 41 existing warnings. No tests, Unity visual review, or device checks were run.
- Unity runtime C# build succeeded with exit code 0, 0 errors and 41 existing warnings after the derived-tone and wrap corrections (`Hotdrop.Runtime.csproj`).
- The Unity-generated Network build (which also compiles the Runtime dependency) succeeded with exit code 0 at a forced rebuild, 0 errors and 121 Unity/Fusion/runtime warnings (`Hotdrop.Network.csproj -t:Rebuild`).
- Both builds write restore artifacts under Unity's `Temp/obj/`, which Unity or an editor session can clear; run `dotnet restore <project> --ignore-failed-sources` immediately before building, or the build fails with `NETSDK1004` (missing `project.assets.json`), which is an environment state, not a code error.
- No tests, Unity visual review, or device checks were run.

## Unmet

- Cross-profile Unity visual/device review was not performed in this pass.
- The A muted role is notably lower-contrast over its panel in the static calculation, and all three profiles fall below 4.5:1 for it on the selected panel. It is now limited to intentionally de-emphasized metadata, empty-seat placeholders, disabled controls, and decorative outlines; the Bible does not define a muted-role threshold. Review its remaining uses in-game and revise only through reviewed visual acceptance. No palette colors were changed without an accepted threshold or art review.
- The wider wrap/clip review is source-only: every long-copy label whose box reserves more than one line already opts into `TextWrappingModes.Normal`, so no additional source-visible spill defect was found beyond the two fixes above. The remaining single-line NoWrap labels still need Unity inspection at the larger-text option and +30% expansion (`UIDesignTokens.TextExpansionTolerance`).
- The wrap scan was re-run against the current working tree after concurrent UI edits (which added wrap modes to several labels and new Support/Contracts copy). The scan still finds no long-copy label in a multi-line box without an explicit wrap mode; the two long-copy labels added since the first pass size their box from `UiDraw.Width(text)` and therefore cannot overflow. This remains a source heuristic, not a rendered measurement.
- Authored profile copy is now migrated to the configured `UIStrings` table.
  Unity import, translated copy, locale switching, and device acceptance remain
  open in `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md` and
  `docs/acceptance/A7_LOCALIZATION_PRODUCT_SCREENS.md`.
