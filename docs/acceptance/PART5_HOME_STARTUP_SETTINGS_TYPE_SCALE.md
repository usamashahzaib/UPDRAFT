# Home, startup, and settings typography acceptance

## Rules applied

- Keep generated home, first-run appearance, startup, settings, and pause screens on the Bible's 12/17/22 dp roles, with no more than three sizes per state and a 12 dp minimum.
- Preserve existing startup availability/recovery actions, Home routing and status, rig-choice flow, Settings values, control customization, and practice pause/resume behavior.
- Keep A/B/C presentation selection as a player setting that updates the UI and procedural Switchyard without changing gameplay.

## Scope and implementation

The Home, first-run rig choice, startup, and offline pause/settings generators now reference named caption, body/action, and compact-title tokens. The settings graphics note's auto-size maximum now respects the 12 dp caption floor. Removed the remaining inline relative-size markup from the passive HUD sample. No navigation, state, setting persistence, account, gameplay, or profile-switch behavior changed.

## Verified this pass

- Existing first-run/Home/settings/support capture and physical-size test source was read; tests and captures were not run.
- Source scan finds no numeric font-size arguments, numeric TMP auto-size bounds, numeric direct TMP font-size assignments, or inline size tags across `Assets/Scripts`.
- The Unity runtime C# build succeeds with 0 errors and 41 existing warnings. Focused `git diff --check` passes.

## Unmet

- Run the existing 16:9/20:9 capture and physical-size acceptance in Unity after implementation; inspect Home goal copy, first-run rig choice, each settings tab, pause, and startup layout.
- Inspect A/B/C contrast and representative larger-text setting behavior in Unity.
- Localization remains an A7 prerequisite because Unity Localization is not configured.
