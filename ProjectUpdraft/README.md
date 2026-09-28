# Project UPDRAFT

Three teammates. Three minutes. Build your own shortcuts — and watch who follows you.

**Third-person 3v3 team arena shooter** → grows into **12-player battle royale** (Phase 2) on the same stack.

Platform: Android first, landscape, 16+  
Monetization: Rewarded ads (post-validation) + permanent cosmetics. No pay-to-win, ever.  
Success gate: Milestone D playtests. Not opinions. Not hype.

---

## Spec

See: `PROJECT_UPDRAFT_SPEC.md` (the final master specification v1.0)

Key pillars:
- **Signature mechanic:** Launch pads — place, arm, launch, steer, anyone can use, enemies can destroy
- **Mode:** Relay — rotating capture zones (Center → West → East), 3-minute matches, 120 points to win
- **Weapons:** ARC Rifle (mid), ZIP SMG (close), BRICK Scattergun (point-blank) — all unlocked, one equipped
- **Bots:** Fill tutorial, offline practice, disconnect substitution, AND public lobbies after 45s queue
- **Monetization gated:** Shop + rewarded ads only enable after Milestone D gameplay validation passes

---

## Milestones

| Gate | Deliverable | Status |
|------|-------------|--------|
| **A** | Foundation: project, Android build, graybox, movement, camera | APK recovered and device defects found; repaired build under verification |
| **B** | Offline combat slice: 3 weapons, HP, respawn, pads, bots, scoring, full loop | Editor full-loop test passed; device/tutorial acceptance remains |
| **C** | Network slice: dedicated server, 6 clients, scoring, reconnect | Server-authoritative Fusion gameplay and ticket flow are implemented; six-client, network-condition, and Linux headless acceptance remain |
| **D** | Gameplay validation: tutorial, sound, placeholder art, telemetry + pad A/B study | ⏳ After C |
| **E** | Product shell: accounts, parties, progression, settings, bot-filled lobbies | ⏳ After D |
| **F** | Monetization: rewarded ads + catalog + purchase lifecycle | ⏳ After D |
| **G** | Closed launch: optimized assets, ops, store materials | ⏳ After F |
| **H** | Controlled one-market release | ⏳ After G |

**Pre-D freeze:** No clans, maps, or battle pass while D is unresolved.

Latest evidence and ordered remaining work: [`../docs/PROGRESS_2026-09-28.md`](../docs/PROGRESS_2026-09-28.md). Source files and placeholder screens are not milestone acceptance.

---

## Quick Start (Unity)

### Prerequisites
- Unity Hub + **Unity 6.3 LTS** (URP)
- Android Build Support (IL2CPP, ARM64)
- .NET 10 SDK (for backend)

### Setup
```bash
# 1. Open in Unity Editor
#    Unity Hub → Add project → Select ProjectUpdraft/ folder

# 2. In Unity Editor, regenerate the slice:
#    Tools/Updraft/Build Offline Slice Scene

# 3. Mobile URP, landscape, original materials and prefabs are generated.

# 4. Configure PlayerSettings for Android:
#    - Minimum API: 28 (Android 9)
#    - Target API: highest installed supported SDK (automatic)
#    - Scripting Backend: IL2CPP
#    - Architecture: ARM64
#    - Build Format: AAB

# 5. For a local device APK, run Build-AndroidDev.ps1 in PowerShell.
#    Store AAB/signing are a separate publisher-owned release workflow.
```

### Backend (for Milestone C+)
```bash
cd Backend
docker-compose up postgres
# In another terminal:
dotnet run --project UpdraftBackend/UpdraftBackend.csproj
# API at http://localhost:8080
```

### Run Backend Tests
```bash
dotnet test Backend/UpdraftBackend.Tests/UpdraftBackend.Tests.csproj
```

---

## Architecture

