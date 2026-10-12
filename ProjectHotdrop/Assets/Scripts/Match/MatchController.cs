using UnityEngine;
using System.Collections.Generic;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Bots;
using Hotdrop.Player;
using Hotdrop.Camera;
using Hotdrop.Input;
using Hotdrop.UI;

namespace Hotdrop.Match
{
    /// <summary>
    /// Orchestrates the complete match loop per spec section 5:
    /// Home â†’ Play/Party â†’ Matchmaking â†’ Server reservation â†’ Load â†’
    /// 8s prep (weapon select, objective shown, spawn-locked, no damage, no pads) â†’
    /// 3s countdown â†’ Match â†’ Results â†’ Play again/Home.
    /// This is the offline practice version; networked version uses Fusion.
    /// </summary>
    public class MatchController : MonoBehaviour, IMatchView
    {
        // Bootstrap/scene-setup wiring assigns these from outside (GameBootstrap,
        // SceneSetup), so they're public rather than SerializeField-private.
        [Header("Config")]
        public MatchRulesConfig matchRules;
        public MapConfig mapConfig;
        public MovementConfig movementConfig;
        public BotConfig botConfig;
        public WeaponDefinition[] weaponDefinitions;

        [Header("Prefabs")]
        public GameObject playerPrefab;
        public GameObject botPrefab;

        [Header("Spawn Points")]
        [SerializeField] Transform[] teamASpawns; // 3 exits from north spawn room
        [SerializeField] Transform[] teamBSpawns; // 3 exits from south spawn room

        [Header("References")]
        public ThirdPersonCamera mainCamera;
        public TouchInputManager touchInput;
        public MatchHUD matchHUD;

        // Match state
        MatchFlowController flowController;
        RelayScoringController scoringController;

        List<PlayerController> allPlayers = new List<PlayerController>();
        List<PlayerController> teamAPlayers = new List<PlayerController>();
        List<PlayerController> teamBPlayers = new List<PlayerController>();

        PlayerController localPlayer;
        int localPlayerTeam = 0;
        int localPlayerWeaponIndex = 0;

        // Bot management
        List<BotController> bots = new List<BotController>();
        bool isOfflinePractice = true;

        // Match timing
        float matchStartTime;
        bool matchEnded;
        MatchPhase previousPhase;
        static bool loadBudgetLogged;

        public int ActivePlayerCount => allPlayers.Count;
        public MatchPhase CurrentPhase => flowController?.Phase ?? MatchPhase.Preparation;
        public static MatchController Current { get; private set; }
        public float MatchTime => flowController?.MatchTime ?? 0f;
        public ZoneId ScoringZone => matchRules.GetScoringZone(MatchTime);
        public Vector3 ActiveObjectivePosition => ZoneCenter(ScoringZone);
        public IReadOnlyList<PlayerController> Players => allPlayers;
        public PlayerController LocalPlayer => localPlayer;

        void OnEnable() { Current = this; MatchView.Register(this); }
        void OnDisable() { if (Current == this) Current = null; MatchView.Unregister(this); }

        MatchRulesConfig IMatchView.Rules => matchRules;
        MapConfig IMatchView.MapConfig => mapConfig;
        bool IMatchView.IsEndgame => flowController != null && (flowController.Phase == MatchPhase.Overtime ||
            flowController.MatchTime >= Mathf.Max(0f, matchRules.regulationDurationSeconds - matchRules.endgameHudTrimSeconds));
        string IMatchView.StudyCohort => Hotdrop.Core.Telemetry.Cohort;
        bool IMatchView.PadsEnabled => Hotdrop.Core.StudySettings.PadsEnabled;
        bool IMatchView.IsLiveMatch => allPlayers.Count > 0;
        IMatchParticipant IMatchView.LocalParticipant => localPlayer;
        IReadOnlyList<IMatchParticipant> IMatchView.Participants => allPlayers;
        IMatchParticipant IMatchView.SpectatedTeammate => SpectatedTeammate;

