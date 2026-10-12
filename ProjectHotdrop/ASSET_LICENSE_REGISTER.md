# Project HOTDROP — Asset License Register
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
com.unity.render-pipelines.universal | Unity Registry | Unity Companion License | Yes | Yes | v17.3.0
com.unity.shadergraph | Unity Registry | Unity Companion License | Yes, for Unity-dependent projects | Yes | v17.3.0. HOTDROP's additive particle shader and particle vertex-color subgraph are adapted from the package's `0_Particle Unlit` graph template and `Particle Vertex Color` subgraph; notice retained in `THIRD_PARTY_NOTICES.md`.
com.unity.textmeshpro | Unity Registry | Unity Companion License | Yes | Yes | v3.2.0
com.unity.ugui | Unity Registry | Unity Companion License | Yes | Yes | v2.0.0

## PHOTON FUSION 2 (Milestone C - External Dependency)
Photon Fusion 2.1.3 Stable 2390 | User-supplied official Unity package | Photon SDK/service terms | Subject to Photon terms | Imported; publisher terms review pending | Original vendor assets/metas retained. App ID configured; networking not yet implemented.
Fusion Simple KCC 2.1.1 build 1273 | User-supplied official Simple KCC sample, addon subtree only | Photon addon terms | Subject to Photon terms | Imported; publisher terms review pending | DLLs/metas/build information retained; sample scene/art not imported. Gameplay adapter not yet implemented.

## FONTS
Liberation Sans | Bundled TextMesh Pro font | SIL Open Font License 1.1 | Yes | Yes | License retained at Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt; reserved font name unchanged.
Noto Naskh Arabic UI | https://github.com/google/fonts/tree/main/ofl/notonaskharabicui | SIL Open Font License 1.1 | Yes | Candidate source verified; font file not imported | Google Fonts metadata identifies Urdu (`ur_Arab`) and the upstream Noto Arabic project; see `docs/acceptance/A7_LOCALIZATION_FONT_PIPELINE.md`.
Noto Sans Devanagari UI | https://github.com/google/fonts/tree/main/ofl/notosansdevanagariui | SIL Open Font License 1.1 | Yes | Candidate source verified; font file not imported | Google Fonts metadata identifies Devanagari language coverage; see `docs/acceptance/A7_LOCALIZATION_FONT_PIPELINE.md`.
RTLTMPro | https://github.com/pnarimani/RTLTMPro | MIT | Yes | Candidate source verified; package not imported | Upstream README claims Arabic, Farsi, and Hebrew support; Urdu compatibility and integration with HOTDROP's UI factory remain unverified. See `docs/acceptance/A7_LOCALIZATION_FONT_PIPELINE.md`.
The prototype uses the bundled font above. Any production font replacement needs its own retained license.

## AUDIO (Original synthetic generation)
Original synthetic audio generator | Assets/Scripts/Core/OriginalGameAudio.cs | Project-original C# synth code | Yes | Yes | 100% original code (recipes for weapons, footsteps, damage, reload, low-HP, zone events, countdowns, UI tap, and 8-second quiet combat music loop). Zero 3rd-party assets.

All 5 gameplay audio categories implemented via project-original code:
- Low-HP warning: Original synthetic pulse wave (GameCue.LowHealth)
- Zone activate/contest/milestone: Original synthetic two-tone/pulsed cues (GameCue.Zone*)
- Countdown/result: Original synthetic synth stabs & chords (GameCue.Countdown, Victory, Defeat)
- UI sounds: Original synthetic click transient (GameCue.UiTap)
- Music (quiet combat): Original synthetic 8-second ambient chord loop (OriginalGameAudio.Music())

## VFX (Original procedural particle generation)
Original procedural VFX generator | Assets/Scripts/Core/OriginalGameVfx.cs | Project-original C# particle code | Yes | Yes | 100% original procedural particle system generator. Zero 3rd-party assets.
HOTDROP particle Shader Graph template and vertex-color subgraph | `Assets/Art/Vfx/HotdropParticleAdditive.shadergraph` and `HotdropParticleVertexColor.shadersubgraph`, adapted from com.unity.shadergraph 17.3.0 | Unity Companion License | Yes, only in connection with Unity-dependent projects | Package LICENSE.md verified; notice copied to `THIRD_PARTY_NOTICES.md` | URP additive particle/beam shading; graph blend mode is additive and receives the existing particle/line vertex colors. Runtime import, rendered output, and platform variant retention remain unverified.

