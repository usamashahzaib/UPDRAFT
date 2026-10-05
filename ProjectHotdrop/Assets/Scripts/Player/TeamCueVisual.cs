using UnityEngine;

namespace Hotdrop.Player
{
    /// <summary>
    /// QA-023: non-colour team cue shared by offline (PlayerController) and online
    /// (NetAvatarVisual) avatars. Colour is never the only signal: relative to the
    /// local viewer, allies wear a round orb above the head plus a horizontal chest
    /// band, enemies a spiky "X" cross above the head plus upward shoulder spikes
    /// (round vs spiky, echoing the pad ring / cross convention), so they read in
    /// grayscale and for colour-blind players. Presentation only: every primitive's
    /// collider is removed so the cue can never affect hit detection.
    /// </summary>
    public static class TeamCueVisual
    {
        public const string RootName = "TeamCue";
        public const string AllyMarkerName = "AllyMarker_Orb";
        public const string AllyPatternName = "AllyPattern_ChestBand";
        public const string EnemyMarkerName = "EnemyMarker_Cross";
        public const string EnemyPatternName = "EnemyPattern_ShoulderSpikes";

        const float MarkerHeight = 2.15f; // just above the ~1.8 m head top of both avatar builds

        /// <summary>
        /// (Re)builds the cue under <paramref name="parent"/>. Safe to call again when the
        /// team or viewer changes: the previous cue is replaced. Returns the cue root.
        /// </summary>
        public static Transform Apply(Transform parent, bool friendly, Material teamMaterial = null)
        {
            if (parent == null) return null;
            var existing = parent.Find(RootName);
            if (existing != null)
            {
                // Detach first so a deferred Destroy never leaves a same-named sibling behind.
                existing.SetParent(null, false);
                DestroySafe(existing.gameObject);
            }

            if (teamMaterial == null) teamMaterial = Resources.Load<Material>(friendly ? "Materials/Friendly" : "Materials/Enemy");
            var neutral = Resources.Load<Material>("Materials/Structure");

            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);

            if (friendly)
            {
                // Head marker: a round orb - smooth silhouette from every yaw and pitch.
                var marker = new GameObject(AllyMarkerName).transform;
                marker.SetParent(root, false);
                marker.localPosition = new Vector3(0f, MarkerHeight, 0f);
                Part(marker, PrimitiveType.Sphere, "Orb", Vector3.zero, Quaternion.identity, new Vector3(0.22f, 0.22f, 0.22f), teamMaterial);

                // Body pattern: one horizontal chest band wrapping the torso.
                var pattern = new GameObject(AllyPatternName).transform;
                pattern.SetParent(root, false);
                Part(pattern, PrimitiveType.Cube, "Band", new Vector3(0f, 1.22f, 0f), Quaternion.identity, new Vector3(0.43f, 0.065f, 0.29f), neutral);
            }
            else
            {
                // Head marker: 3D "X" (two crossed bar pairs, 90 degrees apart) - reads from every yaw.
                var marker = new GameObject(EnemyMarkerName).transform;
                marker.SetParent(root, false);
                marker.localPosition = new Vector3(0f, MarkerHeight, 0f);
                var bar = new Vector3(0.05f, 0.34f, 0.05f);
                Part(marker, PrimitiveType.Cube, "CrossA", Vector3.zero, Quaternion.Euler(0f, 0f, 45f), bar, teamMaterial);
                Part(marker, PrimitiveType.Cube, "CrossB", Vector3.zero, Quaternion.Euler(0f, 0f, -45f), bar, teamMaterial);
                Part(marker, PrimitiveType.Cube, "CrossC", Vector3.zero, Quaternion.Euler(45f, 0f, 0f), bar, teamMaterial);
                Part(marker, PrimitiveType.Cube, "CrossD", Vector3.zero, Quaternion.Euler(-45f, 0f, 0f), bar, teamMaterial);

                // Body pattern: angled spikes rising from both shoulders.
                var pattern = new GameObject(EnemyPatternName).transform;
                pattern.SetParent(root, false);
                var spike = new Vector3(0.07f, 0.28f, 0.07f);
                Part(pattern, PrimitiveType.Cube, "SpikeL", new Vector3(-0.26f, 1.52f, 0f), Quaternion.Euler(0f, 0f, 30f), spike, neutral);
                Part(pattern, PrimitiveType.Cube, "SpikeR", new Vector3(0.26f, 1.52f, 0f), Quaternion.Euler(0f, 0f, -30f), spike, neutral);
            }
            return root;
        }

        static void Part(Transform parent, PrimitiveType type, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false; // presentation only, never a hit surface (even before a deferred Destroy)
            DestroySafe(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) renderer.sharedMaterial = material;
        }

        static void DestroySafe(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }
    }
}

