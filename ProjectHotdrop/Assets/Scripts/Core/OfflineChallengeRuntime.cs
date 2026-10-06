using System;
using System.Collections.Generic;
using UnityEngine;
using Hotdrop.Bots;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Pads;
using Hotdrop.Player;
using Hotdrop.Tutorial;
using Hotdrop.UI;

namespace Hotdrop.Core
{
    /// <summary>Installed, local drills driven by the real player/weapon/pad/bot systems.
    /// No online session or backend progression is created by this route.</summary>
    public sealed class OfflineChallengeRuntime : MonoBehaviour, IMatchView, IObjectivePresentation
    {
        [Serializable]
        public class WorldSnapshot
        {
            public int version = 1;
            public bool recoverable;
            public Vector3 position;
            public Quaternion rotation;
            public int ammo;
            public float hold;
            public int capturedZones;
            public bool reloadCompleted;
            public bool[] destroyedTargets;
            public float[] targetHealth;
        }

        GameBootstrap game;
        OfflineChallengeEntry entry;
        GameObject world;
        PlayerController player;
        readonly List<IMatchParticipant> participants = new List<IMatchParticipant>();
        readonly List<TutorialTarget> targets = new List<TutorialTarget>();
        readonly List<PlayerController> defenders = new List<PlayerController>();
        readonly HashSet<int> countedDefenders = new HashSet<int>();
        bool[] destroyedTargets;
        GameObject marker;
        LaunchPadBehaviour demonstrationPad;
        float hold;
        int capturedZones, pendingCompletions;
        bool ownPadUsed, enemyPadUsed, reloadCompleted, wasReloading;
        bool inPadFlight;
        int lastPadLaunchFrame;
        float priorTimeScale = 1;
        public OfflineChallengeController Controller { get; private set; }
        public OfflineChallengeScreen View { get; private set; }
        public PlayerController Player => player;
        public float HoldSeconds => hold;
        public MatchRulesConfig Rules => game.matchRules;
        public MapConfig MapConfig => game.mapConfig;
        public MatchPhase CurrentPhase => Controller.IsAttemptActive ? MatchPhase.InProgress : MatchPhase.Results;
        public float MatchTime => Controller.CurrentElapsedTime;
        public ZoneId ScoringZone => entry?.objectiveType == ChallengeObjectiveType.RotateBetweenZones
            ? Rules.GetScoringZone(MatchTime) : ZoneId.Center;
        public Vector3 ActiveObjectivePosition
        {
            get
            {
                if (entry == null) return game.mapConfig.centerZone;
                if (entry.objectiveType == ChallengeObjectiveType.BasicRoute)
                    return entry.routePoints[Mathf.Clamp(Controller.CurrentTargetsCompleted, 0, entry.routePoints.Length - 1)];
                if (entry.chapter == ChallengeChapter.FirstFlight) return entry.landingPosition;
                if (entry.chapter == ChallengeChapter.RooftopOperator)
                    foreach (var target in targets) if (target != null) return target.transform.position;
                return game.mapConfig.ZoneCenter(ScoringZone);
            }
        }
        public bool IsLiveMatch => world != null && Controller.IsAttemptActive;
        public string ActiveObjectiveLabel => entry?.objectiveType == ChallengeObjectiveType.BasicRoute ? "CHECKPOINT " + (Controller.CurrentTargetsCompleted + 1)
            : entry?.chapter == ChallengeChapter.FirstFlight ? "LANDING"
            : entry?.chapter == ChallengeChapter.RooftopOperator ? "TARGET" : ScoringZone.ToString().ToUpperInvariant();
        public IMatchParticipant LocalParticipant => player;
        public IReadOnlyList<IMatchParticipant> Participants => participants;
        public IMatchParticipant SpectatedTeammate => null;

