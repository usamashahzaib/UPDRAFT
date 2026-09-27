# Project UPDRAFT — Asset License Register
# Specification Section 16/33: "Original assets or verified commercial licenses — maintain a license register."
# "Free download" ≠ free commercial use.
#
# This file documents every third-party asset, its source, license, and verification status.
# Update before every milestone delivery.
#
# Format: Asset Name | Source/URL | License Type | Commercial Use | Verified | Notes

## UNITY PACKAGES (Package Manager)
com.unity.inputsystem | Unity Registry | Unity Companion License | Yes | Yes | v1.11.2
com.unity.test-framework | Unity Registry | Unity Companion License | Yes | Yes | v1.4.5
com.unity.render-pipelines.universal | Unity Registry | Unity Companion License | Yes | Yes | v17.0.3
com.unity.textmeshpro | Unity Registry | Unity Companion License | Yes | Yes | v3.2.0
com.unity.ugui | Unity Registry | Unity Companion License | Yes | Yes | v2.0.0

## PHOTON FUSION 2 (Milestone C - External Dependency)
Photon Fusion 2 | Photon Engine | Photon SDK License | Yes* | PENDING | *Requires Photon account + App ID. Not in UPM. Manual import needed. BLOCKS Milestone C.

## FONTS
TBD - Need to select a commercial-friendly font for UI
- Option: Inter (SIL Open Font License) - free commercial
- Option: Roboto (Apache 2.0) - free commercial
- Option: Noto Sans (SIL OFL) - free commercial

## AUDIO (Placeholders - to be replaced with original/licensed)
All audio currently: Unity built-in / generated silence
- Footsteps: NEED LICENSED OR ORIGINAL
- Weapon fire (ARC/ZIP/BRICK): NEED LICENSED OR ORIGINAL
- Reload sounds: NEED LICENSED OR ORIGINAL
- Pad deploy/arm/launch/destroy: NEED LICENSED OR ORIGINAL
- Hit/elimination confirm: NEED LICENSED OR ORIGINAL
- Damage taken/low HP: NEED LICENSED OR ORIGINAL
- Zone activate/contest/milestone: NEED LICENSED OR ORIGINAL
- Countdown/result: NEED LICENSED OR ORIGINAL
- UI sounds: NEED LICENSED OR ORIGINAL
- Music (quiet combat): NEED LICENSED OR ORIGINAL

Sources to investigate:
- freesound.org (check CC0 / CC-BY per asset)
- OpenGameArt.org (check license per asset)
- Kenney.nl (CC0 - free commercial)
- Sonniss.com (Game Audio Bundle - paid, commercial)
- Original recording (preferred)

## VFX (Placeholders - Unity built-in particles)
All VFX currently: Unity default particle system
- Muzzle flashes: NEED ORIGINAL OR LICENSED
- Hit sparks: NEED ORIGINAL OR LICENSED
- Pad arming/armed effects: NEED ORIGINAL OR LICENSED
- Launch/landing effects: NEED ORIGINAL OR LICENSED
- Elimination effect: NEED ORIGINAL OR LICENSED

## 3D MODELS (Graybox - Unity primitives only)
Current: All primitives (Cube, Plane, Cylinder, Sphere) - Unity built-in, no license issues
Production replacements needed:
- Character mesh/rig (1 body, material variants): NEED ORIGINAL OR LICENSED
- Weapon meshes (3: ARC, ZIP, BRICK): NEED ORIGINAL OR LICENSED
- Pad mesh (1): NEED ORIGINAL OR LICENSED
- Environment modular pieces (~15-20): NEED ORIGINAL OR LICENSED

## TEXTURES/MATERIALS
Current: Unity default materials / procedural colors
Production: Need PBR texture sets for:
- Character body materials (friendly/enemy/team variants)
- Weapon wraps (cosmetic)
- Environment (concrete, structural blue, etc. per spec palette)

Sources to investigate:
- ambientCG (CC0)
- Poly Haven (CC0)
- Kenney.nl (CC0)
- Original creation (preferred)

## ICONS/UI GRAPHICS
Current: Unity UI primitives / TextMeshPro
Production: Need icon set for:
- Weapon icons (3)
- Pad icon
- Ping types (4)
- Cosmetic categories
- Settings icons

Sources:
- Material Design Icons (Apache 2.0)
- Feather Icons (MIT)
- Custom creation (preferred)

## BACKEND DEPENDENCIES
Microsoft.EntityFrameworkCore | NuGet | MIT | Yes | Yes | v10.0.0
Npgsql.EntityFrameworkCore.PostgreSQL | NuGet | MIT | Yes | Yes | v10.0.0
Microsoft.AspNetCore.Authentication.JwtBearer | NuGet | MIT | Yes | Yes | v10.0.0
Npgsql | NuGet | PostgreSQL License | Yes | Yes | transitive

## EXTERNAL SERVICES (Not Yet Integrated)
Google Play Games Services | Google | Google APIs ToS | Yes* | PENDING | *Requires Google Play Console account
Google Play Billing | Google | Google Play Developer Agreement | Yes* | PENDING | *Requires publisher account
AdMob (Rewarded Ads) | Google | AdMob Terms | Yes* | PENDING | *Requires AdMob account, Milestone D gate
PostgreSQL 16 | PostgreSQL Global Dev Group | PostgreSQL License | Yes | Yes | Docker image
Docker | Docker Inc. | Apache 2.0 | Yes | Yes | For deployment

## VERIFICATION CHECKLIST PER MILESTONE
Milestone A (Foundation): ✓ Unity packages only
Milestone B (Combat Slice): ✓ Unity packages only (primitives, no external assets)
Milestone C (Network): ⚠ Photon Fusion 2 - BLOCKED on account/App ID
Milestone D (Validation): ⚠ Audio/VFX placeholders - NEED ORIGINAL OR LICENSED before external playtest
Milestone E (Product Shell): ⚠ Fonts, icons, UI graphics - NEED LICENSED
Milestone F (Monetization): ⚠ Google Play / AdMob - BLOCKED on accounts
Milestone G (Closed Launch): ALL ASSETS MUST BE VERIFIED COMMERCIAL LICENSES
Milestone H (Release): COMPLETE REGISTER REQUIRED FOR STORE SUBMISSION

## ACTION ITEMS
[ ] Select and license font (Inter/Roboto/Noto recommended)
[ ] Create or license all audio assets (Kenney.nl CC0 as baseline)
[ ] Create or license all VFX (original particle systems)
[ ] Create or license character mesh/rig/animations (~12 core anims)
[ ] Create or license 3 weapon meshes
[ ] Create or license pad mesh
[ ] Create or license ~15-20 modular environment pieces
[ ] Create or license icon set
[ ] Obtain Photon Fusion 2 App ID (for Milestone C)
[ ] Set up Google Play Console (for Milestone F+)
[ ] Set up AdMob account (for Milestone F+)
[ ] Verify every asset license before Milestone G