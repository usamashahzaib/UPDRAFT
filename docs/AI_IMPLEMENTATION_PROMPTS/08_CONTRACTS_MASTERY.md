# Prompt 08 — Contracts, Flight Seals, and permanent mastery

```text
Work in E:\Usama\Game on ProjectHotdrop. Implement contracts and mastery behind disabled-by-default feature flags only after the core gameplay and online result pipeline are validated. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md, the exact contract/mastery rules in docs/HOTDROP_PRODUCT_AMENDMENT_2026-10-01.md, current backend architecture, and result idempotency tests.

Visual references:
- UIUX/stitch_project_continuation_hub (4)/stitch_project_continuation_hub/screen_12_contracts_flight_seals/screen.png
- UIUX/stitch_project_continuation_hub (4)/stitch_project_continuation_hub/screen_13_flight_log_mastery_tracks/screen.png

Redesign while implementing. Remove HOTDROP/HOTDROP duplication, Pass/Leaderboard/Armory navigation, protocol/doctrine language, fake rank/season/telemetry, purchasable-looking Seal exchange, Claim button when auto-grant is required, fake cosmetics, and any reward art without a produced licensed asset/catalog entry.

Backend first:
1. Versioned goal catalog and permanent mastery definitions controlled by the server.
2. Contracts unlock after two eligible public matches. Three templates: Finish Together 2 eligible matches; Hold the Relay 45 seconds of valid uncontested occupation; Build a Team Route two distinct human teammates using the player's owned pads.
3. Exactly one active contract; switching pauses progress without reset. Offline, bots for human-route count, self-use, contested/inactive/protected occupation, and duplicate match receipts do not qualify.
4. Each completed goal instance grants exactly one non-purchasable Flight Seal automatically. Use unique source/reward keys and transactional idempotency.
5. Six Seals unlock one exact choice from a real three-item cosmetic set. Do not expose a reward set until every asset/catalog entry exists. No random selection, duplicates, expiration, or premium purchase.
6. Permanent mastery: Relay Specialist 60/300/1200 valid seconds; Route Builder 3/12/40 qualifying human teammate uses; Weapon Familiarity per ARC/ZIP/BRICK 5/20/60 eligible majority-equipped matches. Cosmetic badges only.
7. Authenticated match server submits bounded facts. Client telemetry never awards progress.

Unity client after backend tests:
1. Contracts screen with locked, three-choice, active, paused, switching, completed, Seal granted, reward-choice pending, catalog complete, offline, loading, and retry states.
2. Mastery screen with three permanent tracks, accurate thresholds, What Counts, next badge preview, earned/equipped state, and no season reset/battle-pass presentation.
3. Integrate only real progress into Home, Results, Profile, and Appearance. Hidden feature flags must leave those screens clean.
4. Use HOTDROP language: Contracts, Flight Seals, Mastery, Relay Specialist, Route Builder, Weapon Familiarity. Avoid operative rank, doctrine, pass, requisition, and military copy.

Do not enable the feature in production, invent reward art, or change combat balance.

Validation:
- Backend unit/integration tests cover duplicate result, retry/outbox, concurrent submission, paused contract, new goal instance, bot/human distinctions, invalid facts, reward choice, and migration.
- Unity tests cover locked/active/offline/retry UI and feature-flag isolation.
- End-to-end staging evidence shows one match advances only eligible active contract/mastery facts and replaying the receipt grants nothing twice.
- Capture corrected Contracts and Mastery screens only with seeded staging data clearly identified in evidence, never as a production claim.
- Report changed files, migrations, tests, feature-flag defaults, catalog gaps, and remaining validation work.
```

