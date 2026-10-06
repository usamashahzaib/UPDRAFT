# HUD and cockpit requirements

## Goal

Keep the Relay fight readable on a phone and make critical team information
understandable without color alone.

## Required arrangement

- Top center: team scores, timer, and Relay status.
- Top left: up to three teammate chips with integrity and pad status; fade after
  five seconds of inactivity [ESTIMATE].
- Top right: minimap and compass.
- Bottom left: movement control.
- Bottom right: Fire, Jump, Pad, and weapon switch, ordered by use frequency.
- Center: dynamic crosshair; do not place persistent status labels there.
- All touch actions: at least 44 px by Bible rule and 48 dp by the current player
  experience plan; implement to the stricter measured target for the device.

## Combat fade

Cockpit Mode fades the roster and radar panels to 15% opacity within 200 ms
while an enemy is within 30 m or the player is firing. It is saved and enabled
by default. These values are [ESTIMATE] until playtested. Keep score, time,
Relay state, crosshair, health, and immediate threat cues readable. Reduced-
motion preference removes camera kick and shake.

## Team markers

Ally uses a chevron marker; enemy uses an outlined diamond. Hit confirmation,
headshot, and elimination need distinct shapes as well as color. Markers must
not reveal enemies through solid cover or outside the mode's reveal rules.

## Map and pings

Map drawing must derive from gameplay coordinates and include footprint,
objective, teammates, elevation, facing, and valid pings. Enemy marks are
limited to visible or intentionally revealed targets. Pings expire after eight
seconds [ESTIMATE] and must identify kind with icon/shape and text or sound.

## Validation

Check 16:9 and 20:9 safe areas, one-handed layout, all supported control
presets, grayscale capture, and enemy/ally in both bright sky and dark
backgrounds. Cockpit fade timing and fade target must be measured in-game.
