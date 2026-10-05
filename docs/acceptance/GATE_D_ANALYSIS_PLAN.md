# Gate D pre-registered analysis plan — data & balance desk

Author: data & balance analyst role. This file pre-registers, **before any tester data
exists**, how Gate D numbers will be computed so choices cannot drift after results
arrive. It records no KPI results and proposes no config changes — there is no dataset
to compute from (checked 2026-09-28: zero `.jsonl`, `.csv`, `.db`, `.sqlite`, `.parquet`
or `.tsv` files in the repo; no `analytics_queue.jsonl`; no backend event exports).

Authoritative sources for every rule below:
- `ProjectUpdraft/PROJECT_HOTDROP_SPEC.md` §164 (event list + mandatory fields), §166–167 (KPIs, cohort definitions), §25 (pad study measures), §13 (matchmaking/bot-fill), §24 (thresholds).
- `ProjectUpdraft/MASTER_PLAN_V2.md` (phase gates, exclusions).
- Threshold values mirrored in `ProjectUpdraft/Assets/Scripts/Config/KpiGateConfig.cs` / `Assets/Resources/Config/KpiGateConfig.asset`.
- Telemetry schema: `ProjectUpdraft/Assets/Scripts/Core/Telemetry.cs` (`schemaVersion` 1, queue file `analytics_queue.jsonl`, `source` ∈ `client` | `offline_sim`).
- Backend ingestion contract: `Backend/HotdropBackend/Endpoints/AnalyticsEndpoints.cs`.

## 1. Data intake contract (what must arrive before any analysis)

1. Raw client queues: `analytics_queue.jsonl` pulled per device (`adb pull /sdcard/Android/data/com.DefaultCompany.ProjectUpdraft/files/`), one file per tester-device, filenames carrying tester code + pull date. Queues are 4 MB capped with oldest data kept — pulls must be scheduled often enough that nothing evicts mid-study (audit: ≥2 pulls/week per device).
2. Server-owned match facts from the backend (only authoritative source for online match outcomes, Elo, substitutions). Client `offline_sim` facts never substitute for server facts.
3. Ingestion endpoint accepts the client line shape (validated by `AnalyticsEndpointsTests.cs`); I need the aggregate export (per pseudonymousId × matchId rollup), not database access.
4. Metadata manifest: build version, `balanceVersion` stamp (expect `v1.0-spec`), cohort assignments (facilitator panel sets `study_pads_on` / `study_pads_off`), tester roster (tester code ↔ pseudonymousId mapping held by the study facilitator, never shipped with the data), device models for the four-device matrix.

**Validation gates before any number is reported:** schema parse rate (target ≥99.5% of lines), duplicate-line check, `pseudonymousId` distribution vs roster, `balanceVersion` homogeneity, timestamp monotonicity per device, and clock-skew bounds (UTC ISO-8601 expected).

## 2. Exclusion rules (pre-registered, applied before any denominator)

A pseudonymousId's events are excluded from balance and retention conclusions when any of:
- `hasBots: true` **on the match being analysed** — and any match where a substitution occurred (bot substituted for a disconnecting human) is excluded from weapon/pad win-rate stats entirely, not just the bot's rows.
- cohort ∈ incentivized/staff/friend cohorts (only `study_pads_on`, `study_pads_off`, and later `default` organic are analysable for Gate D).
- duplicate accounts: same physical device observed under two pseudonymousIds, or two roster entries reporting the same device model + installation fingerprint. First-observed pseudonymousId wins; the duplicate's pre-merge events are dropped.
- sessions failing the build/balance version check (stale builds).
- Study sanity: a tester whose pad-study cohort was set after their `first_open` (mid-study contamination window flagged, first-24h events dropped).

Every published percentage carries: numerator, denominator, exclusions applied (counts per rule), and a 95% Wilson interval.

## 3. KPI computation rules (spec §166–167)

Cohort anchor: **first `first_open` event per pseudonymousId** (UTC). Day D1/D7/D30 = the 24/168/720-hour windows after that anchor (spec: "D1 = match 24–48h after"; D7 = activity in hours 144–168+; D30 = hours 696–720+). Activity = any analysable event from that id (match_completed preferred; tutorial/UI events count only for "returned to app", not for play retention — reported separately).

