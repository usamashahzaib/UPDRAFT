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

## Toolchain status (2026-09-27)

User installed Unity Hub and the .NET SDK locally. Verified in this session:
- **.NET SDK 10.0.401** is present (`C:\Program Files\dotnet\dotnet.exe`) —
  PATH wasn't refreshed in this shell, so it's invoked by full path. The
  backend now **actually builds** (`dotnet build`: 0 errors) and has a real
  test project that **passes** (`dotnet test`: 2/2), see below.
- **Unity Hub** is installed (`C:\Program Files\Unity Hub\`); the user is now
  downloading a Unity Editor version through it. Until that finishes and the
  project is opened once, the `ProjectUpdraft/` C# scripts still can't be
  compiled or have their EditMode tests run.
- Generated the backend's first EF Core migration
  (`Backend/UpdraftBackend/Data/Migrations/…InitialCreate`) via the
  `dotnet-ef` global tool, and added `UpdraftBackend.slnx` grouping both
  backend projects. `dotnet build` on the solution: 0 errors, 2 warnings (a
  pre-existing `NU1903` advisory on a transitive test-only SQLite package).
  `dotnet test`: 2/2 passing.

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
- `Assets/Scripts/Bots/BotPerception.cs`, `BotDecisionMaker.cs` — section 12
  bot logic: LOS/audible-event perception only (never a hidden position
  read), reaction-delay-gated intent switching (600ms easy / 350ms
  standard), retreat-when-hurt, and a per-bot desync chance so team focus
  fire isn't perfectly synchronized.
- `Assets/Scripts/Updraft.Runtime.asmdef` + `Assets/Scripts/Tests/
  Updraft.Tests.asmdef` — proper Unity assembly definitions so the test
  assembly only compiles in the Editor against `UnityEngine.TestRunner`/NUnit,
  and `com.unity.test-framework` added to `Packages/manifest.json`.
- `Assets/Scripts/Tests/*.cs` — NUnit EditMode coverage for
  `RelayScoringController`, `LaunchPad`/`PlayerPadManager`, `ShotResolver`
  (including the "BRICK can't one-shot 100 HP even all-headshots" rule and
  seeded-pellet determinism), and `PlayerHealthController`. **Source only —
  not yet run.** Unity Editor isn't installed yet (see Toolchain status
  above), so these are unverified until it is.
- `Backend/UpdraftBackend/` — ASP.NET Core (net10.0) Minimal API: `Program.cs`
  wiring the section 23 endpoint groups (auth, profile, party, matchmaking,
  matches, commerce, support) over a single `UpdraftDbContext`
  (Npgsql/PostgreSQL 16), entities for the section 23 persistent records, a
  unique `(Reason, SourceId)` index on the credit ledger so a source event
  can never double-grant a reward, and ownership derived from a `player_id`
  session claim rather than request-body IDs. `Dockerfile` + root
  `docker-compose.yml` wire it to a local Postgres container (section 21).
  Every genuinely external call (Google Play identity verification, Google
  Play purchase verification, game-server service-credential auth) is a
  `TODO` comment naming the exact dependency, not a fabricated success path.
  **Verified**: `dotnet build` succeeds with 0 errors/0 warnings.
- `Backend/UpdraftBackend.Tests/` — xUnit project using a real SQLite
  in-memory connection (not EF's InMemory provider, which doesn't enforce
  unique indexes) to regression-test the credit-ledger duplicate-reward
  constraint. **Verified**: `dotnet test` → 2/2 passed. One low-priority
  `NU1903` advisory on a transitive `SQLitePCLRaw` test dependency — test-only
  code path, not shipped; worth bumping when a patched version is available.

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
- Mobile touch input, camera, tutorial UI, bot movement/aim execution wiring
  (decision logic exists; it still needs to drive `PlayerMovementSimulator`/
  `ShotResolver`), cosmetic-shop UI, telemetry — later sections, not started.

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
1. **A Unity Editor version installed through Unity Hub** (6.3 LTS) — Hub
   alone doesn't let this environment open/build the project, create scenes,
   or produce an APK. This is a GUI-driven, multi-GB install with license
   activation, so it wasn't started without confirmation.
2. **Photon Fusion 2 App ID** (Photon account) for any networking work.
3. **PostgreSQL 16** running (locally or via `docker-compose up`) to exercise
   the backend against something other than the test project's in-memory
   SQLite.
4. **Google Play Console account** for signing, store listing, and billing
   (Milestone F/G).
~~.NET 10 SDK~~ — resolved; installed and verified working this session.

## Next steps
1. Install a Unity 6.3 LTS Editor via Unity Hub (`Unity Hub.exe`, already
   present), open `ProjectUpdraft/`, let it generate `Library/`/
   `ProjectSettings/`, and run the EditMode test suite for real.
2. Once compiling in-Editor: build the Switchyard graybox from section 10's
   coordinates and wire prefabs/`MonoBehaviour` adapters to the pure logic
   classes (movement, pads, weapons, scoring) that already exist.
3. Wire `BotDecisionMaker`'s output intents to actually drive
   `PlayerMovementSimulator` input and `ShotResolver` shots.
4. `docker-compose up postgres` and point the backend's connection string at
   it to validate migrations end-to-end (no EF Core migrations generated
   yet — needs `dotnet ef migrations add InitialCreate` once a first schema
   pass is reviewed).
5. Photon Fusion 2 import + App ID once the account exists (Milestone C).

### Git identity note
Commits in this repo use a project-local identity (`Project UPDRAFT
<devnull@projectupdraft.local>`), not the user's personal email, per explicit
instruction.
