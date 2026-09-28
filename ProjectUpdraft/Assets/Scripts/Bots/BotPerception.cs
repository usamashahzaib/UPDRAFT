using System.Collections.Generic;
using UnityEngine;

namespace Updraft.Bots
{
    public struct PerceivedEnemy
    {
        public int entityId;
        public Vector3 lastKnownPosition;
        public float timeSinceSeen;
    }

    /// <summary>
    /// Spec section 12: bots perceive opponents through line-of-sight and
    /// audible gameplay events only — never a hidden position read. The
    /// sight test is a real raycast against the same occluding geometry that
    /// blocks shots, limited to a forward view cone and range.
    /// </summary>
    public class BotPerception
    {
        readonly Dictionary<int, PerceivedEnemy> _known = new Dictionary<int, PerceivedEnemy>();
        readonly List<int> _keys = new List<int>();

        /// <summary>Geometry that blocks sight: floors/platforms, cover and spawn walls. Players never block sight.</summary>
        public static int OccluderMask => LayerMask.GetMask("Ground", "Cover", "SpawnBarrier");

        /// <summary>
        /// True when <paramref name="target"/> is within range, inside the horizontal
        /// view cone around <paramref name="viewForward"/>, and nothing in
        /// <paramref name="occluderMask"/> lies between the eye and the target.
        /// </summary>
        public static bool CanSee(PhysicsScene physics, Vector3 eye, Vector3 viewForward, Vector3 target,
            float range, float viewConeHalfAngleDegrees, int occluderMask)
        {
            Vector3 toTarget = target - eye;
            float distance = toTarget.magnitude;
            if (distance > range) return false;
            if (distance < 0.01f) return true;

            Vector3 flatForward = new Vector3(viewForward.x, 0f, viewForward.z);
            Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatForward.sqrMagnitude > 1e-4f && flatToTarget.sqrMagnitude > 1e-4f &&
                Vector3.Angle(flatForward, flatToTarget) > viewConeHalfAngleDegrees)
                return false;

            return !physics.Raycast(eye, toTarget / distance, distance, occluderMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Call once per tick per potential enemy with the result of a real LOS test.</summary>
        public void ReportVisible(int entityId, Vector3 position)
        {
            _known[entityId] = new PerceivedEnemy { entityId = entityId, lastKnownPosition = position, timeSinceSeen = 0f };
        }

        /// <summary>Call for a loud, audible event (gunfire, pad launch) within hearing range even without LOS.</summary>
        public void ReportHeard(int entityId, Vector3 soundOrigin)
        {
            if (_known.TryGetValue(entityId, out var existing) && existing.timeSinceSeen <= 0f) return; // seen this tick: more precise
            _known[entityId] = new PerceivedEnemy { entityId = entityId, lastKnownPosition = soundOrigin, timeSinceSeen = 0f };
        }

        public void Tick(float deltaTime, float forgetAfterSeconds)
        {
            _keys.Clear();
            _keys.AddRange(_known.Keys);
            foreach (var key in _keys)
            {
                var entry = _known[key];
                entry.timeSinceSeen += deltaTime;
                if (entry.timeSinceSeen > forgetAfterSeconds) _known.Remove(key);
                else _known[key] = entry;
            }
        }

        public void Clear() => _known.Clear();

        public bool TryGetMostRecent(out PerceivedEnemy enemy)
        {
            enemy = default;
            float best = float.MaxValue;
            bool found = false;
            foreach (var kvp in _known)
            {
                if (kvp.Value.timeSinceSeen < best)
                {
                    best = kvp.Value.timeSinceSeen;
                    enemy = kvp.Value;
                    found = true;
                }
            }
            return found;
        }
    }
}
