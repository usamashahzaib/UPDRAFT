# Party Room typography and density acceptance

## Rules applied

- Use Bible scale roles with no more than three sizes per screen and a 12 dp minimum.
- Preserve invite-code join/create, backend-provided membership and readiness, leader queue action, leave, refresh, copy, and stream-safe code masking.
- Keep the A/B/C profile colors attached to semantic Party Room roles.
- Reflow the header, code pill, and footer when display density changes while the screen is open.

## Scope and implementation

The Party Room uses 12/17/22 dp caption, body/action, and title roles in both entry and active-party states. Code, alias, and primary-action text use the body role; labels and statuses use the caption role; the screen heading uses the compact title role. Existing 8-character code input, backend callbacks, member ordering, readiness counts, and queue gating are unchanged.

The entry and active-party layouts watch screen dimensions, density, and text scale. Their header/footer heights and code pill controls are recalculated, then the physical-size floor is applied. Copy and refresh buttons use their actual floored width when spacing the active-party code pill.

## Verification

- Source review found no numeric font-size arguments in `PartyRoomView`.
- Runtime and PlayMode test assemblies compiled with 0 errors and 41 existing runtime warnings after the density reflow changes.
- No PlayMode tests, Unity captures, or device checks were run for the latest reflow changes.
- `git diff --check` passed for the current working tree.

## Unmet

- Review code pill, long aliases, and footer fit in Unity at 16:9 and 20:9, and inspect all A/B/C profile contrast.
- Incoming invitations still have no consumer or channel in the current product flow.
- Party Room copy has since been migrated under A7; locale and device review
  remain open in `docs/acceptance/A7_LOCALIZATION_PARTY_ROOM.md`.