        void Awake()
        {
            game = GetComponent<GameBootstrap>();
            Controller = GetComponent<OfflineChallengeController>();
            if (Controller == null) Controller = gameObject.AddComponent<OfflineChallengeController>();
            Controller.BeginWorld = BuildWorld;
            Controller.CaptureWorld = Capture;
            Controller.CanRestoreWorld = CanRestore;
            Controller.RestoreWorld = Restore;
            Controller.OnStateChanged += StateChanged;
            View = gameObject.AddComponent<OfflineChallengeScreen>();
            View.Initialize(this, game);
        }

        public void OpenLibrary()
        {
            game.GetComponent<OfflineHomeScreen>()?.Hide();
            Controller.OpenLibrary();
        }

        public void Leave()
        {
            Controller.OpenLibrary();
            View.Hide();
            CleanupWorld();
        }

        bool BuildWorld(OfflineChallengeEntry selected)
        {
            CleanupWorld();
            game.GetComponent<OfflineHomeScreen>()?.Hide();
            game.matchController.StopOfflinePractice();
            entry = selected;
            hold = 0; capturedZones = 0; pendingCompletions = 0;
            ownPadUsed = enemyPadUsed = reloadCompleted = wasReloading = false;
            inPadFlight = false;
            countedDefenders.Clear();
            Time.timeScale = 1;
            game.touchInput.enabled = true;
            if (entry.objectiveType == ChallengeObjectiveType.CompleteOfflineRelay)
            {
                game.matchController.OnMatchEnded += RelayCompleted;
                game.matchController.StartOfflinePractice();
                player = game.matchController.LocalPlayer;
                return player != null;
            }
            if (game.playerPrefab == null || game.weaponDefinitions == null || game.weaponDefinitions.Length == 0) return false;
            world = new GameObject("OfflineChallengeWorld");
            var obj = Instantiate(game.playerPrefab, entry.startPosition, Quaternion.Euler(0, entry.startYaw, 0), world.transform);
            player = obj.GetComponent<PlayerController>();
            player.playerCamera = game.mainCamera; player.touchInput = game.touchInput;
            player.Initialize(0, 0, game.weaponDefinitions[Mathf.Clamp(entry.defaultWeaponIndex, 0, game.weaponDefinitions.Length - 1)]);
            player.DisplayName = "YOU";
            player.SetMatchPermissions(true, true, true, false);
            player.OnShotStarted += () => Controller.UpdateGameplay(0, shotFired: true);
            player.OnHitConfirmed += headshot => { Controller.UpdateGameplay(0, shotHit: true); game.matchHUD.ShowHitConfirmed(headshot: headshot); };
            player.OnHealthChanged += game.matchHUD.UpdateHealth;
            player.OnAmmoChanged += game.matchHUD.UpdateAmmo;
            player.OnPadRejected += game.matchHUD.ShowPadRejected;
            player.OnPadChargeReady += game.matchHUD.ShowPadReady;
            player.OnPadActivated += pad =>
            {
                ownPadUsed = pad.OwnerId == player.PlayerId; enemyPadUsed = pad.TeamId != player.TeamId;
                inPadFlight = true; lastPadLaunchFrame = Time.frameCount;
            };
            player.OnDeath += _ => Controller.CompleteChallenge(false);
            participants.Add(player);
            MatchView.Register(this);
            game.mainCamera.SetTarget(player.transform); game.mainCamera.SnapBehindTarget();
            game.matchHUD.Initialize(null, player, game.weaponDefinitions);
            game.matchHUD.HideAllPanels(); game.matchHUD.EnableWeaponSelect(false);
            game.matchHUD.activeZoneText.text = "LOCAL CHALLENGE";
            switch (entry.objectiveType)
            {
                case ChallengeObjectiveType.BasicRoute: SetMarker(entry.routePoints[0]); break;
                case ChallengeObjectiveType.OwnPadRoute:
                case ChallengeObjectiveType.EnemyPadRoute:
                case ChallengeObjectiveType.SafeLanding:
                case ChallengeObjectiveType.LaunchAndHit:
                    Vector3 direction = entry.landingPosition - entry.padPosition; direction.y = 0;
                    player.WarpForTraining(entry.padPosition + Vector3.up * .02f, Quaternion.LookRotation(direction));
                    game.mainCamera.SnapBehindTarget();
                    SetMarker(entry.landingPosition);
                    if (entry.objectiveType != ChallengeObjectiveType.OwnPadRoute) SpawnDemonstrationPad();
                    if (entry.objectiveType == ChallengeObjectiveType.LaunchAndHit) SpawnTargets();
                    break;
                case ChallengeObjectiveType.StationaryTargets:
                case ChallengeObjectiveType.MovingTargets:
                case ChallengeObjectiveType.ReloadDiscipline: SpawnTargets(); break;
                default:
                    player.WarpForTraining(game.mapConfig.centerZone + new Vector3(0, .05f, 4), Quaternion.Euler(0, 180, 0));
                    game.mainCamera.SnapBehindTarget(); SetMarker(ActiveObjectivePosition);
                    if (entry.objectiveType == ChallengeObjectiveType.DefendAgainstBots) SpawnDefenders();
                    break;
            }
            return true;
        }

