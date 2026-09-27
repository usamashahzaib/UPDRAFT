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
    /// server-side bot controller must feed this class the same visibility
    /// queries (raycasts, audio-event radius checks) a human client's
    /// server-side hit validation already needs, so there's no separate
    /// "cheating" code path.
    /// </summary>
    public class BotPerception
    {
        readonly System.Collections.Generic.Dictionary<int, PerceivedEnemy> _known = new();

        /// <summary>Call once per tick per potential enemy with the result of a real LOS raycast.</summary>
        public void ReportVisible(int entityId, Vector3 position)
        {
            _known[entityId] = new PerceivedEnemy { entityId = entityId, lastKnownPosition = position, timeSinceSeen = 0f };
        }

        /// <summary>Call for a loud, audible event (gunfire, pad launch) within hearing range even without LOS.</summary>
        public void ReportHeard(int entityId, Vector3 approximatePosition)
        {
            if (_known.ContainsKey(entityId)) return; // visible sighting is more precise; don't overwrite it
            _known[entityId] = new PerceivedEnemy { entityId = entityId, lastKnownPosition = approximatePosition, timeSinceSeen = 0f };
        }

        public void Tick(float deltaTime, float forgetAfterSeconds)
        {
            var toRemove = new System.Collections.Generic.List<int>();
            var keys = new int[_known.Count];
            _known.Keys.CopyTo(keys, 0);

            foreach (var key in keys)
            {
                var entry = _known[key];
                entry.timeSinceSeen += deltaTime;
                _known[key] = entry;
                if (entry.timeSinceSeen > forgetAfterSeconds) toRemove.Add(key);
            }

            foreach (var key in toRemove) _known.Remove(key);
        }

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
