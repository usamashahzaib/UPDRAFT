# Gate G pre-registered readiness inputs — data & balance desk

Prepared from source inspection on 2026-09-28. This desk supplies numbers, blocker evidence, and
pre-registered measurement rules for Gate G ("premium + operational readiness"). It does **not**
certify the gate: PASS requires independent art/UX/perf QA, closed-test evidence, legal
deliverables, rehearsed restore/rollback, and zero launch-blockers — decisions that belong to the
auditor acting on these inputs.

## 1. Launch-blocker ledger (evidence lines, not verdicts)

| # | Area | What Gate G requires | What exists (evidence) | Desk status |
|---|---|---|---|---|
| B1 | Publisher identity | Publisher-owned Play Console, final app ID, publisher-controlled signing | App ID is placeholder `com.DefaultCompany.ProjectUpdraft` (`ProjectSettings.asset`); `AndroidKeystoreName`/`AndroidKeyaliasName` empty; `ConfigureAndroidBuild.cs` logs "Application ID is a placeholder… signing still requires publisher-owned values" | **External dependency — hard blocker** |
| B2 | Purchase verification (inherited from F) | Working purchase lifecycle before monetized store listing | `CommerceEndpoints.verify-purchase` TODO: no Google Play Developer API call; no RTDN/refund-revocation path | **Inherited F blocker** |
| B3 | Backups + tested restore | Daily encrypted backups, rehearsed restore | No backup job or restore runbook anywhere in repo; `docker-compose.yml` is explicitly "Local dev / staging only" with `CHANGE_ME` credentials | **Not started** |
| B4 | Monitoring + alerts | Health/capacity monitoring; alerts for match + purchase failures | `/health/live` and `/health/ready` exist (`Program.cs`); no metrics export, no alert routing, no incident owner named | **Partial (liveness only)** |
| B5 | Kill switches + rollback | Independent kill switches; config rollback | `Features:MatchmakingEnabled` / `PurchasesEnabled` exist; ads flag proposed in `GATE_F_MONETIZATION_PLAN.md`; balance config is version-stamped (`v1.0-spec`) giving a rollback hook — no rollback procedure documented | **Partial** |
| B6 | Dev/stage/prod separation | Separate configs per environment | `BackendConfig.environmentName`/`baseUrl` exist client-side; no stage/prod deployment artifacts or secret configuration in repo | **Not evidenced** |
| B7 | Store listing assets | Real screenshots/video, Data safety, rating questionnaire, deletion path | Legal drafts exist (`docs/legal/PRIVACY_POLICY.md`, `TERMS_OF_SERVICE.md`, `account-deletion.html`); STEP_E records the web deletion form only queues an **unverified** request; no store screenshots/video exist | **Drafts only** |
| B8 | Placeholder replacement | Production original/verified assets within budget; identical hitboxes; audible identification; enemy-only outlines | Audio + VFX are 100% original synthetic code (register D-10/D-11 VERIFIED). Still placeholder per `ASSET_LICENSE_REGISTER.md`: character mesh/rig, 3 weapon meshes, pad mesh, ~15–20 environment pieces, PBR textures, icon set; font action item open | **Open — art dependency** |
| B9 | Performance budgets, 4-device matrix | All spec §24 budgets met across devices + 20-min thermal | Budgets defined in `GraphicsConfig` (30/60 fps presets, ≤150 draw calls low, ≤250 MB download, ≤600 MB installed, ≤750 MB low-tier RAM, ≤8 s returning startup, ≤10 s arena load, ≤100 KB/s/client); `DevicePerformanceProbe` tracks FPS/PSS/thermal; only a brief A06 smoke run exists — **no FPS was measured** | **Open — needs matrix run** |
| B10 | Inherited test rerun on release candidate | All inherited suites pass on the frozen RC | Recorded backend counts diverge across records (48 in `MILESTONE_STATUS.md`, 71 in `STEP_0.md`, 77 in `STEP_D.md`, 79 in `STEP_E.md`) — never one frozen revision with one captured run; NU1903 warning on `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (vulnerable transitive dep); license register package versions are stale vs `Packages/manifest.json` | **Reconciliation required** |

## 2. Desk-critical data blocker: server ingestion destroys the identity needed for retention KPIs

Evidence (all three verifiable in `AnalyticsEndpoints.cs` / `Entities.cs`):

1. The client queue line carries `PseudonymousId`, and `ClientQueuedEvent` parses it — but
   `ToEntity` never receives or stores it; the `AnalyticsEvent` entity has **no pseudonymous-ID
   column**. The identifier is accepted and discarded.
2. `ToEntity` sets `Timestamp = DateTimeOffset.UtcNow` at ingestion; the client-supplied event
   `Timestamp` from `ClientQueuedEvent` is discarded. Offline-collected events uploaded later get
   the wrong time.
3. The retention job (`AnalyticsRetentionService`) rolls raw events up to `AnalyticsDailyAggregate`
   rows keyed only by date × event × region × device tier — **counts with no user dimension**.

Consequence: D1/D7/D30 retention, per-user crash-free rates, voluntary-rematch-per-user, and every
Gate F/H per-user monetization metric are **not computable from server data**. The Gate D offline
study is unaffected (I ingest raw per-device `analytics_queue.jsonl` pulls), but the moment any
cohort is measured through the backend, the KPI table cannot be produced per spec §166–167.

Proposed fix (spec §164 mandates the pseudonymous ID travel with every event, so persisting it is
spec-compliant, not scope creep): persist `PseudonymousId` and the client `Timestamp` on
`AnalyticsEvent`, and add a per-pseudonymous-ID daily presence rollup inside the existing 30-day
raw / 180-day aggregate retention windows. Must land **before** online-cohort Gate D collection
and before Gate F measurement.

### Status update (2026-09-28): implemented in source, tests recorded

- `AnalyticsEvent` now persists `PseudonymousId` and `ClientTimestamp` on all three ingestion
  routes (`/events`, `/events/batch`, `/server-events`). Server-owned events carry an empty
  pseudonymous ID and null client timestamp by design; unparseable/absent client timestamps are
  stored as null and the rollup falls back to server receive time.
- New `AnalyticsDailyPresence` table: one row per pseudonymous ID per UTC day (unique index),
  with cohort and first-seen time, expiring with the 180-day aggregate window. The retention
  service folds presence rows in the same transaction that deletes expired raw events (no user-day
  can vanish between the two steps), using a watermark that is idempotent on re-run, folds events
  ingested between runs, and catches late-arriving offline batches by client event time.
- Migration `20260928170000_AddAnalyticsIdentityAndPresence` adds the two columns, three indexes,
  and the presence table. It is source-complete but has **not been applied to a real PostgreSQL
  instance** — that remains part of deployment acceptance.
- Evidence: backend suite passed **84/84** (`dotnet test Backend/UpdraftBackend.Tests/
  UpdraftBackend.Tests.csproj`, 2026-09-28), including five new regressions: identity+timestamp
  persistence from the exact Unity queue line shape, server events store no client identity,
  one-row-per-user-day rollup idempotency, late-arrival folding after the watermark advances,
  and presence surviving raw-window expiry. The Unity client required no change — the batch
  uploader already transmits the queue lines verbatim.
- Desk note: ingestion now keeps what spec §164 always required it to keep; this does not by
  itself make any KPI measurable — measurement still waits on the Gate D data drop.

## 3. Pre-registered Gate G measurement set (what I compute when evidence arrives)

- **Performance budget table** per device tier (low ≤3 GB / mid / high, plus the 16 KB-page device): median + P5 FPS vs preset target, PSS p95 vs 750 MB (low), thermal-state timeline over the 20-min run, `startup_budget` ms vs 8 s returning-startup, arena load vs 10 s, KB/s/client vs 100. Each row: measured value, budget, PASS/FAIL, session count. Thermal at state ≥ 1 sustained counts as FAIL for that device.
- **Crash-free session rate** for the release-candidate build specifically, per device tier, with the Gate D exclusion rules. Source: **Google Play vitals** (`users-crashed`, `users-ANR`), with explicit metric definitions. The client `client_error` event is *not* a crash signal — it records caught exceptions and logged errors — so it is reported as a separate error-rate metric and never substituted into the crash-free figure.
- **Inherited-suite evidence**: one frozen RC revision, one captured run per suite, counts recorded with the revision hash; the B10 count discrepancy closes only via that single run.
- **Privacy-policy parity check**: every field actually collected (telemetry envelope per `Telemetry.cs`) cross-checked against the published privacy policy text; any unmatched collection is a launch blocker. This desk supplies the diff; legal signs it.
- **Placeholder-replacement invariants** (measurable by my desk once art lands): hitbox parity — mesh replacement must not move damage/interact volumes (geometry diff + hit-registration telemetry parity, same weapon-distance damage profiles); audible identification — weapon/landing cues must remain distinguishable, verified in QA recognition tasks, not telemetry.
- **Launch-blocker ledger closeout**: "zero launch-blockers" = table §1 all rows closed with evidence, re-audited on the RC.

## 4. Out of scope for this desk

Certifying PASS, approving store submission or the go decision, choosing art vendors or price
points, setting performance budgets (fixed by spec §24/§205), and operating production
infrastructure.
