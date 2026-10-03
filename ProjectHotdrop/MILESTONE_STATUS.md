# Project HOTDROP — Milestone Status

**Specification:** FINAL MASTER SPECIFICATION v1.0

**Audit date:** 2026-09-29 (this continuation; prior audit 2026-09-28)

**Avoided change:** Do NOT start shop, ads, or Phase 2. Gate B is done; Milestone C (six-client dedicated-server slice) is the next gate.

## Milestone status

| Milestone | Current status | Exit condition |
|---|---|---|
| A — Foundation | **Device repair verification in progress** | APKs have installed/launched on the Samsung A06. Initial build was broken; repaired iteration has landscape and working URP rendering. Final movement/camera/device checks remain. |
| B — Offline combat slice | **ACCEPTED** | Prep-phase damage blocked at the authority layer; 142/142 EditMode + 6/6 PlayMode green; friendly fire never damages pads; fresh ARM64 IL2CPP APK frozen (SHA-256 `1b1c6c6e...`). Human-touch device acceptance remains open. |
| C — Network slice | **Runtime implementation present; acceptance incomplete** | Fusion session/server, backend ticket admission, authoritative player/match/pad logic, reconnect handling, and client HUD/input adapters exist. No accepted six-client run, network-condition report, Linux headless deployment, or server operations evidence recorded. |
| D — Gameplay validation | **Not started** | No external pad A/B study, device performance report, telemetry dataset, KPI review, or go/kill decision exists. |
| E — Product shell | **Backend prototype; client journey incomplete** | Auth sessions, persistent parties, deletion and analytics have implementation/tests; matchmaking/commerce remain scaffolds and no deployed end-to-end player journey is accepted. |
| F — Monetization | **Intentionally gated** | Google Play purchase verification, restore/refund handling, and approved catalog are not production-ready. Ads and shop must remain disabled until D passes. |
| G — Launch readiness | **Drafts only** | Legal drafts and deletion page exist; publisher identity, signing, store listing, Data safety, operations, and closed testing remain. |
| H — Controlled release | **Not started** | No launch cohort, measured acquisition economics or release-scale retention evidence exists. |

## Verified during this continuation (2026-09-29)

- Unity EditMode: **142 passed, 0 failed** (0.26 s) — `Run-UnityTests.ps1 -Platform EditMode`, clean shell.
- Unity PlayMode: **6 passed, 0 failed, 0 skipped** (107.9 s) — `Run-UnityTests.ps1 -Platform PlayMode`.
- Gate B fix: `ProjectUpdraft/Assets/Scripts/Network/NetPlayer.cs` (ServerDamage prep gate + Weapons prep shot-gate) and `ProjectUpdraft/Assets/Scripts/Network/NetPad.cs` (ServerDamage prep gate). `Step_B.md` (Gate B record) maps every requirement to test names and numbers.
- Regression test `PrepBlocksDamageAndPads_LiveFire_DestroysEnemyPads_NotFriendly` passes: 0 HP lost during prep, ally pad retains 60 HP (friendly fire blocked), enemy pad `Destroyed` per `PadConfig`.
- License/client: `Unity.Licensing.Client` reaped; zero licensing-client residue after shutdown; `Temp/UnityLockfile` clean; runner pre-flight guards committed (`2f64920`).
- Fresh development APK (ARM64 IL2CPP): `Builds/Android/UpdraftDev.apk`, size 87,428,019 bytes, SHA-256 `1b1c6c6ed1acf9cd69ca451b5bd98861ab45a4445c137dd5f7f3b0d849b60dbf`, source revision `2596e92`.
- Android project settings: API 28 minimum, automatic current installed target API, IL2CPP, ARM64; build script forces APK output (`buildAppBundle = false`).

## Known issues / remaining work

- Gate B human-touch acceptance on a real Samsung A06 (device tier) remains open — only remaining Gate B acceptance.
- Gate C (six-client dedicated-server session) is the next planned block; do not begin shop, ads, or Phase 2.
- Backend: backend suite unaffected by this Unity-only change; reconcile backend test totals when sign-off is attempted.
- Conflicting/trash artifacts from unrelated worktree entries (`docs/acceptance/STEP_E.md`, `GATE_C_ENVIRONMENT_PLAN.md`, `ProjectUpdraft/Assets/Scripts/Core/OriginalGameVfx.cs.meta`, `PlayerProductScreens.cs.meta`) are NOT part of this Gate B record and were left untouched.

## Next gate

Complete A/B acceptance on a real Android reference device; then build Milestone C as a six-client dedicated-server slice. Do not start shop, ads, or Phase 2 before Milestone D passes.