        // Events
        public System.Action<MatchOutcome, int, int> OnMatchEnded; // outcome, scoreA, scoreB

        void Awake()
        {
            flowController = new MatchFlowController(matchRules);
            scoringController = new RelayScoringController(matchRules);
        }

        public void StartOfflinePractice(int botCount = 5, int humanTeam = 0)
        {
            // A local drill or network presenter may have replaced the read-only view.
            // Re-register on every practice start, not just the scene's first OnEnable.
            MatchView.Register(this);
            if (allPlayers.Count > 0) return;
            isOfflinePractice = true;
            localPlayerTeam = humanTeam;

            // Create local player
            CreateLocalPlayer();

            // Create bots to fill 3v3
            int botsPerTeam = 3;
            CreateBots(teamAPlayers, 0, botsPerTeam - (humanTeam == 0 ? 1 : 0));
            CreateBots(teamBPlayers, 1, botsPerTeam - (humanTeam == 1 ? 1 : 0));

            // Start match flow
            BeginMatch();
        }

        public void StartTutorial()
        {
            isOfflinePractice = true;
            localPlayerTeam = 0;

            CreateLocalPlayer();
            // Tutorial creates specific bots/steps - handled by TutorialController
            BeginMatch();
        }

        void CreateLocalPlayer()
        {
            GameObject playerObj = Instantiate(playerPrefab);
            localPlayer = playerObj.GetComponent<PlayerController>();

            // Setup references
            localPlayer.playerCamera = mainCamera;
            localPlayer.touchInput = touchInput;

            // Camera follows local player
            mainCamera.SetTarget(localPlayer.transform);

            // Subscribe to events
            localPlayer.OnDeath += OnPlayerDeath;
            localPlayer.OnRespawn += OnPlayerRespawn;
            localPlayer.OnHealthChanged += (hp) => matchHUD?.UpdateHealth(hp);
            localPlayer.OnDamagedFrom += direction => matchHUD?.ShowDamageFrom(direction);
            localPlayer.OnFlightMotion += (speed, airborne) => matchHUD?.UpdateFlightMotion(speed, airborne);
            localPlayer.OnAmmoChanged += (cur, max) => matchHUD?.UpdateAmmo(cur, max);
            localPlayer.OnPadPlaced += (pad) => matchHUD?.UpdatePadState(pad);
            localPlayer.OnPadActivated += (pad) => matchHUD?.ShowPadActivated();
            localPlayer.OnPadChargeReady += () => matchHUD?.ShowPadReady();
            localPlayer.OnShotBlocked += () => matchHUD?.ShowBlockedShot();
            localPlayer.OnPadRejected += reason => matchHUD?.ShowPadRejected(reason);
            localPlayer.OnHitConfirmed += headshot => matchHUD?.ShowHitConfirmed(headshot: headshot);
            localPlayer.OnEliminated += (killerId, weapon) =>
            {
                var killer = PlayerController.Find(killerId);
                matchHUD?.ShowEliminated(killer != null && killer != localPlayer ? killer : null, weapon, localPlayer.LastAttackDirection);
            };

            // Initialize
            WeaponDefinition startingWeapon = weaponDefinitions[localPlayerWeaponIndex];
            localPlayer.Initialize(0, localPlayerTeam, startingWeapon);
            localPlayer.DisplayName = LocalAlias();
            matchHUD?.Initialize(this, localPlayer);

            TrackPadEvents(localPlayer);
            allPlayers.Add(localPlayer);
            if (localPlayerTeam == 0) teamAPlayers.Add(localPlayer);
            else teamBPlayers.Add(localPlayer);
        }

