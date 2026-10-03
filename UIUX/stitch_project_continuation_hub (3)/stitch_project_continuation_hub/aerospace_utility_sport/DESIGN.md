---
name: Aerospace Utility Sport
colors:
  surface: '#0b1322'
  surface-dim: '#0b1322'
  surface-bright: '#31394a'
  surface-container-lowest: '#060e1d'
  surface-container-low: '#141c2b'
  surface-container: '#18202f'
  surface-container-high: '#222a3a'
  surface-container-highest: '#2d3545'
  on-surface: '#dbe2f8'
  on-surface-variant: '#bbc9cb'
  inverse-surface: '#dbe2f8'
  inverse-on-surface: '#293041'
  outline: '#859396'
  outline-variant: '#3c494b'
  surface-tint: '#38d9ec'
  primary: '#7cefff'
  on-primary: '#00363c'
  primary-container: '#30d5e8'
  on-primary-container: '#005862'
  inverse-primary: '#006973'
  secondary: '#ffb2b7'
  on-secondary: '#67001b'
  secondary-container: '#930d2d'
  on-secondary-container: '#ff9da4'
  tertiary: '#ffda88'
  on-tertiary: '#3f2e00'
  tertiary-container: '#e8bc54'
  on-tertiary-container: '#654c00'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#93f1ff'
  primary-fixed-dim: '#38d9ec'
  on-primary-fixed: '#001f23'
  on-primary-fixed-variant: '#004f57'
  secondary-fixed: '#ffdadb'
  secondary-fixed-dim: '#ffb2b7'
  on-secondary-fixed: '#40000d'
  on-secondary-fixed-variant: '#8f092b'
  tertiary-fixed: '#ffdf9b'
  tertiary-fixed-dim: '#edc157'
  on-tertiary-fixed: '#251a00'
  on-tertiary-fixed-variant: '#5b4300'
  background: '#0b1322'
  on-background: '#dbe2f8'
  surface-variant: '#2d3545'
typography:
  headline-xl:
    fontFamily: Space Grotesk
    fontSize: 40px
    fontWeight: '700'
    lineHeight: 48px
    letterSpacing: -0.02em
  headline-xl-mobile:
    fontFamily: Space Grotesk
    fontSize: 30px
    fontWeight: '700'
    lineHeight: 36px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Space Grotesk
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 34px
    letterSpacing: -0.01em
  headline-lg-mobile:
    fontFamily: Space Grotesk
    fontSize: 22px
    fontWeight: '700'
    lineHeight: 28px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Space Grotesk
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 26px
    letterSpacing: 0em
  title-lg:
    fontFamily: Hanken Grotesk
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: 0.01em
  title-md:
    fontFamily: Hanken Grotesk
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 22px
    letterSpacing: 0.01em
  body-lg:
    fontFamily: Hanken Grotesk
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 22px
    letterSpacing: 0em
  body-md:
    fontFamily: Hanken Grotesk
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
    letterSpacing: 0em
  body-sm:
    fontFamily: Hanken Grotesk
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
    letterSpacing: 0.01em
  label-data-lg:
    fontFamily: JetBrains Mono
    fontSize: 18px
    fontWeight: '700'
    lineHeight: 22px
    letterSpacing: 0.02em
  label-data-md:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 18px
    letterSpacing: 0.03em
  label-data-sm:
    fontFamily: JetBrains Mono
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 14px
    letterSpacing: 0.05em
  label-caps:
    fontFamily: Space Grotesk
    fontSize: 12px
    fontWeight: '700'
    lineHeight: 14px
    letterSpacing: 0.08em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1rem
  margin: 1.5rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-lg: 1.25rem
  space-xl: 2rem
---

## Brand & Style

This design system establishes a high-performance, athletic aerospace utility aesthetic tailored for competitive 3v3 landscape mobile play. It merges the precision of flight test avionics with the energetic tactility of rooftop sports facilities perched above a coastal megacity.

### Emotional Target & Core Pillars
- **Aeronautical Precision:** Instrumentation clarity, zero latency perception, clean flight paths, and calibrated confidence.
- **Athletic Brutalism & Utility:** Physical grip textures, powder-coated metals, weathered structural concrete, and purposeful high-contrast visual cues.
- **Controlled Velocity:** Rapid tactical ingestion of mission-critical data (ammo, capture status, threat vectors) with zero ambient noise or decorative clutter.

### Aesthetic Rules
- **Tactile Metaphors:** UI surfaces read as matte-anodized alloy panels, carbon-stitched strapping, and weather-resistant polymer trims. Avoid glossy skeumorphism or hyper-ornate fantasy frames.
- **Air Route Graphics:** Visual guidance utilizes calibrated flight notation—altitude indices, trajectory dashes, wind-vector ticks, split flight arrows, and circular drop-zone reticles.
- **Tournament Legibility:** High contrast, strict tabular data alignment, and decisive color-coded friend-or-foe telemetry that maintains situational awareness during active touch manipulation.

