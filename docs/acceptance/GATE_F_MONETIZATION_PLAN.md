# Gate F pre-registered monetization measurement plan — data & balance desk

Prepared **before any monetization event exists** (verified 2026-09-28: `CatalogConfig.shopEnabled = 0`,
`AdConfig.rewardedAdsEnabled = 0`, backend `Features:PurchasesEnabled = false`, zero `purchase_*`
telemetry rows in the repo). Same discipline as `GATE_D_ANALYSIS_PLAN.md`: definitions are fixed
now so numbers cannot be bent later. This desk produces numbers and proposals only — it never
certifies Gate F and never approves enabling production payments; that is the auditor's recorded
decision.

## 0. Implementation-state evidence I inspected (source only — nothing is accepted)

| Gate F requirement | What exists | Desk assessment |
|---|---|---|
| Non-deprecated purchase SDK | `com.unity.purchasing 4.15.1` in `Packages/manifest.json` (embeds Google Play Billing 8.0.0 per its changelog). No client store code exists yet — no `IStoreListener`/`UnityPurchasing` references in `Assets/Scripts`. | IAP 4 is deprecated as of 2026-06-08 (Unity end-of-support announcement); IAP 5 is the support path. **Proposed input to implementers:** target IAP 5.x, not 4.x, so the integration does not land on an unsupported major. Play Billing 8 itself meets Google's current floor; re-check the floor at implementation time. |
| Three bundles only | `CatalogConfig`: exactly `signal` ($1.99), `foundry` ($3.99), `skyline` ($5.99), cosmetic-only, no premium currency. | Matches spec shape. Localized price must come from the store at runtime (`referenceUsdPrice` is reference only) — verify at implementation. |
| Previews | Contents listed as strings in `CatalogConfig`; a localized concept-only shop preview now exists in `ShopConceptView`. | No store-backed, on-device bundle preview or purchase flow exists. Gate F measurement starts only after approved SKU/content mapping and the store-backed surface is implemented. |
| Backend verification | `CommerceEndpoints.verify-purchase` has an explicit **TODO**: it stores the transaction as `pending_verification` and **never calls the Google Play Developer API**. | Correct fail-safe direction (nothing grants entitlements), but Gate F cannot pass with the TODO in place. This is the single largest F blocker. |
| Exactly-once groundwork | Idempotency lookup by `GooglePurchaseToken`, a unique EF model index and checked-in database migration; concurrent duplicate inserts now re-read the committed transaction and return `alreadyProcessed`. | Pending-only scaffold still does not validate receipts or grant entitlements. Keep purchases disabled until Developer API verification, acknowledgement, refund/revocation handling, linked-identity checks, and lifecycle acceptance are implemented. |
| Pending/ack/restore/refund/revoke | `PurchaseTransaction.Status` enum covers `pending/verified/acknowledged/refunded/revoked`; `/restore` returns inventory rows. **No RTDN/webhook endpoint exists** (searched `voided|RTDN|webhook|developerNotification` — zero hits); no code transitions to `refunded/revoked`; entitlements are never removed on refund. | Refund/revocation is currently **detection-less**. Gate F explicitly requires it. Needs Google Play RTDN (pub/sub) or a scheduled voided-purchase sweep before acceptance. |
| Linked identity required | Purchase endpoints sit behind `RequireAuthorization`; Google identity linking exists in `AuthEndpoints` (link/revoke). | `verify-purchase` now checks the persisted, server-verified `LinkedPlatformIdentity` after the purchases-off gate and rejects guest/unlinked sessions. The live Google identity and purchase lifecycle still require configured provider credentials and acceptance. |
| Rewarded ads: opt-in, skippable, capped | `AdConfig` caps: crate roll 1/day, credit bonus 20 credits 3/day, cosmetic trial 24 h 1/day. Hard rules encoded as constants: `RewardedOnly`, `Skippable`, `NoForcedInterstitials`, `NoMidMatchAds`. **No ad SDK in the manifest, no ad code anywhere.** | Policy shape is right and kill switches exist server-side (`Features:PurchasesEnabled`) — but there is no ad **kill switch field** (`rewardedAdsEnabled` is a build-time client asset, not remotely toggleable). Propose a server-side ads flag alongside `PurchasesEnabled` so no-fill/quality incidents can be killed without a client release. |
| Purchase-off defaults | `shopEnabled = 0`, `rewardedAdsEnabled = 0`, `PurchasesEnabled = false`. | Correct posture. Keep until Gate D passes and F acceptance is recorded. |

### Google Play integration prerequisites (2026-10-09)

- The current backend has Google Play Games identity OAuth settings only; it has no Android Publisher package name or service-account credential configuration. Those identity credentials do not authorize purchase verification.
- The Unity catalog defines internal bundle IDs, reference USD prices, and human-readable content descriptions. It does not define verified Google Play product SKUs or a mapping from each bundle to the server's canonical cosmetic IDs. Do not infer SKUs from internal bundle IDs or grant items from display descriptions.
- Google's current `ProductPurchaseV2` verification call requires the app package name and purchase token and returns product line items, purchase state, and acknowledgement state. The server must compare returned product IDs and a `PURCHASED` state before treating a token as valid. See the [official getproductpurchasev2 reference](https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.productsv2/getproductpurchasev2).
- Until package/SKU configuration, service-account access, canonical bundle entitlement IDs, and the grant-before-ack flow are supplied and verified, keep `PurchasesEnabled` false and do not acknowledge or grant a purchase. This is a concrete configuration/catalog prerequisite, in addition to the outstanding legal and release acceptance.