All 5 gameplay VFX categories implemented via project-original code:
- Muzzle flashes: Original procedural directional burst (VfxType.MuzzleFlash)
- Hit sparks: Original procedural impact hemisphere burst (VfxType.HitSparks)
- Pad arming/armed effects: Original procedural expanding ring & ambient pulse (VfxType.PadArming, PadArmed)
- Launch/landing effects: Original procedural upward boost & impact ring (VfxType.PadLaunch, PadLanding)
- Elimination effect: Original procedural particle disintegration flash (VfxType.Elimination)

## 3D MODELS (current use and remaining production work)
Current: Unity primitives remain in graybox and competitive geometry. The pad renderer now uses a shared project-original beveled mesh; three CC0 weapon meshes, a CC0 mannequin/animation set, six CC0 environment props, and four registered PBR surface sets are also in use. See the Gate G records for limits and verification status.
Remaining replacements and acceptance:
- Character production bodies/outfits (2 silhouettes, shared rig and animation parity; Amendment v2.1): CC0 mannequin and 8 clips are in use; production outfit meshes, rig parity review, and visual acceptance remain open.
- Weapons: ARC, ZIP, and BRICK CC0 meshes are in use; in-game readability, animation parity, and audio-identification acceptance remain open.
- Pad: project-original 16-segment beveled landing shell is in use; final visual and device render-cost acceptance remain open.
- Environment: 6 CC0 Poly Haven props and 4 PBR surface sets are in use; complete modular-kit scope and rendered Gate G acceptance remain open.

## TEXTURES/MATERIALS
Home rooftop menu illustration — Stylized Flight | `Assets/Resources/HomeRooftop_StylizedFlight.png`; generated output recorded in `docs/acceptance/PART0_HOME_ROOFTOP_PROFILE_ART.md`; https://openai.com/policies/terms-of-use/ | Project-original, AI-assisted image generated for HOTDROP without third-party image reference | No per-asset purchase | Provenance recorded; similarity/legal distribution review pending | Profile-specific menu and venue backdrop; presentation-only.
Home rooftop menu illustration — Grounded Tactical Flight | `Assets/Resources/HomeRooftop_GroundedTacticalFlight.png`; generated output recorded in `docs/acceptance/PART0_HOME_ROOFTOP_PROFILE_ART.md`; https://openai.com/policies/terms-of-use/ | Project-original, AI-assisted image generated for HOTDROP without third-party image reference | No per-asset purchase | Provenance recorded; similarity/legal distribution review pending | Profile-specific menu and venue backdrop; presentation-only.
Home rooftop menu illustration — Hybrid | `Assets/Resources/HomeRooftop_Hybrid.png`; generated output recorded in `docs/acceptance/PART0_HOME_ROOFTOP_PROFILE_ART.md`; https://openai.com/policies/terms-of-use/ | Project-original, AI-assisted image generated for HOTDROP without third-party image reference | No per-asset purchase | Provenance recorded; similarity/legal distribution review pending | Profile-specific menu and venue backdrop; presentation-only.
Archived Home rooftop reference image | `Assets/Resources/HomeRooftop.png` copied from user-supplied `UIUX/stitch_project_continuation_hub/a_wide_landscape_cinematic_composition_of_a_near_future_rooftop_athletic_sports/screen.png` | User-supplied design reference | Publisher clearance pending | Not verified for distribution | Retained for recoverability only; runtime UI now selects the three profile-specific illustrations above. Not a playable map texture or gameplay asset; do not distribute pending rights confirmation.
Switchyard roof dressing | `Assets/Scripts/Map/SwitchyardGenerator.cs` | Project-original collider-free procedural geometry | Yes | Source inspected | Deck seams, parapet, vents and skyline reflect the supplied rooftop direction; no competitive geometry change.
Archived Switchyard project-original roof textures | `ArtArchive/RooftopTextures/RooftopConcrete_ProjectOriginal*` and `RooftopCladding_ProjectOriginal*`; albedos generated for this project with OpenAI image generation, with normal and packed URP mask PNGs derived by `Tools/Generate-RooftopPbrMaps.ps1`. Provenance and prompts: `docs/acceptance/PART0_SWITCHYARD_PROJECT_ORIGINAL_TEXTURES.md`. Terms: https://openai.com/policies/terms-of-use/ | Project-original, AI-assisted albedo and project-derived data maps; OpenAI assigns its rights in Output to the user between the parties, subject to applicable law and terms | No per-asset purchase | Provenance recorded; output similarity/legal review pending | Retained outside Unity `Assets` and excluded from runtime; it is not used by `SwitchyardGenerator`.
Archived Switchyard concrete/cladding textures | `Assets/Art/UnverifiedRooftopTextures/RooftopConcrete.png`, `Assets/Art/UnverifiedRooftopTextures/RooftopCladding.png` | Source and URL not recorded | Unknown — not cleared | Archived; not referenced by runtime or Resources | Preserved outside `Assets/Resources` so the previous files remain recoverable. Do not distribute; recover provenance or remove after owner review.

