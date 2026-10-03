using System.Collections.Generic;
using UnityEngine;

namespace Hotdrop.UI
{
    /// <summary>
    /// QA-024: turns an owned cosmetic id into something players can actually see.
    /// body_* adds a body pattern, wrap_* recolours the weapon, badge_* is shown next to the
    /// player's name. Patterns use neutral tones so they never compete with the ally/enemy
    /// shape cues (QA-023) or team colours, and they are presentation only (no colliders).
    /// </summary>
    public static class CosmeticLook
    {
        public const string PrefKey = "EquippedCosmetic";
        const string RootName = "Cosmetic";

        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        /// <summary>The locally equipped cosmetic (cached from the profile / last successful equip).</summary>
        public static string LocalEquipped
        {
            get => PlayerPrefs.GetString(PrefKey, "");
            set { PlayerPrefs.SetString(PrefKey, value ?? ""); PlayerPrefs.Save(); }
        }

        public static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Standard";
            int cut = id.IndexOf('_');
            string kind = cut > 0 ? id.Substring(0, cut) : "";
            string name = (cut > 0 ? id.Substring(cut + 1) : id).Replace('_', ' ');
            name = name.Length > 0 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name;
            return kind switch { "body" => "Body style: " + name, "wrap" => "Weapon wrap: " + name, "badge" => "Badge: " + name, _ => name };
        }

        /// <summary>Short badge tag for name plates ("" when the cosmetic is not a badge).</summary>
        public static string BadgeTag(string id) =>
            id != null && id.StartsWith("badge_") && id != "badge_rookie" ? "◆" + id.Substring(6).ToUpperInvariant() : "";

        /// <summary>Applies <paramref name="id"/> to an avatar visual root (body parts + a child named "Gun").</summary>
        public static void Apply(Transform visualRoot, string id)
        {
            if (visualRoot == null) return;
            var old = visualRoot.Find(RootName);
            if (old != null) Discard(old.gameObject);
            var gun = visualRoot.Find("Gun");
            if (gun == null)
            {
                var rig = visualRoot.Find("Rig");
                if (rig != null) gun = rig.Find("Gun");
            }
            if (gun != null)
            {
                var renderer = gun.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!gunDefaults.ContainsKey(renderer)) gunDefaults[renderer] = renderer.sharedMaterial;
                    renderer.sharedMaterial = id != null && id.StartsWith("wrap_") && id != "wrap_standard"
                        ? Material(id, WrapColour(id)) : gunDefaults[renderer];
                }
            }
            if (string.IsNullOrEmpty(id) || !id.StartsWith("body_") || id == "body_standard") return;

            var root = new GameObject(RootName).transform;
            root.SetParent(visualRoot, false);
            var mat = Material(id, BodyColour(id));
            switch (id)
            {
                case "body_teal_stripe": // two vertical stripes, front and back
                    for (int side = -1; side <= 1; side += 2)
                        for (int face = -1; face <= 1; face += 2)
                            Part(root, "Stripe", new Vector3(side * 0.12f, 1.1f, face * 0.18f), new Vector3(0.06f, 0.66f, 0.02f), mat);
                    break;
                case "body_rooftop": // shoulder plates
                    for (int side = -1; side <= 1; side += 2)
                        Part(root, "Plate", new Vector3(side * 0.3f, 1.42f, 0f), new Vector3(0.14f, 0.07f, 0.36f), mat);
                    break;
                default: // chevron on the back
                    Part(root, "ChevronL", new Vector3(-0.08f, 1.15f, -0.18f), new Vector3(0.2f, 0.05f, 0.02f), mat, 35f);
                    Part(root, "ChevronR", new Vector3(0.08f, 1.15f, -0.18f), new Vector3(0.2f, 0.05f, 0.02f), mat, -35f);
                    break;
            }
        }

        static readonly Dictionary<Renderer, Material> gunDefaults = new Dictionary<Renderer, Material>();

        /// <summary>The weapon-wrap material for <paramref name="id"/>, or null when it is not a wrap.</summary>
        public static Material WrapMaterialFor(string id) =>
            id != null && id.StartsWith("wrap_") && id != "wrap_standard" ? Material(id, WrapColour(id)) : null;

        static Color BodyColour(string id) => id switch
        {
            "body_teal_stripe" => new Color(0.93f, 0.95f, 0.97f), // light neutral (not the ally cyan)
            "body_rooftop" => new Color(0.12f, 0.12f, 0.14f),
            _ => new Color(0.75f, 0.75f, 0.78f),
        };

        static Color WrapColour(string id) => id switch
        {
            "wrap_hazard" => new Color(0.95f, 0.78f, 0.1f),
            "wrap_updraft" => new Color(0.55f, 0.35f, 0.9f),
            _ => new Color(0.85f, 0.85f, 0.88f),
        };

        static Material Material(string id, Color colour)
        {
            if (materials.TryGetValue(id, out var cached) && cached != null) return cached;
            var baseMat = Resources.Load<Material>("Materials/Structure");
            var mat = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = colour;
            materials[id] = mat;
            return mat;
        }

        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material, float roll = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Discard(collider); }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(0, 0, roll);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void Discard(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        /// <summary>A neutral mannequin (torso, head, legs, gun) for the Appearance preview.</summary>
        public static Transform BuildMannequin(Transform parent)
        {
            // Amendment 2: the Appearance preview shows the same humanoid the match uses,
            // in the player's currently selected body rig.
            var root = new GameObject("Mannequin").transform;
            root.SetParent(parent, false);
            var body = Material("__mannequin", new Color(0.36f, 0.42f, 0.52f));
            Hotdrop.Player.AvatarRigVisual.Build(root.gameObject, Hotdrop.Player.BodyRigStore.Current,
                body, Resources.Load<Material>("Materials/Equipment"), includeGun: true);
            return root;
        }
    }
}


