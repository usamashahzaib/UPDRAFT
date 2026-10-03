using UnityEngine;

namespace Hotdrop.Player
{
    /// <summary>Transforms the procedural avatar motion drives. Nulls are tolerated.</summary>
    public struct AvatarVisualParts
    {
        public Transform Root;
        public Transform Torso;
        public Transform Head;
        public Transform LeftArm;
        public Transform RightArm;
        public Transform LeftLeg;
        public Transform RightLeg;
    }

    /// <summary>
    /// Amendment 2 (grounded visual identity): one shared, original humanoid built from
    /// project primitives and project materials - no third-party meshes, no licence burden.
    /// Both body rigs are the same height (1.8 m, matching the shared capsule) and the same
    /// joint layout; only the silhouette proportions, hair mass and face framing differ, so
    /// Male and Female stay scene-compatible and animation-retargetable (spec 15, v2.1).
    /// Parts carry no colliders: hit detection stays on the authoritative capsule/hitboxes.
    /// </summary>
    public static class AvatarRigVisual
    {
        /// <summary>Feet-to-head-top height; mirrors the shared capsule height (BodyRigGameplayProfile).</summary>
        public const float Height = 1.8f;

        static Material skin, dark;
        static bool materialsResolved;

        public static Material SkinMaterial
        {
            get { ResolveMaterials(); return skin; }
        }

        public static Material DarkMaterial
        {
            get { ResolveMaterials(); return dark; }
        }

        static void ResolveMaterials()
        {
            if (materialsResolved) return;
            materialsResolved = true;
            skin = Original("Materials/AvatarSkin", new Color(0.78f, 0.60f, 0.46f), 0.25f);
            dark = Original("Materials/AvatarDark", new Color(0.14f, 0.16f, 0.19f), 0.3f);
        }

        /// <summary>A project-original shared material; one instance keeps SRP batching intact.</summary>
        static Material Original(string resourcePath, Color colour, float smoothness)
        {
            var asset = Resources.Load<Material>(resourcePath);
            if (asset != null) return asset;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = resourcePath };
            material.color = colour;
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        /// <summary>
        /// Rebuilds the humanoid under <paramref name="root"/> (name it "Body" offline or
        /// "Visual" online), replacing any previous "Rig" child. Used to apply the stored
        /// body rig at spawn without touching the authoritative capsule or hitboxes.
        /// </summary>
        public static AvatarVisualParts Rebuild(GameObject root, BodyRig rig, Material primary, Material accent, bool includeGun)
        {
            var existing = root.transform.Find("Rig");
            if (existing != null)
            {
                existing.SetParent(null, false);
                if (Application.isPlaying) Object.Destroy(existing.gameObject); else Object.DestroyImmediate(existing.gameObject);
            }
            return Build(root, rig, primary, accent, includeGun);
        }

        /// <summary>Wires a motion component to the built parts (any part may be absent).</summary>
        public static void Wire(SportAvatarAnimation motion, AvatarVisualParts parts)
        {
            if (motion == null) return;
            motion.leftLeg = parts.LeftLeg;
            motion.rightLeg = parts.RightLeg;
            motion.leftArm = parts.LeftArm;
            motion.rightArm = parts.RightArm;
            motion.torso = parts.Torso;
        }

