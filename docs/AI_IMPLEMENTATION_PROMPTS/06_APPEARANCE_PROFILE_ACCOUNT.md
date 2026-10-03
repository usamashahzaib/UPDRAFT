# Prompt 06 — Appearance, Profile, Account, recovery, support, and reporting

```text
Work in E:\Usama\Game on ProjectHotdrop. Implement the production product-shell screens for Appearance, Profile, Account/Recovery, Support, Reporting, and deletion. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md and preserve the existing backend contracts and navigation tests.

Visual references:
- UIUX/stitch_project_continuation_hub (5)/stitch_project_continuation_hub/screen_15_appearance_locker/screen.png
- UIUX/stitch_project_continuation_hub (5)/stitch_project_continuation_hub/screen_16_operative_profile/screen.png
- UIUX/stitch_project_continuation_hub (6)/stitch_project_continuation_hub/screen_17_account_recovery_support_reporting/screen.png

These references need substantial correction. Remove military/operative/security-vault language, Mission Control/Operations/Armory/Telemetry/Pass navigation, fake ranks, fake currencies, fake encryption, fake 24/7 support, fake anti-cheat claims, remote concept art, and any profile statistic the backend does not provide. Use HOTDROP's calm Aerospace Utility Sport language.

Relevant implementation:
- Core/PlayerProductScreens.cs
- Core/BackendClient.cs, PlatformIdentity.cs, RecentMatch.cs
- UI/HomeCharacterPreview.cs and CosmeticLook.cs
- Player/BodyRig.cs and AvatarRigVisual.cs
- backend profile/inventory/account/support endpoints and tests
- PlayerProductScreensNavigationTests, BodyRigParityTests, CosmeticLookTests
- docs/legal policies for truthful deletion/privacy language

Architecture requirement:
- PlayerProductScreens.cs is already carrying multiple unrelated pages. Refactor incrementally into focused screen controllers/views only if tests preserve behavior. Do not perform a broad rewrite or break public entry points. Keep async responses versioned/cancel-safe so stale results never paint a closed screen.

Appearance:
1. Use the real rotatable in-game character and real weapon preview.
2. Categories: Body Presentation, Body Style, Weapon Wrap, Badge.
3. Body Presentation remains free and gameplay-neutral. Show Owned/Locked, equipped state, exact real unlock source, Preview, Equip, Saved, loading, retry, offline-cached, and unavailable states.
4. Do not show prices unless the gated shop is truly enabled with verified localized prices.

Profile:
1. Show alias, level 1–20, real XP/credits, equipped badge, eligible matches, wins/draws, objective contribution, real personal bests, mastery highlights only when implemented, and offline chapter completion only when implemented.
2. Keep hidden rating hidden. No age, gender, location, contacts, global rank, fake percentile, military rank, or loss-shaming.
3. Rename uses real validation and error recovery; never expose internal account ID.

Account/Recovery:
1. Clearly separate Guest and Linked states. Explain guest loss risk plainly.
2. Implement Link Google Play Games, Recover Linked Account, Revoke Link, optional recovery/deletion email, sync-pending, and retry using real APIs. Never claim cloud encryption or successful sync without confirmation.
3. Handle local/cloud conflict only according to existing backend behavior; do not invent merging.

Support/Reporting:
1. Privacy, Terms, Support and restore-purchase links only when real destinations/functions exist.
2. Report a recent participant by visible alias and slot. Do not require raw GUID in the normal flow. Use concise categories and bounded optional detail. Confirmation says Report received without promising punishment.
3. No Recent Match and Offline states must remain usable.

Delete Account:
1. Two deliberate steps. Explain deleted and legally retained data using the repository legal policy.
2. Keep Back/Cancel until final confirmation. Display real pending/completed/recovery status; never fake instant deletion.

Validation:
- Existing navigation/body/cosmetic tests pass.
- Add targeted tests for stale async response suppression, guest/linked states, report target selection, two-step deletion, and Back paths.
- Run backend tests for account, profile, inventory, reporting, and deletion.
- Capture Unity screenshots for Appearance, Profile, Guest Account, Linked Account, Report, and both deletion steps.
- Report changed files, tests, real backend fields used, and external Google Play blockers.
```

