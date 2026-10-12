# HOTDROP implementation rules

Before changing the game, read `docs/design/00_PILLARS.md`, the one design spec
for the task, and `docs/design/02_ART_TOKENS.json` when it exists. Do not read
the full design bible for routine implementation work.

- Follow the accepted product decisions in `docs/DECISIONS.md` and the current
  design bible plus its amendments. The amendment wins where it conflicts.
- The owner requested all three presentation directions. Apply them to UI and
  the procedural Switchyard presentation. Authored world assets and
  post-processing follow the approved `docs/design/07_WORLD_ART.md` gate; the
  2026-10-09 decision lifts that gate for asset/post-processing work. Blood and
  gore remain gated by their separate rating review.
- Preserve existing player-facing behavior unless a spec explicitly changes
  it. Read the current implementation and tests before editing.
- Put new balance values in existing config assets/types; do not scatter
  gameplay numbers through code. Do not invent missing rules.
- Route player-facing strings through the project's localization system when
  one is configured. If localization is not configured, record that as a
  prerequisite instead of adding a new framework implicitly.
- Every task report must list the spec rules applied and unmet rules with the
  reason. Do not claim device, network, or platform verification without it.
