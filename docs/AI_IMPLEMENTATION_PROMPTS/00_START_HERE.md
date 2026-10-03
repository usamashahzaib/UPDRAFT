# HOTDROP UI/UX implementation — start here

Run every coding task from `E:\Usama\Game`. Use one task/chat at a time, in the order below. Do not send all prompts to one AI message.

## Authoritative project

- Unity project: `E:\Usama\Game\ProjectHotdrop`
- Backend: `E:\Usama\Game\Backend`
- Product specification: `E:\Usama\Game\ProjectHotdrop\PROJECT_UPDRAFT_SPEC.md` (legacy filename; current product name inside the repository is HOTDROP)
- Product amendment: `E:\Usama\Game\docs\HOTDROP_PRODUCT_AMENDMENT_2026-10-01.md`
- Visual references: `E:\Usama\Game\UIUX`
- Product name to ship: `HOTDROP`

The HTML files are Stitch references. They are not production Unity code, gameplay truth, licensed game assets, or permission to download their remote images. Use `screen.png` for composition and `code.html` only to inspect measurements, colors, and copy.

## Visual audit

| Screens | Treatment |
|---|---|
| 01 Startup, 02 Appearance Choice, 03 Home, 04 Tutorial, 05 Party, 06 Matchmaking, 07 Loadout | Strong structural references. Correct copy and connect them to real state. |
| 08 HUD, 09 Pad Feedback, 10 Match Menu | Do not copy literally. Their lower dashboards, Telemetry/Operatives/Sector Map tabs, fake metrics, voice, dash, armor language, 120 FPS text, and invented weapons are review-sheet contamination. Use only the top gameplay composition and approved palette. |
| 11 Results through 17 Account | Redesign while implementing. They drift into HOTDROP military/operations language, excessive telemetry, Pass/Leaderboard navigation, invented currency, ranks, and inconsistent branding. Preserve useful information hierarchy only. |
| 18 Shop | Future-only. Keep disabled and disconnected until Gate D and purchase recovery validation pass. |
| Two military/gore logo PNG files | Reject. Do not import. They conflict with the non-militaristic sport identity and contain blood/gore. Use `docs/HOTDROP_LOGO_GENERATION_PROMPT.md` for a replacement. |

## Execution order

1. `01_UI_FOUNDATION.md`
2. `02_FIRST_RUN_HOME_TUTORIAL.md`
3. `03_GAMEPLAY_HUD_PAD_SETTINGS.md`
4. `04_RESULTS_AND_REMATCH.md`
5. `05_PARTY_MATCHMAKING_LOADOUT.md`
6. `06_APPEARANCE_PROFILE_ACCOUNT.md`
7. `07_OFFLINE_CHALLENGES.md`
8. `08_CONTRACTS_MASTERY.md`
9. `09_END_TO_END_QA.md`

Future-only, do not execute now: `10_FUTURE_SHOP_AFTER_GATE_D.md`.

Do not execute Shop/ads implementation. The release order remains: polished core → real-device validation → network acceptance → human gameplay validation → retention systems → monetization.

## Standing rules for every AI task

- Read repository instructions, current Git status, the authoritative spec, milestone status, and relevant tests before editing.
- Preserve all existing uncommitted work. Never reset, discard, overwrite, or regenerate the whole project.
- Use existing gameplay systems and ScriptableObject configuration values. Do not hardcode alternate rules to match a mockup.
- Keep HOTDROP branding. Remove UPDRAFT, military ranks, seasons, Pass, Leaderboard, Operations, tactical doctrine, fake telemetry, fake encryption, and developer build information from player UI.
- Keep the design language: `#101827` background, `#1B2940` panels, `#263A59` selected surfaces, `#F4F7FB` text, `#BBC6D8` secondary text, `#30D5E8` friendly/action, `#FF6376` enemy/danger, `#FFD166` objective/reward.
- Use TextMeshPro, Unity UI, nine-sliced sprites, layout groups, safe-area containers, and lightweight effects. No WebView and no runtime HTML/CSS/Tailwind.
- No unlicensed remote images. Create simple original vector/sprite geometry or use verified repository assets recorded in `ASSET_LICENSE_REGISTER.md`.
- Minimum touch size is 48dp. Support 16:9 through 20:9 landscape, cutouts, larger text, left-handed controls, and Reduce Motion.
- Do not alter combat balance, networking authority, progression rewards, or backend contracts merely to make a screen look complete.
- Add tests only where behavior or regression risk warrants them. Run existing relevant EditMode/PlayMode tests and compile before declaring completion.
- Show the final changed-file list, validation results, screenshots from Unity at representative aspect ratios, and exact remaining limitations.
