# HOTDROP pillars and implementation status

This file is the short entry point for implementation. The v1.1 amendment to
`docs/AI_IMPLEMENTATION_PROMPTS/HOTDROP_DESIGN_BIBLE.md` takes precedence over
v1.0 where they disagree.

## Shared pillars

- HOTDROP is a rooftop combat game whose flight and launch pads are core play.
- Relay remains the existing three-minute 3v3 mode. A Battle Royale mode, if
  built, has separate match, spawn, loot, and server-validation rules.
- Cosmetics do not change gameplay power. Purchases, inventory grants,
  contract completion, and match results must be validated by the server.
- Combat information must remain readable without color alone. Use the
  established ally/enemy shape cues as well as color.
- Prefer clear player language, honest network state, and server-authoritative
  outcomes. Treat unverified claims and proposed targets as such.

## Presentation profiles

The v1.1 amendment defines three incompatible visual identities:

- **A — Stylized Flight:** bright, kinetic and golden-hour; v1.0 art and blood
  policy.
- **B — Grounded Tactical Flight:** desaturated realism and realistic blood
  option; replaces v1.0 palette and blood policy.
- **C — Hybrid:** grounded world with clean stylized UI and readable effects.

**Owner direction: support A, B, and C.** They are selectable UI and Switchyard
world-presentation profiles. The procedural arena changes material tints,
ambient light, sun color/intensity, and sky color without changing gameplay
geometry. The owner-approved `07_WORLD_ART.md` authorizes authored world assets
and tiered post-processing; Gate G art implementation and acceptance remain in
progress. Blood/gore and any maximum gore setting remain separately gated by a
reviewed rating decision.

## Decisions still needed for mechanics

The amendment leaves new-mode respawn and fall-punishment rules undefined.
Relay's current rule is individual respawns after 4 seconds at a team spawn exit,
with up to 1.5 seconds of protection that ends on firing or leaving the spawn
room; a weapon chosen while dead equips on respawn. This behavior is backed by
`MatchRulesConfig`, `MovementConfig`, and the online/offline respawn code. Keep it
for Relay until a reviewed mode-specific decision supersedes it; do not reuse
these rules for another mode by assumption.

The no-ads position is a business decision, not a technical law. Keep current
purchase/ads gates and do not add monetization behavior based on the bible
alone.
