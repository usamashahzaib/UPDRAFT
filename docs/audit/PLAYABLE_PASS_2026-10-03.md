# HOTDROP playable presentation pass — 3 October 2026

Implemented in the existing working tree; previous user changes were retained.

## Player-visible changes

- Home **PLAY NOW** immediately starts standard offline 3v3 Relay with five bots. **ONLINE RELAY** remains a separate explicit action, and Challenges opens the twelve installed drills.
- Home's goal card has a direct action: training before completion, the next incomplete challenge afterward, then Relay once all local challenges are complete.
- Both local players and bots now rebuild the current runner visuals. Shared original beveled armor, rounded limbs, visor and back accents replace the sharp block silhouette. The procedural arms hold a weapon-ready pose. Capsule and hitbox geometry remain unchanged.
- Runtime rebuilding also removes loose legacy prefab body parts, preventing the old block body from rendering over the new runner. Team recoloring binds the new torso explicitly. The local player's overhead marker is hidden to keep aiming clear; other actors retain their team identifiers.
- The Home runner preview now faces its camera instead of showing the character's back.
- Cooler rooftop decking and structure materials, fabricated cover panels, safety bands and platform stripes give the arena a consistent industrial-sport treatment. Added dressing is collider-free; its meshes are combined by material to avoid one draw per decorative part.
- Challenge tracker is narrower, uses shorter instructions and left-aligned progress. Full instructions remain on the detail screen.
- The scoreboard is slimmer, clock and scores share one row, objective markers stay below the top HUD, and long teammate names stay on one line. Character-choice overlays are dismissed when entering Home or gameplay.
- Successful challenges promote Next; failures promote Retry. Final challenge labels its return destination. Results show timeout/elimination feedback and previous-best time comparisons.

## Validation

- EditMode: 296 passed, zero failed (`ProjectHotdrop/TestResults/PlayableEditMode.xml`).
- Full gameplay suite after legacy-body and overlay fixes: **18 passed, zero failed**, 238.51 seconds (`ProjectHotdrop/TestResults/PlayableFinalPlayMode.xml`).
- Subsequent preview-framing/team-cue pass: **4 focused PlayMode tests passed**, including real Home → six-player match → Home and physical UI sizing (`PlayableVisualFinal.xml`). After the final HUD edits, the same **4 tests passed again**, process exit 0 (`PlayableHudFinal.xml`).
- Visual review caught and repaired two issues missed by the initial green run: duplicate legacy body meshes and an active runner-choice overlay. Assertions now guard both.
- Added a real-scene Play Now regression: click Home's primary action, verify six players and active match after countdown, return Home and verify actor cleanup. The existing visual capture checks now require all three Home play actions to fit without scrolling.

## Scope and limitations

This is an implemented prototype presentation and flow improvement, not a claim of final production art or completed online acceptance. No monetization/progression gate was enabled. Android touch comfort, thermal performance and six-client networking still require their recorded device/staging checks. Camera-rendered evidence does not include the immediate-mode touch control overlay.

No third-party art was added. See the asset register for the original procedural geometry entry.

## Android delivery — 4 October follow-up

Built and signature-verified `ProjectHotdrop/Builds/Android/HotdropPlayable_2026-10-04.apk` (88,350,817 bytes, Android API 28+, ARM64). See [build receipt](PLAYABLE_BUILD_2026-10-04.md) for the SHA-256, packaging recovery, test provenance and exact source boundary. Two concurrent HUD edits made after player compilation are excluded from this APK. No physical phone was attached for installation or gameplay acceptance.
