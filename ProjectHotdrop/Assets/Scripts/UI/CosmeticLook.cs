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
        public const string WrapPrefKey = "EquippedWeaponWrap";
        public const string BadgePrefKey = "EquippedBadge";
        const string RootName = "Cosmetic";

        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        /// <summary>Amendment 3: the equipped body style (legacy single slot, kept as an adapter).</summary>
        public static string LocalEquipped
        {
            get => PlayerPrefs.GetString(PrefKey, "");
            set { PlayerPrefs.SetString(PrefKey, value ?? ""); PlayerPrefs.Save(); }
        }

        /// <summary>Amendment 3: independent slots, so a body style, weapon wrap and badge coexist.</summary>
        public static string LocalWrap
        {
            get => PlayerPrefs.GetString(WrapPrefKey, "");
            set { PlayerPrefs.SetString(WrapPrefKey, value ?? ""); PlayerPrefs.Save(); }
        }

        public static string LocalBadge
        {
            get => PlayerPrefs.GetString(BadgePrefKey, "");
            set { PlayerPrefs.SetString(BadgePrefKey, value ?? ""); PlayerPrefs.Save(); }
        }

        /// <summary>Everything a remote viewer should see: body | wrap | badge.</summary>
        public static string LocalComposite => string.Join("|", LocalEquipped, LocalWrap, LocalBadge,
            Hotdrop.Player.BodyRigs.Id(Hotdrop.Player.BodyRigStore.Current));

        /// <summary>One player's equipped look. Any part may be empty.</summary>
        public readonly struct Look
        {
            public Look(string bodyStyle, string weaponWrap, string badge, string bodyRig)
            {
                BodyStyle = bodyStyle ?? ""; WeaponWrap = weaponWrap ?? "";
                Badge = badge ?? ""; BodyRig = bodyRig ?? "";
            }
            public string BodyStyle { get; }
            public string WeaponWrap { get; }
            public string Badge { get; }
            public string BodyRig { get; }
            public string Composite => string.Join("|", BodyStyle, WeaponWrap, Badge, BodyRig);
        }

        /// <summary>Parses "body | wrap | badge | rig". Unknown and empty parts are ignored,
        /// so a legacy single cosmetic id still parses as a body style.</summary>
        public static Look Parse(string composite)
        {
            string body = "", wrap = "", badge = "", rig = "";
            if (!string.IsNullOrEmpty(composite))
                foreach (var raw in composite.Split('|'))
                {
                    string id = raw.Trim();
                    if (id.Length == 0) continue;
                    if (id.StartsWith("body_") || id.EndsWith("_body")) { if (body.Length == 0) body = id; }
                    else if (id.StartsWith("wrap_") || id.EndsWith("_wrap")) wrap = id;
                    else if (id.StartsWith("badge_") || id.EndsWith("_badge")) badge = id;
                    else if (id.StartsWith("rig_")) rig = id;
                    else if (body.Length == 0) body = id; // legacy single-slot value
                }
            return new Look(body, wrap, badge, rig);
        }

        public static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return ItemName("");
            int cut = id.IndexOf('_');
            string kind = cut > 0 ? id.Substring(0, cut) : "";
            string name = ItemName(id);
            return kind switch
            {
                "body" => UiDraw.LFormat("appearance.cosmetic.display.bodyStyle", "Body style: {0}", name),
                "wrap" => UiDraw.LFormat("appearance.cosmetic.display.weaponWrap", "Weapon wrap: {0}", name),
                "badge" => UiDraw.LFormat("appearance.cosmetic.display.badge", "Badge: {0}", name),
                _ => name,
            };
        }

        /// <summary>Returns the cosmetic's value text without repeating its slot label.</summary>
        public static string ItemName(string id)
        {
            string name;
            if (string.IsNullOrEmpty(id)) name = "Standard";
            else
            {
                int cut = id.IndexOf('_');
                name = (cut > 0 ? id.Substring(cut + 1) : id).Replace('_', ' ');
                name = name.Length > 0 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name;
            }

            // Cosmetic IDs are server-owned identifiers, not display copy. Resolve known
            // catalog IDs through Localization and keep the readable English form for new
            // or legacy IDs until their authored translation entry is added.
            if (string.IsNullOrEmpty(id))
                return UiDraw.L("appearance.cosmetic.item.standard", name);
            if (!IsLocalizationSafeCosmeticId(id)) return name;
            string itemKey = id == "body_standard" || id == "wrap_standard"
                ? "appearance.cosmetic.item.standard"
                : "appearance.cosmetic.item." + id;
            return UiDraw.L(itemKey, name);
        }

        static bool IsLocalizationSafeCosmeticId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if ((c < 'a' || c > 'z') && (c < '0' || c > '9') && c != '_') return false;
            }
            return true;
        }

        /// <summary>Short badge tag for name plates ("" when the equipped badge is the default).</summary>
        public static string BadgeTag(string badgeId) =>
            badgeId != null && badgeId.StartsWith("badge_") && badgeId != "badge_rookie" ? "◆" + badgeId.Substring(6).ToUpperInvariant() : "";

        /// <summary>Applies <paramref name="id"/> to an avatar visual root. Accepts a single
        /// cosmetic id or a "body|wrap|badge|rig" composite.</summary>
        public static void Apply(Transform visualRoot, string id) => Apply(visualRoot, Parse(id));

        /// <summary>Applies every equipped slot at once, so identity is never one-of-N.</summary>
        public static void Apply(Transform visualRoot, Look look)
        {
            if (visualRoot == null) return;
            var old = visualRoot.Find(RootName);
            if (old != null) Discard(old.gameObject);
            var gun = visualRoot.Find("Gun");
            if (gun == null)
            {
                var rigRoot = visualRoot.Find("Rig");
                if (rigRoot != null) gun = rigRoot.Find("Gun");
            }
            if (gun != null)
            {
                var renderer = gun.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!gunDefaults.ContainsKey(renderer)) gunDefaults[renderer] = renderer.sharedMaterial;
                    renderer.sharedMaterial = ActiveWrap(look.WeaponWrap)
                        ? Material(look.WeaponWrap, WrapColour(look.WeaponWrap)) : gunDefaults[renderer];
                }
            }
            string id = look.BodyStyle;
            if (string.IsNullOrEmpty(id) || !(id.StartsWith("body_") || id.EndsWith("_body")) || id == "body_standard") return;

            var rig = Hotdrop.Player.BodyRigStore.Current;
            if (Hotdrop.Player.BodyRigs.TryParse(look.BodyRig, out var lookRig)) rig = lookRig;
            float horizontalScale = rig == Hotdrop.Player.BodyRig.Female ? .93f : 1f;
            Vector3 VariantPosition(Vector3 position) => new Vector3(position.x * horizontalScale, position.y, position.z * horizontalScale);
            Vector3 VariantSize(Vector3 size) => new Vector3(size.x * horizontalScale, size.y, size.z * horizontalScale);

            var root = new GameObject(RootName).transform;
            root.SetParent(visualRoot, false);
            var mat = Material(id, BodyColour(id));
            switch (id)
            {
                case "body_teal_stripe": // two vertical stripes, front and back
                    for (int side = -1; side <= 1; side += 2)
                        for (int face = -1; face <= 1; face += 2)
                            Part(root, "Stripe", VariantPosition(new Vector3(side * 0.12f, 1.1f, face * 0.18f)), VariantSize(new Vector3(0.06f, 0.66f, 0.02f)), mat);
                    break;
                case "body_rooftop": // shoulder plates
                    for (int side = -1; side <= 1; side += 2)
                        Part(root, "Plate", VariantPosition(new Vector3(side * 0.3f, 1.42f, 0f)), VariantSize(new Vector3(0.14f, 0.07f, 0.36f)), mat);
                    break;
                default: // chevron on the back
                    Part(root, "ChevronL", VariantPosition(new Vector3(-0.08f, 1.15f, -0.18f)), VariantSize(new Vector3(0.2f, 0.05f, 0.02f)), mat, 35f);
                    Part(root, "ChevronR", VariantPosition(new Vector3(0.08f, 1.15f, -0.18f)), VariantSize(new Vector3(0.2f, 0.05f, 0.02f)), mat, -35f);
                    break;
            }
        }

        static readonly Dictionary<Renderer, Material> gunDefaults = new Dictionary<Renderer, Material>();

        /// <summary>
        /// Updates the equipped model's baseline surface without removing an active wrap.
        /// When the wrap is later cleared, Apply restores the current weapon's material.
        /// </summary>
        public static void SetWeaponBaseMaterial(Renderer renderer, Material baseMaterial)
        {
            if (renderer == null || baseMaterial == null) return;
            if (gunDefaults.TryGetValue(renderer, out var previous))
            {
                bool wrapped = renderer.sharedMaterial != previous;
                gunDefaults[renderer] = baseMaterial;
                if (!wrapped) renderer.sharedMaterial = baseMaterial;
            }
            else
            {
                gunDefaults[renderer] = baseMaterial;
                renderer.sharedMaterial = baseMaterial;
            }
        }

        /// <summary>A wrap is only "active" when it is not the default blank wrap.</summary>
        static bool ActiveWrap(string id) =>
            id != null && (id.StartsWith("wrap_") || id.EndsWith("_wrap")) && id != "wrap_standard";

        /// <summary>The weapon-wrap material for <paramref name="id"/>, or null when it is not a wrap.
        /// Accepts a single id or a composite, so callers can pass the whole look.</summary>
        public static Material WrapMaterialFor(string id)
        {
            string wrap = Parse(id).WeaponWrap;
            return ActiveWrap(wrap) ? Material(wrap, WrapColour(wrap)) : null;
        }

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