        void CreateBots(List<PlayerController> teamList, int teamId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject botObj = Instantiate(botPrefab);
                var botController = botObj.GetComponent<BotController>();
                var playerController = botObj.GetComponent<PlayerController>();

                // Setup
                WeaponDefinition weapon = weaponDefinitions[Random.Range(0, weaponDefinitions.Length)];
                int playerId = allPlayers.Count + 1;
                // Spec 12 / C09: bots are driven by the same rules on both teams. Offline practice
                // no longer secretly weakens the player's own teammates; difficulty is a real,
                // visible choice rather than a hidden buff. Standard reaction delay is the default.
                bool easyDifficulty = false;

                botController.Initialize(playerId, teamId, weapon, easyDifficulty);
                playerController.IsBot = true;
                playerController.DisplayName = OfflineAlias.Next(playerId * 7919 + teamId);

                // Subscribe to events
                playerController.OnDeath += OnPlayerDeath;
                playerController.OnRespawn += OnPlayerRespawn;
                var victim = playerController;
                playerController.OnEliminated += (killerId, weapon) =>
                {
                    if (localPlayer != null && killerId == localPlayer.PlayerId)
                    {
                        matchHUD?.ShowHitConfirmed(elimination: true);
                        matchHUD?.ShowEliminationConfirmed(victim);
                    }
                };

                TrackPadEvents(playerController);
                allPlayers.Add(playerController);
                teamList.Add(playerController);
                bots.Add(botController);
            }
        }

        // Offline pad facts for the pad study; online, the server records these.
        void TrackPadEvents(PlayerController player)
        {
            player.OnPadPlaced += pad => Hotdrop.Core.Telemetry.TrackOfflineMatchFact("pad_placed",
                ("mapId", mapConfig != null ? mapConfig.mapName : "unknown"), ("padId", pad.TelemetryId), ("byBot", player.IsBot), ("team", player.TeamId), ("matchTime", MatchTime),
                ("x", pad.Position.x), ("y", pad.Position.y), ("z", pad.Position.z));
            player.OnPadActivated += pad => Hotdrop.Core.Telemetry.TrackOfflineMatchFact("pad_used",
                ("mapId", mapConfig != null ? mapConfig.mapName : "unknown"), ("padId", pad.TelemetryId), ("byBot", player.IsBot), ("team", player.TeamId), ("padTeam", pad.TeamId), ("ownPad", pad.OwnerId == player.PlayerId),
                ("matchTime", MatchTime), ("x", pad.Position.x), ("y", pad.Position.y), ("z", pad.Position.z));
        }

        void BeginMatch()
        {
            float loadStarted = Time.realtimeSinceStartup;
            Hotdrop.Core.Telemetry.CurrentMatchId = System.Guid.NewGuid().ToString("N");
            Hotdrop.Core.Telemetry.CurrentMapId = mapConfig != null ? mapConfig.mapName : "unknown";
            Hotdrop.Core.Telemetry.CurrentMatchHasBots = bots.Count > 0;
            Hotdrop.Core.Telemetry.TrackOfflineMatchFact("match_started", ("mode", "offline_practice"),
                ("bots", bots.Count), ("padsEnabled", Hotdrop.Core.StudySettings.PadsEnabled));
            // Position players at spawn exits
            PositionPlayersAtSpawns();
            if (localPlayer != null) { mainCamera.SetTarget(localPlayer.transform); mainCamera.SnapBehindTarget(); }

            // Reset match state
            flowController = new MatchFlowController(matchRules);
            scoringController = new RelayScoringController(matchRules);
            matchEnded = false;
            previousPhase = flowController.Phase;
            lastCountdownBeep = -1; lastAudioZone = null; wasContested = false; lastMilestoneA = lastMilestoneB = 0;

            ApplyMatchPermissions();

            // Start preparation phase
            matchHUD?.ShowPhase(MatchPhase.Preparation, matchRules.preparationSeconds);
            matchHUD?.UpdateScores(0, 0);
            matchHUD?.UpdateZoneStatus(ActiveObjectivePosition, false, false);
            matchHUD?.ShowRotation(0f);

            // Gate A budget evidence: measured synchronous match load-in
            // (spawn placement, state reset, permissions, HUD init).
            if (!loadBudgetLogged)
            {
                loadBudgetLogged = true;
                Debug.Log($"HOTDROP_BUDGET match_load_in={(Time.realtimeSinceStartup - loadStarted) * 1000f:F0}ms");
            }
        }

