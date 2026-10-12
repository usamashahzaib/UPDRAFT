# Audio requirements

## Goal

Use movement and combat sound to communicate trajectory, threat, and match
phase without requiring players to stare at a meter.

## Required sound groups

- Weapon report: body, mechanism, transient, and environment tail, mixed per
  weapon and distance. Spatial reports now use the Bible's full-mix below 15 m,
  filtered-body 15–under-40 m, and distant-report 40–50 m bands.
- Flight: velocity-linked wind, cloth flutter, and pitch shift when diving.
- Pad: pneumatic launch cue audible to nearby opponents, with charge/launch
  timing that supports counterplay.
- UI: short positive and negative feedback sounds; distinct confirmed hit and
  elimination tones.
- Match score: hub loop and match layers that respond to phase. Full adaptive
  stems are a later content requirement; do not claim the existing synthesized
  cues meet this requirement.

## Player controls and accessibility

Expose separate Master, Music, Effects, and Voice levels, mono output, haptic
intensity, and reduced motion. Settings now include a mono-output switch using
Unity's audio-device configuration and an adjustable mobile haptic-strength
control. Android uses amplitude-controlled vibration where supported; iOS uses
intensity-controlled impact feedback on supported OS versions and the system
vibration fallback on older versions. Captions for nearby pad launch, arm, and destroy
cues are implemented as short in-match text notices and can be disabled in
Settings. Reduced Motion is persistent: it removes weapon camera kick and
impact shake, and reduces interface movement. Authoritative recoil patterns and
weapon balance remain unchanged; only the camera presentation is suppressed.
Gameplay sound captions cover objective-zone activation, contest and score
milestones; nearby pad launch, arm and destruction; and clear-line-of-sight
distant ARC, ZIP, and BRICK fire, plus nearby weapon fire within the existing
10 m caption band. Remote weapon captions require clear line of sight, exclude
the local player's own shots, and share the existing per-weapon rate limit.
Flight wind currently emits a caption once when the local
player enters flight. Audible remote-player footstep events also publish a
rate-limited caption within the shared spatial-audio distance; the local
player's own steps do not produce a caption. The existing local feedback cues
also caption damage taken, low health, confirmed hits, and confirmed
eliminations. The local own-death drone captions as “ELIMINATED” when gameplay
sound captions are enabled. Offline damage
captions use the same cue publisher without replaying the direct player
AudioSource sound. The localized fixed-choice quick-chat wheel is text-only and
uses the existing UI tap sound; reviewed regional voice packs and contextual
threat-callout audio remain unimplemented.
Relay countdown beeps caption the visible number, the GO cue uses the existing
localized GO label, and result cues caption the localized VICTORY, DRAW, or
DEFEAT label. Offline and online Relay use the same result mapping; cancelled
online matches do not announce a result. Victory/defeat cues use the existing
priority-80 announcer tier.
Other critical-cue caption coverage and voice-priority behavior still need
acceptance; do not treat the implemented subset as complete audio coverage.
Damage, low-health, confirmed-hit, and confirmed-elimination cues use the
existing priority-20 threat tier, ahead of launch/landing (40),
objective/announcer (80), and UI (160) cues.

## Remaining acceptance

Mix and verify on a low-end phone speaker and headphones. Confirm distant shots
are directional, critical cues win voice priority, mono does not erase team
identity, and reduced motion disables camera kick and unnecessary screen shake.
Measure haptic strength and verify mono-mode behavior on Android and iOS devices;
the editor/batch environment cannot establish physical device behavior.

Fixed-choice teammate pings use distinct generated SFX for enemy here, group up,
pad ready, and go here. Each received teammate ping also publishes its existing
localized kind label through the gameplay sound-caption path, so the type remains
identifiable when its projected world caption is hidden. Sound captions retain
their existing preference and priority behavior. Device mix and concurrent-ping
readability remain open in docs/acceptance/PART5_RADAR_TEAM_CUES.md.