        void SpawnTargets()
        {
            destroyedTargets = new bool[entry.targetCount];
            for (int i = 0; i < entry.targetCount; i++)
            {
                var prefab = Resources.Load<GameObject>("Prefabs/TutorialTarget");
                var targetObj = prefab != null ? Instantiate(prefab, world.transform) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
                targetObj.name = "ChallengeTarget_" + i; targetObj.transform.SetParent(world.transform, true);
                targetObj.transform.position = entry.targetPositions[i % entry.targetPositions.Length];
                targetObj.layer = LayerMask.NameToLayer("Player");
                var target = targetObj.GetComponent<TutorialTarget>() ?? targetObj.AddComponent<TutorialTarget>();
                int index = i;
                target.CanTakeDamage = () => Controller.CurrentState == ChallengeFlowState.Gameplay &&
                    (entry.objectiveType != ChallengeObjectiveType.LaunchAndHit || (inPadFlight && !player.IsGrounded));
                target.OnDestroyed += () => { if (!destroyedTargets[index]) { destroyedTargets[index] = true; pendingCompletions++; } };
                targets.Add(target);
            }
        }

        void SpawnDefenders()
        {
            for (int i = 0; i < entry.botCount; i++)
            {
                var bot = Instantiate(game.botPrefab, game.mapConfig.centerZone + new Vector3(i == 0 ? -12 : 12, .05f, -8), Quaternion.identity, world.transform);
                bot.GetComponent<BotController>().Initialize(i + 1, 1, game.weaponDefinitions[0], entry.difficulty == "Easy");
                var opponent = bot.GetComponent<PlayerController>(); opponent.IsBot = true; opponent.DisplayName = "Rafi Khan";
                defenders.Add(opponent); participants.Add(opponent);
                opponent.OnDeath += killed =>
                {
                    if (killed.LastKillerId == player.PlayerId && IsOccupying(player, false) && countedDefenders.Add(killed.PlayerId)) pendingCompletions++;
                };
            }
        }

        void SpawnDemonstrationPad()
        {
            var obj = new GameObject("Authored opponent pad"); obj.transform.SetParent(world.transform);
            obj.transform.position = entry.padPosition;
            Vector3 direction = entry.landingPosition - entry.padPosition; direction.y = 0;
            obj.transform.rotation = Quaternion.LookRotation(direction);
            obj.layer = LayerMask.NameToLayer("Pad");
            var data = new LaunchPad(game.padConfig, 999, 1, entry.padPosition, direction.normalized);
            demonstrationPad = obj.AddComponent<LaunchPadBehaviour>(); demonstrationPad.Initialize(data); data.CompleteArming();
        }

