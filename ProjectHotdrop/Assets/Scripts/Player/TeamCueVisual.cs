using UnityEngine;
using Hotdrop.UI;

namespace Hotdrop.Player
{
    /// <summary>
    /// QA-023: non-colour team cue shared by offline (PlayerController) and online
    /// (NetAvatarVisual) avatars. Colour is never the only signal: relative to the
    /// local viewer, allies wear a chevron above the head plus a horizontal chest
    /// band, enemies a diamond outline above the head plus upward shoulder spikes,
    /// so they read in
    /// grayscale and for colour-blind players. Presentation only: every primitive's
    /// collider is removed so the cue can never affect hit detection.
    /// </summary>
    public static class TeamCueVisual
    {
        public const string RootName = "TeamCue";
        public const string AllyMarkerName = "AllyMarker_Chevron";
        public const string AllyPatternName = "AllyPattern_ChestBand";
        public const string EnemyMarkerName = "EnemyMarker_Diamond";
        public const string EnemyPatternName = "EnemyPattern_ShoulderSpikes";

        const float MarkerHeight = 2.15f; // just above the ~1.8 m head top of both avatar builds

        /// <summary>
        /// (Re)builds the cue under <paramref name="parent"/>. Safe to call again when the
        /// team or viewer changes: the previous cue is replaced. Returns the cue root.
        /// </summary>
        public static Transform Apply(Transform parent, bool friendly, Material teamMaterial = null)
        {
            if (parent == null) return null;
            var profileTint = parent.GetComponent<TeamProfileTint>();
            if (profileTint == null) profileTint = parent.gameObject.AddComponent<TeamProfileTint>();
            profileTint.SetFriendly(friendly);
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
                // Two angled bars form a chevron; team identity remains readable without hue.
                var marker = new GameObject(AllyMarkerName).transform;
                marker.SetParent(root, false);
                marker.localPosition = new Vector3(0f, MarkerHeight, 0f);
                Part(marker, PrimitiveType.Cube, "ChevronL", new Vector3(-0.065f, 0f, 0f), Quaternion.Euler(0f, 0f, -35f), new Vector3(0.055f, 0.23f, 0.055f), teamMaterial);
                Part(marker, PrimitiveType.Cube, "ChevronR", new Vector3(0.065f, 0f, 0f), Quaternion.Euler(0f, 0f, 35f), new Vector3(0.055f, 0.23f, 0.055f), teamMaterial);

                // Body pattern: one horizontal chest band wrapping the torso.
                var pattern = new GameObject(AllyPatternName).transform;
                pattern.SetParent(root, false);
                Part(pattern, PrimitiveType.Cube, "Band", new Vector3(0f, 1.22f, 0f), Quaternion.identity, new Vector3(0.43f, 0.065f, 0.29f), neutral);
            }
            else
            {
                // Four bars form an outlined diamond to keep enemy identity separate from ally chevrons.
                var marker = new GameObject(EnemyMarkerName).transform;
                marker.SetParent(root, false);
                marker.localPosition = new Vector3(0f, MarkerHeight, 0f);
                var edge = new Vector3(0.045f, 0.19f, 0.045f);
                Part(marker, PrimitiveType.Cube, "DiamondTopRight", new Vector3(0.065f, 0.065f, 0f), Quaternion.Euler(0f, 0f, -35f), edge, teamMaterial);
                Part(marker, PrimitiveType.Cube, "DiamondBottomRight", new Vector3(0.065f, -0.065f, 0f), Quaternion.Euler(0f, 0f, 35f), edge, teamMaterial);
                Part(marker, PrimitiveType.Cube, "DiamondBottomLeft", new Vector3(-0.065f, -0.065f, 0f), Quaternion.Euler(0f, 0f, -35f), edge, teamMaterial);
                Part(marker, PrimitiveType.Cube, "DiamondTopLeft", new Vector3(-0.065f, 0.065f, 0f), Quaternion.Euler(0f, 0f, 35f), edge, teamMaterial);

                // Body pattern: angled spikes rising from both shoulders.
                var pattern = new GameObject(EnemyPatternName).transform;
                pattern.SetParent(root, false);
                var spike = new Vector3(0.07f, 0.28f, 0.07f);
                Part(pattern, PrimitiveType.Cube, "SpikeL", new Vector3(-0.26f, 1.52f, 0f), Quaternion.Euler(0f, 0f, 30f), spike, neutral);
                Part(pattern, PrimitiveType.Cube, "SpikeR", new Vector3(0.26f, 1.52f, 0f), Quaternion.Euler(0f, 0f, -30f), spike, neutral);
            }
            // SetFriendly runs before the cue renderers exist; refresh now so newly
            // created team-coded markers inherit the selected A/B/C profile tint.
            profileTint.SetFriendly(friendly);
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

    /// <summary>Recolors team-coded renderers when the player changes presentation profile.</summary>
    public sealed class TeamProfileTint : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        MaterialPropertyBlock block;
        bool friendly;

        public void SetFriendly(bool isFriendly)
        {
            friendly = isFriendly;
            Apply();
        }

        void OnEnable() => UiPresentationProfile.Changed += Apply;
        void OnDisable() => UiPresentationProfile.Changed -= Apply;

        void Apply()
        {
            if (this == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            Color tint = friendly ? UiPresentationProfile.Palette.Ally : UiPresentationProfile.Palette.Enemy;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                string materialName = material.name.ToLowerInvariant();
                if (!materialName.Contains("friendly") && !materialName.Contains("enemy")) continue;
                renderer.GetPropertyBlock(block);
                if (material.HasProperty(BaseColorId)) block.SetColor(BaseColorId, tint);
                if (material.HasProperty(LegacyColorId)) block.SetColor(LegacyColorId, tint);
                renderer.SetPropertyBlock(block);
                block.Clear();
            }
        }
    }
}