## Colors

The palette is engineered around high atmospheric contrast and immediate split-second identification under direct outdoor sunlight or low-light mobile sessions.

### Key Palettes & Token Mappings
- **Canvas & Surfaces:**
  - `bg-deep`: `#101827` (Base tactical canvas, infinite backdrop, root viewport shade).
  - `surface-panel`: `#1B2940` (Raised modular instrument panels, flyout containers, dock frames).
  - `surface-elevated`: `#263A59` (Selected states, active tabs, floating tactical overlays).
  - `surface-structural`: `#49627B` (Panel borders, framing rails, telemetry dividing struts).
  - `surface-world`: `#C3B59E` (Raw concrete/weathered runway neutral, structural badges).
- **Signal & Telemetry:**
  - `action-cyan`: `#30D5E8` (Friendly player signatures, primary action targets, capture routes).
  - `action-cyan-deep`: `#138EA3` (Depressed states, secondary friendly tracks, shaded progress fills).
  - `hazard-coral`: `#FF6376` (Enemy combatants, incoming ordnance, catastrophic objective loss).
  - `hazard-coral-deep`: `#B93A50` (Enemy suppression barriers, critical zone contested markers).
  - `objective-gold`: `#FFD166` (Neutral capture zones, core cargo, tournament victory milestones).
  - `signal-success`: `#7ED68A` (Shields active, loadout locked, match ready).
  - `signal-warning`: `#FFB45C` (Low ammunition, out of bounds vector, structural breach).
  - `surface-disabled`: `#536176` (Locked equipment, uncharged tacticals, empty slots).
- **Text & Contrast Hierarchy:**
  - `text-primary`: `#F4F7FB` (Pure contrast tabular stats, primary labels; achieves >10:1 ratio against `bg-deep`).
  - `text-secondary`: `#BBC6D8` (Equipment names, mission conditions, sub-headers).
  - `text-muted`: `#7F8CA3` (Inactive units, telemetry units `M/S`, metadata, timestamps).

### Color Rules in HUD & Navigation
- Never blend friend (`#30D5E8`) and foe (`#FF6376`) hues within the same tactical cluster.
- In-game objective markers must shift instantly between `objective-gold`, `action-cyan`, and `hazard-coral` based on zone control percentage.

## Typography

Typography functions as flight instrumentation. Readability at high speed under thumb occlusion dictates every scale choice.

### Font Pairings & Roles
- **Display & Section Headers (`Space Grotesk`):** Industrial, geometric sans-serif delivering structural presence. Used for victor banners, game-mode titling, and match phase updates.
- **Operational Copy & UI Flow (`Hanken Grotesk`):** Razor-sharp, clean grotesque for loadout specs, player tags, team logs, and system settings.
- **Flight Data, Ammo & HUD Telemetry (`JetBrains Mono`):** Monospaced characters with mandatory `tabular-nums` formatting to prevent score, ping, coordinate, and ammo readout jitter during real-time combat.

### Typographic Directives
- **Case Formatting:** Short competitive labels (e.g., `ALT`, `RNG`, `SEC-03`, `DEFEAT`, `VICTORY`, `READY`) must use uppercase with wide tracking (`0.08em`). Descriptive sentences and options use standard sentence case.
- **Zero Layout Shift:** Dynamic scores and timers must use `label-data-lg` or `label-data-md` to maintain spatial anchors across changing digit widths.

## Layout & Spacing

The layout is anchored to a native target canvas of **2400x1080** (20:9 ultrawide mobile landscape), accommodating dynamic aspect ratios down to 16:9 and up to 21.5:9 tablets and folding devices.

### Landscape Thumb Ergonomics & Cutout Safe Zones
- **Horizontal Margins (Hardware Cutouts):** Lateral canvas margins are fixed to dynamic safe boundaries: `48dp` on the left and right edges (or programmatic device notch depths) to keep thumb interactions clear of curved device bezels and camera islands.
- **Dual Thumb Clusters:** All live interactive controls (ultimates, fire triggers, tactical shifts, ping dial) reside strictly within the bottom-left and bottom-right 35% ergonomic arc radii.
- **Passive HUD Center:** The upper and middle horizontal screen spans are reserved for line-of-sight flight paths, objective bars, compass needles, and score headers.
- **Touch Target Integrity:** Every interactive zone enforces a hard minimum bounding box of **48x48dp**, regardless of internal icon scale.

### Grid Rhythm & Nine-Slice Layout
- **Modular Shell:** Menu layers and in-game pause overlays operate on a 12-column fluid grid system bounded by `margin: 1.5rem` (`24dp`) and `gutter: 1rem` (`16dp`).
- **Unity Nine-Sliced Canvas:** Structural panels utilize 3-sliced horizontal bars and 9-sliced panels with fixed 16px corner anchors and expandable center textures to prevent pixel deformation across varied screen densities.

