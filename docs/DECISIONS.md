
## 2026-09-28 — Device verification and Milestone B completion pass

Verified on the Samsung SM-A065F (Android 14, Mali-G52) by driving the real APK over adb
(screenshots + injected touches), not only editor tests.

Defects found on device and fixed:
- **Aim speed scaled the wrong way with screen density** (multiplied by dpi instead of dividing),
  ~10x too slow on a 300-dpi phone and different on every device. Aim is now degrees per
  physical inch (`aimDegreesPerInch`, default 45 at sensitivity 1). Joystick radius is in dp.
- **Pause RESUME re-opened pause**: per-frame button flags survived while input was disabled.
  They are cleared in `TouchInputManager.OnDisable`.
- **Spawn floor was saturated team colour** (unreadable, violates "no glossy floors" and
  "colour never the only signal"). Now concrete with a team stripe; also flush with the arena
  floor (the raised 0.5 m floor exceeded the 0.35 m step height).
- **Player spawned facing the spawn-room back wall**: camera yaw was taken before the player was
  placed. Camera now snaps behind the player on spawn and respawn; spawns use the controller-safe warp.
- **Bots always went to Center** (`matchTime = 0` placeholder). They now follow the live
  rotation via `MatchController.Current.ActiveObjectivePosition`.
- **Overtime scored on Center** instead of the zone active at regulation end (West).
  `MatchRulesConfig.GetScoringZone` fixes it; covered by EditMode tests.
- Legacy `Input.gyro` would throw under the new-Input-System-only backend; gyro aim now uses
  `InputSystem.Gyroscope` and is hidden on phones without one.
- Team colours for pads and bodies were absolute (team 0 = cyan) rather than relative to the
  viewer; now relative, with a non-colour symbol (friendly ring, enemy cross) on pads.

Decisions (simplest consistent choices where the spec is silent):
- The tutorial runs on first launch only (`PlayerPrefs["TutorialDone"]`); Home offers replay.
- HUD extras are built at runtime by `MatchHUD` so the generated scene stays unchanged.
- Clock counts down regulation time; overtime shows "OT m:ss".
- Contextual ping kind comes from the crosshair target: visible enemy → ENEMY HERE (location
  only), usable pad → PAD READY, open sky → GROUP UP on the pinger, else GO HERE. Bot teammates
  follow GO HERE/GROUP UP and call out enemies they can actually see.
- Pad: tap places on release; hold > 180 ms shows the arc (green valid / red with reason);
  dragging the finger outside 2x the button cancels at no cost.
- Audio is original synthesis (no third-party audio to license). Priority: threats (20) >
  launch/landing (40) > objective/announcer (80) > UI (160). Obstruction = low-pass through
  Ground/Cover/SpawnBarrier.
- Offline aliases use the same word lists as the backend generator; online aliases always
  come from the server.
- Telemetry queues to `persistentDataPath/analytics_queue.jsonl` (4 MB cap) so offline pad-study
  sessions can be collected with `adb pull`; offline match facts are tagged `offline_sim` and are
  never uploaded as server facts.
- Pad study (spec 25): facilitator panel (tap the Home title 5x) sets cohort
  `study_pads_on` / `study_pads_off`; the no-pad version hides the button, skips the four pad
  tutorial steps and stops bots placing pads.
- Photon App ID must not be committed: `PhotonAppSettings.asset` is git-ignored.
- `.gitattributes` routes binaries through Git LFS (spec 20). Run `git lfs install` in a clone
  before committing binaries; two already-committed TMP binaries need `git add --renormalize`.

Backend (same day):
- Matchmaking is a persistent queue with a composer worker (spec 13): Δ150 window, +50 per 10 s
  after 20 s, capped at Δ350; mirrored compositions only (3v3, duo+solo vs duo+solo, solos vs
  solos); the oldest group is always in the next match; bot-fill after 45 s.
- Match results apply team-average Elo (K=24; skipped for bot-filled, bot-substituted and
  server-failed matches), XP/levels (cap 20), ledger credits and unlocks exactly once.
- Join tickets are 90 s signed JWTs bound to player+match+team; the dedicated server validates
  them through `/api/matches/validate-ticket` using a service token obtained from
  `GameServer:ApiKey` (fails closed when unset).
- Global per-player/IP rate limit (300/min) plus the stricter auth limit; independent
  `Features:MatchmakingEnabled` / `Features:PurchasesEnabled` kill switches (purchases default off).
