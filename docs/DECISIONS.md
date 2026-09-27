# Project UPDRAFT — Build Log & Decisions

Working title per spec; check name availability before publishing.

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
- ASP.NET Core backend (`Backend/UpdraftBackend`) — folder created, project
  not yet scaffolded.
- Mobile touch input, camera, tutorial, bots, UI screens, matchmaking,
  progression, shop, telemetry — later sections, not started.

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
1. Add an EditMode test assembly (`Assets/Tests`) covering
   `RelayScoringController`, `ShotResolver`, and pad placement — cheapest way
   to prove the rules are correct before any scene exists.
2. Scaffold the ASP.NET Core backend skeleton (accounts/parties/matchmaking
   stubs) so API shapes exist even before Postgres is provisioned.
3. Once Unity Editor is available: open the project, add the packages in
   `Packages/manifest.json`, build the Switchyard graybox from section 10's
   coordinates, and wire prefabs to the scripts above.
