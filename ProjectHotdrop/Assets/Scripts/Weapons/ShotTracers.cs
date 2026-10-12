using UnityEngine;

namespace Hotdrop.Weapons
{
    /// <summary>
    /// Pooled hitscan tracers (spec section 15: small reusable VFX; section 23:
    /// pooled effects, no per-frame allocation). Presentation only: damage is
    /// resolved by the shot trace, never by these lines.
    /// </summary>
    public class ShotTracers : MonoBehaviour
    {
        const int PoolSize = 48;
        const float Lifetime = 0.06f;

        static ShotTracers instance;
        LineRenderer[] lines;
        float[] expiresAt;
        Material friendly, enemy, beam;
        int next;

        static ShotTracers Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("ShotTracers").AddComponent<ShotTracers>();
                return instance;
            }
        }

        void Awake()
        {
            friendly = Resources.Load<Material>("Materials/Friendly");
            enemy = Resources.Load<Material>("Materials/Enemy");
            // Additive beam (07_WORLD_ART): team colour as vertex colour, HDR on rich tiers so
            // bloom turns a shot into a streak of light. Falls back to the team materials.
            beam = Resources.Load<Material>("Materials/VfxBeam");
            lines = new LineRenderer[PoolSize];
            expiresAt = new float[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var line = new GameObject("Tracer").AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false);
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.035f;
                line.endWidth = 0.015f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                lines[i] = line;
            }
        }

        public static void Show(Vector3 from, Vector3 to, bool friendlyToViewer)
        {
            var self = Instance;
            var line = self.lines[self.next];
            self.expiresAt[self.next] = Time.time + Lifetime;
            self.next = (self.next + 1) % PoolSize;
            if (self.beam != null)
            {
                var team = friendlyToViewer ? Hotdrop.UI.UIDesignTokens.Ally : Hotdrop.UI.UIDesignTokens.HazardCoral;
                float glow = Hotdrop.Map.SwitchyardAtmosphere.Quality > 0 ? 3f : 1f;
                line.sharedMaterial = self.beam;
                line.startColor = new Color(team.r * glow, team.g * glow, team.b * glow, 1f);
                line.endColor = new Color(team.r * glow, team.g * glow, team.b * glow, .35f);
                line.startWidth = .06f; line.endWidth = .03f;
            }
            else line.sharedMaterial = friendlyToViewer ? self.friendly : self.enemy;
            // Start slightly ahead of the muzzle so the line never covers the shooter's own hands.
            line.SetPosition(0, Vector3.MoveTowards(from, to, 0.6f));
            line.SetPosition(1, to);
            line.enabled = true;
        }

        void Update()
        {
            float now = Time.time;
            for (int i = 0; i < PoolSize; i++)
                if (lines[i].enabled && now >= expiresAt[i]) lines[i].enabled = false;
        }
    }
}