        void PositionPlayersAtSpawns()
        {
            // Team A (North)
            for (int i = 0; i < teamAPlayers.Count; i++)
            {
                Transform spawn = teamASpawns[i % teamASpawns.Length];
                teamAPlayers[i].WarpForTraining(spawn.position, spawn.rotation);
            }

            // Team B (South)
            for (int i = 0; i < teamBPlayers.Count; i++)
            {
                Transform spawn = teamBSpawns[i % teamBSpawns.Length];
                teamBPlayers[i].WarpForTraining(spawn.position, spawn.rotation);
            }
        }

        void Update()
        {
            if (matchEnded || allPlayers.Count == 0) return;

            float dt = Time.deltaTime;
            flowController.Tick(dt);

            if (flowController.Phase != previousPhase)
            {
                HandlePhaseEntered(flowController.Phase);
                previousPhase = flowController.Phase;
            }

            ApplyMatchPermissions();

            // Update HUD timer
            matchHUD?.UpdateTimer(GetDisplayTime());

            // Handle phase transitions
            HandlePhaseTransitions();

            // Tick scoring during InProgress/Overtime
            if (flowController.Phase == MatchPhase.InProgress || flowController.Phase == MatchPhase.Overtime)
            {
                TickScoring(dt);
            }

            // Check match end
            if (scoringController.Outcome != MatchOutcome.InProgress && !matchEnded)
            {
                EndMatch(scoringController.Outcome);
            }

            if (flowController.Phase == MatchPhase.InProgress || flowController.Phase == MatchPhase.Overtime)
                matchHUD?.ShowRotation(flowController.MatchTime);

            UpdateSpectatorCamera();
            UpdateMatchAudio();
        }

        int lastCountdownBeep = -1;
        ZoneId? lastAudioZone;
        bool wasContested;
        int lastMilestoneA, lastMilestoneB;

