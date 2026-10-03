using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Pads
{
    /// <summary>
    /// Hold-to-preview launch arc (spec 6): drawn from the would-be pad, green when the
    /// placement is valid and red when it is not. Shared by offline play and the online
    /// presenter so both show the same preview (QA-006).
    /// </summary>
    public class PadPreviewArc
    {
        LineRenderer line;
        Material material;
        readonly Vector3[] points = new Vector3[32];

        public void Hide()
        {
            if (line != null && line.enabled) line.enabled = false;
        }

        public void Show(Vector3 position, Vector3 forward, bool valid, PadConfig pad, float gravity)
        {
            if (line == null)
            {
                line = new GameObject("Pad preview").AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.startWidth = 0.12f; line.endWidth = 0.05f;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                material = new Material(Resources.Load<Material>("Materials/Objective"));
                line.sharedMaterial = material;
            }
            Vector3 velocity = forward * pad.launchHorizontalVelocity + Vector3.up * pad.launchUpwardVelocity;
            Vector3 point = position + Vector3.up * 0.1f;
            int count = 0;
            for (; count < points.Length; count++)
            {
                points[count] = point;
                velocity += Vector3.down * gravity * 0.06f;
                point += velocity * 0.06f;
                if (point.y < position.y - 0.5f) { count++; break; }
            }
            line.positionCount = count;
            line.SetPositions(points);
            material.color = valid ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.39f, 0.46f);
            line.enabled = true;
        }

        public void Destroy()
        {
            if (line != null) Object.Destroy(line.gameObject);
            if (material != null) Object.Destroy(material);
            line = null; material = null;
        }
    }
}


