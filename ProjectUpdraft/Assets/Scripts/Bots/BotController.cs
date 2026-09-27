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

        void TickPerception(float dt)
        {
            perception.Tick(dt, forgetAfterSeconds);

            // Scan for visible enemies
            hasVisibleEnemy = false;
            var enemies = FindObjectsOfType<PlayerController>();
            foreach (var enemy in enemies)
            {
                if (enemy == playerController || !enemy.IsAlive) continue;
                if (enemy.TeamId == playerController.TeamId) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist > perceptionRadius) continue;

                // LOS check
                Vector3 eyePos = transform.position + Vector3.up * 1.6f;
                Vector3 targetPos = enemy.transform.position + Vector3.up * 1.6f;
                Vector3 dir = (targetPos - eyePos).normalized;

                if (Physics.Raycast(eyePos, dir, dist, losMask, QueryTriggerInteraction.Ignore))
                {
                    // Hit something before enemy - no LOS
                    continue;
                }

                // Visible!
                perception.ReportVisible(enemy.PlayerId, enemy.transform.position);
                hasVisibleEnemy = true;
                lastKnownEnemyPos = enemy.transform.position;

                if (aimTarget != null)
                    aimTarget.position = lastKnownEnemyPos;
            }

            // Hear loud events (gunfire, pad launches) - simplified
            // In full implementation, this would subscribe to game events
        }

        void TickDecision(float dt)
        {
            // Get active objective
            Vector3 activeObjective = GetActiveObjective();

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
            float matchTime = 0f; // Would come from MatchController in real game
            var activeZone = matchRules.GetActiveZone(matchTime);
            if (activeZone.HasValue)
            {
                switch (activeZone.Value)
                {
                    case ZoneId.Center: return centerObjective;
                    case ZoneId.West: return westObjective;
                    case ZoneId.East: return eastObjective;
                }
            }
            return centerObjective;
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

        void ExecuteRotateToObjective()
        {
            Vector3 objective = GetActiveObjective();
            Vector3 toObjective = (objective - transform.position).normalized;
            toObjective.y = 0;

            // Check if we should use a pad to reach objective
            if (ShouldUsePadForObjective(objective))
            {
                currentIntent = BotIntent.UseRoute;
                return;
            }

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

            // Move toward enemy but stop at engagement range
            float dist = Vector3.Distance(transform.position, lastKnownEnemyPos);
            float engageRange = playerController.CurrentWeapon?.fullDamageDistance ?? 20f;

            if (dist > engageRange * 0.75f)
            {
                moveInput = new Vector2(0, 0.8f); // approach
            }
            else if (dist < engageRange * 0.4f)
            {
                moveInput = new Vector2(0, -0.5f); // back away
            }

            // Face enemy
            Quaternion targetRot = Quaternion.LookRotation(toEnemy);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);

            // Fire
            firePressed = true;
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

        bool ShouldUsePadForObjective(Vector3 objective)
        {
            // Check if there's a pad that helps reach the objective
            // Simplified: check if we're near a pad and objective is elevated
            if (playerController.PadManager.HasCharge)
            {
                float heightDiff = objective.y - transform.position.y;
                if (heightDiff > 2f) // objective is significantly higher
                {
                    return true;
                }
            }
            return false;
        }

        void FindRouteTarget()
        {
            // Find nearby pad or ramp to elevated position
            var pads = FindObjectsOfType<LaunchPadBehaviour>();
            foreach (var pad in pads)
            {
                if (pad.PadData.TeamId == playerController.TeamId && pad.PadData.CanBeUsed)
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
            if (padCheckTimer <= 0f && playerController.PadManager.HasCharge)
            {
                padCheckTimer = 2f;
                if (ShouldPlacePad())
                {
                    playerController.TryPlacePad();
                }
            }
        }

        bool ShouldPlacePad()
        {
            // Place pad when trying to reach elevated objective or escape
            Vector3 objective = GetActiveObjective();
            float heightDiff = objective.y - transform.position.y;
            return heightDiff > 2f || currentIntent == BotIntent.Retreat;
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
