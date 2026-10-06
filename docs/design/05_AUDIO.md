# Audio requirements

## Goal

Use movement and combat sound to communicate trajectory, threat, and match
phase without requiring players to stare at a meter.

## Required sound groups

- Weapon report: body, mechanism, transient, and environment tail, mixed per
  weapon and distance.
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
distant ARC, ZIP, and BRICK fire. Weapon captions exclude the local firing range
and are rate-limited. Flight wind currently emits a caption once when the local
player enters flight. There is no quick-chat or threat-callout system in the
current client, so their audio mix/caption behavior has no implementation yet.
Other critical-cue caption coverage and voice-priority behavior still need
acceptance; do not treat the implemented subset as complete audio coverage.

## Remaining acceptance

Mix and verify on a low-end phone speaker and headphones. Confirm distant shots
are directional, critical cues win voice priority, mono does not erase team
identity, and reduced motion disables camera kick and unnecessary screen shake.
Measure haptic strength and verify mono-mode behavior on Android and iOS devices;
the editor/batch environment cannot establish physical device behavior.