        /// <summary>Builds the humanoid in a child "Rig" node; returns the driven parts.</summary>
        public static AvatarVisualParts Build(GameObject root, BodyRig rig, Material primary, Material accent, bool includeGun)
        {
            bool female = rig == BodyRig.Female;
            var rigRoot = new GameObject("Rig").transform;
            rigRoot.SetParent(root.transform, false);
            var rootT = rigRoot;
            var parts = new AvatarVisualParts { Root = rootT };

            float shoulderW = female ? 0.33f : 0.39f;
            float chestW = female ? 0.34f : 0.40f;
            float hipW = female ? 0.30f : 0.27f;

            // Pelvis (accent) then torso (primary). The torso is the first renderer so the
            // existing bodyRenderer/team-colour path keeps working unchanged.
            parts.Torso = Part(rootT, "Torso", PrimitiveType.Cube, new Vector3(0f, 1.30f, 0f),
                new Vector3(chestW, 0.44f, 0.23f), primary);
            Part(rootT, "Pelvis", PrimitiveType.Cube, new Vector3(0f, 1.00f, 0f),
                new Vector3(hipW, 0.20f, 0.21f), accent);

            parts.Head = Part(rootT, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.67f, 0f),
                new Vector3(0.23f, 0.24f, 0.23f), SkinMaterial);
            // Face: one calm visor/eye band, readable at 10 m on a phone without a photoreal face.
            // Parented to the rig (not the scaled head sphere) so the sizes stay absolute.
            Part(rootT, "Face", PrimitiveType.Cube, new Vector3(0f, 1.675f, 0.113f),
                new Vector3(0.145f, 0.038f, 0.02f), DarkMaterial);
            if (female)
                Part(rootT, "Hair", PrimitiveType.Cube, new Vector3(0f, 1.70f, -0.09f),
                    new Vector3(0.21f, 0.14f, 0.10f), DarkMaterial);
            else
                Part(rootT, "Cap", PrimitiveType.Cube, new Vector3(0f, 1.755f, -0.005f),
                    new Vector3(0.235f, 0.055f, 0.235f), DarkMaterial);

            // Shoulder pivots drive arm swing; the visible limb hangs 0.2 m below each pivot.
            parts.LeftArm = Limb(rootT, "ArmL", new Vector3(-(shoulderW * 0.5f + 0.05f), 1.46f, 0f),
                new Vector3(0.09f, 0.46f, 0.11f), accent);
            parts.RightArm = Limb(rootT, "ArmR", new Vector3(shoulderW * 0.5f + 0.05f, 1.46f, 0f),
                new Vector3(0.09f, 0.46f, 0.11f), accent);

            // Hip pivots drive the leg cycle; the visible leg reaches the ground.
            parts.LeftLeg = Limb(rootT, "LegL", new Vector3(-(hipW * 0.5f - 0.02f), 0.96f, 0f),
                new Vector3(0.12f, 0.94f, 0.15f), accent);
            parts.RightLeg = Limb(rootT, "LegR", new Vector3(hipW * 0.5f - 0.02f, 0.96f, 0f),
                new Vector3(0.12f, 0.94f, 0.15f), accent);

            if (includeGun)
            {
                // Offline players carry separate weapon meshes; this keeps the appearance
                // preview and the online avatar visually armed for weapon-wrap previews.
                var gun = Part(rootT, "Gun", PrimitiveType.Cube, new Vector3(0.26f, 1.18f, 0.34f),
                    new Vector3(0.10f, 0.12f, 0.55f), accent);
                gun.localRotation = Quaternion.Euler(0f, 8f, 0f);
            }

            return parts;
        }

        /// <summary>A joint pivot with one visible limb hanging below it.</summary>
        static Transform Limb(Transform parent, string name, Vector3 pivot, Vector3 size, Material material)
        {
            var pivotT = new GameObject(name).transform;
            pivotT.SetParent(parent, false);
            pivotT.localPosition = pivot;
            Part(pivotT, "Limb", PrimitiveType.Cube, new Vector3(0f, -size.y * 0.45f, 0f), size, material);
            return pivotT;
        }

        /// <summary>Presentation-only primitive: every collider is disabled and removed.</summary>
        public static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) renderer.sharedMaterial = material;
            return go.transform;
        }
    }

    /// <summary>
    /// Amendment 1/2: the local player's body-rig preference, stored like every other local
    /// setting. The first launch offers the choice; Appearance and the More menu can change it
    /// later. The rig is appearance only - gameplay reads BodyRigs.SharedGameplay, never this.
    /// </summary>
    public static class BodyRigStore
    {
        public const string PrefKey = "BodyRig";
        public const string ChosenKey = "BodyRigChosen";

        public static BodyRig Current => BodyRigs.FromStoredId(PlayerPrefs.GetString(PrefKey, ""));

        public static bool Chosen => PlayerPrefs.GetInt(ChosenKey, 0) != 0;

        public static void Set(BodyRig rig, bool markChosen = true)
        {
            PlayerPrefs.SetString(PrefKey, BodyRigs.Id(rig));
            if (markChosen) PlayerPrefs.SetInt(ChosenKey, 1);
            PlayerPrefs.Save();
        }
    }
}

