# Part 2 Contracts and Mastery palette acceptance

## Acceptance criteria

- Contracts and Mastery semantic surfaces, selected/ready states, actions, and text use active A/B/C profile tokens.
- Bronze, silver, and gold remain recognizable award-tier colors; no progression thresholds, state, reward, or feature gate changes.
- Profile token colors are generated at render time for inline rich text as well as UI graphics.
- Keep the Mastery page's player-facing archive label free of the obsolete `//` divider.
- Rebuilding either page after profile selection uses the selected palette.

## Verified this pass

- The Mastery track tone reads the palette's derived action/objective mix instead of re-deriving it locally, so a live profile switch remaps the track icon and plate along with the rest of the semantic palette.
- The "Permanence Pact" note is sentence-length copy in a narrow right column (about 354 units at the 1280 reference width for 51 characters at 17 dp) and previously relied on the `UiDraw.Text` NoWrap+Overflow default, so it ran past its card. It now wraps inside the card with a two-line box; the same pattern is already used by the card's body copy.
- The Mastery archive label resolves to the localized "LIFETIME PROGRESS" value, with the same fallback in `GoalsPagesView`; neither player-facing source contains the obsolete `//` divider.
- Unity runtime C# build succeeded with 0 errors and 41 existing warnings; no tests were run.

## Review still required

Review all progression states, contrast, and color-vision distinction in the live UI. Contracts and Mastery remain read-only while their server feature gate is disabled.