        void Update()
        {
            if (Controller.CurrentState != ChallengeFlowState.Gameplay || !Controller.IsAttemptActive || player == null) return;
            Controller.UpdateGameplay(Time.deltaTime);
            if (!Controller.IsAttemptActive || world == null) return;
            bool padLanding = inPadFlight && player.IsGrounded && Time.frameCount > lastPadLaunchFrame;
            if (entry.objectiveType == ChallengeObjectiveType.MovingTargets)
                for (int i = 0; i < targets.Count; i++) if (targets[i] != null)
                    targets[i].transform.position = entry.targetPositions[i % entry.targetPositions.Length] +
                        Vector3.right * Mathf.Sin(MatchTime * entry.movingTargetSpeed + i) * entry.movingTargetDistance;
            if (wasReloading && !player.IsReloading && player.CurrentAmmo == player.MaxAmmo) reloadCompleted = true;
            wasReloading = player.IsReloading;
            switch (entry.objectiveType)
            {
                case ChallengeObjectiveType.BasicRoute:
                    int step = Controller.CurrentTargetsCompleted;
                    if (step < entry.routePoints.Length && player.IsGrounded && Vector3.Distance(player.Position, entry.routePoints[step]) <= entry.checkpointRadius)
                    { Controller.RegisterTargetCompleted(); if (Controller.IsAttemptActive) SetMarker(entry.routePoints[Controller.CurrentTargetsCompleted]); }
                    break;
                case ChallengeObjectiveType.OwnPadRoute:
                case ChallengeObjectiveType.EnemyPadRoute:
                case ChallengeObjectiveType.SafeLanding:
                    bool launched = entry.objectiveType == ChallengeObjectiveType.OwnPadRoute ? ownPadUsed : enemyPadUsed;
                    if (launched && padLanding && Vector3.Distance(player.Position, entry.landingPosition) <= entry.landingRadius)
                        Controller.RegisterTargetCompleted();
                    break;
                case ChallengeObjectiveType.CaptureOneZone:
                    if (IsOccupying(player)) hold += Time.deltaTime;
                    if (hold >= entry.holdSeconds) Controller.RegisterTargetCompleted();
                    break;
                case ChallengeObjectiveType.RotateBetweenZones:
                    int bit = 1 << (int)ScoringZone;
                    if ((capturedZones & bit) == 0 && IsOccupying(player))
                    { capturedZones |= bit; Controller.RegisterTargetCompleted(); }
                    if (Controller.IsAttemptActive) SetMarker(ActiveObjectivePosition);
                    break;
            }
            if (padLanding) inPadFlight = false;
            if (Controller.IsAttemptActive && demonstrationPad == null &&
                (entry.objectiveType == ChallengeObjectiveType.EnemyPadRoute || entry.objectiveType == ChallengeObjectiveType.SafeLanding || entry.objectiveType == ChallengeObjectiveType.LaunchAndHit))
                SpawnDemonstrationPad();
            View.SetObjectiveHint(ObjectiveHint());
        }

        void LateUpdate()
        {
            if (Controller.CurrentState != ChallengeFlowState.Gameplay || pendingCompletions == 0) return;
            // Defer until real weapon hit-confirm callbacks have counted accuracy for this shot.
            if (entry.objectiveType == ChallengeObjectiveType.ReloadDiscipline && !reloadCompleted) return;
            int count = pendingCompletions; pendingCompletions = 0; Controller.RegisterTargetCompleted(count);
        }

        bool IsOccupying(PlayerController p, bool requireUncontested = true)
        {
            var d = p.Position - ActiveObjectivePosition;
            bool contested = false;
            foreach (var bot in defenders) if (bot != null && bot.IsAlive)
            { var b = bot.Position - ActiveObjectivePosition; if (b.x * b.x + b.z * b.z <= Rules.zoneCaptureRadius * Rules.zoneCaptureRadius && bot.Position.y <= Rules.zoneMaxFeetHeight) contested = true; }
            return p.IsAlive && !p.IsSpawnProtected && p.Position.y <= game.mapConfig.groundLevelY + Rules.zoneMaxFeetHeight &&
                d.x * d.x + d.z * d.z <= Rules.zoneCaptureRadius * Rules.zoneCaptureRadius && (!requireUncontested || !contested);
        }

