using UnityEngine;
using Updraft.Config;
using Updraft.Bots;
using Updraft.Movement;
using Updraft.Weapons;
using Updraft.Pads;
using Updraft.Match;
using Updraft.Player;

namespace Updraft.Bots
{
    /// <summary>
    /// MonoBehaviour that drives a bot using BotDecisionMaker, BotPerception,
    /// PlayerMovementSimulator, and ShotResolver. Same movement/HP/ammo/cooldowns
    /// as humans, driven through the same input interface. LOS + audible-event
    /// perception only — never hidden position reads.
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
        [SerializeField] Transform aimTarget; // for debugging

        [Header("Behavior")]
        [SerializeField] bool isEasyDifficulty = false;
        [SerializeField] float perceptionRadius = 50f;
        [SerializeField] float hearingRadius = 30f;
        [SerializeField] float forgetAfterSeconds = 5f;
        [SerializeField] LayerMask losMask;
        [SerializeField] LayerMask groundMask;

        // Decision/perception only — actual movement/pad/weapon simulation is
        // owned by PlayerController (this drives it via SetExternalInput, the
        // same way TouchInputManager drives a human) so bots never run a
        // second, conflicting CharacterController.Move each frame.
        BotDecisionMaker decisionMaker;
        BotPerception perception;

        // Runtime state
        BotIntent currentIntent;
        Vector3 moveInput;
        bool jumpPressed;
        bool firePressed;
        Vector3 lastKnownEnemyPos;
        bool hasVisibleEnemy;
        float stuckTimer;
        Vector3 lastPosition;
        int botSeed;

        // Authored route target used by the current graybox navigation.
        Vector3 currentRouteTarget;
        bool hasRouteTarget;
        bool routeTargetIsPad;

        // Objective positions
        Vector3 centerObjective;
        Vector3 westObjective;
        Vector3 eastObjective;

        // Pad awareness
        float padCheckTimer;

        void Awake()
        {
            objectivePath = new UnityEngine.AI.NavMeshPath();
            playerController = GetComponent<PlayerController>();

            // PlayerController (via its own [RequireComponent(typeof(CharacterController))])
            // owns CharacterController setup and the actual movement simulation.
            botSeed = Random.Range(0, int.MaxValue);
            decisionMaker = new BotDecisionMaker(botConfig, botSeed);
            perception = new BotPerception();

            // Cache objectives
            centerObjective = mapConfig.centerZone;
            westObjective = mapConfig.westZone;
            eastObjective = mapConfig.eastZone;
        }

        public void Initialize(int playerId, int teamId, WeaponDefinition startingWeapon, bool easyDifficulty)
        {
            playerController.Initialize(playerId, teamId, startingWeapon);
            isEasyDifficulty = easyDifficulty;
        }

        void Update()
        {
            if (!playerController.IsAlive) return;

            float dt = Time.deltaTime;

            // Tick perception
            TickPerception(dt);

            // Tick decision maker
            TickDecision(dt);

            // Execute intent (sets moveInput/jumpPressed/firePressed for this frame)
            ExecuteIntent(dt);

            if (unstuckJumpRequested)
            {
                jumpPressed = true;
                unstuckJumpRequested = false;
            }

            // Decide whether to place a pad this frame (separate from movement input)
            HandlePadPlacement();

            // Drive PlayerController exactly like a human's TouchInputManager would.
            playerController.SetExternalInput(
                new Vector2(moveInput.x, moveInput.y), jumpPressed, firePressed, reloadPressed: false);

            // Update stuck detection
            UpdateStuckDetection(dt);
        }

        // Target tracking: reaction delay before the first shot, and an aim error
        // that shrinks while the target stays visible (imperfect tracking, never
        // reduced damage).
        PlayerController target;
        float fireAllowedAt;
        float aimErrorDegrees;
        Vector3 aimErrorAxis;
        Vector3 heardPosition;
        bool hasHeardEvent;

        void TickPerception(float dt)
        {
            perception.Tick(dt, forgetAfterSeconds);

            // Line of sight only: bots never read the position of an enemy they cannot see or hear.
            PlayerController nearest = null, previousStillVisible = null;
            float nearestDist = float.MaxValue;
            hasHeardEvent = false;
            Vector3 eyePos = transform.position + Vector3.up * 1.6f;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var enemy = players[i];
                if (enemy == playerController || !enemy.IsAlive || enemy.TeamId == playerController.TeamId) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist <= hearingRadius && Time.time - enemy.LastShotTime < 0.5f)
                {
                    perception.ReportHeard(enemy.PlayerId, enemy.transform.position);
                    heardPosition = enemy.transform.position;
                    hasHeardEvent = true;
                }
                if (dist > perceptionRadius) continue;