## Elevation & Depth

Visual hierarchy uses physical material tiering, surface luminance transitions, and directional hazard framing rather than generic soft drop shadows.

### Material & Surface Hierarchy
- **Level 0 (World Space Canvas):** Camera render viewport / Match environment.
- **Level 1 (Sub-Panel Shell - `#101827`):** Recessed equipment docks, base tactical drawer containers with 90% opacity, bordered by subtle 1px `#49627B` framing.
- **Level 2 (Raised Utility Instrument - `#1B2940`):** Core interactive card surfaces, tactical map housings, mini-scoreboards.
- **Level 3 (Elevated / Active State - `#263A59`):** Selected operative modules, active loadout slots, active objective capture rings.
- **Level 4 (HUD Warning / Urgent Overlay):** High-priority warning indicators and victory callouts utilizing 1px `#30D5E8` or `#FF6376` edge glows (`0 0 12px rgba(48, 213, 232, 0.35)`).

### Outlines & Border Engineering
- Avoid heavy, muddy black shadows. All surfaces are defined by a crisp 1px structural border (`#49627B` at 60% opacity) that gives elements a precision-machined, anodized sheet metal look.
- Inactive panels use low-contrast borders; active or hovered elements switch borders to full-intensity `action-cyan` or `hazard-coral`.

## Shapes

The form language is disciplined, athletic, and mechanical. Shapes reflect structural aerospace components—chamfered brackets, modular cartridge plates, and reinforced landing frames.

### Corner Radii Guidelines
- **Base Components (Inputs, Chips, Small Buttons):** `0.5rem` (`8px` to `10px`).
- **Standard Structural Elements (Cards, Panels, Modal Windows):** `rounded-lg` (`1rem` / `16px`).
- **Large HUD Shells & Floating Drawers:** `rounded-xl` (`1.5rem` / `24px`).
- **Chamfers & Angles:** In graphic accents and progress indicators, 45-degree corner notches (4px to 8px) may be paired with structural borders to evoke precision CNC-milled hardware.

## Components

### Buttons
- **Primary Action (Deploy, Fire, Confirm):** Solid rectangular block with `roundedness: 2` (`10-12px`). Background: `#30D5E8`, text: `#101827` (`headline-md` weight), 1px solid border `#30D5E8`. Active/pressed state drops to `#138EA3` with a 2dp downward inner translation. Minimum height: `52dp`.
- **Secondary Utility (Loadout, Spectate, Cancel):** Background `#1B2940`, text: `#F4F7FB`, 1px border `#49627B`. On press, background shifts to `#263A59` with a subtle cyan edge tick.
- **Destructive / Threat Action:** Background `#FF6376`, text: `#101827`, border: `#FF6376`.

### Tactical Chips & Status Badges
- Compact labels for operative role, ping, ping route, and match condition.
- Rendered in `#1B2940` with 1px border `#49627B`, `roundedness: 1` (`6-8px`).
- Prefix with tabular monospaced indicators (e.g., `[SYS // 24MS]`, `[ZONE A // CAPTURED]`).

### Cards & Operative Modules
- Nine-slice compatible backing using `#1B2940` base.
- Header strip framed with 1px `#49627B` divider containing flight trajectory markers (`--/-->`).
- Active card selection displays a 2px outer outline of `#30D5E8` and an elevated fill of `#263A59`.

### Selection Controls (Checkboxes, Toggles, Radio Switches)
- **Switches:** Recessed pill frame `#101827` (48x28dp), mechanical slider thumb in `#C3B59E`. Active thumb snaps right, shifting track fill to `#30D5E8`.
- **Radio Buttons / Mode Selectors:** Diamond or chamfered square indicator with an inner solid tick when active. Border `#49627B`, active inner core `#30D5E8`.

### HUD Input & Keypad Fields
- For team chat, callouts, or lobby room codes.
- Background `#101827`, interior border 1px `#49627B`. Active focus introduces a `#30D5E8` caret and border. Text rendered in `JetBrains Mono` (`label-data-md`).

### Match-Specific Components
- **Zone Capture Progress Meter:** Segmented dual-tone bar representing 3v3 team tug-of-war. Cyan (`#30D5E8`) builds from left; Coral (`#FF6376`) builds from right. Neutral center ticks render in `#FFD166`.
- **Flight Reticle & Trajectory Guide:** Concentric, segmented circular landing indicator with directional wind-vector ticks spaced at 90-degree intervals.
- **Operative Health / Fuel Gauges:** Tabular value overlay paired with an angled segmented fill bar that flashes `#FF6376` when depleted below 20%.