Current: Switchyard concrete and cladding clone registered Poly Haven `concrete_panels` and `worn_corrugated_iron` URP Lit materials, including matched albedo, normal, and packed mask maps. Four aligned roof/surface atlas variants in `Assets/Art/Surfaces/Atlas/` share those textures with `SwitchyardGenerator`, which applies A/B/C profile tint through the established material roles and scales every surface channel together. Earlier project-original roof textures and their derivation script are retained outside Unity `Assets` as unused source material.
Production: Gate G render review must identify any uncovered environment roles before adding more surface sets. Character body materials (friendly/enemy/team variants) and cosmetic weapon wraps still need reviewed production PBR materials.

Sources to investigate:
- ambientCG (CC0)
- Poly Haven (CC0)
- Kenney.nl (CC0)
- Original creation (preferred)

## ICONS/UI GRAPHICS
Radar ping silhouettes | `Assets/Scripts/UI/RadarPingSprites.cs` | Project-original procedural vector shapes rasterized at runtime; no third-party artwork | Yes | Source inspected; Unity/device visual review pending | Cached 32 px monochrome arrow, diamond, roundel, and launch-triangle sprites distinguish Go Here, Enemy Here, Group Up, and Pad Ready independently of color. No gameplay geometry, localization text, or enemy visibility changes.
Weapon portraits | `Assets/Resources/UIUX/WeaponPortraits/weapon_arc.png`, `weapon_zip.png`, `weapon_brick.png` | Generated in-editor from the registered Quaternius Low Poly Animated Guns (FPS Pack), CC0 | Yes, source verified; portrait appearance review pending | Preparation cards and touch controls load these first | Editor-generated 768×384 white silhouettes with transparency from the same ARC/ZIP/BRICK weapon prefabs used in-game. Import rules disable mipmaps, clamp, use bilinear filtering, and cap at 1024. Existing fallback assets at `Assets/Resources/UIUX/weapon_arc.png`, `weapon_zip.png`, `weapon_brick.png` remain; their individual provenance/license is still unverified.
Shared glyphs | `Assets/Resources/Fonts/MaterialSymbols-Outlined.ttf`, `MaterialSymbols-Filled.ttf`, `Assets/Scripts/UI/UIIcons.cs` | Material Symbols, Apache 2.0 (Google) | Yes, subject to upstream license terms | Source attribution recorded in code; retained license-file audit pending | Existing glyph subset serves cosmetic categories and Settings; `flight_takeoff` is used for pad controls. Do not add glyph names/codepoints without confirming font coverage.

Current: Material Symbols glyphs, existing UIUX touch-control weapon art, and project-original geometric radar ping sprites.
Production still needs source/provenance and portrait-quality review for the existing weapon silhouettes. Pad/cosmetic/Settings symbols are already present in current UI callsites. Radar ping silhouettes require visual review at HUD size and in grayscale.

