# Project UPDRAFT — Milestone Status

**Specification:** FINAL MASTER SPECIFICATION v1.0

**Audit date:** 2026-09-27

**Detailed evidence:** [`../docs/IMPLEMENTATION_AUDIT.md`](../docs/IMPLEMENTATION_AUDIT.md)

This file replaces the earlier “code ready = complete” assessment. A milestone is complete only when its exit condition in the master specification has been demonstrated.

| Milestone | Current status | Exit condition |
|---|---|---|
| A — Foundation | **Implemented, not accepted** | Android settings and the graybox slice exist, but no build has been played on the reference phone. |
| B — Offline combat slice | **Functional prototype, not accepted** | Core logic and scene smoke tests pass; a human still needs to complete a full match on-device, including the tutorial and touch controls. |
| C — Network slice | **Not implemented** | Photon Fusion server authority, allocator, six clients, reconnect, and network-condition tests are absent. |
| D — Gameplay validation | **Not started** | No external pad A/B study, device performance report, telemetry dataset, KPI review, or go/kill decision exists. |
| E — Product shell | **Scaffold only** | Backend route shapes/configs exist; production auth, persistent parties/matchmaking, progression journey, and public bot-filled lobbies do not. |
| F — Monetization | **Intentionally gated** | Google Play purchase verification, restore/refund handling, and approved catalog are not production-ready. Ads and shop must remain disabled until D passes. |
| G — Launch readiness | **Drafts only** | Legal drafts and deletion page exist; publisher identity, signing, store listing, Data safety, operations, and closed testing remain. |

## Verified on 2026-09-27

- Unity EditMode: **28/28 passed**.
- Unity PlayMode: **1/1 passed** (Main scene loads and runs five seconds without logged errors; startup has one tutorial player and one audio listener).
- Backend integration/unit tests: **44/44 passed**.
- Android project settings: API 28 minimum, automatic current installed target API, IL2CPP, ARM64; AAB is selected by the build configuration helper.

These tests establish a stable desktop-editor prototype and backend scaffold. They are not substitutes for the milestone device, multiplayer, playtest, security, or operations gates.

## Next gate

Do not begin shop, ads, or Phase 2. First complete A/B acceptance on a real Android reference device; then build Milestone C as a six-client dedicated-server slice.
