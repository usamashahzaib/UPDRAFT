# Home, startup, and settings typography acceptance

## Rules applied

- Keep generated home, first-run appearance, startup, settings, and pause screens on the Bible's 12/17/22 dp roles, with no more than three sizes per state and a 12 dp minimum.
- Preserve existing startup availability/recovery actions, Home routing and status, rig-choice flow, Settings values, control customization, and practice pause/resume behavior.
- Keep A/B/C presentation selection as a player setting that updates the UI and procedural Switchyard without changing gameplay.

## Scope and implementation

The Home, first-run rig choice, startup, and offline pause/settings generators now reference named caption, body/action, and compact-title tokens. The settings graphics note's auto-size maximum now respects the 12 dp caption floor. Removed the remaining inline relative-size markup from the passive HUD sample. The profile-card description and save-status copy were shortened after the existing 16:9 accessibility capture showed both lines clipping. No navigation, state, setting persistence, account, gameplay, or profile-switch behavior changed.

## Verified this pass

- Existing first-run/Home/settings/support capture and physical-size test source was read; tests and captures were not run.
- The existing 1920×1080 accessibility capture was visually inspected. It showed the profile card's description and save-status lines clipped at the right edge. The source copy has been shortened; no post-fix Unity capture was made.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore`: 0 errors, 41 Unity/runtime warnings after the profile-card copy change.
- Source scan finds no numeric font-size arguments, numeric TMP auto-size bounds, numeric direct TMP font-size assignments, or inline size tags across `Assets/Scripts`.
- The Unity runtime C# build succeeds with 0 errors and 41 existing warnings. Focused `git diff --check` passes.

## Unmet

- Run the existing 16:9/20:9 capture and physical-size acceptance in Unity after the copy fix; inspect Home goal copy, first-run rig choice, each settings tab, pause, and startup layout.
- Inspect A/B/C contrast and representative larger-text setting behavior in Unity.
- Home, Startup, and Settings copy has since been migrated under A7. Locale,
  translated-layout, and device review remain open in the corresponding A7
  acceptance records.
