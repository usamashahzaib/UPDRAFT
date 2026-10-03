using System.Collections.Generic;
using UnityEngine;
using Hotdrop.Bots;
using Hotdrop.Config;

namespace Hotdrop.Network
{
    /// <summary>
    /// Server-side input for labeled bots (bot-filled slots, disconnect and AFK
    /// substitution). Bots produce the same NetInput a human client sends, so they
    /// obey identical movement, cadence, ammo and cooldown rules (spec section 12).
    /// Perception is line of sight plus audible gunfire only.
    /// </summary>
    public static class NetBotDriver
    {
        class Brain
        {
            public BotDecisionMaker decisions;
            public NetPlayer target;
            public float fireAllowedAt;
            public float aimError;
            public Vector3 aimAxis;
            public Vector3[] corners = new Vector3[32];
            public int cornerCount, corner;
            public float nextPath;
            public Vector3 pathGoal;
            public float yaw;
            public System.Random rng;
        }

        static readonly Dictionary<NetPlayer, Brain> brains = new Dictionary<NetPlayer, Brain>();
        static UnityEngine.AI.NavMeshPath path;
        static BotConfig config;

        public static NetInput Decide(NetPlayer self, NetMatch match)
        {
            config ??= Resources.Load<BotConfig>("Config/BotConfig");
            if (path == null) { try { path = new UnityEngine.AI.NavMeshPath(); } catch { path = null; } }
            PruneDestroyed(); // QA-034
            if (!brains.TryGetValue(self, out var brain))
            {
                brain = new Brain { decisions = new BotDecisionMaker(config, self.Slot * 7919 + 13), rng = new System.Random(self.Slot * 31 + 7), yaw = self.transform.eulerAngles.y };
                brains[self] = brain;
            }

            var runner = self.Runner;
            float now = runner.SimulationTime;
            float dt = runner.DeltaTime;
            var input = new NetInput { Pitch = 0f };
            Vector3 eye = self.transform.position + Vector3.up * 1.6f;
            var physics = runner.GetPhysicsScene();
            int losMask = BotPerception.OccluderMask;
            Vector3 viewForward = Quaternion.Euler(0f, brain.yaw, 0f) * Vector3.forward;

            // Perception (shared rule with offline bots): nearest enemy in the view cone with
            // clear line of sight, sticky while still visible; gunfire is heard, never a hidden read.
            NetPlayer visible = null, sticky = null;
            float best = float.MaxValue;
            Vector3? heard = null;
            foreach (var other in NetPlayer.All)
            {
                if (other.Runner != runner || other.Team == self.Team || other.Dead) continue;
                Vector3 chest = other.transform.position + Vector3.up * 1.4f;
                float d = Vector3.Distance(eye, chest);
                if (d <= config.hearingRangeMeters && other.FireCooldown.IsRunning && !other.FireCooldown.Expired(runner)) heard = other.transform.position;
                if (!BotPerception.CanSee(physics, eye, viewForward, chest, config.sightRangeMeters, config.viewConeHalfAngleDegrees, losMask)) continue;
                if (other == brain.target) sticky = other;
                if (d < best) { best = d; visible = other; }
            }
            var chosen = sticky ?? visible;
            if (chosen != null && chosen != brain.target)
            {
                brain.fireAllowedAt = now + brain.decisions.ReactionSeconds(false) + brain.decisions.FocusFireDesyncSeconds();
                brain.aimError = config.initialAimErrorStandardDegrees;
                brain.aimAxis = new Vector3((float)brain.rng.NextDouble() - 0.5f, (float)brain.rng.NextDouble() - 0.5f, (float)brain.rng.NextDouble() - 0.5f);
            }
            brain.target = chosen;

            Vector3 objective = match.ActiveObjectivePosition;
            if (chosen == null && heard.HasValue && Vector3.Distance(self.transform.position, objective) < 25f) objective = heard.Value;

            var intent = brain.decisions.Tick(new BotDecisionInputs
            {
                currentHealthFraction = self.HealthFraction,
                hasVisibleEnemy = chosen != null,
                enemyPosition = chosen != null ? chosen.transform.position : Vector3.zero,
                selfPosition = self.transform.position,
                activeObjectivePosition = objective,
            }, dt, false);

            Vector3 goal = intent switch
            {
                BotIntent.Retreat => self.Team == 0 ? NetConfigs.Map.northSpawnCenter : NetConfigs.Map.southSpawnCenter,
                _ => objective,
            };

            if (intent == BotIntent.EngageEnemy && chosen != null)
            {
                brain.aimError = Mathf.Max(config.minimumAimErrorStandardDegrees, brain.aimError - dt * config.aimErrorRecoveryDegreesPerSecond);
                Vector3 aim = chosen.transform.position + Vector3.up * 1.2f - eye;
                aim = Quaternion.AngleAxis(brain.aimError, Vector3.Cross(aim, brain.aimAxis)) * aim;
                var look = Quaternion.LookRotation(aim).eulerAngles;
                brain.yaw = look.y;
                input.Pitch = look.x > 180f ? look.x - 360f : look.x;
                float engage = self.Weapon.fullDamageDistance;
                float dist = Vector3.Distance(self.transform.position, chosen.transform.position);
                // Objective first: keep advancing along the route while shooting; hold once inside the zone.
                Vector3 flat = objective - self.transform.position; flat.y = 0f;
                Vector3 worldMove = Vector3.zero;
                if (flat.magnitude > NetConfigs.Map.zoneRadius * 0.7f)
                {
                    Vector3 next = NextCorner(brain, self.transform.position, objective, now) - self.transform.position;
                    next.y = 0f;
                    if (next.sqrMagnitude > 0.01f) worldMove = next.normalized;
                }
                else if (dist < engage * 0.4f)
                {
                    Vector3 away = self.transform.position - chosen.transform.position; away.y = 0f;
                    worldMove = away.normalized * 0.5f;
                }
                Quaternion facing = Quaternion.Euler(0f, brain.yaw, 0f);
                input.Move = new Vector2(Vector3.Dot(worldMove, facing * Vector3.right), Vector3.Dot(worldMove, facing * Vector3.forward));
                input.Set(NetButton.Fire, now >= brain.fireAllowedAt);
            }
            else
            {
                Vector3 next = NextCorner(brain, self.transform.position, goal, now);
                Vector3 flat = next - self.transform.position; flat.y = 0f;
                if (flat.sqrMagnitude > 1f)
                {
                    brain.yaw = Mathf.MoveTowardsAngle(brain.yaw, Quaternion.LookRotation(flat).eulerAngles.y, 360f * dt);
                    input.Move = new Vector2(0, 1f);
                }
            }
            input.Yaw = brain.yaw;
            return input;
        }

