# Project UPDRAFT — Specification Implementation Audit

**Specification audited:** FINAL MASTER SPECIFICATION v1.0

**Audit date:** 2026-09-27

**Repository state:** post-fix working tree; pre-existing unrelated edits were preserved

## Executive verdict

The repository contains a working Unity editor prototype for much of Milestones A and B and a tested ASP.NET Core backend scaffold. It is **not a completed game, not production-ready, and not past either milestone exit gate**. Earlier status text incorrectly treated source files, configuration assets, and placeholders as completed features.

The highest-impact offline runtime defects found during this audit were fixed and regression-tested. Work that requires publisher credentials, external services, real devices, six concurrent clients, human playtests, or an operating environment remains explicitly open rather than being represented by mocks.

## What is implemented and verified

### Foundation and offline rules

- Unity 6.3 project, URP scene, generated Switchyard graybox, player/bot prefabs, config assets, assembly separation, and reproducible editor setup commands.
- Plain-C# implementations for movement, health/regen, seeded hitscan spread/damage, launch-pad state, match phases, Relay scoring/overtime, zone occupancy, and bot decisions.
- Third-person camera, touch input bridge, offline player controller, basic bots, tutorial controller, match HUD, and a local 3v3 practice launcher.
- Android configuration helper applies the specified API 28 minimum, current installed target API, IL2CPP, ARM64, and AAB selection. Publisher-owned application ID and signing are deliberately not fabricated.
- Legal drafts cover privacy, terms, and public account deletion, using visible placeholders for publisher details.

### Backend scaffold

- One ASP.NET Core modular service using EF Core/PostgreSQL, Docker configuration, migrations, and endpoint groups for auth, profile, party, matchmaking, matches, commerce, and support.
- Ownership-sensitive player routes require an authenticated player claim.
- Match-result submission requires a game-server service claim rather than a player session.
- The credit ledger uniqueness key is player-scoped: `(PlayerId, Reason, SourceId)`.
- Equipped cosmetics are persisted.
- The public account-deletion page is included in build/publish output and its form creates a pending-verification request without requiring the app.

## Defects fixed in this audit

### Unity runtime

- Removed the duplicate startup path that could run tutorial and offline practice simultaneously.
- Connected touch aim, fire-button drag aim, invert-Y, DPI fallback, and pad-button input to gameplay.
- Corrected aim assist to sensitivity slowdown only, with cover checks and a non-allocating candidate query; removed aim snapping.
- Corrected third-person firing to resolve the camera aim point and trace from the muzzle toward it.
- Enforced preparation/countdown restrictions for combat, movement, weapon selection, and pad placement.
- Added respawn protection and excluded protected players from scoring.
- Preserved pad recharge/lifetime across death, enforced activation lockout and the six-placement match cap, and retired the replaced pad.
- Preserved pad-launch horizontal velocity and enabled bounded air steering instead of letting ground movement overwrite the launch.
- Replaced broken tutorial reflection with explicit pad APIs and made tutorial shutdown deterministic before practice starts.
- Removed unusable NavMeshAgents from bots; the graybox has no baked NavMesh.
- Ensured one AudioListener exists and guarded empty footstep arrays.
- Propagated overtime into match flow/HUD state and initialized the HUD for practice.

### Backend and delivery

- Fixed unauthenticated session-management routes and a party-ready binding defect.
- Disabled the unsafe Google-link path until Google server-auth-code verification exists.
- Restricted match results to a game-server authorization policy.
- Fixed integration-test hosting so the PostgreSQL and SQLite providers are not registered together.
- Added the schema migration for equipped cosmetics, deletion contact fields, and the corrected ledger index.
- Corrected Docker build context/content copying for the public deletion page.
- Added a real public deletion form handler with validation and duplicate-pending suppression.

## Verification evidence

| Check | Result | Scope |
|---|---:|---|
| Unity EditMode tests | **28/28 passed** | Core health, weapons, scoring, pads, placement cap, launched movement, and air steering |
| Unity PlayMode tests | **1/1 passed** | Main scene startup and five-second runtime smoke; no logged error/exception |
| Backend tests | **44/44 passed** | Auth boundaries, party/profile/match/matchmaking/commerce/support routes, persistence, and ledger constraints |
| EF Core migration generation | **Passed** | Model snapshot and `CorrectLedgerAndEquippedCosmetic` migration generated |

The PlayMode test is deliberately described as a smoke test. It does not simulate a complete human tutorial or match and does not validate Android touch ergonomics.