        string ObjectiveHint()
        {
            switch (entry.objectiveType)
            {
                case ChallengeObjectiveType.BasicRoute: return "Reach the marked checkpoint";
                case ChallengeObjectiveType.CaptureOneZone: return $"Hold Center uncontested: {hold:0.0} / {entry.holdSeconds:0} s";
                case ChallengeObjectiveType.RotateBetweenZones: return $"Active: {ScoringZone} · Next rotation: {Rules.SecondsUntilRotation(MatchTime):0} s";
                case ChallengeObjectiveType.ReloadDiscipline: return reloadCompleted ? "Reload complete · eliminate the targets" : "Eliminate targets and complete a magazine reload";
                case ChallengeObjectiveType.DefendAgainstBots: return "Eliminate both bots while inside the active zone";
                case ChallengeObjectiveType.LaunchAndHit: return "Jump on the opponent pad · eliminate targets while airborne";
                case ChallengeObjectiveType.OwnPadRoute: return "Place pad toward marker · jump to launch";
                case ChallengeObjectiveType.EnemyPadRoute:
                case ChallengeObjectiveType.SafeLanding: return "Jump on the opponent pad · land inside the platform marker";
                default: return entry.description;
            }
        }

        void SetMarker(Vector3 position)
        {
            if (marker == null)
            {
                var prefab = Resources.Load<GameObject>("Prefabs/TutorialMarker");
                marker = prefab != null ? Instantiate(prefab, world.transform) : new GameObject("Challenge objective marker");
                marker.transform.SetParent(world.transform, true);
                if (prefab == null)
                {
                    var ring = marker.AddComponent<LineRenderer>(); ring.sharedMaterial = Resources.Load<Material>("Materials/Objective");
                    ring.useWorldSpace = false; ring.loop = true; ring.widthMultiplier = .12f; ring.positionCount = 32;
                    for (int i = 0; i < 32; i++) ring.SetPosition(i, new Vector3(Mathf.Cos(i * Mathf.PI / 16), .04f, Mathf.Sin(i * Mathf.PI / 16)));
                }
            }
            marker.transform.position = position;
            float radius = entry.objectiveType == ChallengeObjectiveType.BasicRoute ? entry.checkpointRadius :
                entry.chapter == ChallengeChapter.FirstFlight ? entry.landingRadius : Rules.zoneCaptureRadius;
            marker.transform.localScale = new Vector3(radius, 1, radius);
        }

        void RelayCompleted(MatchOutcome _, int __, int ___)
        { game.matchHUD.HideAllPanels(); Controller.RegisterTargetCompleted(); }

        void StateChanged(ChallengeFlowState state)
        {
            if (state == ChallengeFlowState.Paused)
            { priorTimeScale = Time.timeScale; Time.timeScale = 0; game.touchInput.enabled = false; }
            else if (state == ChallengeFlowState.Gameplay)
            { Time.timeScale = priorTimeScale > 0 ? priorTimeScale : 1; game.touchInput.enabled = true; }
            else
            {
                Time.timeScale = 1; game.touchInput.enabled = false;
                if (state != ChallengeFlowState.Result) CleanupWorld();
                else
                {
                    player?.SetMatchPermissions(false, false, false, false);
                    foreach (var bot in defenders) if (bot != null) bot.SetMatchPermissions(false, false, false, false);
                }
            }
        }