        static Vector3 NextCorner(Brain b, Vector3 from, Vector3 goal, float now)
        {
            if (now >= b.nextPath || Vector3.Distance(b.pathGoal, goal) > 1f)
            {
                b.nextPath = now + 0.75f;
                b.pathGoal = goal;
                b.corner = 1;
                b.cornerCount = 0;
                if (UnityEngine.AI.NavMesh.SamplePosition(from, out var o, 2f, UnityEngine.AI.NavMesh.AllAreas) &&
                    UnityEngine.AI.NavMesh.SamplePosition(goal, out var g, 2f, UnityEngine.AI.NavMesh.AllAreas) &&
                    UnityEngine.AI.NavMesh.CalculatePath(o.position, g.position, UnityEngine.AI.NavMesh.AllAreas, path))
                    b.cornerCount = path.GetCornersNonAlloc(b.corners);
            }
            while (b.corner < b.cornerCount - 1 && Vector3.Distance(from, b.corners[b.corner]) < 1f) b.corner++;
            return b.corner < b.cornerCount ? b.corners[b.corner] : goal;
        }

        public static void Forget(NetPlayer player) => brains.Remove(player);

        static readonly List<NetPlayer> pruneScratch = new List<NetPlayer>();

        // QA-034: safety net behind NetPlayer.Despawned -> Forget. Drops brains whose
        // player has been destroyed (Unity null) so a long-lived server never retains
        // past players, and clears stale targets held by live brains.
        static void PruneDestroyed()
        {
            pruneScratch.Clear();
            foreach (var pair in brains)
            {
                if (pair.Key == null) pruneScratch.Add(pair.Key);
                else if (pair.Value.target == null) pair.Value.target = null; // release a destroyed target reference
            }
            for (int i = 0; i < pruneScratch.Count; i++) brains.Remove(pruneScratch[i]);
            pruneScratch.Clear();
        }
    }
}