### Purchase-token race handling (2026-10-07)

- `HotdropDbContext` and the initial database migration already define a unique index on
  `PurchaseTransaction.GooglePurchaseToken`; the earlier note that the index was missing was
  stale.
- `CommerceEndpoints.verify-purchase` now handles a concurrent duplicate insert by clearing the
  failed pending entity from the request context and re-reading the winning row. If that token
  exists, it returns an idempotent response only when the account and product match the stored
  transaction; a token already bound to another account or product returns a generic 409 conflict.
  If no row exists, the original database failure propagates. The handler still grants no
  entitlement, and the purchase feature flag remains required.
- Once that feature flag is enabled, the endpoint rejects null/blank request fields and unknown
  product IDs, using the same exact three bundle IDs as `/catalog`; opaque purchase tokens are not
  rewritten or trimmed.
- Once enabled, the endpoint also requires the account's persisted `LinkedPlatformIdentity`; the
  link endpoint sets it only after server-side Google Play Games verification, and unlink clears it.
  Purchases remain disabled by default.
- The existing `CommerceEndpointsTests` cover sequential duplicate requests, not simultaneous
  requests or invalid request cases. Backend compilation succeeded with 0 errors and one `NU1900`
  vulnerability-feed warning; no tests were run for this change.

## 1. Monetization KPI definitions (pre-registered, measured only after Gate D passes)

Every rate carries numerator, denominator, exclusions (bot-substituted, incentivized cohorts,
duplicates — same rules as the Gate D plan), and a Wilson 95% interval. Minimum cell: n ≥ 30
exposed users before a rate is treated as signal; below that, "insufficient data".

- **Purchase conversion**: users with ≥1 verified purchase ÷ users exposed to the shop (shop shown = `/catalog` fetch + shop screen open event), 28-day window, per cohort. Funnel reported stepwise: shop exposed → bundle previewed → `purchase_started` → verified. Where the funnel leaks (e.g. started-but-never-verified) is a required line, not optional.
- **Bundle mix**: verified purchases per bundle ÷ total verified purchases (n = purchase count). Signal to watch: if the $5.99 tier takes most volume, the price ladder is mis-set; if $1.99 dominates with near-zero higher tiers, consider whether the ladder has a step problem before touching prices. No price changes proposed from fewer than 100 verified purchases.
- **Ad fill rate**: `rewarded_show_completed` ÷ `rewarded_offer_shown` (a requested-but-unfilled ad shows as offer shown + no-fill reason in payload; never silently counted as viewed). No-fill reason breakdown (network, no-inventory, timeout) is mandatory.
- **Voluntary ad engagement**: `rewarded_offer_shown` ÷ eligible offer impressions (offer surfaced), and completion-through (skip events ÷ show events) to prove skippability in practice.
- **Reward-claim caps**: claims per user per day per reward type vs `AdConfig` caps (1 / 3 / 1). Cap-hitting distribution (share of active users reaching each cap) — this is an LTV input, not a tuning target; caps are only changed for economy-abuse evidence, never to raise engagement.
- **LTV vs CPI (Gate H economics input)**: gross revenue per install (verified purchase revenue + ad revenue per install) vs CPI by source. Formulas fixed now: LTV inputs = ARPDAU × expected lifetime from measured retention curves (Gate D cohort data), ad revenue = impressions × eCPM. Gate H viability verdicts reference these inputs; the desk does not set CPI bids or marketing budgets.

## 2. Telemetry additions required before measurement is possible (proposal to implementers)

Current approved event sets in `AnalyticsEndpoints.cs` include `purchase_started` (client) and
`purchase_verified` (server) — insufficient for the funnel above. Required additions, proposed
**as proposals only** (implementers own the code):

- Client: `shop_opened`, `bundle_previewed`, `purchase_failed` (with failure reason), `rewarded_offer_shown`, `rewarded_ad_completed`, `rewarded_ad_skipped`, `rewarded_ad_nofill` (with reason), `reward_claim_granted` (reward type).
- Server: `purchase_acknowledged`, `purchase_refunded`, `purchase_revoked`, `entitlement_revoked` — the refund/revocation states currently have no corresponding event at all.
- All events carry the mandatory envelope (build/balance version, pseudonymous ID, cohort, etc.) per spec §164.

Without these, ad fill rates and refund impact are unmeasurable — flagging now, before any
ad SDK is integrated.

## 3. Explicitly out of scope for this desk

- Setting or changing prices, cap values, or reward amounts (proposals only, evidence-based).
- Certifying Gate F; approving `PurchasesEnabled = true` in production.
- Choosing an ad network, SDK version, or store mediation setup (auditor/implementer territory).
- Any pay-to-win lever: catalog is cosmetic-only and permanent; the desk will reject any
  proposal that touches combat power through commerce.
