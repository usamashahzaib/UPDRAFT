using UnityEngine;
using System.Collections.Generic;
using Updraft.Config;
using Updraft.Match;
using Updraft.Bots;
using Updraft.Player;
using Updraft.Camera;
using Updraft.Input;
using Updraft.UI;

namespace Updraft.Match
{
    /// <summary>
    /// Orchestrates the complete match loop per spec section 5:
    /// Home → Play/Party → Matchmaking → Server reservation → Load →
    /// 8s prep (weapon select, objective shown, spawn-locked, no damage, no pads) →
    /// 3s countdown → Match → Results → Play again/Home.
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
            localPlayer.OnAmmoChanged += (cur, max) => matchHUD?.UpdateAmmo(cur, max);
            localPlayer.OnPadPlaced += (pad) => matchHUD?.UpdatePadState(pad);
            localPlayer.OnPadActivated += (pad) => matchHUD?.ShowPadActivated();
            localPlayer.OnPadChargeReady += () => matchHUD?.ShowPadReady();
            localPlayer.OnShotBlocked += () => matchHUD?.ShowBlockedShot();
            localPlayer.OnPadRejected += reason => matchHUD?.ShowPadRejected(reason);
            localPlayer.OnHitConfirmed += () => matchHUD?.ShowHitConfirmed();
            localPlayer.OnEliminated += (killerId, weapon) =>
            {
                var killer = PlayerController.Find(killerId);
                matchHUD?.ShowEliminated(killer != null && killer != localPlayer ? killer.DisplayName : "", weapon);
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
                bool easyDifficulty = (teamId == localPlayerTeam); // easy bots on player's team

                botController.Initialize(playerId, teamId, weapon, easyDifficulty);
                playerController.IsBot = true;
                playerController.DisplayName = "[BOT] " + OfflineAlias.Next(playerId * 7919 + teamId);

                // Subscribe to events
                playerController.OnDeath += OnPlayerDeath;
                playerController.OnRespawn += OnPlayerRespawn;
                var victim = playerController;
                playerController.OnEliminated += (killerId, weapon) =>
                {
                    if (localPlayer != null && killerId == localPlayer.PlayerId) matchHUD?.ShowEliminationConfirmed(victim.DisplayName);
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
            player.OnPadPlaced += pad => Updraft.Core.Telemetry.TrackOfflineMatchFact("pad_placed",
                ("byBot", player.IsBot), ("team", player.TeamId), ("matchTime", MatchTime),
                ("x", pad.Position.x), ("z", pad.Position.z));
            player.OnPadActivated += pad => Updraft.Core.Telemetry.TrackOfflineMatchFact("pad_used",
                ("byBot", player.IsBot), ("team", player.TeamId), ("padTeam", pad.TeamId), ("ownPad", pad.OwnerId == player.PlayerId),
                ("matchTime", MatchTime));
        }

        void BeginMatch()
        {
            float loadStarted = Time.realtimeSinceStartup;
            Updraft.Core.Telemetry.CurrentMatchId = System.Guid.NewGuid().ToString("N");
            Updraft.Core.Telemetry.CurrentMatchHasBots = bots.Count > 0;
            Updraft.Core.Telemetry.TrackOfflineMatchFact("match_started", ("mode", "offline_practice"),
                ("bots", bots.Count), ("padsEnabled", Updraft.Core.StudySettings.PadsEnabled));
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
                Debug.Log($"UPDRAFT_BUDGET match_load_in={(Time.realtimeSinceStartup - loadStarted) * 1000f:F0}ms");
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
            Updraft.Core.GameAudio.SetMusic(phase == MatchPhase.InProgress || phase == MatchPhase.Overtime);
            if (phase == MatchPhase.Countdown)
            {
                int n = Mathf.CeilToInt(matchRules.countdownSeconds - flowController.PhaseElapsed);
                if (n != lastCountdownBeep && n > 0) { lastCountdownBeep = n; Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.Countdown); }
                return;
            }
            if (phase != MatchPhase.InProgress && phase != MatchPhase.Overtime) return;
            if (lastCountdownBeep != 0) { lastCountdownBeep = 0; Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.CountdownGo); }

