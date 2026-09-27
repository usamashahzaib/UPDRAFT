# Project UPDRAFT — Build Log & Decisions

Working title per spec; check name availability before publishing.

## Spec revision: now building the "FINAL MASTER SPECIFICATION (v1.0)"

The user replaced the earlier draft spec with a merged final version. Core
gameplay numbers (movement, weapons, pads, map, match flow) are unchanged, so
everything already built stays valid. Deltas from the earlier draft, applied
in this pass:

1. **Bots now fill public lobbies**, not just tutorial/practice/disconnect
   substitution — once queue time exceeds 45s (`MatchmakingConfig.
   botFillQueueThresholdSeconds`), remaining slots fill with labeled `[BOT]`
   players so a match still starts. Bot-filled matches stay flagged and
   excluded from balance/rating stats, same as before.
2. **Monetization corrected**: rewarded ads (AdMob, rewarded-only, capped,
   skippable — `AdConfig`) are now an approved lever alongside permanent
   cosmetics, both still gated behind Milestone D validation passing.
3. **Explicit KPI go/kill gate** (`KpiGateConfig`): D1 ≥35%, D7 ≥15%,
   D30 ≥5%, tutorial completion ≥80%, crash-free ≥99.5%, voluntary rematch
   ≥50%, median queue <45s. Two failed KPI passes → fix the core loop or pivot
   to Phase 2, no sunk-cost spending.
4. **Phase 2 (Arena Royale, 12-player)** is named as a future mode built on
   the same stack/art, gated on sustained D7 ≥15% after live launch. **Not
   started** — building it before then would violate the pre-D feature freeze
   this spec itself sets (section 26/33: no parallel feature creep, don't
   build Phase 2 before Milestone D passes).

## Current milestone: A (Foundation) → B (Combat prototype), in progress

### What exists now
- `ProjectUpdraft/` — Unity project skeleton (folder layout only; no Unity Editor
  was run to generate `ProjectSettings/`/`Library/` — that requires opening the
  project in the actual Unity 6.3 LTS Editor at least once).
- `Assets/Scripts/Config/*` — ScriptableObject configs matching the spec's exact
  numbers: `MovementConfig`, `WeaponDefinition` (ARC/ZIP/BRICK), `PadConfig`,
  `MatchRulesConfig` (Relay zone schedule, win conditions, overtime).
- `Assets/Scripts/Match/RelayScoringController.cs` — full Relay scoring state
  machine (section 4): per-second scoring, contested/empty resets, zone
  schedule, score-limit win, timer win, overtime hold, draw. Engine-agnostic
  and unit-testable.
- `Assets/Scripts/Match/ZoneOccupancy.cs` — valid-occupation check (alive,
  feet radius, feet height, not spawn-protected).
- `Assets/Scripts/Movement/PlayerHealthController.cs` — 100 HP, 5s delay then
  25/s regen, interrupted by damage (section 6).
- `Assets/Scripts/Pads/LaunchPad.cs`, `PadPlacementValidator.cs`,
  `PlayerPadManager.cs` — full pad lifecycle: arming/lifetime/health, one
  charge + 16s recharge that survives death, placement restrictions (surface
  tilt, spawn/objective exclusion zones, no overlap), one-second per-player
  activation lockout (section 7).
- `Assets/Scripts/Weapons/ShotResolver.cs` — ARC/ZIP/BRICK damage falloff,
  head multiplier, seeded evenly-distributed pellet spread for BRICK
  (section 8).
- `Assets/Scripts/Match/MatchFlowController.cs` — preparation (8s, no
  damage/pad placement) → countdown (3s) → in-progress/overtime → results
  phase machine (section 5).
- Remaining config groups from section 33: `MapConfig` (Switchyard graybox
  coordinates, section 10), `BotConfig`, `MatchmakingConfig`,
  `ProgressionConfig`, `GraphicsConfig`, `CatalogConfig` (cosmetic bundles,
  shop disabled by default until the section 26 validation gate passes).
- `Assets/Scripts/Config/AdConfig.cs` — rewarded-ad rules (extra crate roll,
  credit bonus, cosmetic trial, all capped/day) and `KpiGateConfig.cs` — the
  soft-launch go/kill thresholds, both from the final spec.
- `Assets/Scripts/Tests/RelayScoringControllerTests.cs` — NUnit EditMode
  tests for scoring/contest/reset/win/overtime behavior. Source only; not yet
  run (no Unity Editor/Test Framework available in this environment).
