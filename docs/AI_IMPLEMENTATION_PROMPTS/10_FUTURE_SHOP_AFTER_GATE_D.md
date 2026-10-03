# Prompt 10 — Future Shop, only after Gate D and purchase prerequisites

**Do not run this prompt now.** It exists so Screen 18 has an implementation destination after the required evidence exists.

```text
Before editing anything, verify from repository acceptance records that Gate D gameplay validation passed, CatalogConfig.shopEnabled was explicitly authorized, publisher-owned Play Console identity/signing exists, Google Play Billing is configured, and purchase verification/restore/refund/revocation testing is approved. If any prerequisite is missing, do not implement or enable the Shop. Report the blockers and stop.

When every prerequisite is satisfied, work in E:\Usama\Game on ProjectHotdrop and Backend. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md, ProjectHotdrop/PROJECT_UPDRAFT_SPEC.md, the monetization section of docs/HOTDROP_PRODUCT_AMENDMENT_2026-10-01.md, legal policies, CatalogConfig, billing mode, commerce endpoints, migrations, and tests.

Visual reference:
- UIUX/stitch_project_continuation_hub (6)/stitch_project_continuation_hub/screen_18_future_shop_concept_disabled_from_main_flow/screen.png

Redesign rather than copy. Remove Operations/Armory/Telemetry/Pass navigation, fake coins/gems, military requisition language, fake discounts/crossed-out prices, remote concept art, fake entitlement IDs, environment flags, and prices embedded in the mockup.

Implement only three permanent cosmetic bundles—Signal, Foundry, Skyline—with exact produced/licensed contents and Google Play-returned localized prices. Every item must have a catalog entry, ownership rule, preview on the real in-game model, and license/provenance record. Label COSMETIC ONLY and permanent ownership.

Purchase lifecycle must be backend-verified and idempotent: price loading, purchase started, pending, verified, acknowledged, failed, already owned, restore after reinstall, refund/revocation, network interruption, duplicate callback, concurrent request, and service unavailable. Never grant from client receipt claims alone and never show success before verification.

No premium currency, random reward, loot box, power, paid revive, paid respawn, forced ad, fake sale, countdown, scarcity, duplicate cosmetic, or mid-match purchase. Shop remains absent from Home unless separately approved after validation; open it only through the authorized product-shell route.

Validation must include backend/Google Play test purchase evidence, exactly-once grants, restore, refund/revocation, pending recovery, reinstall, offline failure, concurrent requests, legal copy, localized price handling, and real-device screenshots. Keep shopEnabled false until the complete lifecycle passes and the owner explicitly authorizes production enablement.

Report prerequisites, changed files, migrations, catalog/license records, tests, purchase evidence, flags, and every remaining blocker. Do not claim production readiness from mocks or sandbox success alone.
```