            var zone = ScoringZone;
            if (lastAudioZone != zone)
            {
                if (lastAudioZone.HasValue) Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.ZoneActivate);
                lastAudioZone = zone;
            }
            // Milestones every 30 points, so both teams hear the race tighten.
            int milestoneA = scoringController.ScoreA / 30, milestoneB = scoringController.ScoreB / 30;
            if (milestoneA > lastMilestoneA || milestoneB > lastMilestoneB) Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.ZoneMilestone);
            lastMilestoneA = milestoneA; lastMilestoneB = milestoneB;
        }

        void NotifyContest(bool contested)
        {
            if (contested && !wasContested) Updraft.Core.GameAudio.Play2D(Updraft.Core.GameCue.ZoneContest);
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
            Updraft.Core.GameAudio.SetMusic(false);
            Updraft.Core.GameAudio.Play2D(isDraw || !isWin ? Updraft.Core.GameCue.Defeat : Updraft.Core.GameCue.Victory);
            Updraft.UI.PingSystem.Instance?.Clear();
            int padsPlaced = 0, padsUsed = 0;
            foreach (var p in allPlayers) padsUsed += p.PadLaunches;
            foreach (var p in allPlayers) padsPlaced += p.PadManager.PlacementsThisMatch;
            Updraft.Core.Telemetry.TrackOfflineMatchFact("match_completed",
                ("outcome", isDraw ? "draw" : isWin ? "win" : "loss"), ("scoreAlly", localPlayerTeam == 0 ? scoringController.ScoreA : scoringController.ScoreB),
                ("scoreEnemy", localPlayerTeam == 0 ? scoringController.ScoreB : scoringController.ScoreA),
                ("matchSeconds", flowController.MatchTime), ("overtime", flowController.MatchTime > matchRules.regulationDurationSeconds),
                ("localElims", localPlayer.Eliminations), ("localDeaths", localPlayer.Deaths),
                ("localZoneSeconds", localPlayer.ObjectiveSeconds), ("localPadLaunches", localPlayer.PadLaunches),
                ("localWeapon", localPlayer.CurrentWeapon.id.ToString()), ("padsPlaced", padsPlaced), ("padsUsed", padsUsed),
                ("padsEnabled", Updraft.Core.StudySettings.PadsEnabled));
            matchHUD?.ShowResults(isWin, isDraw, scoringController.ScoreA, scoringController.ScoreB, allPlayers,
                "OFFLINE PRACTICE  ·  NO RATING OR CURRENCY");

            OnMatchEnded?.Invoke(outcome, scoringController.ScoreA, scoringController.ScoreB);
        }

        public void OnPlayAgain()
        {
            Updraft.Core.Telemetry.Track("results_play_again_clicked");
            TearDownMatch();
            // Results disabled the controls; a rematch must hand them back.
            if (touchInput != null) touchInput.enabled = true;
            StartOfflinePractice();
        }

        public void OnReturnHome()
        {
            Updraft.Core.GameBootstrap.Instance.ReturnToHome();
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
            Updraft.Core.GameAudio.SetMusic(false);
            Updraft.UI.PingSystem.Instance?.Clear();
            foreach (var player in allPlayers) if (player != null) Destroy(player.gameObject);
            Updraft.Pads.LaunchPadBehaviour.DestroyAll();
            allPlayers.Clear(); teamAPlayers.Clear(); teamBPlayers.Clear(); bots.Clear();
            localPlayer = null;
            SpectatedTeammate = null;
        }

        public void SetLocalWeapon(int weaponIndex)
        {
            localPlayerWeaponIndex = weaponIndex;
            Updraft.Core.Telemetry.Track("weapon_selected", ("weapon", weaponDefinitions[weaponIndex].id.ToString()),
                ("whileDead", localPlayer != null && !localPlayer.IsAlive));
            // Spec section 7/10: no switching while alive; choose during preparation or while waiting to respawn.
            if (localPlayer != null && (flowController.Phase == MatchPhase.Preparation || !localPlayer.IsAlive))
                localPlayer.SetWeapon(weaponIndex);
        }
    }
}
