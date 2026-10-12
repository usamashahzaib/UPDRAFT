using UnityEngine;
using System.Collections.Generic;

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
        static Shader characterRimShader;
        static Material suitRim, darkRim, damageScuff;
        static readonly Dictionary<Material, Material> rimMaterials = new Dictionary<Material, Material>();
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
            if (asset != null) return WithRim(asset);
            var shader = CharacterRimShader();
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = resourcePath };
            material.color = colour;
            material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_RimColor")) material.SetColor("_RimColor", new Color(.68f, .79f, .87f, 1f));
            return material;
        }

        static Shader CharacterRimShader()
        {
            if (characterRimShader == null)
            {
                characterRimShader = Resources.Load<Shader>("Shaders/CharacterRimLit");
                if (characterRimShader == null) characterRimShader = Shader.Find("Hotdrop/Character Rim Lit");
            }
            return characterRimShader;
        }

        /// <summary>Returns a shared rim-lit variant while preserving the source tint and maps.</summary>
        public static Material WithRim(Material source)
        {
            if (source == null) return null;
            var shader = CharacterRimShader();
            if (shader == null) return source;
            if (rimMaterials.TryGetValue(source, out var cached) && cached != null) return cached;
            cached = new Material(source) { shader = shader, name = source.name + " Rim" };
            if (source.HasProperty("_BaseColor")) cached.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            else if (source.HasProperty("_Color")) cached.SetColor("_BaseColor", source.GetColor("_Color"));
            if (source.HasProperty("_BaseMap")) cached.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            if (source.HasProperty("_Smoothness")) cached.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
            cached.SetColor("_RimColor", new Color(.68f, .79f, .87f, 1f));
            rimMaterials[source] = cached;
            return cached;
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
            var parts = Build(root, rig, primary, accent, includeGun);
            var damageVisual = root.GetComponent<AvatarDamageTelegraph>();
            if (damageVisual != null) damageVisual.RefreshAfterRigRebuild();
            return parts;
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

        static GameObject operatorPrefab;
        static bool operatorResolved;
        static Material suit;

        /// <summary>
        /// The CC0 skinned operator (07_WORLD_ART: Quaternius Universal Animation Library, Humanoid,
        /// fitted to the 1.8 m capsule). Null falls back to the original primitive humanoid.
        /// </summary>
        public static GameObject OperatorPrefab
        {
            get
            {
                if (!operatorResolved) { operatorResolved = true; operatorPrefab = Resources.Load<GameObject>("Characters/Operator"); }
                return operatorPrefab;
            }
        }

        public static bool UsesOperator => OperatorPrefab != null;

        /// <summary>Matte tactical suit under the team armour; shared so SRP batching holds.</summary>
        public static Material SuitMaterial
        {
            get
            {
                if (suit == null) suit = Original("Materials/AvatarSuit", new Color(0.27f, 0.30f, 0.33f), 0.22f);
                return suit;
            }
        }

        /// <summary>Shared base layers that carry the silhouette rim on both avatar rigs.</summary>
        static Material RimBaseMaterial(bool useDark)
        {
            var source = useDark ? DarkMaterial : SuitMaterial;
            var cached = useDark ? darkRim : suitRim;
            if (cached != null) return cached;
            cached = WithRim(source);
            if (useDark) darkRim = cached; else suitRim = cached;
            return cached;
        }

        /// <summary>Builds the humanoid in a child "Rig" node; returns the driven parts.</summary>
        public static AvatarVisualParts Build(GameObject root, BodyRig rig, Material primary, Material accent, bool includeGun)
        {
            var prefab = OperatorPrefab;
            if (prefab != null) return BuildOperator(root, rig, primary, accent, includeGun, prefab);
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

        /// <summary>
        /// Skinned operator: animated athletic body in a matte suit, with team-coloured armour
        /// (chest plate, flight-pack stripe, helmet band) on its bones so friend/foe colour sits on
        /// dedicated pieces. Torso is the chest plate, which keeps the team-colour path unchanged.
        /// </summary>
        static AvatarVisualParts BuildOperator(GameObject root, BodyRig rig, Material primary, Material accent, bool includeGun, GameObject prefab)
        {
            bool female = rig == BodyRig.Female;
            var rigRoot = new GameObject("Rig").transform;
            rigRoot.SetParent(root.transform, false);
            var model = Object.Instantiate(prefab, rigRoot, false);
            model.name = "Operator";
            // Female shares the skeleton, clips and capsule; only the silhouette is slimmer.
            if (female) model.transform.localScale = Vector3.Scale(model.transform.localScale, new Vector3(.93f, 1f, .93f));
            var animator = model.GetComponent<Animator>();
            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int count = skinned.sharedMesh != null ? Mathf.Max(1, skinned.sharedMesh.subMeshCount) : 1;
                var materials = new Material[count];
                for (int i = 0; i < count; i++) materials[i] = i == 0 ? RimBaseMaterial(false) : RimBaseMaterial(true);
                skinned.sharedMaterials = materials;
                skinned.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            Transform Bone(HumanBodyBones bone) => animator != null && animator.isHuman ? animator.GetBoneTransform(bone) : null;
            var hips = Bone(HumanBodyBones.Hips) ?? model.transform;
            var chest = Bone(HumanBodyBones.UpperChest) ?? Bone(HumanBodyBones.Chest) ?? hips;
            var head = Bone(HumanBodyBones.Head) ?? chest;
            var parts = new AvatarVisualParts
            {
                Root = rigRoot,
                Head = head,
                LeftArm = Bone(HumanBodyBones.LeftUpperArm),
                RightArm = Bone(HumanBodyBones.RightUpperArm),
                LeftLeg = Bone(HumanBodyBones.LeftUpperLeg) ?? hips,
                RightLeg = Bone(HumanBodyBones.RightUpperLeg) ?? hips,
            };

            float chestY = chest.position.y - rigRoot.position.y;
            float headY = head.position.y - rigRoot.position.y;
            parts.Torso = Armour(rigRoot, chest, "Chest plate", PrimitiveType.Cube, new Vector3(0f, chestY - .02f, .115f), new Vector3(.30f, .24f, .07f), primary);
            Armour(rigRoot, chest, "Plate carrier", PrimitiveType.Cube, new Vector3(0f, chestY - .05f, .02f), new Vector3(.36f, .30f, .2f), DarkMaterial);
            // The flight pack is the HOTDROP silhouette: a dark shell, team stripe, twin thrusters.
            Armour(rigRoot, chest, "Flight pack", PrimitiveType.Cube, new Vector3(0f, chestY - .04f, -.17f), new Vector3(.30f, .40f, .15f), DarkMaterial);
            Armour(rigRoot, chest, "Flight pack stripe", PrimitiveType.Cube, new Vector3(0f, chestY + .06f, -.248f), new Vector3(.22f, .05f, .012f), primary);
            foreach (float side in new[] { -1f, 1f })
                Armour(rigRoot, chest, "Thruster", PrimitiveType.Cylinder, new Vector3(side * .1f, chestY - .27f, -.19f), new Vector3(.08f, .07f, .08f), accent);
            Armour(rigRoot, head, "Helmet", PrimitiveType.Sphere, new Vector3(0f, headY + .045f, -.01f), new Vector3(.25f, .23f, .27f), DarkMaterial);
            Armour(rigRoot, head, "Visor", PrimitiveType.Cube, new Vector3(0f, headY + .03f, .105f), new Vector3(.19f, .065f, .06f), accent);
            Armour(rigRoot, head, "Helmet team band", PrimitiveType.Cube, new Vector3(0f, headY + .14f, 0f), new Vector3(.05f, .03f, .24f), primary);
            if (female)
                Armour(rigRoot, head, "Helmet rear", PrimitiveType.Sphere, new Vector3(0f, headY - .04f, -.13f), new Vector3(.12f, .14f, .1f), DarkMaterial);
            foreach (var arm in new[] { parts.LeftArm, parts.RightArm })
                if (arm != null)
                    Armour(rigRoot, arm, "Shoulder guard", PrimitiveType.Sphere, rigRoot.InverseTransformPoint(arm.position) + new Vector3(0f, .03f, 0f), new Vector3(.15f, .12f, .16f), accent);

            // Field kit on the legs and hands: the silhouette reads as an equipped operator rather
            // than a bare mannequin. Legs are vertical at bind pose, so rig-space boxes line up.
            Vector3 At(Transform bone) => rigRoot.InverseTransformPoint(bone.position);
            foreach (var side in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftHand),
                                         (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightHand) })
            {
                var thigh = Bone(side.Item1); var shin = Bone(side.Item2); var foot = Bone(side.Item3); var handBone = Bone(side.Item4);
                if (thigh != null && shin != null)
                {
                    var mid = (At(thigh) + At(shin)) * .5f;
                    Armour(rigRoot, thigh, "Thigh rig", PrimitiveType.Cube, mid + new Vector3(0f, 0f, .075f), new Vector3(.13f, .2f, .05f), DarkMaterial);
                    Armour(rigRoot, shin, "Knee pad", PrimitiveType.Cube, At(shin) + new Vector3(0f, .02f, .07f), new Vector3(.11f, .11f, .05f), accent);
                }
                if (shin != null && foot != null)
                {
                    var mid = (At(shin) + At(foot)) * .5f;
                    Armour(rigRoot, shin, "Shin guard", PrimitiveType.Cube, mid + new Vector3(0f, 0f, .055f), new Vector3(.1f, .22f, .04f), DarkMaterial);
                    Armour(rigRoot, foot, "Boot", PrimitiveType.Cube, At(foot) + new Vector3(0f, -.035f, .05f), new Vector3(.12f, .11f, .26f), DarkMaterial);
                }
                if (handBone != null)
                    Armour(rigRoot, handBone, "Glove", PrimitiveType.Sphere, At(handBone), new Vector3(.09f, .09f, .09f), DarkMaterial);
            }

            if (includeGun)
            {
                // A direct Rig child (weapon wraps and tests find "Rig/Gun") that follows the hand.
                var gun = Part(rigRoot, "Gun", PrimitiveType.Cube, Vector3.zero, Vector3.one, accent);
                var follow = gun.gameObject.AddComponent<OperatorWeaponFollow>();
                follow.hand = Bone(HumanBodyBones.RightHand);
                follow.rig = rigRoot;
                // CC0 weapon models (07_WORLD_ART); the primitive rifle remains the fallback.
                if (!follow.SetWeapon("ARC"))
                {
                    gun.localScale = new Vector3(.07f, .11f, .42f);
                    Part(gun, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, .1f, .82f), new Vector3(.45f, .45f, .5f), DarkMaterial).localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Part(gun, "Magazine", PrimitiveType.Cube, new Vector3(0f, -.75f, .15f), new Vector3(.7f, .9f, .22f), DarkMaterial);
                    Part(gun, "Stock", PrimitiveType.Cube, new Vector3(0f, -.1f, -.62f), new Vector3(.8f, .8f, .35f), DarkMaterial);
                    Part(gun, "Sight", PrimitiveType.Cube, new Vector3(0f, .62f, .05f), new Vector3(.7f, .35f, .3f), DarkMaterial);
                }
            }
            return parts;
        }

        /// <summary>Rigid armour on a bone: placed in rig space at bind pose, then carried by the bone.</summary>
        static Transform Armour(Transform rigRoot, Transform bone, string name, PrimitiveType type, Vector3 rigPosition, Vector3 size, Material material)
        {
            var t = Part(bone, name, type, Vector3.zero, Vector3.one, material);
            t.position = rigRoot.TransformPoint(rigPosition);
            t.rotation = rigRoot.rotation;
            var lossy = bone.lossyScale;
            t.localScale = new Vector3(size.x / Mathf.Max(1e-4f, lossy.x), size.y / Mathf.Max(1e-4f, lossy.y), size.z / Mathf.Max(1e-4f, lossy.z));
            var renderer = t.GetComponent<Renderer>();
            if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return t;
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
            if (material != null) renderer.sharedMaterial = WithRim(material);
            return go.transform;
        }

        /// <summary>Persists a non-gore suit-damage cue for the current life only.</summary>
        public static void SetPersistentDamage(GameObject avatarRoot, bool damaged)
        {
            if (avatarRoot == null) return;
            var visual = avatarRoot.GetComponent<AvatarDamageTelegraph>();
            if (visual == null)
            {
                if (!damaged) return;
                visual = avatarRoot.AddComponent<AvatarDamageTelegraph>();
            }
            visual.SetDamaged(damaged);
        }

        internal static Material DamageScuffMaterial
        {
            get
            {
                if (damageScuff != null) return damageScuff;
                var shader = CharacterRimShader();
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) return null;
                damageScuff = new Material(shader) { name = "Avatar suit scuff" };
                damageScuff.color = new Color(.72f, .74f, .72f, 1f);
                damageScuff.SetFloat("_Smoothness", .2f);
                return damageScuff;
            }
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

    /// <summary>Collider-free marks retained on the avatar until respawn after taking damage.</summary>
    public sealed class AvatarDamageTelegraph : MonoBehaviour
    {
        Transform damageRoot;
        bool damaged;

        public void SetDamaged(bool damaged)
        {
            this.damaged = damaged;
            if (damageRoot == null)
            {
                if (!damaged) return; // Rebuild lazily after a rig replacement only when needed.
                Build();
            }
            if (damageRoot != null) damageRoot.gameObject.SetActive(damaged);
        }

        public void RefreshAfterRigRebuild()
        {
            damageRoot = null;
            if (damaged) SetDamaged(true);
        }

        void Build()
        {
            damageRoot = new GameObject("DamageTelegraph").transform;
            damageRoot.SetParent(transform, false);
            var material = AvatarRigVisual.DamageScuffMaterial;
            if (material == null) return;

            // Fractured visor strokes and suit scrapes are visual-only and do not alter hitboxes.
            Mark("Visor crack 1", new Vector3(-.025f, 1.69f, .143f), new Vector3(.009f, .075f, .006f), 24f, material);
            Mark("Visor crack 2", new Vector3(.005f, 1.675f, .144f), new Vector3(.008f, .048f, .006f), -32f, material);
            Mark("Visor crack 3", new Vector3(.025f, 1.657f, .143f), new Vector3(.007f, .038f, .006f), 35f, material);
            Mark("Chest scrape 1", new Vector3(.105f, 1.285f, .158f), new Vector3(.009f, .085f, .006f), -38f, material);
            Mark("Chest scrape 2", new Vector3(.09f, 1.255f, .159f), new Vector3(.007f, .048f, .006f), 30f, material);
            damageRoot.gameObject.SetActive(false);
        }

        void Mark(string name, Vector3 position, Vector3 size, float angle, Material material)
        {
            var mark = AvatarRigVisual.Part(damageRoot, name, PrimitiveType.Cube, position, size, material);
            mark.localRotation = Quaternion.Euler(0f, 0f, angle);
            var renderer = mark.GetComponent<Renderer>();
            if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>Holds the operator's weapon in its right hand, aimed along the runner's facing.</summary>
    public sealed class OperatorWeaponFollow : MonoBehaviour
    {
        public Transform hand, rig;
        string current;
        bool gripOrigin;

        /// <summary>
        /// Shows the CC0 model for a weapon id (Resources/Weapons/ARC|ZIP|BRICK).
        /// Its source materials change with the model; CosmeticLook retains an active body wrap.
        /// </summary>
        public bool SetWeapon(string id)
        {
            if (id == current) return true;
            var prefab = Resources.Load<GameObject>("Weapons/" + id);
            if (prefab == null) return false;
            var source = prefab.GetComponent<MeshFilter>();
            var filter = GetComponent<MeshFilter>();
            if (source == null || filter == null) return false;
            current = id;
            gripOrigin = true;
            transform.localScale = Vector3.one;
            filter.sharedMesh = source.sharedMesh;
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                Hotdrop.UI.CosmeticLook.SetWeaponBaseMaterial(renderer, prefab.GetComponent<MeshRenderer>().sharedMaterial);
            var metal = transform.Find("Metal");
            var sourceMetal = prefab.transform.Find("Metal");
            if (sourceMetal != null)
            {
                if (metal == null)
                {
                    metal = new GameObject("Metal", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                    metal.SetParent(transform, false);
                    var r = metal.GetComponent<MeshRenderer>();
                    r.sharedMaterial = sourceMetal.GetComponent<MeshRenderer>().sharedMaterial;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                metal.GetComponent<MeshFilter>().sharedMesh = sourceMetal.GetComponent<MeshFilter>().sharedMesh;
                var sourceMetalRenderer = sourceMetal.GetComponent<MeshRenderer>();
                var metalRenderer = metal.GetComponent<MeshRenderer>();
                if (sourceMetalRenderer != null && metalRenderer != null)
                    metalRenderer.sharedMaterial = sourceMetalRenderer.sharedMaterial;
            }
            if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return true;
        }

        void LateUpdate()
        {
            if (hand == null || rig == null) return;
            var forward = rig.forward;
            // CC0 models have their origin at the grip; the primitive fallback is centred on the receiver.
            var offset = gripOrigin ? forward * .04f : forward * .14f + rig.up * .02f;
            transform.SetPositionAndRotation(hand.position + offset, Quaternion.LookRotation(forward, rig.up));
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
        public const string SyncPendingKey = "BodyRigSyncPending";

        public static BodyRig Current => BodyRigs.FromStoredId(PlayerPrefs.GetString(PrefKey, ""));

        public static bool Chosen => PlayerPrefs.GetInt(ChosenKey, 0) != 0;
        public static bool SyncPending => PlayerPrefs.GetInt(SyncPendingKey, 0) != 0;

        public static void Set(BodyRig rig, bool markChosen = true)
        {
            PlayerPrefs.SetString(PrefKey, BodyRigs.Id(rig));
            if (markChosen) PlayerPrefs.SetInt(ChosenKey, 1);
            PlayerPrefs.SetInt(SyncPendingKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Applies an account-owned preference without queuing it back to the account.</summary>
        public static void ApplyAccountPreference(BodyRig rig)
        {
            PlayerPrefs.SetString(PrefKey, BodyRigs.Id(rig));
            PlayerPrefs.SetInt(ChosenKey, 1);
            PlayerPrefs.SetInt(SyncPendingKey, 0);
            PlayerPrefs.Save();
        }

        /// <summary>Clears the pending marker only if the saved local choice is still current.</summary>
        public static void MarkAccountSynced(BodyRig rig)
        {
            if (Current != rig) return;
            PlayerPrefs.SetInt(SyncPendingKey, 0);
            PlayerPrefs.Save();
        }
    }
}

