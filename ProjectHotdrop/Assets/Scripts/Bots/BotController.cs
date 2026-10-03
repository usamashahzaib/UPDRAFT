using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Pads;
using Hotdrop.Match;
using Hotdrop.Player;

namespace Hotdrop.Bots
{
    /// <summary>
    /// Drives a bot's PlayerController through the same input interface a human
    /// uses (spec section 12): same movement, HP, ammo, cooldowns and weapons.
    /// Perception is line of sight inside a view cone plus audible gunfire —
    /// never a hidden position read. Decisions go through BotDecisionMaker,
    /// whose reaction delay is a real hold before a new intent is acted on.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerController))]
    public class BotController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] BotConfig botConfig;
        [SerializeField] MovementConfig movementConfig;
        [SerializeField] WeaponDefinition[] weaponDefinitions;
        [SerializeField] PadConfig padConfig;
        [SerializeField] MatchRulesConfig matchRules;
        [SerializeField] MapConfig mapConfig;

        [Header("References")]
        [SerializeField] PlayerController playerController;

        [Header("Behavior")]
        [SerializeField] bool isEasyDifficulty = false;

        // A heard shooter pulls the bot toward the sound only while it is near the objective anyway.
        const float InvestigateNearObjectiveMeters = 25f;
        const float EyeHeight = 1.6f;
        const float TargetChestHeight = 1.4f;

        BotDecisionMaker decisionMaker;
        BotPerception perception;
        int occluderMask;

        BotIntent currentIntent;
        Vector3 destination;
        Vector2 moveInput;
        bool jumpPressed;
        bool firePressed;
        bool wasAlive;

        // Tracking: reaction delay before the first shot, and an aim error that
        // shrinks while the target stays visible (imperfect tracking, never reduced damage).
        PlayerController target;
        Vector3 lastKnownEnemyPos;
        bool hasVisibleEnemy;
        float fireAllowedAt;
        float aimErrorDegrees;
        Vector3 aimErrorAxis;
        Vector3 heardPosition;
        bool hasHeardEvent;

        float stuckTimer;
        Vector3 lastPosition;
        bool unstuckJumpRequested;

        public BotIntent CurrentIntent => currentIntent;
        public bool IsEasy => isEasyDifficulty;
        public PlayerController Target => target;

        void Awake()
        {
            try { path = new UnityEngine.AI.NavMeshPath(); } catch { path = null; }
            playerController = GetComponent<PlayerController>();
            decisionMaker = new BotDecisionMaker(botConfig, Random.Range(0, int.MaxValue));
            perception = new BotPerception();
            // Built from layer names, never from a serialized mask that could silently be empty.
            occluderMask = BotPerception.OccluderMask;
        }

        public void Initialize(int playerId, int teamId, WeaponDefinition startingWeapon, bool easyDifficulty)
        {
            playerController.Initialize(playerId, teamId, startingWeapon);
            isEasyDifficulty = easyDifficulty;
        }

        void Update()
        {
            if (!playerController.IsAlive) { wasAlive = false; return; }
            if (!wasAlive) { wasAlive = true; ResetAwareness(); }

            float dt = Time.deltaTime;
            TickPerception(dt);
            TickDecision(dt);
            ExecuteIntent();

            if (unstuckJumpRequested)
            {
                jumpPressed = true;
                unstuckJumpRequested = false;
            }

            HandlePadPlacement();

            // Drive PlayerController exactly like a human's TouchInputManager would.
            playerController.SetExternalInput(moveInput, jumpPressed, firePressed, reloadPressed: false);
            UpdateStuckDetection(dt);
        }

        /// <summary>A respawned bot knows nothing it has not perceived since.</summary>
        void ResetAwareness()
        {
            decisionMaker.Reset();
            perception.Clear();
            target = null;
            hasVisibleEnemy = false;
            hasHeardEvent = false;
            padTarget = null;
            lastPosition = transform.position;
            stuckTimer = 0f;
        }

        // ---- Perception (spec 12: LOS + audible events only) ----

        void TickPerception(float dt)
        {
            perception.Tick(dt, botConfig.heardShotMemorySeconds);

            PlayerController nearest = null, previousStillVisible = null;
            float nearestDist = float.MaxValue;
            hasHeardEvent = false;
            float nearestHeard = float.MaxValue;
            Vector3 eye = transform.position + Vector3.up * EyeHeight;
            var physics = Physics.defaultPhysicsScene;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var enemy = players[i];
                if (enemy == playerController || !enemy.IsAlive || enemy.TeamId == playerController.TeamId) continue;

                // Gunfire is heard at the position it was fired from, not where the shooter is now.
                if (Time.time - enemy.LastShotTime <= botConfig.heardShotMemorySeconds)
                {
                    float heardDist = Vector3.Distance(transform.position, enemy.LastShotPosition);
                    if (heardDist <= botConfig.hearingRangeMeters && heardDist < nearestHeard)
                    {
                        nearestHeard = heardDist;
                        heardPosition = enemy.LastShotPosition;
                        hasHeardEvent = true;
                        perception.ReportHeard(enemy.PlayerId, enemy.LastShotPosition);
                    }
                }

                Vector3 chest = enemy.transform.position + Vector3.up * TargetChestHeight;
                if (!BotPerception.CanSee(physics, eye, transform.forward, chest,
                        botConfig.sightRangeMeters, botConfig.viewConeHalfAngleDegrees, occluderMask))
                    continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                perception.ReportVisible(enemy.PlayerId, enemy.transform.position);
                if (enemy == target) previousStillVisible = enemy;
                if (dist < nearestDist) { nearestDist = dist; nearest = enemy; }
            }

            var chosen = previousStillVisible ?? nearest;
            if (chosen != null && chosen != target)
            {
                // New target: human-like reaction delay (plus team desync) and a fresh aim error.
                fireAllowedAt = Time.time + decisionMaker.ReactionSeconds(isEasyDifficulty) + decisionMaker.FocusFireDesyncSeconds();
                aimErrorDegrees = isEasyDifficulty ? botConfig.initialAimErrorEasyDegrees : botConfig.initialAimErrorStandardDegrees;
                aimErrorAxis = Random.onUnitSphere;
                // Call out a newly seen enemy's location (location only, rate-limited by the ping system).
                Hotdrop.UI.PingSystem.Instance?.RequestPing(playerController,
                    new Ray(eye, chosen.transform.position + Vector3.up * 1.2f - eye));
            }
            target = chosen;
            hasVisibleEnemy = target != null;
            if (hasVisibleEnemy)
            {
                lastKnownEnemyPos = target.transform.position;
                float floor = isEasyDifficulty ? botConfig.minimumAimErrorEasyDegrees : botConfig.minimumAimErrorStandardDegrees;
                aimErrorDegrees = Mathf.Max(floor, aimErrorDegrees - dt * botConfig.aimErrorRecoveryDegreesPerSecond);
            }
        }

        // ---- Decision ----

        void TickDecision(float dt)
        {
            Vector3 objective = GetActiveObjective();
            destination = objective;

            // A heard but unseen shooter pulls the bot toward the sound, never onto the hidden player.
            if (!hasVisibleEnemy && hasHeardEvent && Vector3.Distance(transform.position, objective) < InvestigateNearObjectiveMeters)
                destination = heardPosition;
            // Teammates' "go here" / "group up" pings take precedence while they last.
            var pings = Hotdrop.UI.PingSystem.Instance;
            if (!hasVisibleEnemy && pings != null && pings.TryGetTeamRally(playerController.TeamId, playerController.PlayerId, out var rally))
                destination = rally;

            currentIntent = decisionMaker.Tick(new BotDecisionInputs
            {
                currentHealthFraction = playerController.CurrentHealth / playerController.MaxHealth,
                hasVisibleEnemy = hasVisibleEnemy,
                enemyPosition = lastKnownEnemyPos,
                selfPosition = transform.position,
                activeObjectivePosition = destination,
            }, dt, isEasyDifficulty);
        }

        /// <summary>Follows the live Relay rotation (spec 3), read through the match view like the HUD does.</summary>
        Vector3 GetActiveObjective()
        {
            var match = MatchView.Current;
            return match != null ? match.ActiveObjectivePosition : mapConfig.centerZone;
        }

        void ExecuteIntent()
        {
            moveInput = Vector2.zero;
            jumpPressed = false;
            firePressed = false;

            switch (currentIntent)
            {
                case BotIntent.RotateToObjective: ExecuteRotateToObjective(); break;
                case BotIntent.EngageEnemy: ExecuteEngageEnemy(); break;
                case BotIntent.Retreat: ExecuteRetreat(); break;
            }
        }

        void ExecuteRotateToObjective()
        {
            if (!TryPathDirection(destination, out var toDestination)) return;

            // A nearby pad (any team's) that launches toward the destination is a shortcut worth taking.
            if (TryDriveToLaunchPad(destination)) return;

            moveInput = new Vector2(0, 1f);
            Face(toDestination);
        }

        void ExecuteEngageEnemy()
        {
            if (!hasVisibleEnemy)
            {
                // Still reacting to losing sight: head for where the enemy was last seen, holding fire.
                if (TryPathDirection(lastKnownEnemyPos, out var toLastSeen)) { moveInput = new Vector2(0, 1f); Face(toLastSeen); }
                return;
            }

            Vector3 toEnemy = lastKnownEnemyPos - transform.position;
            toEnemy.y = 0;
            toEnemy.Normalize();
            float dist = Vector3.Distance(transform.position, lastKnownEnemyPos);
            float engageRange = playerController.CurrentWeapon?.fullDamageDistance ?? 20f;

            // A committed pad route (usually our own fresh pad) beats a long-range duel.
            if (padTarget != null && dist > 25f && TryDriveToLaunchPad(GetActiveObjective())) return;

            Face(toEnemy);

            // The objective wins matches: keep pushing along the route while shooting
            // (spec 12: attack/rotate toward the objective). Inside the zone, strafe to hold it.
            Vector3 objective = GetActiveObjective();
            Vector2 flatToObjective = new Vector2(objective.x - transform.position.x, objective.z - transform.position.z);
            float holdRadius = mapConfig.zoneRadius * 0.7f;
            Vector3 worldMove;
            if (flatToObjective.magnitude > holdRadius && TryPathDirection(objective, out var pathDir))
                worldMove = pathDir;
            else if (dist < engageRange * 0.4f)
                worldMove = -toEnemy * 0.5f; // too close: give ground but stay in the zone
            else
                worldMove = transform.right * (Mathf.PingPong(Time.time * 0.6f + playerController.PlayerId, 2f) - 1f) * 0.6f;
            // Move input is local to the body, which is facing the enemy.
            moveInput = new Vector2(Vector3.Dot(worldMove, transform.right), Vector3.Dot(worldMove, transform.forward));

            // Aim at the upper body with a decaying tracking error, in 3D so
            // airborne and elevated targets can be hit.
            Vector3 muzzle = transform.position + Vector3.up * TargetChestHeight;
            Vector3 aim = target.transform.position + Vector3.up * 1.2f - muzzle;
            aim = Quaternion.AngleAxis(aimErrorDegrees, Vector3.Cross(aim, aimErrorAxis)) * aim;
            playerController.SetExternalAim(aim);

            firePressed = Time.time >= fireAllowedAt;
        }

        /// <summary>Hurt: fall back along the navigation mesh to the team's own middle spawn exit to regenerate.</summary>
        void ExecuteRetreat()
        {
            Vector3 fallback = mapConfig.SpawnExit(playerController.TeamId, 1).position;
            if (!TryPathDirection(fallback, out var toFallback)) return; // safe at the exit: wait for regen
            moveInput = new Vector2(0, 1f);
            Face(toFallback);
        }

        void Face(Vector3 flatDirection)
        {
            if (flatDirection.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flatDirection), 360f * Time.deltaTime);
        }

        // ---- Navigation ----

        UnityEngine.AI.NavMeshPath path;
        readonly Vector3[] pathCorners = new Vector3[32];
        int pathCornerCount;
        float nextPathTime;
        int pathCorner;
        Vector3 pathGoal;

        /// <summary>Flat direction along the NavMesh path to <paramref name="goal"/>; false once within 2 m of it.</summary>
        bool TryPathDirection(Vector3 goal, out Vector3 direction)
        {
            Vector3 waypoint = goal;
            if (path == null)
            {
                try { path = new UnityEngine.AI.NavMeshPath(); } catch { path = null; }
            }
            if (path == null) { direction = Vector3.zero; return false; }
            if (Time.time >= nextPathTime || Vector3.Distance(pathGoal, goal) > 1f)
            {
                nextPathTime = Time.time + 0.75f;
                pathGoal = goal;
                pathCorner = 1;
                path.ClearCorners();
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var origin, 2f, UnityEngine.AI.NavMesh.AllAreas) &&
                    UnityEngine.AI.NavMesh.SamplePosition(goal, out var end, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    UnityEngine.AI.NavMesh.CalculatePath(origin.position, end.position, UnityEngine.AI.NavMesh.AllAreas, path);
                pathCornerCount = path.GetCornersNonAlloc(pathCorners);
            }
            while (pathCorner < pathCornerCount - 1 && Vector3.Distance(transform.position, pathCorners[pathCorner]) < 1f) pathCorner++;
            if (pathCorner < pathCornerCount) waypoint = pathCorners[pathCorner];
            direction = waypoint - transform.position;
            direction.y = 0;
            direction = direction.normalized;
            return direction.sqrMagnitude > 0.01f &&
                   Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(goal.x, goal.z)) >= 2f;
        }

        // ---- Launch pads (spec 6/12): bots place and use pads by the same rules as humans ----
        LaunchPad padTarget;
        float padScanTimer;
        float padGiveUpAt;
        float padCheckTimer;

        /// <summary>Flat distance of a launch on level ground: v_h * 2 v_up / g.</summary>
        float LaunchRange() => padConfig.launchHorizontalVelocity * 2f * padConfig.launchUpwardVelocity / movementConfig.gravity;

        static bool PadGone(LaunchPad pad) => pad == null || pad.State == PadState.Destroyed || pad.State == PadState.Expired;

        /// <summary>Walks onto a pad that shortens the route and jumps once it is armed. False when there is none.</summary>
        bool TryDriveToLaunchPad(Vector3 objective)
        {
            if (!Hotdrop.Core.StudySettings.PadsEnabled || !playerController.PadManager.CanActivate) { padTarget = null; return false; }
            padScanTimer -= Time.deltaTime;
            if (padTarget == null && padScanTimer <= 0f)
            {
                padScanTimer = 0.25f;
                padTarget = PickPad(objective);
                if (padTarget != null) padGiveUpAt = Time.time + 4f;
            }
            if (padTarget == null) return false;
            if (PadGone(padTarget) || Time.time > padGiveUpAt) { padTarget = null; return false; }

            Vector3 toPad = padTarget.Position - transform.position;
            toPad.y = 0f;
            if (toPad.magnitude <= padConfig.diameter * 0.35f)
            {
                // On the pad: wait out arming, then jump (PlayerController launches instead of jumping).
                if (padTarget.CanBeUsed) { jumpPressed = true; padTarget = null; }
                return true;
            }
            moveInput = new Vector2(0f, toPad.magnitude < 2f ? 0.6f : 1f);
            Face(toPad.normalized);
            return true;
        }

        LaunchPad PickPad(Vector3 objective)
        {
            Vector3 toObjective = objective - transform.position;
            toObjective.y = 0f;
            float range = LaunchRange();
            if (toObjective.magnitude < range * 0.8f) return null; // walking is faster than overshooting

            LaunchPad best = null;
            float bestDistance = 10f;
            var pads = LaunchPadBehaviour.All;
            for (int i = 0; i < pads.Count; i++)
            {
                var pad = pads[i].PadData;
                if (PadGone(pad)) continue;
                Vector3 toPad = pad.Position - transform.position;
                if (Mathf.Abs(toPad.y) > 1f) continue;
                toPad.y = 0f;
                Vector3 padToObjective = objective - pad.Position;
                padToObjective.y = 0f;
                Vector3 launch = pad.ForwardDirection;
                launch.y = 0f;
                // It must fire toward the objective, sit on the way there, and not overshoot it.
                if (Vector3.Dot(launch.normalized, padToObjective.normalized) < 0.8f) continue;
                if (padToObjective.magnitude > toObjective.magnitude + 2f || padToObjective.magnitude < range * 0.7f) continue;
                if (toPad.magnitude < bestDistance) { bestDistance = toPad.magnitude; best = pad; }
            }
            return best;
        }

        void HandlePadPlacement()
        {
            padCheckTimer -= Time.deltaTime;
            if (padCheckTimer > 0f || !playerController.PadManager.HasCharge || !Hotdrop.Core.StudySettings.PadsEnabled) return;
            padCheckTimer = 2f;
            if (!ShouldPlacePad(out var launchDirection)) return;

            // Pads launch along the placer's facing, so aim the body at the objective first.
            var facing = transform.rotation;
            transform.rotation = Quaternion.LookRotation(launchDirection);
            string rejected = playerController.TryPlacePad();
            if (rejected == null)
            {
                // Use our own pad straight away: it sits just ahead, launching toward the objective.
                padTarget = playerController.PadManager.ActivePad;
                padGiveUpAt = Time.time + 4f;
            }
            else
            {
                transform.rotation = facing;
                if (Debug.isDebugBuild) Debug.Log($"[BotPad] {playerController.DisplayName} placement rejected: {rejected}");
            }
        }

        bool ShouldPlacePad(out Vector3 direction)
        {
            // A pad is a forward launch (~17 m on level ground): worth it when the objective is at
            // least one launch away, the flight path is clear, and we are not in a close fight.
            direction = GetActiveObjective() - transform.position;
            direction.y = 0f;
            if (currentIntent == BotIntent.Retreat || padTarget != null) return false;
            var phase = MatchView.Current != null ? MatchView.Current.CurrentPhase : MatchPhase.InProgress;
            if (phase != MatchPhase.InProgress && phase != MatchPhase.Overtime) return false;
            if (!playerController.IsGrounded) return false;
            if (hasVisibleEnemy && Vector3.Distance(transform.position, lastKnownEnemyPos) < 25f) return false;
            float range = LaunchRange();
            if (direction.magnitude < range * 0.9f) return false;
            direction.Normalize();
            return !Physics.Raycast(transform.position + Vector3.up * 1.5f, direction, range, occluderMask, QueryTriggerInteraction.Ignore);
        }

        void UpdateStuckDetection(float dt)
        {
            float moved = Vector3.Distance(transform.position, lastPosition);
            if (moved < 0.1f && moveInput.sqrMagnitude > 0.01f)
            {
                stuckTimer += dt;
                if (stuckTimer > 3f)
                {
                    unstuckJumpRequested = true; // applied next frame after ExecuteIntent resets inputs
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = transform.position;
        }
    }
}



