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
            ApplyZoneColor();
            UiPresentationProfile.Changed += ApplyZoneColor;
        }

        void ApplyZoneColor()
        {
            Color color = UiPresentationProfile.Palette.WorldZone(zoneId);
            if (ring != null) ring.startColor = ring.endColor = color;
            if (beacon != null) beacon.startColor = beacon.endColor = color;
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