        void UpdateMatchAudio()
        {
            var phase = flowController.Phase;
            if (phase == MatchPhase.InProgress || phase == MatchPhase.Overtime)
                Hotdrop.Core.GameAudio.SetMatchMusic(flowController.MatchTime, phase == MatchPhase.Overtime, matchRules);
            else if (phase == MatchPhase.Countdown)
                Hotdrop.Core.GameAudio.SetCountdownMusic(flowController.PhaseElapsed, matchRules.countdownSeconds, matchRules);
            else
                Hotdrop.Core.GameAudio.StopMusic();
            if (phase == MatchPhase.Countdown)
            {
                int n = Mathf.CeilToInt(matchRules.countdownSeconds - flowController.PhaseElapsed);
                if (n != lastCountdownBeep && n > 0)
                {
                    lastCountdownBeep = n;
                    Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.Countdown);
                    Hotdrop.Core.GameAudio.ReportLocalCaption(Hotdrop.Core.GameCue.Countdown, n.ToString());
                }
                return;
            }
            if (phase != MatchPhase.InProgress && phase != MatchPhase.Overtime) return;
            if (lastCountdownBeep != 0)
            {
                lastCountdownBeep = 0;
                Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.CountdownGo);
                Hotdrop.Core.GameAudio.ReportLocalCaption(Hotdrop.Core.GameCue.CountdownGo, Hotdrop.UI.UiDraw.L("hud.countdown.go", "GO"));
            }

            var zone = ScoringZone;
            if (lastAudioZone != zone)
            {
                if (lastAudioZone.HasValue) Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.ZoneActivate);
                lastAudioZone = zone;
            }
            // Milestones every 30 points, so both teams hear the race tighten.
            int milestoneA = scoringController.ScoreA / 30, milestoneB = scoringController.ScoreB / 30;
            if (milestoneA > lastMilestoneA || milestoneB > lastMilestoneB) Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.ZoneMilestone);
            lastMilestoneA = milestoneA; lastMilestoneB = milestoneB;
        }

        void NotifyContest(bool contested)
        {
            if (contested && !wasContested) Hotdrop.Core.GameAudio.Play2D(Hotdrop.Core.GameCue.ZoneContest);
            wasContested = contested;
        }

        public PlayerController SpectatedTeammate { get; private set; }

        // Spec section 10: while waiting to respawn, view a living teammate.
        void UpdateSpectatorCamera()
        {
            if (localPlayer == null || mainCamera == null) return;
            if (localPlayer.IsAlive)
            {
                if (SpectatedTeammate != null) { SpectatedTeammate = null; mainCamera.SetTarget(localPlayer.transform); }
                return;
            }
            if (SpectatedTeammate != null && SpectatedTeammate.IsAlive) return;
            SpectatedTeammate = null;
            foreach (var p in allPlayers)
                if (p != localPlayer && p.TeamId == localPlayer.TeamId && p.IsAlive) { SpectatedTeammate = p; break; }
            if (SpectatedTeammate != null) mainCamera.SetTarget(SpectatedTeammate.transform);
        }

        static string LocalAlias()
        {
            string alias = PlayerPrefs.GetString("LocalAlias", "");
            if (string.IsNullOrEmpty(alias))
            {
                alias = OfflineAlias.Next(System.Environment.TickCount);
                PlayerPrefs.SetString("LocalAlias", alias);
                PlayerPrefs.Save();
            }
            return alias;
        }

        void HandlePhaseTransitions()
        {
            switch (flowController.Phase)
            {
                case MatchPhase.Preparation:
                    // Weapon select, no damage, no pads
                    matchHUD?.EnableWeaponSelect(true);
                    matchHUD?.ShowPreparationCountdown(flowController.PhaseElapsed);
                    break;

                case MatchPhase.Countdown:
                    matchHUD?.EnableWeaponSelect(false);
                    matchHUD?.ShowCountdown(flowController.PhaseElapsed);
                    break;

                case MatchPhase.InProgress:
                    break;

                case MatchPhase.Overtime:
                    break;

                case MatchPhase.Results:
                    break;
            }
        }

        void HandlePhaseEntered(MatchPhase phase)
        {
            switch (phase)
            {
                case MatchPhase.Countdown:
                    matchHUD?.ShowPhase(MatchPhase.Countdown, matchRules.countdownSeconds);
                    break;
                case MatchPhase.InProgress:
                    foreach (var player in allPlayers)
                    {
                        player.ResetForMatchStart();
                        player.ResetStats();
                    }
                    matchHUD?.OnMatchStart();
                    matchStartTime = Time.time;
                    break;
                case MatchPhase.Overtime:
                    matchHUD?.ShowOvertime();
                    break;
                case MatchPhase.Results:
                    break;
            }
        }

        void ApplyMatchPermissions()
        {
            bool live = flowController.Phase == MatchPhase.InProgress || flowController.Phase == MatchPhase.Overtime;
            bool selecting = flowController.Phase == MatchPhase.Preparation;
            foreach (var player in allPlayers)
                player.SetMatchPermissions(live, live, live, selecting);
        }

        void TickScoring(float dt)
        {
            // Check zone occupancy for each team
            Vector3 activeZoneCenter = GetActiveZoneCenter();
            bool teamAPresent = CheckTeamInZone(teamAPlayers, activeZoneCenter, dt);
            bool teamBPresent = CheckTeamInZone(teamBPlayers, activeZoneCenter, dt);

            scoringController.Tick(teamAPresent, teamBPresent, flowController.MatchTime, dt);

            if (scoringController.IsInOvertime && flowController.Phase == MatchPhase.InProgress)
                flowController.NotifyOvertimeStarted();

            // Update HUD scores
            matchHUD?.UpdateScores(scoringController.ScoreA, scoringController.ScoreB);
            matchHUD?.UpdateZoneStatus(activeZoneCenter, teamAPresent, teamBPresent);
            NotifyContest(teamAPresent && teamBPresent);
        }

        Vector3 GetActiveZoneCenter() => ZoneCenter(matchRules.GetScoringZone(flowController.MatchTime));

        public Vector3 ZoneCenter(ZoneId zone) => zone switch
        {
            ZoneId.West => mapConfig.westZone,
            ZoneId.East => mapConfig.eastZone,
            _ => mapConfig.centerZone,
        };

        bool CheckTeamInZone(List<PlayerController> team, Vector3 zoneCenter, float dt)
        {
            bool present = false;
            foreach (var player in team)
            {
                if (!player.IsAlive) continue;
                // The controller's pivot is at the feet (CharacterController centre is raised by half height).
                if (ZoneOccupancy.IsValid(player.transform.position, zoneCenter, mapConfig.groundLevelY, matchRules, player.IsAlive, player.IsSpawnProtected))
                {
                    player.ObjectiveSeconds += dt;
                    present = true;
                }
            }
            return present;
        }

        float GetDisplayTime()
        {
            if (flowController.Phase == MatchPhase.Preparation || flowController.Phase == MatchPhase.Countdown)
            {
                return 0f;
            }
            return flowController.MatchTime;
        }

        void OnPlayerDeath(PlayerController victim)
        {
            // Award point to killer? No - kills don't score per spec
            // Just handle respawn
        }

        void OnPlayerRespawn(PlayerController player)
        {
            // PlayerController has already placed itself at a mirrored spawn exit.
            if (player == localPlayer)
            {
                matchHUD?.HideCombatFeedback();
                SpectatedTeammate = null;
                mainCamera.SetTarget(localPlayer.transform);
                mainCamera.SnapBehindTarget();
            }
        }

        void EndMatch(MatchOutcome outcome)
        {
            matchEnded = true;
            flowController.NotifyMatchEnded();
            ApplyMatchPermissions();

            // Stop all gameplay
            foreach (var player in allPlayers)
            {
                player.enabled = false;
            }
            foreach (var bot in bots)
            {
                bot.enabled = false;
            }

            // Show results
            bool isWin = (outcome == MatchOutcome.TeamAWins && localPlayerTeam == 0) ||
                         (outcome == MatchOutcome.TeamBWins && localPlayerTeam == 1);
            bool isDraw = outcome == MatchOutcome.Draw;

            if (SpectatedTeammate != null) { SpectatedTeammate = null; mainCamera.SetTarget(localPlayer.transform); }
            Hotdrop.Core.GameAudio.ResolveMatchMusic(!isWin && !isDraw);
            var resultCue = isDraw ? Hotdrop.Core.GameCue.ZoneMilestone
                : isWin ? Hotdrop.Core.GameCue.Victory : Hotdrop.Core.GameCue.Defeat;
            Hotdrop.Core.GameAudio.Play2D(resultCue);
            string resultCaption = Hotdrop.UI.ResultsView.Localized(isDraw ? "outcome.draw"
                : isWin ? "outcome.victory" : "outcome.defeat", isDraw ? "DRAW" : isWin ? "VICTORY" : "DEFEAT");
            Hotdrop.Core.GameAudio.ReportLocalCaption(resultCue, resultCaption);
            Hotdrop.UI.PingSystem.Instance?.Clear();
            int padsPlaced = 0, padsUsed = 0;
            foreach (var p in allPlayers) padsUsed += p.PadLaunches;
            foreach (var p in allPlayers) padsPlaced += p.PadManager.PlacementsThisMatch;
            Hotdrop.Core.Telemetry.TrackOfflineMatchFact("match_completed",
                ("outcome", isDraw ? "draw" : isWin ? "win" : "loss"), ("scoreAlly", localPlayerTeam == 0 ? scoringController.ScoreA : scoringController.ScoreB),
                ("scoreEnemy", localPlayerTeam == 0 ? scoringController.ScoreB : scoringController.ScoreA),
                ("matchSeconds", flowController.MatchTime), ("overtime", flowController.MatchTime > matchRules.regulationDurationSeconds),
                ("localElims", localPlayer.Eliminations), ("localDeaths", localPlayer.Deaths),
                ("localZoneSeconds", localPlayer.ObjectiveSeconds), ("localPadLaunches", localPlayer.PadLaunches),
                ("localWeapon", localPlayer.CurrentWeapon.id.ToString()), ("padsPlaced", padsPlaced), ("padsUsed", padsUsed),
                ("padsEnabled", Hotdrop.Core.StudySettings.PadsEnabled));
            string endCondition = scoringController.EndReason switch
            {
                MatchEndReason.ScoreLimit => Hotdrop.UI.MatchHUD.Localized("end.scoreLimit", "SCORE LIMIT"),
                MatchEndReason.OvertimeHold => Hotdrop.UI.MatchHUD.Localized("end.overtimeHold", "OVERTIME HOLD"),
                MatchEndReason.OvertimeDraw => Hotdrop.UI.MatchHUD.Localized("end.overtimeDraw", "OVERTIME DRAW"),
                _ => Hotdrop.UI.MatchHUD.Localized("end.time", "TIME"),
            };

            matchHUD?.ShowResults(isWin, isDraw, scoringController.ScoreA, scoringController.ScoreB, endCondition, allPlayers,
                Hotdrop.UI.MatchHUD.Localized("result.practiceFooter", "PRACTICE MATCH — NO ONLINE XP, CREDITS, CONTRACTS, OR MASTERY"),
                // Amendment 1: tell the player the next reachable goal, not just the result.
                Hotdrop.Core.NextGoal.Short(Hotdrop.Core.GameBootstrap.Instance != null ? Hotdrop.Core.GameBootstrap.Instance.gameObject : null));

            OnMatchEnded?.Invoke(outcome, scoringController.ScoreA, scoringController.ScoreB);
        }

        public void OnPlayAgain()
        {
            Hotdrop.Core.Telemetry.Track("results_play_again_clicked");
            TearDownMatch();
            // Results disabled the controls; a rematch must hand them back.
            if (touchInput != null) touchInput.enabled = true;
            StartOfflinePractice();
        }

        public void OnReturnHome()
        {
            Hotdrop.Core.GameBootstrap.Instance.ReturnToHome();
        }

        public void StopOfflinePractice()
        {
            TearDownMatch();
            matchEnded = true;
        }

        /// <summary>
        /// Removes everything a match created (players, bots, pads, pings) so
        /// repeated matches leave nothing behind.
        /// </summary>
        void TearDownMatch()
        {
            matchEnded = false;
            Hotdrop.Core.GameAudio.SetMusic(false);
            Hotdrop.UI.PingSystem.Instance?.Clear();
            foreach (var player in allPlayers) if (player != null) Destroy(player.gameObject);
            Hotdrop.Pads.LaunchPadBehaviour.DestroyAll();
            allPlayers.Clear(); teamAPlayers.Clear(); teamBPlayers.Clear(); bots.Clear();
            localPlayer = null;
            SpectatedTeammate = null;
        }

        public void SetLocalWeapon(int weaponIndex)
        {
            localPlayerWeaponIndex = weaponIndex;
            Hotdrop.Core.Telemetry.Track("weapon_selected", ("weapon", weaponDefinitions[weaponIndex].id.ToString()),
                ("whileDead", localPlayer != null && !localPlayer.IsAlive));
            // Spec section 7/10: no switching while alive; choose during preparation or while waiting to respawn.
            if (localPlayer != null && (flowController.Phase == MatchPhase.Preparation || !localPlayer.IsAlive))
                localPlayer.SetWeapon(weaponIndex);
        }
    }
}



