# Prompt 03 — Loadout, gameplay HUD, pad feedback, and settings

```text
Work in E:\Usama\Game on ProjectHotdrop. Implement the production gameplay UI using the repository's authoritative rules and Prompt 01 UI foundation. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md first.

Visual references:
- UIUX/stitch_project_continuation_hub (2)/stitch_project_continuation_hub/screen_07_loadout_preparation_8s_countdown/screen.png
- UIUX/stitch_project_continuation_hub (3)/stitch_project_continuation_hub/screen_08_gameplay_hud_classic_preset/screen.png
- UIUX/stitch_project_continuation_hub (3)/stitch_project_continuation_hub/screen_09_launch_pad_placement_feedback/screen.png
- UIUX/stitch_project_continuation_hub (3)/stitch_project_continuation_hub/screen_10_match_menu_settings_online_live/screen.png

Important: Screens 08–10 contain review-dashboard contamination. Do not implement Telemetry/Operatives/Sector Map tabs, lower design-analysis panels, voice, dash, armor, invented weapons, invented placement physics, fake server tick/FPS/encryption, tactical protocol copy, or military language. Extract only useful composition, control reach, palette, and hierarchy.

Relevant code:
- UI/MatchHUD.cs
- Input/TouchInputManager.cs
- Core/OfflinePauseMenu.cs
- Pads/PadPreviewArc.cs, PadPlacementRules.cs, PlayerPadManager.cs, LaunchPadBehaviour.cs
- Match/MatchFlowController.cs and MatchController.cs
- Config/WeaponConfig.cs, MatchRulesConfig.cs, PadConfig.cs, GraphicsConfig.cs
- Camera/ThirdPersonCamera.cs
- existing combat, pad, touch, physical-size, and smoke tests

Deliver:
1. Eight-second loadout/preparation state with ARC, ZIP, and BRICK. Read every displayed stat from real config. All weapons are unlocked. Do not show sliding, arbitrary power bars, fake optimal ranges, or invented tips. Spawn locked, damage blocked, and pad placement unavailable must be clear.
2. Classic gameplay HUD: compact ally score / timer / enemy score; active objective and next-zone countdown; two teammate cards; honest minimap/objective diagram without unseen enemies; health; ammo; pad charge/cooldown; objective edge marker; network warning; Pause and contextual Ping.
3. Controls supporting simultaneous move, aim, and fire. Fire-drag aims. Jump becomes LAUNCH only while on an armed pad. Focus Aim uses the current camera behavior and separate sensitivity; no fake scope. Add a Simple preset only if it can preserve required actions without hidden conflicts.
4. Pad interaction states driven by real code: tap place, hold-to-preview after configured threshold, steer preview, cancel, valid/invalid reason, arming, armed, damaged, destroyed, expiring, friendly/enemy identity, manual LAUNCH. Use real PadConfig values. Anyone can use armed pads; enemies can destroy them.
5. HUD states for preparation, countdown, live, capturing, contested, next-zone warning, overtime, four-second respawn/spectate/weapon select, reconnecting, labeled bot substitution, low network, AFK warning/removal, and match server failure where supported.
6. Offline PAUSED and online MATCH MENU behaviors. Online never claims to pause. Settings tabs: Controls, Graphics, Audio, Accessibility, Account/Support link. Use real sliders/values, save immediately, provide Reset, and show unsupported options honestly.
7. Customize Controls with move/resize/opacity/reset/save, safe-area limits, overlap prevention, left-handed layout, and persistence.

Do not change authoritative movement, damage, scoring, pad, weapon, respawn, or network rules. Do not introduce crouch, sprint, slide, prone, grenades, healing, inventory, vehicles, hero powers, or mid-life weapon switching.

Validation:
- Existing combat/pad/touch tests remain green.
- Add targeted tests for HUD state mapping, control-layout persistence, online-vs-offline pause semantics, and display values sourced from config.
- Run EditMode plus PlayMode OfflineSliceSmokeTest and UiPhysicalSizeTests.
- Capture real Unity screenshots at 16:9 and 20:9 for loadout, normal HUD, contested objective, valid/invalid pad preview, online Match Menu, and offline Pause.
- Report exact files, pass counts, screenshots, and any state not yet backed by runtime data.
```