## What remains to implement or prove

### P0 — Milestones A/B exit gates

1. Set the publisher-owned Android application ID and signing configuration, produce an AAB/dev build, and play it on the 3 GB reference Android 9+ phone.
2. Complete the tutorial and a full offline 3v3 match using touch input. Verify every phase, scoring transition, overtime/draw, elimination/respawn, all three weapons, pad destruction/use by both teams, and results/rematch.
3. Add automated input-driven PlayMode coverage for the complete match path. The current smoke test only observes five seconds of startup.
4. Complete the specified touch settings: left-handed layout; per-button move/size/opacity/reset; saved sensitivity, vibration, and gyro; notch/safe-area and 48dp validation.
5. Complete missing combat presentation and UX: blocked-shot indicator, killer/weapon elimination display, respawn countdown/teammate view/weapon selection, hit feedback, pad preview/cancel feedback, authored sounds, and readable placeholder materials/VFX.
6. Validate the Switchyard acceptance rules by traversal and sightline testing; add fall/map-escape handling and confirm all routes/platforms remain reachable with pads.
7. Run the device/performance matrix and record FPS, thermal, memory, startup/load, draw calls, triangles, and package sizes. No current evidence satisfies section 23.

### P1 — Milestone C network slice

- Import and pin Photon Fusion 2, configure the App ID outside source control, and implement server-authoritative network behaviours/KCC.
- Add short-lived account-bound join tickets, controlled dedicated Linux server allocation, server-authenticated result/telemetry submission, and reconnect to the same avatar without state refill.
- Validate six clients at 30/80/150 ms RTT with loss/jitter, lag compensation capped at 150 ms, deterministic same-tick ties, and the specified bandwidth budget.

### P1 — Backend needed for the production journey

- Issue and validate short-lived guest access/refresh tokens, persist revocation/session state, and manage signing keys outside source control. The current guest response is not a usable production session.
- Verify Google Play Games server auth codes before linking identity.
- Replace the in-memory party store and placeholder matchmaking responses with shared persistent coordination and the composition/rating/bot-fill rules.
- Implement purchase verification against Google Play, acknowledgement, idempotent entitlement delivery, restore, refund/revocation handling, and the linked-identity-before-purchase rule.
- Implement deletion ownership verification, notification/work queue, actual account/data erasure, and completion tracking; the form currently records only a pending request.
- Add API rate limits, validation limits, suspicious-pattern logs, environment-separated configuration, health/capacity monitoring, encrypted backups plus restore tests, kill switches, rollback, and strongly authenticated admin tooling.

### P1 — Analytics and Milestone D

- Implement the versioned event pipeline from section 24. Config assets do not emit, transport, retain, or aggregate events.
- Enforce 30-day raw and 180-day aggregate retention operationally, not only in the privacy draft.
- Run the 30–50 person counterbalanced pad A/B study on real phones, publish sample sizes/cohorts and the KPI table, and make the required go/kill decision.
- Keep shop, rewarded ads, and Phase 2 disabled until this gate passes.

### P2 — Product shell and launch readiness

- Finish accounts/recovery, parties, contextual pings, reports moderation, progression/rewards, settings, complete home/results journeys, and clearly labelled bot-filled public lobbies.
- Replace placeholder assets with licensed/original assets and complete the license register.
- Fill every legal placeholder after counsel review; complete Play Data safety, rating, screenshots/video, publisher-controlled signing, monitoring/rollback, and any required closed-test period.

## Known risks requiring attention

- The test project currently resolves `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 with a high-severity NuGet advisory (`NU1903`). It is test-only, but it should be upgraded when package restore can reach NuGet; the current environment's package proxy rejected the lookup.
- Unity IAP 4.15.1 logs as deprecated/unsupported. Do not implement Milestone F on it; select and test the supported purchase stack after Milestone D.
- An automated AAB verification attempt reached Unity's Android toolchain but stalled at `Detecting Android SDK` even though platforms 34–37 and build-tools 36 are installed. No AAB success is claimed; the local SDK detection/toolchain must be repaired before the device gate.
- Existing nullable-reference warnings in several backend tests do not fail the build but should be cleaned up before treating warnings as errors.
- The repository has a pre-existing dirty working tree and generated logs/results. This audit did not discard or overwrite unrelated work.

## Required sequence

The specification's order remains the controlling plan: finish and demonstrate A/B on-device, then C, then D. Only a positive D decision permits E/F productization. Phase 2 remains frozen until the specified live-retention gate is sustained.
