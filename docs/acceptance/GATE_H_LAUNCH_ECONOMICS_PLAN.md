# Gate H pre-registered launch-economics plan — data & balance desk

Prepared before any launch experiment exists. Same discipline as the Gate D/F/G plans: definitions
are fixed now; the numbers that fill them must come from measured production data. This desk
supplies the dashboard, the economics arithmetic, and the approval evidence bundles. It does **not**
certify Gate H, set budgets, choose markets, or operate infrastructure.

Standing rule adopted from the gate definition: **a failed launch experiment is not completion** —
correct, retest, or pivot; failed passes count under the two-failed-passes rule inherited from
spec §24/§26.

## 1. Measured real-cohort dashboard (all rates: numerator, denominator, exclusions, Wilson 95% CI)

Exclusions inherited from `GATE_D_ANALYSIS_PLAN.md` §2 (bot-substituted matches, incentivized
cohorts, duplicates, stale builds) apply to every balance/retention metric below.

| Metric | Source | Definition |
|---|---|---|
| Human/bot mix | `MatchmakingTicket.HasBots`, client envelope `botSubstituted`/`botCount`, server match facts | Share of public matches bot-filled; share of slots that are bots; substitution rate. Reported per scheduled window. Bot-filled matches stay excluded from balance/ELO conclusions. |
| Queues | Ticket `EnqueuedAt`→matched | Median + IQR during scheduled windows; share >45 s and >60 s; cancelled-queue share. The 45 s fill threshold is only revisited with this evidence plus the D-plan retention split. |
| Crashes | `client_error` + device-manifest ANRs | Crash-free session rate per device tier, RC build only, per G-G plan §3 definitions. |
| Rematches | `results_play_again_clicked` ÷ completed-match results views | Voluntary instant rematch rate ≥ 50% threshold from `KpiGateConfig`; per cohort. |
| D1/D7 (later D30) | `AnalyticsDailyPresence` (post-ingestion-fix) | Spec §167 windows from `first_open` anchor; never projected from shorter windows; per-cohort split incl. tutorial completers; n ≥ 30 per cell. |
| Purchase reliability | `PurchaseTransaction` statuses + `purchase_started`/`purchase_verified` | Verified ÷ started; pending-resolution time; duplicate/refund/revocation handling rates. **Blocked until the Gate F verification TODO and RTDN path exist** — no reliability number can be produced from a stub. |
| Cost per match | `MatchAllocation` server-occupancy spans × measured $/server-hour + DB/hosting/monitoring/support amortized per §29 cost list | Occupancy-based, not calendar-based; a server idling at capacity cap is billed to the launch experiment, not hidden. |
| Support load | `PlayerReport` + deletion requests | Reports per 1,000 DAU; deletion-request rate; time-to-first-human-response; top report reasons. Spec §205 "someone reads reports before launch" is an evidence item, not a formality. |

## 2. Economics: net LTV vs CPI (spec §30 arithmetic, fixed now)

Net LTV per acquired player = gross revenue per player, **minus** refunds/chargebacks, **minus**
platform cut (store commission on the same revenue), **minus** service costs per player (share of
dedicated-server compute + backend/DB + backups + monitoring + support attributable to acquired
cohorts, per the §1 cost-per-match pipeline), **minus** revenue-share/SDK fees where applicable.
Only the remainder is LTV for the decision rule.

Decision rule: scale spend **only when net LTV per acquired player > CPI for that source, with
margin**, measured over ≥ 28-day cohorts; margin size is a budget-owner decision, not mine.
Explicitly prohibited inputs (per spec §30 and the desk charter): ad spend compared to **gross**
sales; LTV projections not anchored on measured retention curves; any revenue assumed from
unshipped monetization. Ads/purchases remain off until Gate D and F pass — early cohorts therefore
have near-zero revenue, and the desk will refuse any LTV claim built on them.