                Vector3 targetPos = enemy.transform.position + Vector3.up * 1.4f;
                if (Physics.Raycast(eyePos, (targetPos - eyePos).normalized, dist, losMask, QueryTriggerInteraction.Ignore)) continue;

                perception.ReportVisible(enemy.PlayerId, enemy.transform.position);
                if (enemy == target) previousStillVisible = enemy;
                if (dist < nearestDist) { nearestDist = dist; nearest = enemy; }
            }

            var chosen = previousStillVisible ?? nearest;
            if (chosen != null && chosen != target)
            {
                // New target: human-like reaction delay and a fresh aim error.
                float reaction = isEasyDifficulty ? botConfig.easyPracticeReactionSeconds : botConfig.standardReactionSeconds;
                // Desynchronise teammates so a bot team never fires in perfect unison.
                if (Random.value < botConfig.focusFireDesyncChance) reaction += Random.Range(0.1f, 0.35f);
                fireAllowedAt = Time.time + reaction;
                aimErrorDegrees = isEasyDifficulty ? 7f : 4.5f;
                aimErrorAxis = Random.onUnitSphere;
                // Call out a newly seen enemy's location (location only, rate-limited by the ping system).
                Vector3 eye = transform.position + Vector3.up * 1.6f;
                Updraft.UI.PingSystem.Instance?.RequestPing(playerController, new Ray(eye, chosen.transform.position + Vector3.up * 1.2f - eye));
            }
            target = chosen;
            hasVisibleEnemy = target != null;
            if (hasVisibleEnemy)
            {
                lastKnownEnemyPos = target.transform.position;
                aimErrorDegrees = Mathf.Max(isEasyDifficulty ? 2.5f : 1.2f, aimErrorDegrees - dt * 3f);
                if (aimTarget != null) aimTarget.position = lastKnownEnemyPos;
            }
        }

        void TickDecision(float dt)
        {
            // Get active objective
            Vector3 activeObjective = GetActiveObjective();

            // A heard but unseen shooter pulls the bot toward the sound, never onto the hidden player.
            if (!hasVisibleEnemy && hasHeardEvent && Vector3.Distance(transform.position, activeObjective) < 25f)
                activeObjective = heardPosition;
            // Teammates' "go here" / "group up" pings take precedence while they last.
            var pings = Updraft.UI.PingSystem.Instance;
            if (!hasVisibleEnemy && pings != null && pings.TryGetTeamRally(playerController.TeamId, playerController.PlayerId, out var rally))
                activeObjective = rally;
            var inputs = new BotDecisionInputs
            {
                currentHealthFraction = playerController.CurrentHealth / playerController.MaxHealth,
                hasVisibleEnemy = hasVisibleEnemy,
                enemyPosition = lastKnownEnemyPos,
                selfPosition = transform.position,
                activeObjectivePosition = activeObjective
            };

            currentIntent = decisionMaker.Tick(inputs, dt, isEasyDifficulty);
        }

        Vector3 GetActiveObjective()
        {
            var match = MatchController.Current;
            return match != null ? match.ActiveObjectivePosition : centerObjective;
        }

        void ExecuteIntent(float dt)
        {
            moveInput = Vector2.zero;
            jumpPressed = false;
            firePressed = false;

            switch (currentIntent)
            {
                case BotIntent.RotateToObjective:
                    ExecuteRotateToObjective();
                    break;

                case BotIntent.EngageEnemy:
                    ExecuteEngageEnemy();
                    break;

                case BotIntent.Retreat:
                    ExecuteRetreat();
                    break;

                case BotIntent.UseRoute:
                    ExecuteUseRoute();
                    break;
            }
        }

        UnityEngine.AI.NavMeshPath objectivePath;
        readonly Vector3[] objectiveCorners = new Vector3[32];
        int objectiveCornerCount;
        float nextPathTime;
        int pathCorner;
        Vector3 pathObjective;

        /// <summary>Flat direction along the NavMesh path to the objective; false once within 2 m of it.</summary>
        bool TryObjectiveDirection(Vector3 objective, out Vector3 direction)
        {
            Vector3 destination = objective;
            if (Time.time >= nextPathTime || Vector3.Distance(pathObjective, objective) > 1f)
            {
                nextPathTime = Time.time + 0.75f;
                pathObjective = objective;
                pathCorner = 1;
                objectivePath.ClearCorners();
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var origin, 2f, UnityEngine.AI.NavMesh.AllAreas) &&
                    UnityEngine.AI.NavMesh.SamplePosition(objective, out var goal, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    UnityEngine.AI.NavMesh.CalculatePath(origin.position, goal.position, UnityEngine.AI.NavMesh.AllAreas, objectivePath);
                objectiveCornerCount = objectivePath.GetCornersNonAlloc(objectiveCorners);
            }
            while (pathCorner < objectiveCornerCount - 1 && Vector3.Distance(transform.position, objectiveCorners[pathCorner]) < 1f) pathCorner++;
            if (pathCorner < objectiveCornerCount) destination = objectiveCorners[pathCorner];
            direction = destination - transform.position;
            direction.y = 0;
            direction = direction.normalized;
            return direction.sqrMagnitude > 0.01f &&
                   Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(objective.x, objective.z)) >= 2f;
        }

        void ExecuteRotateToObjective()
        {
            Vector3 objective = GetActiveObjective();
            if (!TryObjectiveDirection(objective, out var toObjective)) return;

            // A nearby pad (any team's) that launches toward the objective is a shortcut worth taking.
            if (TryDriveToLaunchPad(objective)) return;

            // Simple move toward objective
            moveInput = new Vector2(0, 1f); // forward in local space
            // Rotate toward objective
            Quaternion targetRot = Quaternion.LookRotation(toObjective);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
        }

        void ExecuteEngageEnemy()
        {
            if (!hasVisibleEnemy) return;

            Vector3 toEnemy = (lastKnownEnemyPos - transform.position).normalized;
            toEnemy.y = 0;

            float dist = Vector3.Distance(transform.position, lastKnownEnemyPos);
            float engageRange = playerController.CurrentWeapon?.fullDamageDistance ?? 20f;

            // A committed pad route (usually our own fresh pad) beats a long-range duel.
            if (padTarget != null && dist > 25f && TryDriveToLaunchPad(GetActiveObjective())) return;

            // Face enemy
            Quaternion targetRot = Quaternion.LookRotation(toEnemy);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);

            // The objective wins matches: keep pushing along the route while shooting
            // (spec 12: attack/rotate toward the objective). Inside the zone, strafe to hold it.
            Vector3 objective = GetActiveObjective();
            Vector2 flatToObjective = new Vector2(objective.x - transform.position.x, objective.z - transform.position.z);
            float holdRadius = mapConfig.zoneRadius * 0.7f;
            Vector3 worldMove;
            if (flatToObjective.magnitude > holdRadius && TryObjectiveDirection(objective, out var pathDir))
                worldMove = pathDir;
            else if (dist < engageRange * 0.4f)
                worldMove = -toEnemy * 0.5f; // too close: give ground but stay in the zone
            else
                worldMove = transform.right * (Mathf.PingPong(Time.time * 0.6f + playerController.PlayerId, 2f) - 1f) * 0.6f;
            // Move input is local to the body, which is facing the enemy.
            moveInput = new Vector2(Vector3.Dot(worldMove, transform.right), Vector3.Dot(worldMove, transform.forward));

            // Aim at the upper body with a decaying tracking error, in 3D so
            // airborne and elevated targets can be hit.
            Vector3 muzzle = transform.position + Vector3.up * 1.4f;
            Vector3 aim = target.transform.position + Vector3.up * 1.2f - muzzle;
            aim = Quaternion.AngleAxis(aimErrorDegrees, Vector3.Cross(aim, aimErrorAxis)) * aim;
            playerController.SetExternalAim(aim);

            firePressed = Time.time >= fireAllowedAt;
        }

        void ExecuteRetreat()
        {
            // Move away from enemy toward cover/spawn
            Vector3 retreatTarget = playerController.TeamId == 0 ? mapConfig.northSpawnCenter : mapConfig.southSpawnCenter;
            Vector3 toRetreat = (retreatTarget - transform.position).normalized;
            toRetreat.y = 0;

            moveInput = new Vector2(0, 1f);
            Quaternion targetRot = Quaternion.LookRotation(toRetreat);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
        }

        void ExecuteUseRoute()
        {
            // Navigate to route target (pad or platform)
            if (!hasRouteTarget)
            {
                FindRouteTarget();
            }

            if (hasRouteTarget)
            {
                Vector3 toTarget = (currentRouteTarget - transform.position).normalized;
                toTarget.y = 0;

                if (Vector3.Distance(transform.position, currentRouteTarget) < 2f)
                {
                    // Standing on the target — if it's an armed pad, jump to launch
                    // (PlayerController's own pad-activation logic reads JumpPressed).
                    if (routeTargetIsPad) jumpPressed = true;
                    hasRouteTarget = false;
                    return;
                }

                moveInput = new Vector2(0, 1f);
                Quaternion targetRot = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
            }
        }

        // ---- Launch pads (spec 6/12): bots place and use pads by the same rules as humans ----
        LaunchPad padTarget;
        float padScanTimer;
        float padGiveUpAt;

        /// <summary>Flat distance of a launch on level ground: v_h * 2 v_up / g.</summary>
        float LaunchRange() => padConfig.launchHorizontalVelocity * 2f * padConfig.launchUpwardVelocity / movementConfig.gravity;

        static bool PadGone(LaunchPad pad) => pad == null || pad.State == PadState.Destroyed || pad.State == PadState.Expired;

        /// <summary>Walks onto a pad that shortens the route and jumps once it is armed. False when there is none.</summary>
        bool TryDriveToLaunchPad(Vector3 objective)
        {
            if (!Updraft.Core.StudySettings.PadsEnabled || !playerController.PadManager.CanActivate) { padTarget = null; return false; }
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
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toPad.normalized), 360f * Time.deltaTime);
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
            foreach (var behaviour in FindObjectsByType<LaunchPadBehaviour>(FindObjectsSortMode.None))
            {
                var pad = behaviour.PadData;
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

        void FindRouteTarget()
        {
            // Find nearby pad or ramp to elevated position
            var pads = FindObjectsByType<LaunchPadBehaviour>(FindObjectsSortMode.None);
            foreach (var pad in pads)
            {
                // Spec section 6: anyone can use any pad, enemy pads included.
                if (pad.PadData.CanBeUsed && Vector3.Distance(transform.position, pad.transform.position) < 20f)
                {
                    currentRouteTarget = pad.transform.position;
                    hasRouteTarget = true;
                    routeTargetIsPad = true;
                    return;
                }
            }

            // Fallback: find platform ramp
            // Simplified - just pick a platform center
            Vector3[] platforms = mapConfig.platformCenters;
            if (platforms.Length > 0)
            {
                currentRouteTarget = platforms[Random.Range(0, platforms.Length)];
                hasRouteTarget = true;
                routeTargetIsPad = false;
            }
        }

        void HandlePadPlacement()
        {
            // Place pad if we have charge and it's strategic (activation itself is
            // handled by PlayerController reading JumpPressed — see ExecuteUseRoute).
            padCheckTimer -= Time.deltaTime;
            if (padCheckTimer <= 0f && playerController.PadManager.HasCharge && Updraft.Core.StudySettings.PadsEnabled)
            {
                padCheckTimer = 2f;
                if (ShouldPlacePad(out var launchDirection))
                {
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
            }
        }

        bool ShouldPlacePad(out Vector3 direction)
        {
            // A pad is a forward launch (~17 m on level ground): worth it when the objective is at
            // least one launch away, the flight path is clear, and we are not in a close fight.
            direction = GetActiveObjective() - transform.position;
            direction.y = 0f;
            if (currentIntent == BotIntent.Retreat || currentIntent == BotIntent.UseRoute || padTarget != null) return false;
            var phase = MatchController.Current != null ? MatchController.Current.CurrentPhase : MatchPhase.InProgress;
            if (phase != MatchPhase.InProgress && phase != MatchPhase.Overtime) return false;
            if (!playerController.IsGrounded) return false;
            if (hasVisibleEnemy && Vector3.Distance(transform.position, lastKnownEnemyPos) < 25f) return false;
            float range = LaunchRange();
            if (direction.magnitude < range * 0.9f) return false;
            direction.Normalize();
            return !Physics.Raycast(transform.position + Vector3.up * 1.5f, direction, range, losMask, QueryTriggerInteraction.Ignore);
        }

        bool unstuckJumpRequested;

        void UpdateStuckDetection(float dt)
        {
            float moved = Vector3.Distance(transform.position, lastPosition);
            if (moved < 0.1f)
            {
                stuckTimer += dt;
                if (stuckTimer > 3f)
                {
                    // Try to unstuck - jump next frame's ExecuteIntent won't reset this away.
                    unstuckJumpRequested = true;
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = transform.position;
        }

        // Called by game when bot hears a loud event
        public void OnHeardEvent(int entityId, Vector3 position)
        {
            perception.ReportHeard(entityId, position);
        }

        // Called by game when bot takes damage
        public void OnDamaged(float amount, Vector3 fromDirection)
        {
            // Retreat behavior is handled by decision maker based on health fraction
        }
    }
}
