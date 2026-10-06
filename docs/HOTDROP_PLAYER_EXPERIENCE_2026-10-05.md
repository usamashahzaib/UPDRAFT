# HOTDROP Player Experience Plan — 5 October 2026

## Product Decision

Keep the existing three-minute 3v3 Relay playable while adding a separate Battle Royale mode. Both modes can share weapon, inventory, cover, avatar, touch-control and visual systems. They need separate match rules, spawn rules, loot tables, scoring and server validation. A loot feature is not complete until it works for both modes and remains fair with Relay respawns.

## Current Evidence

- ARC, ZIP and BRICK are the three configured weapons. Preparation already references `weapon_arc`, `weapon_zip` and `weapon_brick` art, but those are simple white silhouettes rather than pictures of the actual equipped meshes. The online avatar also has one generic primitive gun. Final weapon art requires one distinct game mesh and matching portrait for each weapon.
- Offline and online shooting already trace from the muzzle; the first physical obstruction stops a pellet. Bot sight uses a geometry raycast. These rules need a device and six-client cover matrix, especially for high/low cover, roof edges, moving targets, lag compensation and different player elevations.
- The current radar tracks the player's camera heading, teammates and objective. It does not yet draw the arena footprint, building outlines, floor/elevation cues or enemy pings.
- The game has body styles, weapon wraps, profile progression and an editable touch layout. It does not yet have a battle-loot inventory, backpack levels, arbitrary face/clothing colors, a player-chosen blood color or a separate Battle Royale match loop.
- The playable rooftop now has a facade and parapet presentation pass, while the actual collision/navigation surface remains the current Switchyard arena. Final art needs a section-by-section visual and collision audit so every roof, wall and cover edge reads as the surface the player stands or shoots against.

## Implementation Order And Acceptance

### 1. Combat Readability And Fairness

1. Replace the three weapon silhouettes with portraits rendered from the *same distinct models* equipped by players. Keep attachment, wrap and color changes synchronized in the portrait, lobby and world. No unrelated stock gun photos.
2. Use the camera ray to select an aim point, then resolve each pellet from the muzzle. Show a blocked-shot cue when cover interrupts that line. Check the same obstruction order in offline prediction, online prediction and server damage. Never highlight an enemy through solid cover.
3. Test wall, low cover, roof lip, glass policy, elevated target, self/teammate pass-through, pad, spawn barrier, and network rewind at the configured maximum latency. A hit marker appears only for a server-confirmed damaging hit.
4. Add actual firing feedback to the Fire button: equipped gun image and brief flash on a discharged shot. Empty magazine and reload feedback must look different.

### 2. Character And Arena Art

1. Produce consistently styled human faces, hands, hair, clothing and movement animations with commercial rights recorded in `ASSET_LICENSE_REGISTER.md`. Maintain one shared gameplay capsule and hitboxes across all cosmetic styles.
2. Separate face tone, hair, top, trousers and accent color choices from gameplay power. Support a player-chosen public name subject to server validation, uniqueness policy and abuse moderation. Replicate the selected appearance and name online.
3. Color the world through materials and location-specific vegetation where architecture supports it. Japan-inspired flowering trees and northern Pakistan-inspired conifers are optional biome packages, not props to scatter across an industrial roof. Put vegetation in safe distant or noncompetitive spaces until tested for sightlines and performance.
4. Review each building from gameplay camera heights. A player on a roof must see a coherent roof deck, roof edge and building mass below. Decorative walls must not suggest cover when bullets pass through them; any object that visibly promises cover needs a matching collider and server shot rule.

### 3. Controls, HUD And Map

1. Ship two-, three-, four- and five-finger presets built on the existing per-control editor. Let players move and resize buttons within safe areas; detect intersections with protected HUD and other action buttons. Save a layout per device orientation. Keep all actions at least 48 dp at minimum size.
2. Expose separate hip-fire, focus-aim, vertical and horizontal sensitivity; optional gyro; camera shake; vibration; aim-assist strength within fair server policy; and per-weapon recoil learning aids. Every setting must persist and have a reset action. Do not allow a client setting to modify server spread or damage.
3. Add a visual Fire state, profile name, eliminations/deaths and measured online RTT. Offline play shows no invented ping. Show connection quality without hiding a degraded network state.
4. Draw a navigable minimap from the same map coordinates as gameplay: player position and facing, objective, teammates, building footprint, levels and valid pings. Only expose enemies through a visible or intentionally revealed gameplay event. Test world-to-map conversion on both teams and at every roof level.
5. Audit UI text for Title Case and safe-area overlap at 16:9 and 20:9. Use color as a supporting cue with a symbol/text cue, not as the only signal.

### 4. Shared Loot And Backpack Rules

1. Define item IDs, stack limits, weight/slot cost, maximum carried ammo, drop permissions and backpack capacities for Levels 1/2/3. The inventory UI must show `used / capacity` and the reason a pickup failed. Make take, split, swap and drop operations atomic on the server.
2. On elimination, create one server-owned loot container from the victim's *remaining* eligible items. Consumption and transfer must be finalized before death. Use unique instance IDs and transaction sequence numbers to prevent duplicate pickup or replay.
3. In Battle Royale, persistent death drops the normal remaining inventory. In Relay, keep the short respawn loop: drop only explicitly allowed temporary supplies, expire containers quickly, and give every respawn the standard weapon/ammo baseline. This avoids a runaway snowball where the winning team captures all weapons.
4. Validate distance, line of sight, alive state, backpack capacity, item ownership, match phase and container version on each pickup/drop. Show loot only within interaction range and keep controls clear of Fire/aim zones.
5. Persist cosmetic loadouts and progression separately from consumable match loot. Never grant online currency from an offline loot state.

### 5. Bots, Modes And Release Gates

1. Use normal personal names in play. Bot behavior must obey the same movement, weapon cadence, muzzle obstruction, perception and objective rules as humans. Keep server records that identify AI seats for rating, reporting and abuse controls; the player-facing results can say `Mixed Roster` without a `Bot` tag beside a name.
2. Tune reaction time, aim error, pathing, cover use and pad use by visible difficulty level. Measure objective contribution and win rate across at least 100 seeded offline runs per level, then perform human playtests. Do not silently give one team weaker bots.
3. Build Battle Royale as a separate server-authoritative loop: drop/spawn, shrinking safe area, elimination, loot, spectate and winner. Reuse the shared systems above and keep Relay matchmaking stable.
4. Gate release on one complete phone journey, a six-client online cover/loot test, low-end frame and memory profiling, safe-area screenshots, legal asset records and a signed installable Android build. Do not label the experience complete from editor screenshots alone.

## Change In This Pass

Practice and fill-seat aliases use natural first and family names. The match HUD reads live K/D and profile name from its participant, while online RTT comes from Fusion. The Fire button displays the selected weapon silhouette and flashes when a shot is discharged. The control editor now rejects overlap between action hit targets and keeps them in the safe area; the Ping and Pause default hit targets have been spaced apart. The main action labels now use Title Case. These changes still require Unity compile and phone validation; the test runner is currently blocked before compilation by Unity Package Manager IPC startup.
