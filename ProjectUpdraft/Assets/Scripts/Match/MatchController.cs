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
    public class MatchController : MonoBehaviour
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

        // Events
        public System.Action<MatchOutcome, int, int> OnMatchEnded; // outcome, scoreA, scoreB

        void Awake()
        {
            flowController = new MatchFlowController(matchRules);
            scoringController = new RelayScoringController(matchRules);
        }

        void Start()
        {
            // For offline practice, start immediately with bot match
            if (isOfflinePractice)
            {
                StartOfflinePractice();
            }
        }

        public void StartOfflinePractice(int botCount = 5, int humanTeam = 0)
        {
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

            // Initialize
            WeaponDefinition startingWeapon = weaponDefinitions[localPlayerWeaponIndex];
            localPlayer.Initialize(0, localPlayerTeam, startingWeapon);

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
                playerController.Initialize(playerId, teamId, weapon);

                // Subscribe to events
                playerController.OnDeath += OnPlayerDeath;
                playerController.OnRespawn += OnPlayerRespawn;

                allPlayers.Add(playerController);
                teamList.Add(playerController);
                bots.Add(botController);
            }
        }

        void BeginMatch()
        {
            // Position players at spawn exits
            PositionPlayersAtSpawns();

            // Reset match state
            flowController = new MatchFlowController(matchRules);
            scoringController = new RelayScoringController(matchRules);
            matchEnded = false;

            // Start preparation phase
            matchHUD?.ShowPhase(MatchPhase.Preparation, matchRules.preparationSeconds);
            matchHUD?.ShowNextZone(matchRules.GetUpcomingZone(0));
        }

        void PositionPlayersAtSpawns()
        {
            // Team A (North)
            for (int i = 0; i < teamAPlayers.Count; i++)
            {
                Transform spawn = teamASpawns[i % teamASpawns.Length];
                teamAPlayers[i].transform.position = spawn.position;
                teamAPlayers[i].transform.rotation = spawn.rotation;
            }

            // Team B (South)
            for (int i = 0; i < teamBPlayers.Count; i++)
            {
                Transform spawn = teamBSpawns[i % teamBSpawns.Length];
                teamBPlayers[i].transform.position = spawn.position;
                teamBPlayers[i].transform.rotation = spawn.rotation;
            }
        }

        void Update()
        {
            if (matchEnded) return;

            float dt = Time.deltaTime;
            flowController.Tick(dt);

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

            // Update next zone warning
            if (flowController.Phase == MatchPhase.InProgress)
            {
                var upcoming = matchRules.GetUpcomingZone(flowController.MatchTime);
                if (upcoming.HasValue)
                {
                    matchHUD?.ShowNextZone(upcoming.Value);
                }
            }
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
                    if (flowController.PhaseElapsed < Time.deltaTime) // just entered
                    {
                        matchHUD?.OnMatchStart();
                        matchStartTime = Time.time;
                    }
                    break;

                case MatchPhase.Overtime:
                    if (flowController.PhaseElapsed < Time.deltaTime) // just entered
                    {
                        matchHUD?.ShowOvertime();
                    }
                    break;

                case MatchPhase.Results:
                    if (!matchEnded)
                    {
                        EndMatch(scoringController.Outcome);
                    }
                    break;
            }
        }

        void TickScoring(float dt)
        {
            // Check zone occupancy for each team
            Vector3 activeZoneCenter = GetActiveZoneCenter();
            bool teamAPresent = CheckTeamInZone(teamAPlayers, activeZoneCenter);
            bool teamBPresent = CheckTeamInZone(teamBPlayers, activeZoneCenter);

            scoringController.Tick(teamAPresent, teamBPresent, flowController.MatchTime, dt);

            // Update HUD scores
            matchHUD?.UpdateScores(scoringController.ScoreA, scoringController.ScoreB);
            matchHUD?.UpdateZoneStatus(activeZoneCenter, teamAPresent, teamBPresent);
        }

        Vector3 GetActiveZoneCenter()
        {
            var activeZone = matchRules.GetActiveZone(flowController.MatchTime);
            if (activeZone.HasValue)
            {
                switch (activeZone.Value)
                {
                    case ZoneId.Center: return mapConfig.centerZone;
                    case ZoneId.West: return mapConfig.westZone;
                    case ZoneId.East: return mapConfig.eastZone;
                }
            }
            return mapConfig.centerZone;
        }

        bool CheckTeamInZone(List<PlayerController> team, Vector3 zoneCenter)
        {
            foreach (var player in team)
            {
                if (!player.IsAlive) continue;

                // Check if player has spawn protection (simplified)
                bool spawnProtected = false; // Would track respawn time

                // Valid occupation check
                Vector3 feetPos = player.transform.position; // Simplified - should be feet position
                if (ZoneOccupancy.IsValid(feetPos, zoneCenter, mapConfig.groundLevelY, matchRules, player.IsAlive, spawnProtected))
                {
                    return true;
                }
            }
            return false;
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
            // Respawn at team spawn
            Transform[] spawns = player.TeamId == 0 ? teamASpawns : teamBSpawns;
            Transform spawn = spawns[Random.Range(0, spawns.Length)];
            player.transform.position = spawn.position;
            player.transform.rotation = spawn.rotation;
        }

        void EndMatch(MatchOutcome outcome)
        {
            matchEnded = true;

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

            matchHUD?.ShowResults(isWin, isDraw, scoringController.ScoreA, scoringController.ScoreB);

            OnMatchEnded?.Invoke(outcome, scoringController.ScoreA, scoringController.ScoreB);
        }

        public void OnPlayAgain()
        {
            // Reset and restart
            matchEnded = false;

            foreach (var player in allPlayers)
            {
                Destroy(player.gameObject);
            }
            foreach (var bot in bots)
            {
                Destroy(bot.gameObject);
            }

            allPlayers.Clear();
            teamAPlayers.Clear();
            teamBPlayers.Clear();
            bots.Clear();

            StartOfflinePractice();
        }

        public void OnReturnHome()
        {
            // Load home scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
        }

        public void SetLocalWeapon(int weaponIndex)
        {
            localPlayerWeaponIndex = weaponIndex;
            if (localPlayer != null && flowController.Phase == MatchPhase.Preparation)
            {
                localPlayer.SetWeapon(weaponIndex);
            }
        }
    }
}