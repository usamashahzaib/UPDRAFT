# Project UPDRAFT — Milestone Status

**Specification:** FINAL MASTER SPECIFICATION v1.0

**Audit date:** 2026-09-28

**Latest detailed evidence:** [`../docs/PROGRESS_2026-09-28.md`](../docs/PROGRESS_2026-09-28.md). Acceptance record: [`../docs/acceptance/STEP_0.md`](../docs/acceptance/STEP_0.md).

This file replaces the earlier “code ready = complete” assessment. A milestone is complete only when its exit condition in the master specification has been demonstrated.

| Milestone | Current status | Exit condition |
|---|---|---|
| A — Foundation | **Device repair verification in progress** | APKs have installed/launched on the Samsung A06. Initial build was broken; repaired iteration has landscape and working URP rendering. Final movement/camera/device checks remain. |
| B — Offline combat slice | **Functional prototype, not accepted** | Expanded editor test verifies all nine tutorial stages, real firing, enemy-pad launch, pause, complete accelerated match and rematch. Human touch acceptance and remaining HUD/pad features are open. |
| C — Network slice | **Runtime implementation present; acceptance incomplete** | Fusion session/server, backend ticket admission, authoritative player/match/pad logic, reconnect handling, and client HUD/input adapters exist in `Assets/Scripts/Network`. No accepted six-client run, network-condition report, Linux headless deployment, or server operations evidence is recorded. |
| D — Gameplay validation | **Not started** | No external pad A/B study, device performance report, telemetry dataset, KPI review, or go/kill decision exists. |
| E — Product shell | **Backend prototype; client journey incomplete** | Auth sessions, persistent parties, deletion and analytics have implementation/tests; matchmaking/commerce remain scaffolds and no deployed end-to-end player journey is accepted. |
| F — Monetization | **Intentionally gated** | Google Play purchase verification, restore/refund handling, and approved catalog are not production-ready. Ads and shop must remain disabled until D passes. |
| G — Launch readiness | **Drafts only** | Legal drafts and deletion page exist; publisher identity, signing, store listing, Data safety, operations, and closed testing remain. |
| H — Controlled release | **Not started** | No launch cohort, measured acquisition economics or release-scale retention evidence exists. |

## Verified on 2026-09-28

- Unity EditMode: **29/29 passed**, including native Input System configuration.
- Unity PlayMode: **1/1 passed**, expanded 31.80-second regression covers all nine tutorial stages, startup/skip, map geometry, real target damage, enemy-pad activation, pause, Home, six players, Results and rematch.
- Backend integration/unit tests: **48/48 passed**.
- Android project settings: API 28 minimum, automatic current installed target API, IL2CPP, ARM64; AAB is selected by the build configuration helper.

These recorded tests establish a desktop-editor prototype and backend scaffold. They are not substitutes for the milestone device, multiplayer, playtest, security, or operations gates. The acceptance record reports a different backend test total (71 versus 48 here); reconcile the evidence before sign-off.

## Next gate

Do not begin shop, ads, or Phase 2. First complete A/B acceptance on a real Android reference device; then build Milestone C as a six-client dedicated-server slice.
