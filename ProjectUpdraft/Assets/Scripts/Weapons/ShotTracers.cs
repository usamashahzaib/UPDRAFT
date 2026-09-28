using UnityEngine;

namespace Updraft.Weapons
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
        Material friendly, enemy;
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
            line.sharedMaterial = friendlyToViewer ? self.friendly : self.enemy;
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