Sources:
- Google Material Symbols (Apache 2.0; existing local font files)
- Custom creation (project-original radar shapes)

2026-10-03 installed travel challenges | Assets/Scripts/Config/OfflineChallengeConfig.cs, Core/OfflineChallengeRuntime.cs, UI/OfflineChallengeScreen.cs, Core/StartupConnectionScreen.cs | Project-original authored code, shared Unity primitive prefabs, procedural UI geometry | Yes | Source inspected; device/gameplay acceptance pending | Twelve local drills use existing TutorialTarget/TutorialMarker/Player/Bot prefabs and the unchanged Switchyard. No Stitch bitmap, streamed art, paid entitlement or new third-party asset imported. Challenge/save schema v2; completion Bronze only, Silver/Gold disabled until reviewed target-device tuning. Startup route motif uses original bars/rings/diamond, not missing font glyphs. These are graybox/presentation assets, not accepted final production models or bundle contents.
2026-10-02 menu parity pass | Assets/Scripts/UI/UIKit.cs, MenuLayout.cs, UiBorder.cs | Project-original procedural Unity UI geometry | Yes | Source inspected | Shared rounded surfaces, four-edge frames and button states; no Stitch/remote bitmap imported. Runner previews reuse AvatarRigVisual/CosmeticLook graybox models; these are not production character art. See docs/audit/UIUX_PARITY_2026-10-02.md for limitations.

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
Milestone C (Network): ⚠ Fusion SDK/App ID and Simple KCC imported; authority, adapter, Linux server and six-client acceptance remain open
Milestone D (Validation): ⚠ Audio/VFX placeholders - NEED ORIGINAL OR LICENSED before external playtest
Milestone E (Product Shell): ⚠ Fonts, icons, UI graphics - NEED LICENSED
Milestone F (Monetization): ⚠ Google Play / AdMob - BLOCKED on accounts
Milestone G (Closed Launch): ALL ASSETS MUST BE VERIFIED COMMERCIAL LICENSES
Milestone H (Release): COMPLETE REGISTER REQUIRED FOR STORE SUBMISSION

## ACTION ITEMS

### 2026-10-03 playable presentation pass

The shared beveled armor mesh in `AvatarRigVisual`, capsule limbs, visor details,
weapon-ready procedural pose, rooftop equipment face panels and safety stripes are
original project-authored procedural geometry/code. No downloaded model, texture,
animation or third-party license was added by this pass. Existing material colors
were adjusted; the existing Home illustration's provenance is unchanged. These
changes improve the prototype presentation and do not certify final production art.

