using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.UI;

namespace Hotdrop.Map
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
        LineRenderer[] identityTicks;
        Material lineMaterial;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        public void Initialize(ZoneId id, LineRenderer zoneRing, Vector3 center, Material material)
        {
            zoneId = id;
            ring = zoneRing;
            if (material != null)
            {
                lineMaterial = new Material(material) { name = "Switchyard " + id + " zone marker" };
                if (lineMaterial.HasProperty(BaseColorId)) lineMaterial.SetColor(BaseColorId, Color.white);
                if (lineMaterial.HasProperty(LegacyColorId)) lineMaterial.SetColor(LegacyColorId, Color.white);
                if (ring != null) ring.sharedMaterial = lineMaterial;
            }
            beacon = new GameObject("Beacon").AddComponent<LineRenderer>();
            beacon.transform.SetParent(transform, false);
            beacon.sharedMaterial = lineMaterial != null ? lineMaterial : material;
            beacon.useWorldSpace = true;
            beacon.positionCount = 2;
            beacon.SetPosition(0, center + Vector3.up * 0.2f);
            beacon.SetPosition(1, center + Vector3.up * 9f);
            beacon.startWidth = 0.35f;
            beacon.endWidth = 0.05f;
            beacon.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beacon.enabled = false;
            BuildIdentityTicks(center);
            ApplyZoneColor();
            UiPresentationProfile.Changed += ApplyZoneColor;
        }

        void BuildIdentityTicks(Vector3 center)
        {
            if (ring == null || ring.positionCount == 0) return;
            float radius = Vector3.Distance(ring.GetPosition(0), center);
            if (radius <= 0f) return;

            // Bible §4.3 uses distinct zone hues; these one/two/three notches make
            // the zone identity readable without relying on hue alone.
            int count = IdentityTickCount(zoneId);
            identityTicks = new LineRenderer[count];
            for (int i = 0; i < count; i++)
            {
                var lineObject = new GameObject("Zone identity tick " + (i + 1));
                lineObject.transform.SetParent(transform, false);
                var tick = lineObject.AddComponent<LineRenderer>();
                tick.sharedMaterial = lineMaterial != null ? lineMaterial : ring.sharedMaterial;
                tick.useWorldSpace = true;
                tick.positionCount = 2;
                tick.startWidth = tick.endWidth = 0.18f;
                tick.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tick.receiveShadows = false;

                float angle = (90f + (i - (count - 1) * 0.5f) * 14f) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                tick.SetPosition(0, center + radial * (radius * 0.78f) + Vector3.up * 0.08f);
                tick.SetPosition(1, center + radial * (radius * 0.92f) + Vector3.up * 0.08f);
                identityTicks[i] = tick;
            }
        }

        public static int IdentityTickCount(ZoneId id) => id switch
        {
            ZoneId.West => 1,
            ZoneId.Center => 2,
            ZoneId.East => 3,
            _ => 1,
        };

        void ApplyZoneColor()
        {
            Color color = UiPresentationProfile.Palette.WorldZone(zoneId);
            if (ring != null) ring.startColor = ring.endColor = color;
            if (beacon != null) beacon.startColor = beacon.endColor = color;
            if (identityTicks != null)
                foreach (var tick in identityTicks)
                    if (tick != null) tick.startColor = tick.endColor = color;
        }

        void OnDestroy()
        {
            UiPresentationProfile.Changed -= ApplyZoneColor;
            if (lineMaterial != null)
            {
                if (Application.isPlaying) Destroy(lineMaterial);
                else DestroyImmediate(lineMaterial);
            }
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


