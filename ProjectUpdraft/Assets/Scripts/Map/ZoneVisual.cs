using UnityEngine;
using Updraft.Config;
using Updraft.Match;

namespace Updraft.Map
{
    /// <summary>
    /// Makes the one active zone unmistakable (spec section 3: one active zone,
    /// next zone shown 10 s early). Active: thick ring plus a tall beacon.
    /// Upcoming: blinking ring. Inactive: thin ring. Uses only the saved
    /// Objective material so nothing depends on runtime shader lookup.
    /// </summary>
    public class ZoneVisual : MonoBehaviour
    {
        public ZoneId zoneId;
        LineRenderer ring;
        LineRenderer beacon;

        public void Initialize(ZoneId id, LineRenderer zoneRing, Vector3 center, Material material)
        {
            zoneId = id;
            ring = zoneRing;
            beacon = new GameObject("Beacon").AddComponent<LineRenderer>();
            beacon.transform.SetParent(transform, false);
            beacon.sharedMaterial = material;
            beacon.useWorldSpace = true;
            beacon.positionCount = 2;
            beacon.SetPosition(0, center + Vector3.up * 0.2f);
            beacon.SetPosition(1, center + Vector3.up * 9f);
            beacon.startWidth = 0.35f;
            beacon.endWidth = 0.05f;
            beacon.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beacon.enabled = false;
        }

        void Update()
        {
            if (ring == null) return;
            // MatchView works for both offline practice and the networked match.
            var match = MatchView.Current;
            bool live = match != null && match.Participants.Count > 0 &&
                        (match.CurrentPhase == MatchPhase.InProgress || match.CurrentPhase == MatchPhase.Overtime ||
                         match.CurrentPhase == MatchPhase.Preparation || match.CurrentPhase == MatchPhase.Countdown);
            bool active = live && match.ScoringZone == zoneId;
            bool upcoming = live && !active && match.Rules.GetUpcomingZone(match.MatchTime) == zoneId;

            float width = active ? 0.4f : upcoming ? (Mathf.PingPong(Time.time * 3f, 1f) > 0.5f ? 0.3f : 0.06f) : 0.08f;
            ring.startWidth = ring.endWidth = live ? width : 0.14f;
            beacon.enabled = active;
        }
    }
}