Capacity sizing for the budget: ACU = DAU × avg daily minutes ÷ 1440 (spec §29), with per-6-player-
match CPU/RAM/bandwidth benchmark measured **before** capacity is bought; peak factor measured, not
assumed. Controls verified as evidence, not promises: max simultaneous servers configured, clean
rejection at cap observed in a test, billing alerts active, no uncapped autoscaling.

## 3. What my explicit approval of market / budget / max capacity / spend requires (evidence bundles)

The gate text makes this desk's approval an input to PASS. My approval can only attach to
documented evidence, and **none of the four bundles can be completed today**:

1. **Market** — concentrated-market selection memo with measured RTT sample from the actual target
   market (STEP_E records region `eu` as an unmeasured config value), cohort-recruitment plan, and
   the D-study device-matrix footprint for that market. Status: not measurable.
2. **Budget** — itemized monthly operating budget per spec §29 line items, with the capacity
   benchmark behind it and named billing-alert thresholds. Status: no deployment exists to cost.
3. **Max capacity** — the measured per-match resource benchmark, the configured server cap, and a
   captured clean-rejection-at-cap test. Status: six-client Gate C acceptance itself is not yet
   done; capacity benchmarking cannot precede it.
4. **Spend** — CPI by source and 28-day net-LTV-per-source against the §2 arithmetic, from a live
   store listing with working purchase verification. Status: blocked by Gate F's verification stub
   and by shop/ads being correctly disabled pre-D.

Until each bundle exists, the desk's recorded position is **"cannot approve — evidence absent"**,
which is different from "approved" and different from "rejected".

## 4. Staged rollout and rollback gates (pre-registered ladder)

Stage promotion requires the prior stage's full-window evidence, with denominators:

- **S0 closed test** — 12 testers × 14 days (spec §203), all inherited suites rerun on the frozen
  RC (G-G plan §3 rule: one revision, one captured run per suite).
- **S1 limited scheduled sessions** — the spec §29 "limited scheduled tests" posture; KPIs over
  real windows only (D1 measurable, D7 awaited, never projected).
- **S2 staged percentage rollout** — promotion requires: crash-free ≥ 99.5% on the stage's device
  mix, median queue < 45 s in scheduled windows, purchase-reliability pipeline live, no open
  critical incidents, support load trended and read.
- **S3 spend scaling** — §2 rule per source; any cohort failing D1 ≥ 35% or crash-free ≥ 99.5%
  freezes further spend until corrected and retested.

Rollback triggers (any one freezes promotion; kill switches are the immediate lever):
crash-free below threshold on any tier, purchase-failure spike above the F plan's funnel-leak
alert, queue median above 45 s sustained, refund/revocation handling failures, data-safety or
privacy-parity regression (G-G plan §3), or a critical vulnerability in an inherited dependency
(the NU1903 `SQLitePCLRaw.lib.e_sqlite3` finding remains open and is a launch blocker until
resolved). Rollback must be rehearsed evidence per G-G B3/B5 — an unrehearsed plan is a gap.

## 5. Publisher-owned handover ledger (spec §32, per deliverable)

Full source repo · editable Unity project · backend source · DB migrations · build scripts ·
deploy config · lockfiles · asset-license register (complete, all rows verified-commercial per
G-G B8) · balance config files (versioned) · test results (frozen-RC runs) · device perf report
(four-device matrix) · setup docs · known-issues list · account/key inventory (publisher-owned;
no dev-held credentials; the current placeholder app ID and empty keystore fields per G-G B1 must
be resolved to final publisher-owned values first). Handover is complete only when every row has
an evidence link and the publisher confirms access — not when files are "provided".

## 6. Out of scope for this desk

Certifying Gate H, choosing the market or marketing budget, setting price/cap values, approving
store submission, operating production infrastructure, and any pay-to-win lever (the catalog stays
cosmetic-only; this desk rejects commerce changes touching combat power).
