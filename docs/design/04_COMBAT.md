# Combat and weapon requirements

## Goal

Make hits responsive and fair across offline play, prediction, and authoritative
server resolution.

## Weapon rules

- Keep fictional weapon names ARC, ZIP, and BRICK. Borrow generic silhouette
  families only; no manufacturer names, logos, serial marks, or copied trade
  dress.
- Store damage, cadence, range, spread, recoil curve, and head multiplier in
  authoritative balance assets. Bible targets (TTK 0.9–1.4 s, head multiplier
  1.5x, hitscan under 25 m) are [ESTIMATE] and must not override current live
  settings without balance review.
- Keep the network camera aim-ray distance and minimum submitted aim-point
  distance in the shared `MatchRulesConfig`; the server uses the same upper
  limit as the client ray and still resolves hits within each weapon's own
  configured maximum range.
- Recoil uses a configured authored pattern plus bounded spread, not pure
  random recoil. ARC, ZIP, and BRICK currently have eight-shot vertical
  multiplier curves in their weapon assets; offline and online local feedback
  use the same curve. These are starting patterns and require playtest tuning.
- The stationary/moving grounded spread-state threshold is shared through
  `MovementConfig`, so offline fire and authoritative online fire classify the
  same ground velocity consistently.
- Pad launch applies the configured 0.3 s weapon lockout [ESTIMATE] to create
  counterplay; tune after playtest.
- Never show a hit marker until authoritative damage is confirmed online.
- Keep muzzle obstruction order consistent across offline, prediction, and
  server. Walls and pads stop shots; verify every cover/elevation case.

## Gore setting

The amendment defines Off, Stylized, and Realistic. Realistic is available only
for B/C product builds and uses short-lived pooled effects, no dismemberment.
Maximum shipped content determines rating; regional defaults come from server
configuration. Rating and regional rules remain [VERIFY]. Do not make local
client preferences override server-mandated limits.

## Animation and sound acceptance

Each weapon needs a matching portrait and equipped mesh, ADS sight picture,
first-person reload beats (mag-out, mag-in, bolt), and readable third-person
silhouette at 40 m. Each shot has body, mechanism, transient, and environment
tail layers. Device and six-client cover tests remain required.