        public string Capture()
        {
            var snapshot = new WorldSnapshot
            {
                // In-process pause always resumes the live world. After OS process loss only
                // grounded, non-combat movement/objective checkpoints are safely reconstructed.
                recoverable = world != null && player != null && player.IsAlive && player.IsGrounded &&
                    player.HasPadCharge && player.PadManager.ActivePad == null && player.CurrentHealth == player.MaxHealth &&
                    (entry.objectiveType == ChallengeObjectiveType.BasicRoute || entry.objectiveType == ChallengeObjectiveType.CaptureOneZone || entry.objectiveType == ChallengeObjectiveType.RotateBetweenZones),
                position = player != null ? player.Position : Vector3.zero,
                rotation = player != null ? player.transform.rotation : Quaternion.identity,
                ammo = player != null ? player.CurrentAmmo : 0, hold = hold, capturedZones = capturedZones,
                reloadCompleted = reloadCompleted, destroyedTargets = destroyedTargets
            };
            snapshot.targetHealth = new float[targets.Count];
            for (int i = 0; i < targets.Count; i++) snapshot.targetHealth[i] = targets[i] != null ? targets[i].health : 0;
            return JsonUtility.ToJson(snapshot);
        }

        public bool CanRestore(LocalChallengeSaveSnapshot snapshot)
        {
            try
            {
                var s = JsonUtility.FromJson<WorldSnapshot>(snapshot.customStateJson);
                return s != null && s.version == 1 && s.recoverable && s.position.x >= game.mapConfig.boundsMin.x &&
                    s.position.x <= game.mapConfig.boundsMax.x && s.position.z >= game.mapConfig.boundsMin.y &&
                    s.position.z <= game.mapConfig.boundsMax.y && s.position.y >= game.mapConfig.groundLevelY - .25f && s.position.y <= game.mapConfig.groundLevelY + Rules.zoneMaxFeetHeight &&
                    !float.IsNaN(s.hold) && !float.IsInfinity(s.hold) && s.hold >= 0 && s.hold <= (Controller.config.GetChallengeById(snapshot.challengeId)?.holdSeconds ?? 0) &&
                    s.capturedZones >= 0 && s.capturedZones < 8;
            }
            catch { return false; }
        }

        bool Restore(LocalChallengeSaveSnapshot snapshot)
        {
            if (!CanRestore(snapshot)) return false;
            var selected = Controller.config.GetChallengeById(snapshot.challengeId);
            if (selected == null || !(selected.objectiveType == ChallengeObjectiveType.BasicRoute ||
                selected.objectiveType == ChallengeObjectiveType.CaptureOneZone || selected.objectiveType == ChallengeObjectiveType.RotateBetweenZones)) return false;
            var state = JsonUtility.FromJson<WorldSnapshot>(snapshot.customStateJson);
            if (!BuildWorld(selected)) return false;
            player.WarpForTraining(state.position, state.rotation); player.SetAmmo(Mathf.Clamp(state.ammo, 0, player.MaxAmmo));
            hold = state.hold; capturedZones = state.capturedZones;
            if (entry.objectiveType == ChallengeObjectiveType.BasicRoute) SetMarker(entry.routePoints[snapshot.targetsCompleted]);
            game.mainCamera.SnapBehindTarget();
            return true;
        }

        void CleanupWorld()
        {
            Time.timeScale = 1;
            if (game == null) return;
            game.matchController.OnMatchEnded -= RelayCompleted;
            if (entry?.objectiveType == ChallengeObjectiveType.CompleteOfflineRelay && player != null)
                game.matchController.StopOfflinePractice();
            MatchView.Unregister(this);
            if (world != null) { world.SetActive(false); Destroy(world); }
            LaunchPadBehaviour.DestroyAll();
            world = null; marker = null; player = null; demonstrationPad = null;
            participants.Clear(); targets.Clear(); defenders.Clear(); destroyedTargets = null;
        }

        void OnDestroy()
        {
            if (Controller != null) Controller.OnStateChanged -= StateChanged;
            CleanupWorld();
        }
    }
}
