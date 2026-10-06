using UnityEngine;
using Hotdrop.Config;
using Hotdrop.UI;
using TMPro;

namespace Hotdrop.Map
{
    /// <summary>
    /// Generates the Switchyard graybox arena from MapConfig coordinates (spec section 10).
    /// 1 Unity unit = 1 meter. Origin = Center zone.
    /// Footprint 96x80m (X -48..+48, Z -40..+40). Ground Y=0.
    /// Bright industrial rooftop training arena. No real military branding.
    /// </summary>
    public class SwitchyardGenerator : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] public MapConfig mapConfig;
        [SerializeField] Material groundMaterial;
        Material roofDeckMaterial;
        Material roofCladdingMaterial;
        [SerializeField] Material platformMaterial;
        [SerializeField] Material coverMaterial;
        [SerializeField] Material spawnBarrierMaterial;
        [SerializeField] Material zoneMaterial;
        [SerializeField] Material friendlyMaterial;
        [SerializeField] Material enemyMaterial;
        Material presentationDarkMaterial;
        Material presentationSafetyMaterial;
        Material presentationConcreteMaterial;
        Material presentationSeamMaterial;
        Material presentationFacadeMaterial;
        Material presentationWindowMaterial;
        Material presentationGlassMaterial;
        Material presentationZoneWestMaterial;
        Material presentationZoneCenterMaterial;
        Material presentationZoneEastMaterial;
        Light presentationSun;
        readonly System.Collections.Generic.List<Mesh> presentationMeshes = new System.Collections.Generic.List<Mesh>();
        MaterialPropertyBlock profileBlock;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        [Header("Prefabs")]
        [SerializeField] GameObject rampPrefab;
        [SerializeField] GameObject sightlineBlockerPrefab;
        bool generated;

        void Awake()
        {
            GenerateMap();
        }

        public void GenerateMap()
        {
            if (Application.isPlaying && generated) return;
            generated = true;
            EnsureMaterials();
            foreach (var mesh in presentationMeshes) if (mesh != null)
            { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            presentationMeshes.Clear();
            // Clear existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            GenerateGround();
            GenerateSpawnRooms();
            GeneratePlatforms();
            GenerateRamps();
            GenerateZones();
            GenerateSightlineBlockers();
            GenerateOuterLaneBreakers();
            GenerateCenterPresentation();
            GenerateRooftopPresentation();
            GenerateLandmarkPresentation();
            SetupLighting();
            ApplyWorldPresentationProfile();
            if (Application.isPlaying)
            {
                var navigation = GetComponent<OfflineNavigation>();
                if (navigation == null) navigation = gameObject.AddComponent<OfflineNavigation>();
                navigation.Build();
            }
        }

        void EnsureMaterials()
        {
            if (groundMaterial == null) groundMaterial = MakeMaterial("Concrete", "#C3B59E");
            if (platformMaterial == null) platformMaterial = MakeMaterial("Structure", "#49627B");
            if (coverMaterial == null) coverMaterial = MakeMaterial("Cover", "#7A8D9D");
            if (friendlyMaterial == null) friendlyMaterial = MakeMaterial("Friendly", "#30D5E8");
            if (enemyMaterial == null) enemyMaterial = MakeMaterial("Enemy", "#FF6376");
            if (zoneMaterial == null) zoneMaterial = MakeMaterial("Objective", "#FFD166");
            if (presentationDarkMaterial == null) presentationDarkMaterial = MakeMaterial("SwitchyardEquipment", "#26384A");
            if (presentationSafetyMaterial == null) presentationSafetyMaterial = MakeMaterial("SwitchyardSafety", "#E0A936");
            if (presentationConcreteMaterial == null) presentationConcreteMaterial = MakeMaterial("SwitchyardRoof", "#A6ACAA");
            if (presentationSeamMaterial == null) presentationSeamMaterial = MakeMaterial("SwitchyardSeam", "#61727D");
            if (presentationFacadeMaterial == null) presentationFacadeMaterial = MakeMaterial("SwitchyardFacade", "#68797A");
            if (presentationWindowMaterial == null) presentationWindowMaterial = MakeMaterial("SwitchyardWindows", "#D6A96B");
            if (presentationGlassMaterial == null) presentationGlassMaterial = MakeGlassMaterial(presentationWindowMaterial);
            if (presentationZoneWestMaterial == null) presentationZoneWestMaterial = MakeMaterial("SwitchyardZoneWest", "#B6A06B");
            if (presentationZoneCenterMaterial == null) presentationZoneCenterMaterial = MakeMaterial("SwitchyardZoneCenter", "#84998F");
            if (presentationZoneEastMaterial == null) presentationZoneEastMaterial = MakeMaterial("SwitchyardZoneEast", "#9F8D7D");
        }

        /// <summary>
        /// Collider-free roof dressing drawn from the UIUX rooftop direction. It gives the
        /// playable Switchyard a roof edge, deck scale, service equipment and a distant city
        /// silhouette while leaving MapConfig geometry, cover and navigation unchanged.
        /// </summary>
        void GenerateRooftopPresentation()
        {
            var root = new GameObject("Rooftop visual dressing (no collision)");
            root.transform.SetParent(transform, false);
            float west = mapConfig.boundsMin.x;
            float east = mapConfig.boundsMax.x;
            float south = mapConfig.boundsMin.y;
            float north = mapConfig.boundsMax.y;
            float width = east - west;
            float depth = north - south;

            // Shallow seams establish a concrete module scale without creating steps.
            for (float x = west + 8f; x < east; x += 8f)
                PresentationPart(root.transform, "Deck seam X", PrimitiveType.Cube,
                    new Vector3(x, mapConfig.groundLevelY + 0.008f, 0f),
                    new Vector3(0.045f, 0.008f, depth), presentationSeamMaterial);
            for (float z = south + 8f; z < north; z += 8f)
                PresentationPart(root.transform, "Deck seam Z", PrimitiveType.Cube,
                    new Vector3(0f, mapConfig.groundLevelY + 0.009f, z),
                    new Vector3(width, 0.008f, 0.045f), presentationSeamMaterial);

            // The parapet sits just outside the configured arena footprint. Its renderer has
            // no collider, so it cannot become new cover or change a sightline test.
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? west - 0.7f : east + 0.7f;
                float z = side == 0 ? south - 0.7f : north + 0.7f;
                PresentationPart(root.transform, "Roof parapet", PrimitiveType.Cube,
                    new Vector3(x, 0.35f, 0f), new Vector3(0.45f, 0.7f, depth + 2f), presentationConcreteMaterial);
                PresentationPart(root.transform, "Roof parapet", PrimitiveType.Cube,
                    new Vector3(0f, 0.35f, z), new Vector3(width + 2f, 0.7f, 0.45f), presentationConcreteMaterial);
                PresentationPart(root.transform, "Parapet cap", PrimitiveType.Cube,
                    new Vector3(x, 0.73f, 0f), new Vector3(0.62f, 0.08f, depth + 2f), presentationDarkMaterial);
                PresentationPart(root.transform, "Parapet cap", PrimitiveType.Cube,
                    new Vector3(0f, 0.73f, z), new Vector3(width + 2f, 0.08f, 0.62f), presentationDarkMaterial);
            }

            // Distinct, team-neutral bands on the north and south parapets let players
            // call out the West/Center/East roof sections without relying on team colors.
            var zoneSections = new[]
            {
                (x: mapConfig.westZone.x, id: ZoneId.West),
                (x: mapConfig.centerZone.x, id: ZoneId.Center),
                (x: mapConfig.eastZone.x, id: ZoneId.East),
            };
            System.Array.Sort(zoneSections, (a, b) => a.x.CompareTo(b.x));
            for (int i = 0; i < zoneSections.Length; i++)
            {
                float left = i == 0 ? west : (zoneSections[i - 1].x + zoneSections[i].x) * .5f;
                float right = i == zoneSections.Length - 1 ? east : (zoneSections[i].x + zoneSections[i + 1].x) * .5f;
                float stripWidth = right - left - .2f;
                if (stripWidth <= 0f) continue;
                Material trim = ZoneTrimMaterial(zoneSections[i].id);
                foreach (float edgeZ in new[] { south - .7f, north + .7f })
                    PresentationPart(root.transform, "Roof edge zone trim " + zoneSections[i].id,
                        PrimitiveType.Cube, new Vector3((left + right) * .5f, mapConfig.groundLevelY + .775f, edgeZ),
                        new Vector3(stripWidth, .016f, .3f), trim);
            }

            // The playable surface is the roof of a real building volume, not a floating
            // platform. Dress the tower below the roofline while keeping all new geometry
            // outside the arena collision and shot-blocking rules.
            float facadeY = mapConfig.groundLevelY - 12f;
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? west - 2.2f : east + 2.2f;
                float z = side == 0 ? south - 2.2f : north + 2.2f;
                PresentationPart(root.transform, "Tower facade", PrimitiveType.Cube,
                    new Vector3(x, facadeY, 0f), new Vector3(4.5f, 24f, depth + 4.5f), presentationFacadeMaterial);
                PresentationPart(root.transform, "Tower facade", PrimitiveType.Cube,
                    new Vector3(0f, facadeY, z), new Vector3(width + 4.5f, 24f, 4.5f), presentationFacadeMaterial);
                for (float y = mapConfig.groundLevelY - 3f; y >= mapConfig.groundLevelY - 19f; y -= 4f)
                {
                    for (float span = -depth * .5f + 8f; span < depth * .5f - 4f; span += 12f)
                    {
                        float faceX = side == 0 ? west - 4.48f : east + 4.48f;
                        PresentationPart(root.transform, "Warm tower window", PrimitiveType.Cube,
                            new Vector3(faceX, y, span), new Vector3(0.045f, 1.1f, 3.4f), presentationWindowMaterial);
                    }
                    for (float span = -width * .5f + 8f; span < width * .5f - 4f; span += 12f)
                    {
                        float faceZ = side == 0 ? south - 4.48f : north + 4.48f;
                        PresentationPart(root.transform, "Warm tower window", PrimitiveType.Cube,
                            new Vector3(span, y, faceZ), new Vector3(3.4f, 1.1f, 0.045f), presentationWindowMaterial);
                    }
                }
            }

            // Repeated vent silhouettes on existing spawn roofs, well above players.
            foreach (var spawn in new[] { mapConfig.northSpawnCenter, mapConfig.southSpawnCenter })
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 pos = spawn + new Vector3(i * 5f, 3.95f, 0f);
                    PresentationPart(root.transform, "Roof service vent", PrimitiveType.Cube,
                        pos, new Vector3(2.8f, 0.9f, 2.2f), presentationDarkMaterial);
                    PresentationPart(root.transform, "Vent cover", PrimitiveType.Cube,
                        pos + Vector3.up * 0.49f, new Vector3(3.0f, 0.08f, 2.4f), presentationConcreteMaterial);
                }
            }

            // Fixed, deterministic distant buildings provide the coastal city scale seen in
            // the reference. They are beyond the arena and never enter the navigation mesh.
            for (int i = 0; i < 22; i++)
            {
                float x = -96f + i * 9f;
                float height = 12f + ((i * 17) % 23);
                float z = i % 2 == 0 ? north + 55f : south - 68f;
                PresentationPart(root.transform, "Distant city block", PrimitiveType.Cube,
                    new Vector3(x, height * 0.5f - 19f, z),
                    new Vector3(5f + i % 3, height, 5f), presentationSeamMaterial);
            }

            // Turn solid gameplay cover into readable fabricated equipment. These shallow
            // panels hug the original faces; the collision footprint remains authoritative.
            foreach (var source in GetComponentsInChildren<MeshRenderer>())
            {
                bool cover = source.name.StartsWith("OuterBreaker_") || source.name.StartsWith("SightlineBlocker_");
                if (!cover) continue;
                var t = source.transform;
                for (int side = -1; side <= 1; side += 2)
                {
                    var panel = PresentationPart(root.transform, "Equipment face", PrimitiveType.Cube,
                        t.TransformPoint(new Vector3(0, 0, side * .505f)),
                        new Vector3(t.localScale.x * .86f, t.localScale.y * .72f, .035f), presentationDarkMaterial);
                    panel.rotation = t.rotation;
                    var stripe = PresentationPart(root.transform, "Equipment safety band", PrimitiveType.Cube,
                        t.TransformPoint(new Vector3(0, .33f, side * .52f)),
                        new Vector3(t.localScale.x * .88f, .10f, .04f), presentationSafetyMaterial);
                    stripe.rotation = t.rotation;
                }
            }
            foreach (var platform in mapConfig.platformCenters)
            {
                PresentationPart(root.transform, "Platform landing stripe", PrimitiveType.Cube,
                    platform + new Vector3(0, .015f, 0),
                    new Vector3(mapConfig.platformSize.x - .5f, .018f, .18f), presentationSafetyMaterial);
            }
            // Each material is one mesh rather than a draw call for every seam/panel/building.
            CombineDressing(root.transform);
        }

        Material ZoneTrimMaterial(ZoneId zone) => zone switch
        {
            ZoneId.West => presentationZoneWestMaterial,
            ZoneId.East => presentationZoneEastMaterial,
            _ => presentationZoneCenterMaterial,
        };

        void CombineDressing(Transform root)
        {
            var groups = new System.Collections.Generic.Dictionary<Material, System.Collections.Generic.List<CombineInstance>>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterial == null) continue;
                if (!groups.TryGetValue(renderer.sharedMaterial, out var parts))
                    groups.Add(renderer.sharedMaterial, parts = new System.Collections.Generic.List<CombineInstance>());
                parts.Add(new CombineInstance { mesh = filter.sharedMesh, transform = root.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                renderer.enabled = false;
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "Batched rooftop " + group.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray()); presentationMeshes.Add(mesh);
                var obj = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(root, false);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = group.Key;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void OnDestroy()
        {
            UiPresentationProfile.Changed -= ApplyWorldPresentationProfile;
            foreach (var mesh in presentationMeshes) if (mesh != null)
            { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            if (roofDeckMaterial != null)
            { if (Application.isPlaying) Destroy(roofDeckMaterial); else DestroyImmediate(roofDeckMaterial); }
            if (roofCladdingMaterial != null)
            { if (Application.isPlaying) Destroy(roofCladdingMaterial); else DestroyImmediate(roofCladdingMaterial); }
            if (presentationGlassMaterial != null)
            { if (Application.isPlaying) Destroy(presentationGlassMaterial); else DestroyImmediate(presentationGlassMaterial); }
        }

        Material DeckMaterial()
        {
            var texture = Resources.Load<Texture2D>("Environment/RooftopConcrete");
            if (texture == null || groundMaterial == null) return groundMaterial;
            if (roofDeckMaterial != null) return roofDeckMaterial;
            roofDeckMaterial = new Material(groundMaterial) { name = "Weathered rooftop concrete" };
            roofDeckMaterial.color = new Color(.80f, .73f, .61f);
            roofDeckMaterial.mainTexture = texture;
            roofDeckMaterial.mainTextureScale = new Vector2(6f, 5f);
            if (roofDeckMaterial.HasProperty("_BaseMap"))
            {
                roofDeckMaterial.SetTexture("_BaseMap", texture);
                roofDeckMaterial.SetTextureScale("_BaseMap", new Vector2(6f, 5f));
            }
            if (roofDeckMaterial.HasProperty("_Smoothness")) roofDeckMaterial.SetFloat("_Smoothness", .04f);
            return roofDeckMaterial;
        }

        Material CladdingMaterial()
        {
            var texture = Resources.Load<Texture2D>("Environment/RooftopCladding");
            if (texture == null || coverMaterial == null) return coverMaterial;
            if (roofCladdingMaterial != null) return roofCladdingMaterial;
            roofCladdingMaterial = new Material(coverMaterial) { name = "Weathered rooftop cladding" };
            roofCladdingMaterial.color = new Color(.88f, .92f, .96f);
            roofCladdingMaterial.mainTexture = texture;
            if (roofCladdingMaterial.HasProperty("_BaseMap")) roofCladdingMaterial.SetTexture("_BaseMap", texture);
            if (roofCladdingMaterial.HasProperty("_Smoothness")) roofCladdingMaterial.SetFloat("_Smoothness", .08f);
            return roofCladdingMaterial;
        }

        /// <summary>
        /// Amendment 2 representative art sample: an original, collider-free communications
        /// tower over Center. It dresses the existing objective without adding cover, a walkable
        /// surface, sightline obstruction, or a new route. West/East stay deliberately graybox
        /// until the owner reviews this one finished section.
        /// </summary>
        void GenerateCenterPresentation()
        {
            var root = new GameObject("Center communications tower (presentation only)");
            root.transform.SetParent(transform, false);
            Vector3 center = mapConfig.centerZone;

            PresentationPart(root.transform, "Tower mast", PrimitiveType.Cylinder,
                center + Vector3.up * 4.0f, new Vector3(0.22f, 4f, 0.22f), presentationDarkMaterial);
            PresentationPart(root.transform, "Signal cap", PrimitiveType.Sphere,
                center + Vector3.up * 8.05f, new Vector3(0.5f, 0.5f, 0.5f), presentationSafetyMaterial);

            // Four suspended antenna arms make Center identifiable from both spawn sides.
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var arm = PresentationPart(root.transform, "Antenna arm", PrimitiveType.Cube,
                    center + Vector3.up * 6.7f + radial * 1.05f, new Vector3(0.12f, 0.12f, 2.1f), presentationDarkMaterial);
                arm.rotation = Quaternion.Euler(0f, angle, 0f);
                PresentationPart(root.transform, "Antenna beacon", PrimitiveType.Sphere,
                    center + Vector3.up * 6.7f + radial * 2.05f, new Vector3(0.25f, 0.25f, 0.25f), presentationSafetyMaterial);
            }

            // Flat, non-colliding rooftop equipment markings provide industrial wear/readability
            // without modifying the existing objective radius or the competitive ground plane.
            for (int i = -2; i <= 2; i++)
            {
                PresentationPart(root.transform, "Safety deck stripe", PrimitiveType.Cube,
                    center + new Vector3(i * 1.25f, 0.026f, -4.9f), new Vector3(0.65f, 0.012f, 0.18f), presentationSafetyMaterial);
                PresentationPart(root.transform, "Safety deck stripe", PrimitiveType.Cube,
                    center + new Vector3(i * 1.25f, 0.026f, 4.9f), new Vector3(0.65f, 0.012f, 0.18f), presentationSafetyMaterial);
            }
        }

        /// <summary>
        /// Procedural, collider-free landmark prototypes for Switchyard orientation.
        /// Their silhouettes sit outside the playable footprint so they do not create
        /// routes, cover, or new line-of-sight breaks before map playability review.
        /// </summary>
        void GenerateLandmarkPresentation()
        {
            float west = mapConfig.boundsMin.x;
            float east = mapConfig.boundsMax.x;
            float north = mapConfig.boundsMax.y;
            float centerZ = (mapConfig.boundsMin.y + north) * .5f;
            float ground = mapConfig.groundLevelY;

            // Rotating tower crane beyond the west edge.
            float craneX = west - 3f;
            var craneRoot = new GameObject("Rotating tower crane landmark (visual only)").transform;
            craneRoot.SetParent(transform, false);
            PresentationPart(craneRoot, "Crane mast", PrimitiveType.Cylinder,
                new Vector3(craneX, ground + 6f, centerZ), new Vector3(.5f, 6f, .5f), presentationDarkMaterial);
            PresentationPart(craneRoot, "Crane mast safety band", PrimitiveType.Cube,
                new Vector3(craneX, ground + 3.5f, centerZ), new Vector3(.65f, .22f, .65f), presentationSafetyMaterial);
            var rotor = new GameObject("Crane rotating boom").transform;
            rotor.SetParent(craneRoot, false);
            rotor.position = new Vector3(craneX, ground + 12f, centerZ);
            PresentationPart(rotor, "Crane boom", PrimitiveType.Cube,
                rotor.position + Vector3.right * 3f, new Vector3(22f, .38f, .42f), presentationSafetyMaterial);
            PresentationPart(rotor, "Crane counterweight", PrimitiveType.Cube,
                rotor.position + Vector3.left * 7f, new Vector3(3f, 1.2f, 1.1f), presentationDarkMaterial);
            PresentationPart(rotor, "Crane hanging cable", PrimitiveType.Cylinder,
                rotor.position + Vector3.right * 14f + Vector3.down * 2.2f, new Vector3(.035f, 2.2f, .035f), presentationDarkMaterial);
            PresentationPart(rotor, "Crane hook", PrimitiveType.Cube,
                rotor.position + Vector3.right * 14f + Vector3.down * 4.5f, new Vector3(.5f, .16f, .16f), presentationSafetyMaterial);
            rotor.gameObject.AddComponent<SwitchyardCraneRotor>();

            // Translucent skybridge silhouette stays beyond the north map boundary.
            float bridgeZ = north + 9f;
            float bridgeY = ground + 10f;
            float bridgeWidth = east - west + 20f;
            foreach (float x in new[] { west - 6f, east + 6f })
                PresentationPart(transform, "Skybridge support (visual only)", PrimitiveType.Cube,
                    new Vector3(x, ground + 4.5f, bridgeZ), new Vector3(1.2f, 9f, 2f), presentationDarkMaterial);
            PresentationPart(transform, "Glass skybridge deck (visual only)", PrimitiveType.Cube,
                new Vector3((west + east) * .5f, bridgeY, bridgeZ), new Vector3(bridgeWidth, .22f, 2.6f), presentationGlassMaterial);
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(west - 8f, east + 8f, i / 6f);
                PresentationPart(transform, "Glass skybridge panel (visual only)", PrimitiveType.Cube,
                    new Vector3(x, bridgeY + 1.6f, bridgeZ), new Vector3(bridgeWidth / 7f - .18f, 2.8f, .12f), presentationGlassMaterial);
                PresentationPart(transform, "Skybridge frame", PrimitiveType.Cube,
                    new Vector3(x, bridgeY + 3.05f, bridgeZ), new Vector3(.16f, .16f, .5f), presentationDarkMaterial);
            }

            // Sign faces the arena. The lettering is a temporary TMP prototype, not final art.
            var signCenter = new Vector3((west + east) * .5f, bridgeY + 1.7f, bridgeZ - 1.45f);
            PresentationPart(transform, "SWITCHYARD sign panel", PrimitiveType.Cube,
                signCenter, new Vector3(24f, 3.4f, .22f), presentationDarkMaterial);
            var sign = new GameObject("SWITCHYARD neon landmark");
            sign.transform.SetParent(transform, false);
            sign.transform.position = signCenter + Vector3.back * .13f;
            sign.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var text = sign.AddComponent<TextMeshPro>();
            text.text = "SWITCHYARD";
            text.fontSize = 3.1f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UiPresentationProfile.Palette.WorldWindow;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = new Vector2(23f, 3.2f);
            var signRenderer = sign.GetComponent<MeshRenderer>();
            if (signRenderer != null) signRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Material MakeGlassMaterial(Material source)
        {
            if (source == null) return null;
            var glass = new Material(source) { name = "Switchyard translucent landmark glass" };
            if (glass.HasProperty("_Surface")) glass.SetFloat("_Surface", 1f);
            if (glass.HasProperty("_Blend")) glass.SetFloat("_Blend", 0f);
            if (glass.HasProperty("_ZWrite")) glass.SetFloat("_ZWrite", 0f);
            if (glass.HasProperty("_SrcBlend")) glass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (glass.HasProperty("_DstBlend")) glass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            Color tint = glass.color; tint.a = .28f; glass.color = tint;
            return glass;
        }

        /// <summary>Original visual primitive only: remove its collider before it can affect play.</summary>
        Transform PresentationPart(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, true);
            part.transform.position = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            }
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part.transform;
        }

        static Material MakeMaterial(string name, string hex)
        {
            var asset = Resources.Load<Material>("Materials/" + name);
            if (asset != null) return asset;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.color = color;
            material.SetFloat("_Smoothness", 0.08f);
            return material;
        }

        void SetupLighting()
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            presentationSun = FindFirstObjectByType<Light>();
            if (presentationSun == null)
            {
                presentationSun = new GameObject("Switchyard sunlight").AddComponent<Light>();
                presentationSun.transform.SetParent(transform, false);
                presentationSun.type = LightType.Directional;
                presentationSun.transform.rotation = Quaternion.Euler(50, -35, 0);
                presentationSun.shadows = LightShadows.None;
            }
            UiPresentationProfile.Changed -= ApplyWorldPresentationProfile;
            UiPresentationProfile.Changed += ApplyWorldPresentationProfile;
        }

        void ApplyWorldPresentationProfile()
        {
            var palette = UiPresentationProfile.Palette;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = palette.WorldAmbient;
            if (presentationSun != null)
            {
                presentationSun.color = palette.WorldSun;
                presentationSun.intensity = palette.WorldSunIntensity;
            }
            var camera = UnityEngine.Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = palette.WorldSky;
            }

            if (profileBlock == null) profileBlock = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                string name = material.name.ToLowerInvariant();
                Color tint;
                if (name.Contains("friendly")) tint = palette.Ally;
                else if (name.Contains("enemy")) tint = palette.Enemy;
                else if (name.Contains("zonewest")) tint = palette.WorldZoneWest;
                else if (name.Contains("zonecenter")) tint = palette.WorldZoneCenter;
                else if (name.Contains("zoneeast")) tint = palette.WorldZoneEast;
                else if (name.Contains("objective") || name.Contains("safety")) tint = palette.WorldSafety;
                else if (name.Contains("landmark glass")) { tint = palette.WorldWindow; tint.a = .28f; }
                else if (name.Contains("window")) tint = palette.WorldWindow;
                else if (name.Contains("equipment")) tint = palette.WorldEquipment;
                else if (name.Contains("facade")) tint = palette.WorldFacade;
                else if (name.Contains("cover") || name.Contains("cladding")) tint = palette.WorldCover;
                else if (name.Contains("concrete") || name.Contains("roof")) tint = palette.WorldConcrete;
                else if (name.Contains("structure") || name.Contains("seam")) tint = palette.WorldStructure;
                else continue;

                renderer.GetPropertyBlock(profileBlock);
                if (material.HasProperty(BaseColorId)) profileBlock.SetColor(BaseColorId, tint);
                if (material.HasProperty(LegacyColorId)) profileBlock.SetColor(LegacyColorId, tint);
                renderer.SetPropertyBlock(profileBlock);
                profileBlock.Clear();
            }
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name.Contains("SWITCHYARD neon")) text.color = palette.WorldWindow;
        }

        void GenerateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.parent = transform;
            ground.transform.position = new Vector3(0, mapConfig.groundLevelY - 0.25f, 0);
            ground.transform.localScale = new Vector3(
                mapConfig.boundsMax.x - mapConfig.boundsMin.x,
                0.5f,
                mapConfig.boundsMax.y - mapConfig.boundsMin.y
            );

            var renderer = ground.GetComponent<Renderer>();
            if (groundMaterial != null) renderer.sharedMaterial = DeckMaterial();

            // Add collider (Plane already has MeshCollider)
            ground.layer = LayerMask.NameToLayer("Ground");
        }

        void GenerateSpawnRooms()
        {
            // North spawn room (Team A)
            CreateSpawnRoom("NorthSpawnRoom", mapConfig.northSpawnCenter, mapConfig.spawnRoomSize, 0);

            // South spawn room (Team B)
            CreateSpawnRoom("SouthSpawnRoom", mapConfig.southSpawnCenter, mapConfig.spawnRoomSize, 1);
        }

        void CreateSpawnRoom(string name, Vector3 center, Vector2 size, int teamId)
        {
            GameObject room = new GameObject(name);
            room.transform.parent = transform;

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.parent = room.transform;
            // Flush with the arena floor (a raised floor would exceed the 0.35 m step height).
            // Concrete, not team colour: a saturated floor fills the screen and hides players.
            floor.transform.localPosition = new Vector3(0, mapConfig.groundLevelY + 0.01f, 0);
            floor.transform.localScale = new Vector3(size.x, 0.02f, size.y);
            floor.GetComponent<Renderer>().sharedMaterial = DeckMaterial();

            // Mirrored front with exactly three four-metre exits toward the arena.
            float facing = teamId == 0 ? -1f : 1f;
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;

            // Team identity as a border stripe plus the exit headers, never the whole floor.
            var stripe = CreateSpawnBox(room, "Team stripe", new Vector3(0, 0.025f, facing * (halfZ - 0.6f)), new Vector3(size.x - 1f, 0.03f, 0.5f),
                teamId == 0 ? friendlyMaterial : enemyMaterial);
            var stripeCollider = stripe.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(stripeCollider); else DestroyImmediate(stripeCollider);
            CreateSpawnBox(room, "Back", new Vector3(0, 1.5f, -facing * halfZ), new Vector3(size.x, 3, 0.5f), platformMaterial);
            CreateSpawnBox(room, "Left", new Vector3(-halfX, 1.5f, 0), new Vector3(0.5f, 3, size.y), platformMaterial);
            CreateSpawnBox(room, "Right", new Vector3(halfX, 1.5f, 0), new Vector3(0.5f, 3, size.y), platformMaterial);
            float cursor = -halfX;
            for (int i = 0; i < 3; i++)
            {
                float exitX = (i - 1) * 6f;
                float length = exitX - 2f - cursor;
                if (length > 0)
                    CreateSpawnBox(room, "Front pier", new Vector3(cursor + length * 0.5f, 1.5f, facing * halfZ), new Vector3(length, 3, 0.5f), platformMaterial);
                var barrier = CreateSpawnBox(room, "Exit barrier", new Vector3(exitX, 1.5f, facing * halfZ), new Vector3(4, 3, 0.12f), platformMaterial);
                barrier.GetComponent<Renderer>().enabled = false;
                barrier.AddComponent<SpawnExitBarrier>().OwnerTeam = teamId;
                CreateSpawnBox(room, "Exit header", new Vector3(exitX, 3, facing * halfZ), new Vector3(4, 0.2f, 0.5f), teamId == 0 ? friendlyMaterial : enemyMaterial);
                cursor = exitX + 2f;
            }
            if (cursor < halfX)
                CreateSpawnBox(room, "Front pier", new Vector3((cursor + halfX) * 0.5f, 1.5f, facing * halfZ), new Vector3(halfX - cursor, 3, 0.5f), platformMaterial);
            CreateSpawnBox(room, "Roof", new Vector3(0, 3.25f, 0), new Vector3(size.x, 0.5f, size.y), platformMaterial);
            room.transform.position = center;
        }

        GameObject CreateSpawnBox(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            box.layer = LayerMask.NameToLayer("SpawnBarrier");
            return box;
        }


        void GeneratePlatforms()
        {
            for (int i = 0; i < mapConfig.platformCenters.Length; i++)
            {
                Vector3 center = mapConfig.platformCenters[i];
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = $"Platform_{i}";
                platform.transform.parent = transform;
                platform.transform.position = center - Vector3.up * 0.25f;
                platform.transform.localScale = new Vector3(mapConfig.platformSize.x, 0.5f, mapConfig.platformSize.y);

                var renderer = platform.GetComponent<Renderer>();
                if (platformMaterial != null) renderer.material = platformMaterial;

                platform.layer = LayerMask.NameToLayer("Ground");
            }
        }

        void GenerateRamps()
        {
            // Each platform has two walkable ramps, both facing the middle of the arena, so the
            // layout mirrors across X = 0 and Z = 0 and neither team gets a better route (spec 9).
            foreach (Vector3 platformCenter in mapConfig.platformCenters)
            {
                float towardCenterX = -Mathf.Sign(platformCenter.x);
                float towardCenterZ = -Mathf.Sign(platformCenter.z);

                // Ramp 1: X side facing the centre line
                CreateRamp(new Vector3(platformCenter.x + towardCenterX * (mapConfig.platformSize.x * 0.5f + 7f), mapConfig.groundLevelY, platformCenter.z),
                    platformCenter + new Vector3(towardCenterX * mapConfig.platformSize.x * 0.5f, 0, 0),
                    new Vector3(6f, mapConfig.platformHeight, 4f));

                // Ramp 2: Z side facing midfield
                CreateRamp(new Vector3(platformCenter.x, mapConfig.groundLevelY, platformCenter.z + towardCenterZ * (mapConfig.platformSize.y * 0.5f + 7f)),
                    platformCenter + new Vector3(0, 0, towardCenterZ * mapConfig.platformSize.y * 0.5f),
                    new Vector3(4f, mapConfig.platformHeight, 6f));
            }
        }

        void CreateRamp(Vector3 groundPos, Vector3 platformEdge, Vector3 size)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.parent = transform;
            ramp.transform.position = (groundPos + platformEdge) * 0.5f;
            ramp.transform.localScale = new Vector3(4f, 0.15f, Vector3.Distance(groundPos, platformEdge));

            // Rotate to slope
            Vector3 dir = (platformEdge - groundPos).normalized;
            float angle = Mathf.Atan2(platformEdge.y - groundPos.y, Vector3.Distance(new Vector3(groundPos.x, 0, groundPos.z), new Vector3(platformEdge.x, 0, platformEdge.z))) * Mathf.Rad2Deg;
            ramp.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            ramp.transform.position -= ramp.transform.up * 0.075f;

            var renderer = ramp.GetComponent<Renderer>();
            if (platformMaterial != null) renderer.material = platformMaterial;

            ramp.layer = LayerMask.NameToLayer("Ground");
        }

        void GenerateZones()
        {
            // Center zone
            CreateZoneMarker("CenterZone", mapConfig.centerZone, ZoneId.Center);

            // West zone
            CreateZoneMarker("WestZone", mapConfig.westZone, ZoneId.West);

            // East zone
            CreateZoneMarker("EastZone", mapConfig.eastZone, ZoneId.East);
        }

        void CreateZoneMarker(string name, Vector3 center, ZoneId zoneId)
        {
            GameObject zone = new GameObject(name);
            zone.transform.parent = transform;

            // Visual ring on ground
            GameObject ring = CreateRing(center, mapConfig.zoneRadius, 0.5f);
            ring.name = "ZoneRing";
            ring.transform.parent = zone.transform;

            var renderer = ring.GetComponent<Renderer>();
            if (zoneMaterial != null) renderer.material = zoneMaterial;
            zone.AddComponent<ZoneVisual>().Initialize(zoneId, ring.GetComponent<LineRenderer>(), center, zoneMaterial);

            // Zone center marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "CenterMarker";
            marker.transform.parent = zone.transform;
            marker.transform.position = center + Vector3.up * 0.1f;
            marker.transform.localScale = new Vector3(1f, 0.2f, 1f);
            marker.GetComponent<Renderer>().material = zoneMaterial;
            marker.GetComponent<Collider>().enabled = false;

            // Trigger collider for occupancy detection
            GameObject trigger = new GameObject("Trigger");
            trigger.transform.parent = zone.transform;
            trigger.transform.position = center;
            var col = trigger.AddComponent<SphereCollider>();
            col.radius = mapConfig.zoneRadius;
            col.isTrigger = true;
            trigger.layer = LayerMask.NameToLayer("Zone");

            // Store zone ID for detection
            var zoneData = trigger.AddComponent<ZoneData>();
            zoneData.zoneId = zoneId;
            zoneData.zoneCenter = center;
            zoneData.zoneRadius = mapConfig.zoneRadius;
        }

        GameObject CreateRing(Vector3 center, float radius, float thickness)
        {
            GameObject ring = new GameObject("ZoneRing");
            var line = ring.AddComponent<LineRenderer>();
            line.sharedMaterial = zoneMaterial;
            line.startWidth = line.endWidth = 0.14f;
            line.loop = true;
            line.positionCount = 64;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0.04f, Mathf.Sin(angle) * radius));
            }
            return ring;
        }

        void GenerateSightlineBlockers()
        {
            // Central sightline blocker per spawn (Z = +/-20). 18 m wide so no line from any of the
            // three exits (X = -8..8) reaches the other team's exits (spec 9: no spawn-to-spawn shot).
            float blockerHeight = 5f;
            float blockerWidth = 18f;
            float blockerDepth = 2f;

            // North spawn blocker (between north spawn and center)
            CreateBlocker("SightlineBlocker_North", new Vector3(0, blockerHeight * 0.5f, mapConfig.sightlineBlockerZ), blockerWidth, blockerHeight, blockerDepth);

            // South spawn blocker
            CreateBlocker("SightlineBlocker_South", new Vector3(0, blockerHeight * 0.5f, -mapConfig.sightlineBlockerZ), blockerWidth, blockerHeight, blockerDepth);
        }

        void CreateBlocker(string name, Vector3 position, float width, float height, float depth)
        {
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = name;
            blocker.transform.parent = transform;
            blocker.transform.position = position;
            blocker.transform.localScale = new Vector3(width, height, depth);

            var renderer = blocker.GetComponent<Renderer>();
            if (coverMaterial != null) renderer.sharedMaterial = CladdingMaterial();

            blocker.layer = LayerMask.NameToLayer("Cover");
        }

        void GenerateOuterLaneBreakers()
        {
            // Short cover segments around each zone perimeter
            float breakerHeight = 2f;
            float breakerWidth = 3f;
            float breakerDepth = 1f;
            int segmentsPerZone = 4;

            Vector3[] zones = { mapConfig.centerZone, mapConfig.westZone, mapConfig.eastZone };

            foreach (Vector3 zoneCenter in zones)
            {
                for (int i = 0; i < segmentsPerZone; i++)
                {
                    float angle = (360f / segmentsPerZone) * i;
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * (mapConfig.zoneRadius + 3f);
                    Vector3 pos = zoneCenter + offset + Vector3.up * (breakerHeight * 0.5f);
                    Quaternion rot = Quaternion.Euler(0, angle + 90f, 0);

                    GameObject breaker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    breaker.name = $"OuterBreaker_{zoneCenter.x}_{zoneCenter.z}_{i}";
                    breaker.transform.parent = transform;
                    breaker.transform.position = pos;
                    breaker.transform.rotation = rot;
                    breaker.transform.localScale = new Vector3(breakerWidth, breakerHeight, breakerDepth);

                    var renderer = breaker.GetComponent<Renderer>();
                    if (coverMaterial != null) renderer.sharedMaterial = CladdingMaterial();

                    breaker.layer = LayerMask.NameToLayer("Cover");
                }
            }
        }
    }

    /// <summary>
    /// Data component for zone triggers.
    /// </summary>
    public class ZoneData : MonoBehaviour
    {
        public ZoneId zoneId;
        public Vector3 zoneCenter;
        public float zoneRadius;
    }

    /// <summary>Slow landmark motion only; has no gameplay, physics, or navigation role.</summary>
    public sealed class SwitchyardCraneRotor : MonoBehaviour
    {
        const float DegreesPerSecond = 4f;
        void Update() => transform.Rotate(Vector3.up, DegreesPerSecond * Time.deltaTime, Space.World);
    }
}


