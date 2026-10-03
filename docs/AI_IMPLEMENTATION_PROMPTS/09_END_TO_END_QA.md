# Prompt 09 — End-to-end UI/UX verification and Android evidence

```text
Work in E:\Usama\Game on the current ProjectHotdrop checkout after Prompts 01–08 that were actually authorized and completed. Perform a source, Unity, and real-device UI/UX verification pass. Read docs/AI_IMPLEMENTATION_PROMPTS/00_START_HERE.md, current Git diff, milestone status, acceptance records, and all relevant tests first.

Do not redesign screens during the audit unless a reproducible defect requires a focused fix. Do not enable Shop, ads, contracts, or mastery merely to demonstrate them. Preserve feature flags and current gate order.

Audit the connected journeys:
1. Fresh run: Startup → Appearance Choice → nine-step Tutorial → Home.
2. Returning offline: Startup/Home → Offline Relay or Challenge Library → Gameplay → Pause/Resume → Results → Retry/Home.
3. Online when the environment exists: Home → Party or Play → Matchmaking → Reservation → Loadout → Gameplay → Results → Play Again/Home.
4. Product shell: Home → Appearance / Profile / Account / Support / Report → safe Back.
5. Every recoverable failure: No internet, auth unavailable, backend unavailable, server full, queue cancelled, party member dropped, update required, match server failure, reward pending, local save recovery.

Visual acceptance:
- HOTDROP is the only player-facing product name.
- No UPDRAFT, Season, Rank, Pass, Leaderboard, Operations, Mission Control, Tactical Doctrine, fake Telemetry, fake encryption, developer frame label, state switcher, or component modal appears in production UI.
- No military/gore logo PNG is included in Resources, Addressables, scenes, builds, or store assets.
- UI matches the approved Aerospace Utility Sport palette and hierarchy without copying contaminated review dashboards.
- All touch targets are at least 48dp. Test 16:9 and 20:9, cutout simulation, larger text, left-handed controls, and Reduce Motion.
- No clipped text, off-screen modal, unreachable Back, hidden destructive action, duplicate screen, crosshair obstruction, or thumb-zone collision.
- HUD values come from runtime/config, not mock values.

Behavior acceptance:
- Match/scoring/pad/weapon/network authority remains unchanged.
- Settings persist after restart.
- First-run state and body choice persist.
- Online match menu does not pause; offline pause does.
- Results actions fire once; pending rewards retry safely.
- Offline state cannot grant online currency/inventory/progression.
- Account deletion remains two-step and report uses recent aliases.
- Feature-disabled systems disappear cleanly.

Run:
- Unity compile.
- Full EditMode suite.
- Full PlayMode suite.
- Backend test suite when backend-related prompts were implemented.
- Android development build using the repository script.
- Install and exercise on the available Samsung A06 or equivalent reference device when connected.

Collect evidence:
- Exact Git revision and dirty diff summary.
- Build path, size, SHA-256, package/version identifiers.
- Test pass/fail/skip counts and logs.
- Screenshots or recording of fresh run, full match, results/rematch, settings persistence, and representative failures.
- Performance observations on the reference device: sustained FPS/frame time, memory where available, thermal behavior, touch comfort, and visible cutout/safe-area behavior.
- A defect list ranked by player impact, with fixed versus remaining clearly separated.

Do not certify retention, fun, launch readiness, or a milestone from automated tests alone. State exactly what human/device/network evidence is still missing.
```