- Tutorial completion: `tutorial_completed` (payload `skipped=false`) ÷ unique ids with `tutorial_started`, per cohort. Separately report step-level drop-off from `tutorial_step` (worst step named with its denominator).
- D1/D7/D30 retention: survivors ÷ ids reaching anchor, **separately** for tutorial completers vs non-completers, practice-only vs public-match users, and incentivized vs organic. Minimum denominators before a rate is treated as a gate signal: **n ≥ 30 per cohort cell** (Wilson interval reported regardless).
- Crash-free session rate: sessions without a process death ÷ all sessions, per device tier, sourced from **Google Play vitals** (`users-crashed`, `users-ANR`) with explicit metric definitions. `client_error` is an *error* event only — a caught exception or logged error is not a crash, and absence of uploaded events is never read as absence of crashes. `client_error` is reported separately as an error-rate signal.
- Voluntary instant rematch: `results_play_again_clicked` ÷ `match_completed` views where the results screen was shown, per cohort, excluding bot-substituted matches.
- Median queue time: server-side enqueue→matched spans for public queue during scheduled test windows only; reported as median + IQR, with the fraction waiting >45 s and >60 s (the `showStillSearchingAfterSeconds` mark).
- **Hard rule: D7/D30 are never estimated from a cohort younger than the window.** Until the calendar reaches it, the KPI line reads "not yet measurable — earliest date X".

## 4. Balance analyses (first pass on the data drop)

Each analysis is run on the excluded dataset from §2 and produces candidate config deltas in the fixed format: config asset + field, current value, proposed value, evidence (n, effect size, interval), and a hold-for-more-data verdict when n is thin. Bots are never tuned to force outcomes; no proposal may touch damage/movement/scoring invisibly.

1. Weapon usage & outcomes: selection counts from `weapon_selected`, per-weapon damage/kills/damage-taken shares and win-rate deltas from server match facts, split by device tier and cohort. Lethality cross-check: expected TTK per weapon from the frozen asset values (ARC 18 dmg @480 RPM; ZIP 13 @780; BRICK 8×9 @75; head ×1.25) vs measured.
2. Pad placement heatmaps: `pad_placed` positions binned to an arena grid (map geometry from `MapConfig`/`SwitchyardGenerator`), with `pad_used` conversion per bin, per cohort (pads-on only). Watch for placement starvation (few placements), hot-spots colliding with the 5 m spawn-exit and 6 m capture-radius no-place rules, and destroy events (`pad_destroyed`).
3. Zone capture stats: captures per zone slot from the `MatchRulesConfig` schedule (0→1→2→0→2→1 at 30 s steps), time-to-capture, holds, and match-deciding zone distribution.
4. Bot difficulty calibration: bot vs human damage/kills/accuracy shares in analysable matches, reaction/aim-error observables, with the `BotConfig` current values (reaction 0.35 s standard / 0.6 s easy; aim error 4.5°→1.2° standard, 7°→2.5° easy) as the reference. Calibration only ever moves toward measured parity with humans; never toward forced wins.
5. Match-length distribution: `match_completed` durations vs the 180 s regulation + 30 s OT design, overtime frequency, and 120-point score-to-win reach rate.
6. Pad A/B study scorecard (spec §25): per cohort — unprompted explanation (from facilitator interviews, not telemetry), purposeful placement share, mid-air aim comfort, ground-route viability, voluntary replay rate; plus the §3 KPI split by cohort. Verdict format: pads create opportunity / confusion, with denominators.

## 5. Bot-fill policy inputs

From server queue data only: wait-time distribution per scheduled window, fill rate at the current 45 s threshold (`MatchmakingConfig.botFillQueueThresholdSeconds`), Elo-impact of filled matches (they are already Elo-excluded), and return-to-queue behaviour after a filled vs unfilled match. The fill threshold is only moved if measured wait times hurt the §3 retention splits.

## 6. Monetization inputs (deferred)

Spec: no monetization work until Gate D passes. When Gate H approaches, the desk will define ad-fill rates, reward-claim caps (`AdConfig`), bundle conversion (`CatalogConfig`), and LTV-vs-CPI inputs — with the same denominator rules. No numbers exist today.

## 7. Output formats

- KPI report: table per §3 with n, rate, Wilson 95% CI, threshold, PASS/FAIL/NOT-MEASURABLE, and the exclusions ledger.
- Balance report: the fixed delta format from §4, plus "insufficient data — collect N more matches" where n < the pre-registered minimum (default n ≥ 60 analysable matches per weapon/zone cell before a delta is proposed).
- This desk never certifies gates and never overrides the auditor: the numbers here are inputs to the recorded go decision.
