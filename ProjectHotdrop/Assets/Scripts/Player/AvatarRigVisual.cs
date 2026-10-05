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
        static Mesh roundedBox;
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
            // Older serialized prefabs have loose body parts instead of a Rig parent.
            // Remove those too, otherwise both silhouettes render on top of each other.
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i);
                if (child.name == "Rig" || child.name == "TeamCue" || child.name == "Cosmetic") continue;
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                if (Application.isPlaying) Object.Destroy(child.gameObject); else Object.DestroyImmediate(child.gameObject);
            }
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
            // A small utility-sport vest and straps break up the prototype's solid block of
            // team colour. The exposed torso sides remain readable as friend/foe cues.
            Part(rootT, "Vest front", PrimitiveType.Cube, new Vector3(0f, 1.285f, 0.125f),
                new Vector3(chestW * 0.76f, 0.31f, 0.035f), DarkMaterial);
            Part(rootT, "Vest stripe", PrimitiveType.Cube, new Vector3(0f, 1.38f, 0.146f),
                new Vector3(chestW * 0.68f, 0.025f, 0.015f), accent);
            Part(rootT, "Waist belt", PrimitiveType.Cube, new Vector3(0f, 1.075f, 0f),
                new Vector3(hipW + 0.04f, 0.055f, 0.24f), DarkMaterial);

            parts.Head = Part(rootT, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.67f, 0f),
                new Vector3(0.25f, 0.25f, 0.25f), primary);
            // Face: one calm visor/eye band, readable at 10 m on a phone without a photoreal face.
            // Parented to the rig (not the scaled head sphere) so the sizes stay absolute.
            Part(rootT, "Face", PrimitiveType.Cube, new Vector3(0f, 1.675f, 0.105f),
                new Vector3(0.215f, 0.087f, 0.055f), DarkMaterial);
            Part(rootT, "Visor reflection", PrimitiveType.Cube, new Vector3(0f, 1.695f, 0.135f),
                new Vector3(0.165f, 0.012f, 0.008f), primary);
            Part(rootT, "Back armor", PrimitiveType.Cube, new Vector3(0f, 1.30f, -0.125f),
                new Vector3(chestW * .78f, .32f, .075f), DarkMaterial);
            Part(rootT, "Back team stripe", PrimitiveType.Cube, new Vector3(0f, 1.32f, -0.168f),
                new Vector3(chestW * .60f, .055f, .012f), primary);
            if (female)
                Part(rootT, "Helmet rear", PrimitiveType.Sphere, new Vector3(0f, 1.70f, -0.065f),
                    new Vector3(0.22f, 0.18f, 0.15f), DarkMaterial);
            else
                Part(rootT, "Cap", PrimitiveType.Cube, new Vector3(0f, 1.755f, -0.005f),
                    new Vector3(0.235f, 0.055f, 0.235f), DarkMaterial);

            // Shoulder pivots drive arm swing; the visible limb hangs 0.2 m below each pivot.
            parts.LeftArm = Limb(rootT, "ArmL", new Vector3(-(shoulderW * 0.5f + 0.05f), 1.46f, 0f),
                new Vector3(0.09f, 0.46f, 0.11f), accent);
            parts.RightArm = Limb(rootT, "ArmR", new Vector3(shoulderW * 0.5f + 0.05f, 1.46f, 0f),
                new Vector3(0.09f, 0.46f, 0.11f), accent);
            Part(parts.LeftArm, "Shoulder guard", PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0f),
                new Vector3(0.16f, 0.13f, 0.16f), DarkMaterial);
            Part(parts.RightArm, "Shoulder guard", PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0f),
                new Vector3(0.16f, 0.13f, 0.16f), DarkMaterial);
            Part(parts.LeftArm, "Glove", PrimitiveType.Cube, new Vector3(0f, -0.40f, 0f),
                new Vector3(0.105f, 0.105f, 0.13f), DarkMaterial);
            Part(parts.RightArm, "Glove", PrimitiveType.Cube, new Vector3(0f, -0.40f, 0f),
                new Vector3(0.105f, 0.105f, 0.13f), DarkMaterial);

            // Hip pivots drive the leg cycle; the visible leg reaches the ground.
            parts.LeftLeg = Limb(rootT, "LegL", new Vector3(-(hipW * 0.5f - 0.02f), 0.96f, 0f),
                new Vector3(0.12f, 0.94f, 0.15f), accent);
            parts.RightLeg = Limb(rootT, "LegR", new Vector3(hipW * 0.5f - 0.02f, 0.96f, 0f),
                new Vector3(0.12f, 0.94f, 0.15f), accent);
            foreach (var leg in new[] { parts.LeftLeg, parts.RightLeg })
            {
                Part(leg, "Knee guard", PrimitiveType.Cube, new Vector3(0f, -0.46f, 0.086f),
                    new Vector3(0.145f, 0.15f, 0.035f), DarkMaterial);
                Part(leg, "Runner shoe", PrimitiveType.Cube, new Vector3(0f, -0.87f, 0.065f),
                    new Vector3(0.16f, 0.12f, 0.26f), DarkMaterial);
            }

            if (includeGun)
            {
                // Offline players carry separate weapon meshes; this keeps the appearance
                // preview and the online avatar visually armed for weapon-wrap previews.
                var gun = Part(rootT, "Gun", PrimitiveType.Cube, new Vector3(0.26f, 1.18f, 0.34f),
                    new Vector3(0.10f, 0.12f, 0.55f), accent);
                gun.localRotation = Quaternion.Euler(0f, 8f, 0f);
                Part(rootT, "Sight", PrimitiveType.Cube, new Vector3(0.26f, 1.25f, 0.36f),
                    new Vector3(0.055f, 0.045f, 0.11f), DarkMaterial);
                Part(rootT, "Barrel", PrimitiveType.Cylinder, new Vector3(0.26f, 1.175f, 0.65f),
                    new Vector3(0.05f, 0.09f, 0.05f), DarkMaterial).localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            return parts;
        }

        /// <summary>A joint pivot with one visible limb hanging below it.</summary>
        static Transform Limb(Transform parent, string name, Vector3 pivot, Vector3 size, Material material)
        {
            var pivotT = new GameObject(name).transform;
            pivotT.SetParent(parent, false);
            pivotT.localPosition = pivot;
            Part(pivotT, "Limb", PrimitiveType.Capsule, new Vector3(0f, -size.y * 0.45f, 0f),
                new Vector3(size.x * 1.12f, size.y * .5f, size.z * 1.12f), material);
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
            if (type == PrimitiveType.Cube) go.GetComponent<MeshFilter>().sharedMesh = RoundedBox();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null) renderer.sharedMaterial = material;
            return go.transform;
        }

        // Shared original mesh: bevels catch the key light without changing any collider.
        // Six 5x5 grids keep the silhouette inexpensive enough for all six runners.
        static Mesh RoundedBox()
        {
            if (roundedBox != null) return roundedBox;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            Vector3[] faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            float[] grid = { -.5f, -.38f, 0f, .38f, .5f };
            foreach (var normal in faces)
            {
                var u = Vector3.Cross(normal, Mathf.Abs(normal.y) > .5f ? Vector3.forward : Vector3.up);
                var v = Vector3.Cross(normal, u);
                int start = vertices.Count;
                for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
                {
                    var p = normal * .5f + u * grid[x] + v * grid[y];
                    var core = new Vector3(Mathf.Clamp(p.x, -.38f, .38f), Mathf.Clamp(p.y, -.38f, .38f), Mathf.Clamp(p.z, -.38f, .38f));
                    var n = (p - core).normalized;
                    vertices.Add(core + n * .12f); normals.Add(n);
                }
                for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
                {
                    int a = start + y * 5 + x;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(a + 6);
                    triangles.Add(a); triangles.Add(a + 6); triangles.Add(a + 5);
                }
            }
            roundedBox = new Mesh { name = "HOTDROP original beveled armor" };
            roundedBox.SetVertices(vertices); roundedBox.SetNormals(normals); roundedBox.SetTriangles(triangles, 0);
            roundedBox.RecalculateBounds();
            return roundedBox;
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

