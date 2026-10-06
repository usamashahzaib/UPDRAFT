# HUD typography acceptance

## Rules applied

- Use the Bible's exact typography scale and no more than three sizes on one screen.
- Keep active Relay HUD text at or above the 12 dp visible-text floor.
- Preserve score, timer, roster, objective, suit-integrity, combat-caption, and hit-confirmation information.

## Scope and implementation

The active screen 08 Relay HUD, including its touch-control captions, world objective caption, death-state overlay, and transient hit feedback, uses 12, 17, and 22 design dp only. The active HUD hierarchy maps labels/secondary facts to 12, player-facing values and names to 17, and score/hit-marker emphasis to 22. Existing MatchHUD font sizes for the HUD death overlay, health value, hit marker, and combat feedback are aligned to the same three sizes. The results screen has its own role-based type scale; the legacy result header no longer embeds relative-size rich text. Control-editor auto-size ceilings use the title-medium token while preserving the 12 dp floor. Preparation, weapon selection, and tutorial screens are separate states.

No gameplay, network, or telemetry behavior changes.

## Verified this pass

- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:minimal` succeeded with 0 errors and 41 existing warnings.
- Source review confirms active HUD text uses the caption/value/emphasis tokens (12/17/22 dp); touch labels use the 12 dp caption token.
- `git diff --check` passed for the implementation, acceptance record, and tracker edits.

## Acceptance evidence required

- Runtime C# compilation succeeds.
- Inspect the live HUD and death overlay at 16:9 and 20:9, including long pilot names, three-digit scores, combat feedback, sound captions, and the center hit marker.

## Unmet

- Unity visual review on 16:9/20:9 and device-size review were not run in this pass.
- Remaining screen-specific type-scale acceptance is tracked separately; localization and live Unity visual acceptance remain open.