```
ProjectUpdraft/
├── Assets/Scripts/
│   ├── Config/           # 12 ScriptableObject configs (never hardcode values)
│   ├── Core/             # GameBootstrap, SceneSetup
│   ├── Input/            # TouchInputManager (mobile controls)
│   ├── Camera/           # ThirdPersonCamera (TPP shoulder)
│   ├── Player/           # PlayerController (MB wrapper)
│   ├── Pads/             # LaunchPad, PadPlacementValidator, PlayerPadManager, LaunchPadBehaviour
│   ├── Weapons/          # ShotResolver (deterministic, server-authoritative)
│   ├── Movement/         # PlayerMovementSimulator, PlayerHealthController (pure C#)
│   ├── Match/            # MatchController, RelayScoringController, ZoneOccupancy, MatchFlowController
│   ├── Bots/             # BotController, BotPerception, BotDecisionMaker
│   ├── Map/              # SwitchyardGenerator (exact spec coordinates)
│   ├── Tutorial/         # TutorialController, TutorialTarget, TutorialUI
│   ├── UI/               # MatchHUD, TutorialUI
│   ├── Editor/           # CreateConfigAssets, CreatePrefabs, CreateScene
│   └── Tests/            # EditMode unit tests (NUnity)
│
├── Backend/
│   ├── UpdraftBackend/          # ASP.NET Core 10 Minimal API
│   │   ├── Domain/              # Entities (PlayerAccount, CreditLedgerEntry, etc.)
│   │   ├── Data/                # EF Core DbContext + Migrations
│   │   └── Endpoints/           # Auth, Profile, Party, Matchmaking, Matches, Commerce, Support
│   └── UpdraftBackend.Tests/    # xUnit tests (SQLite in-memory, tests unique constraint)
│
├── ASSET_LICENSE_REGISTER.md    # Every asset + license (spec §16/33)
├── MILESTONE_STATUS.md          # Implementation status per spec
├── DECISIONS.md                 # Build log & decisions
└── docker-compose.yml           # Postgres + Backend
```

---

## Config Assets (Section 33)

All gameplay values live in `Assets/Resources/Config/*.asset` — **never hardcoded**:

| Asset | Spec Sections |
|-------|---------------|
| `MovementConfig` | 6 |
| `WeaponDefinition` (ARC/ZIP/BRICK) | 8 |
| `PadConfig` | 7 |
| `MatchRulesConfig` | 4, 5 |
| `MapConfig` (Switchyard) | 10 |
| `BotConfig` | 12 |
| `MatchmakingConfig` | 14 |
| `ProgressionConfig` | 19 |
| `GraphicsConfig` | 24 |
| `CatalogConfig` | 19 |
| `AdConfig` | 19 |
| `KpiGateConfig` | 24 |

Create via: `Tools/Updraft/Create Config Assets`

---

## Key Spec Rules Implemented

- **Movement:** 6/5.1 m/s, 35 m/s² accel, 20 m/s² gravity, 6 m/s jump, 1.8m capsule, 4s respawn, 25 HP/s regen after 5s
- **Pads:** 1 charge / 16s recharge, 18s lifetime / 0.75s arm, 60 HP / 1.4m dia, 14/12 m/s launch, 30°/s air steer, 1s lockout
- **Weapons:** Exact damage/fire rate/mag/spread/recoil/falloff per spec table
- **Scoring:** 1 pt/sec uncontested occupation, contested=0, empty=0, 120 to win, 180s regulation, 30s OT (3s hold wins)
- **Match Flow:** 8s prep (no dmg/pads) → 3s countdown → Match → Results → Play Again
- **Bots:** LOS + audio perception only, 600ms/350ms reaction, retreat at 30% HP, desync focus fire
- **Monetization:** `CatalogConfig.shopEnabled = false`, `AdConfig.rewardedAdsEnabled = false` until Milestone D

---

## External Dependencies (Cannot Be Worked Around)

| Dependency | Blocks | Action |
|------------|--------|--------|
| Unity 6.3 LTS Editor | Building APK, running tests | Install via Unity Hub |
| Photon Fusion 2 App ID | Milestone C networking | Create Photon account |
| PostgreSQL 16 | Backend integration | `docker-compose up postgres` |
| Google Play Console | Signing, store, billing (F+) | Publisher account + $25 |
| AdMob Account | Rewarded ads (F+) | AdMob account |

---

## License Register

See `ASSET_LICENSE_REGISTER.md` — every third-party asset tracked with license verification status.

**Unity packages:** ✅ Verified (Unity Companion License)  
**Audio/VFX/Models/Fonts/Icons:** ⚠️ Need original or commercial license  
**Photon/Google/AdMob:** ⚠️ Pending accounts

---

## Decisions Log

See `DECISIONS.md` — running log of spec interpretations, toolchain status, and standing decisions.

---

> "The first thing to buy is six people finishing a match and independently wanting another. Everything larger — shop, ads, Phase 2 BR — is bought with that evidence." — Spec §32