## WORLD ART, GATE G START (2026-10-09, docs/design/07_WORLD_ART.md)
All CC0 1.0 (https://creativecommons.org/publicdomain/zero/1.0/): commercial use allowed, no attribution required. Source URLs are listed in this register; where present, source notes are also kept in `LICENSE_*.txt` beside the assets.
Poly Haven HDRI sunset_jhbcentral | https://polyhaven.com/a/sunset_jhbcentral | CC0 | Yes | Yes | Assets/Art/Sky/Sky_A_*.jpg, tonemapped JPG downscaled to 4096x2048
Poly Haven HDRI portland_landing_pad | https://polyhaven.com/a/portland_landing_pad | CC0 | Yes | Yes | Assets/Art/Sky/Sky_B_*.jpg
Poly Haven HDRI homecoming_center_rooftop | https://polyhaven.com/a/homecoming_center_rooftop | CC0 | Yes | Yes | Assets/Art/Sky/Sky_C_*.jpg
Poly Haven textures blue_metal_plate, concrete_panels, metal_plate, worn_corrugated_iron | https://polyhaven.com/a/blue_metal_plate; https://polyhaven.com/a/concrete_panels; https://polyhaven.com/a/metal_plate; https://polyhaven.com/a/worn_corrugated_iron | CC0 1.0 | Yes | Yes | Assets/Art/Surfaces, 1k, repacked for URP; `Assets/Art/Surfaces/Atlas/` contains derived aligned albedo/normal/mask atlases with wrapped gutters; exact source URLs retained in `Assets/Art/Surfaces/LICENSE_CC0_PolyHaven.txt`
Poly Haven models exterior_aircon_unit, utility_box_02, security_light, barrel_03, plastic_crate_02, power_box_01 | https://polyhaven.com/a/exterior_aircon_unit; https://polyhaven.com/a/utility_box_02; https://polyhaven.com/a/security_light; https://polyhaven.com/a/barrel_03; https://polyhaven.com/a/plastic_crate_02; https://polyhaven.com/a/power_box_01 | CC0 1.0 | Yes | Yes | Assets/Art/Props; adjacent `LICENSE_CC0_PolyHaven.txt` records the six source URLs, CC0 terms, and license re-verification on 2026-10-11; `SOURCE_SHA256.txt` records hashes for 45 local FBX/texture files. Original retrieval dates and historical page snapshots remain unavailable. Unity reimported 39 texture files and applied sRGB diffuse, linear data, NormalMap normal, and Android ASTC 6×6 at 512 px. Thirty-eight unique FBX-referenced filenames resolve; the extra plastic-crate diffuse is not FBX-referenced and the FBX has no diffuse reference. The model/texture files are untracked in Git at this audit, so a clean checkout will not include them until committed. Material audit: 11 unique source URP Lit materials, 10 opaque and one transparent, no cutout; source materials bind diffuse/normal maps but not the restored metal/roughness/opacity maps. The Unity bake now publishes eight mapped atlas variants across five eligible prefabs; the crate and currently opaque opacity-linked variants remain excluded. Runtime rendering, mip behavior, draw formation, memory, and device checks remain open. See docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md.
Quaternius Universal Animation Library (Standard) | https://quaternius.com/packs/universalanimationlibrary.html (via https://opengameart.org/content/universal-animation-library) | CC0 | Yes | Yes | Assets/Art/Characters/Operator/UAL_Operator.fbx: mannequin mesh, Humanoid rig, 8 clips used
Quaternius Low Poly Animated Guns (FPS Pack) | https://opengameart.org/content/low-poly-animated-guns | CC0 | Yes | Yes | Assets/Art/Weapons: Rifle (ARC), P90 (ZIP), Shotgun (BRICK), recoloured
Project-original procedural textures | generated by script | Project-original | Yes | Yes | Assets/Art/Vfx/vfx_soft_dot.png, vfx_beam.png; Assets/Art/Surfaces/hazard_stripes_albedo.jpg
HOTDROP launch-pad landing shell | Project-authored runtime mesh in `Assets/Scripts/Pads/LaunchPadBehaviour.cs` | Project-original | Yes | Yes | Shared offline/network beveled mesh; contains no imported artwork; original cylinder bounds/scales and gameplay colliders retained; Unity render/performance acceptance pending
HOTDROP pilot rim-lit shader | Project-authored `Assets/Resources/Shaders/CharacterRimLit.shader` | Project-original ShaderLab/HLSL | Yes | Yes | Single-pass URP lighting with view-angle Fresnel rim; no third-party code, texture, geometry, or license

[ ] Select and license font (Inter/Roboto/Noto recommended)
[ ] Create or license all audio assets (Kenney.nl CC0 as baseline)
[ ] Create or license all VFX (original particle systems)
[~] Create or license character mesh/rig/animations (~12 core anims) - CC0 Quaternius mannequin + 8 clips in use; outfitted character still pending
[x] Create or license 3 weapon meshes (CC0 Quaternius FPS Pack, 2026-10-09)
[~] Create or license pad mesh — project-original 16-segment beveled landing shell generated in `LaunchPadBehaviour.SharedVisualMesh` and shared by offline/network presentation; collision geometry unchanged; Unity visual/performance acceptance pending.
[~] Create or license ~15-20 modular environment pieces - 6 CC0 Poly Haven props + 4 CC0 PBR surfaces in use
[ ] Create or license icon set
[x] Import user-supplied Photon Fusion 2 and Simple KCC; configure supplied App ID
[ ] Review publisher obligations under Photon SDK/addon/service terms before release
[ ] Set up Google Play Console (for Milestone F+)
[ ] Set up AdMob account (for Milestone F+)
[ ] Verify every asset license before Milestone G