- `Backend/UpdraftBackend/` — ASP.NET Core (net10.0) Minimal API skeleton:
  `Program.cs` wiring the section 23 endpoint groups (auth, profile, party,
  matchmaking, matches, commerce, support) over a single `UpdraftDbContext`
  (Npgsql/PostgreSQL 16), entities for the section 23 persistent records, a
  unique `(Reason, SourceId)` index on the credit ledger so a source event
  can never double-grant a reward, and ownership derived from a `player_id`
  session claim rather than request-body IDs. `Dockerfile` + root
  `docker-compose.yml` wire it to a local Postgres container (section 21).
  Every genuinely external call (Google Play identity verification, Google
  Play purchase verification, game-server service-credential auth) is a
  `TODO` comment naming the exact dependency, not a fabricated success path.

These are deliberately plain C# (no `MonoBehaviour`/Fusion coupling) so the
same rules can run in offline practice and on the dedicated server, per
section 21 ("keep presentation separate from authoritative gameplay rules").

### Not yet done (explicitly deferred, not skipped)
- Unity scene assembly (graybox arena, spawn rooms, camera rig, prefabs) —
  this requires driving the actual Unity Editor GUI, which this environment
  cannot do headlessly. **Needs**: Unity 6.3 LTS installed locally, then open
  `ProjectUpdraft/` once to generate project files, then author the Switchyard
  graybox per section 10's coordinates.
- `MonoBehaviour`/`NetworkBehaviour` wrappers that feed real Unity input,
  `CharacterController`/KCC movement, and Physics raycasts into the pure
  logic above.
- Photon Fusion 2 integration — **blocked on an external dependency**: a
  Photon account + Fusion App ID (photonengine.com). Cannot be fabricated;
  Fusion isn't distributable via the standard Unity Package Manager registry
  and needs manual import once the App ID exists.
- Backend auth is unsigned/unverified: session issuance has no real JWT
  signing key, and Google Play identity/purchase verification calls are
  `TODO`s. **Blocked on**: a signing-key strategy and a Google Play Console
  service-account credential.
- Mobile touch input, camera, tutorial, bots, UI screens, cosmetic-shop UI,
  telemetry — later sections, not started.

### Standing decisions (recorded per section 34: "for ordinary unspecified
implementation details, choose the simplest solution and record it")
- Config values live in `ScriptableObject`s under `Assets/Scripts/Config`,
  matching section 33's `MovementConfig`/`WeaponConfig`/`PadConfig`/
  `MatchRulesConfig` naming.
- Gameplay math (scoring, health, pad lifecycle, weapon falloff/spread) is
  implemented as plain C# classes with no Unity engine dependency beyond
  `UnityEngine.Vector3`/`Mathf`, so it can be exercised by an EditMode test
  assembly without a scene.
- Team representation is a simple `TeamId { TeamA, TeamB }` enum until party/
  matchmaking systems need richer identity.

### External dependencies that block real progress (cannot be worked around)
1. **Unity 6.3 LTS Editor** must be installed locally to open/build the
   project, create scenes, and produce an actual playable APK.
2. **Photon Fusion 2 App ID** (Photon account) for any networking work.
3. **.NET 10 SDK + PostgreSQL 16** for the backend (Milestone E onward).
4. **Google Play Console account** for signing, store listing, and billing
   (Milestone F/G).
None of these are needed to keep writing gameplay-rule code, so that work
continues in parallel.

## Next steps
1. Add an `.asmdef` for `Assets/Scripts/Tests` referencing `UnityEngine.TestRunner`/
   NUnit once Unity is available, and actually run the EditMode tests.
2. Add EditMode tests for `LaunchPad`/`PadPlacementValidator`/`ShotResolver`/
   `PlayerHealthController` to match the coverage `RelayScoringController` has.
3. Player movement controller (KCC-style ground/air movement matching section
   6's speed/acceleration/gravity/jump numbers) — next gameplay piece to write.
4. Bot decision logic (perception via line-of-sight, route-following,
   objective-seeking) per section 13 — needs `MapConfig` route data first.
5. Once Unity Editor is available: open the project, add the packages in
   `Packages/manifest.json`, build the Switchyard graybox from section 10's
   coordinates, and wire prefabs to the scripts above.
6. Once the .NET SDK is available: `dotnet build`/`dotnet test` the backend —
   it has not been compiled in this environment yet.

### Git identity note
Commits in this repo use a project-local identity (`Project UPDRAFT
<devnull@projectupdraft.local>`), not the user's personal email, per explicit
instruction.
