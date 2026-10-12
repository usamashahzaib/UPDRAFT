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
        Material spawnRoomFloorMaterial;
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
        SwitchyardAtmosphere atmosphere;
        readonly System.Collections.Generic.List<Mesh> presentationMeshes = new System.Collections.Generic.List<Mesh>();
        readonly SwitchyardInstancedRenderer instancedRenderer = new SwitchyardInstancedRenderer();
        MaterialPropertyBlock profileBlock;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        [Header("Prefabs")]
        [SerializeField] GameObject rampPrefab;
        [SerializeField] GameObject sightlineBlockerPrefab;
        bool generated;
        bool geometryOnlyGeneration;

        void Awake()
        {
            GenerateMap();
        }

        public void GenerateMap() => GenerateMapInternal(includePresentation: true);

        /// <summary>
        /// Builds only the static gameplay colliders needed by the Editor sightline audit.
        /// It does not create presentation objects, materials, lights, or profile subscriptions.
        /// </summary>
        public void GenerateMapForSightlineAudit()
        {
            if (Application.isPlaying)
                throw new System.InvalidOperationException("The Switchyard sightline audit only runs outside Play mode.");
            GenerateMapInternal(includePresentation: false);
        }

        void GenerateMapInternal(bool includePresentation)
        {
            if (Application.isPlaying && generated) return;
            // This generator only builds Switchyard, so a missing or differently named config must not
            // slip past the Switchyard-scoped validator. Nothing is mutated before these checks pass.
            if (mapConfig == null)
                throw new System.InvalidOperationException("SwitchyardGenerator requires a MapConfig.");
            if (!string.Equals(mapConfig.mapName, MapConfig.SwitchyardName, System.StringComparison.Ordinal))
                throw new System.InvalidOperationException($"SwitchyardGenerator only builds \"{MapConfig.SwitchyardName}\"; MapConfig.mapName is \"{mapConfig.mapName}\".");
            var movementConfig = Resources.Load<MovementConfig>("Config/MovementConfig");
            if (movementConfig == null)
                throw new System.InvalidOperationException("SwitchyardGenerator requires the MovementConfig resource for player-clearance validation.");
            string boundsError = mapConfig.ValidateBounds();
            if (boundsError != null)
                throw new System.InvalidOperationException("MapConfig bounds: " + boundsError);
            string objectiveZoneError = mapConfig.ValidateObjectiveZones();
            if (objectiveZoneError != null)
                throw new System.InvalidOperationException("MapConfig objective zones: " + objectiveZoneError);
            string spawnGeometryError = mapConfig.ValidateSpawnGeometry(MapConfig.SpawnRoomWallThicknessMeters);
            if (spawnGeometryError != null)
                throw new System.InvalidOperationException("MapConfig spawn geometry: " + spawnGeometryError);
            string spawnLayoutError = mapConfig.ValidateSwitchyardSpawnLayout();
            if (spawnLayoutError != null)
                throw new System.InvalidOperationException("MapConfig Switchyard spawn layout: " + spawnLayoutError);
            string sightlineBlockerError = mapConfig.ValidateSwitchyardSightlineBlockers();
            if (sightlineBlockerError != null)
                throw new System.InvalidOperationException("MapConfig Switchyard sightline blockers: " + sightlineBlockerError);
            string spawnCapsuleError = mapConfig.ValidateSwitchyardSpawnCapsuleClearance(movementConfig);
            if (spawnCapsuleError != null)
                throw new System.InvalidOperationException("MapConfig Switchyard spawn capsule clearance: " + spawnCapsuleError);
            string roofGeometryError = mapConfig.ValidateSwitchyardRoofGeometry(movementConfig);
            if (roofGeometryError != null)
                throw new System.InvalidOperationException("MapConfig Switchyard roof geometry: " + roofGeometryError);
            string spawnRoomClearanceError = mapConfig.ValidateSwitchyardSpawnRoomClearance();
            if (spawnRoomClearanceError != null)
                throw new System.InvalidOperationException("MapConfig Switchyard spawn-room clearance: " + spawnRoomClearanceError);
            bool previousGeometryOnly = geometryOnlyGeneration;
            geometryOnlyGeneration = !includePresentation;
            try
            {
                // Presentation helpers can populate the surfacing mesh cache before Apply runs.
                if (includePresentation && Application.isPlaying) SwitchyardSurfacing.Acquire(transform);
                generated = true;
                if (includePresentation) EnsureMaterials();
                if (includePresentation) instancedRenderer.Clear();
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
                if (includePresentation) GenerateZones();
                GenerateSightlineBlockers();
                GenerateOuterLaneBreakers();
                if (includePresentation)
                {
                    GenerateCenterPresentation();
                    GenerateRooftopPresentation();
                    GenerateLandmarkPresentation();
                    // CC0 PBR surfaces on generated blocks (07_WORLD_ART); render mesh/material only.
                    if (Application.isPlaying) SwitchyardSurfacing.Apply(transform);
                    if (Application.isPlaying) SwitchyardProps.Apply(transform, SwitchyardAtmosphere.Quality == 0);
                    SetupLighting();
                    ApplyWorldPresentationProfile();
                }
                if (Application.isPlaying)
                {
                    instancedRenderer.Build(transform);
                    var navigation = GetComponent<OfflineNavigation>();
                    if (navigation == null) navigation = gameObject.AddComponent<OfflineNavigation>();
                    navigation.Build();
                }
            }
            finally
            {
                geometryOnlyGeneration = previousGeometryOnly;
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
            // Spans below are centred on the configured rectangle, not the world origin.
            Vector2 mapMid = mapConfig.BoundsCenter;

            // Shallow seams establish a concrete module scale without creating steps.
            for (float x = west + 8f; x < east; x += 8f)
                PresentationPart(root.transform, "Deck seam X", PrimitiveType.Cube,
                    new Vector3(x, mapConfig.groundLevelY + 0.008f, mapMid.y),
                    new Vector3(0.045f, 0.008f, depth), presentationSeamMaterial);
            for (float z = south + 8f; z < north; z += 8f)
                PresentationPart(root.transform, "Deck seam Z", PrimitiveType.Cube,
                    new Vector3(mapMid.x, mapConfig.groundLevelY + 0.009f, z),
                    new Vector3(width, 0.008f, 0.045f), presentationSeamMaterial);

            // The parapet sits just outside the configured arena footprint. Its renderer has
            // no collider, so it cannot become new cover or change a sightline test.
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? west - 0.7f : east + 0.7f;
                float z = side == 0 ? south - 0.7f : north + 0.7f;
                PresentationPart(root.transform, "Roof parapet", PrimitiveType.Cube,
                    new Vector3(x, mapConfig.groundLevelY + 0.35f, mapMid.y), new Vector3(0.45f, 0.7f, depth + 2f), presentationConcreteMaterial);
                PresentationPart(root.transform, "Roof parapet", PrimitiveType.Cube,
                    new Vector3(mapMid.x, mapConfig.groundLevelY + 0.35f, z), new Vector3(width + 2f, 0.7f, 0.45f), presentationConcreteMaterial);
                PresentationPart(root.transform, "Parapet cap", PrimitiveType.Cube,
                    new Vector3(x, mapConfig.groundLevelY + 0.73f, mapMid.y), new Vector3(0.62f, 0.08f, depth + 2f), presentationDarkMaterial);
                PresentationPart(root.transform, "Parapet cap", PrimitiveType.Cube,
                    new Vector3(mapMid.x, mapConfig.groundLevelY + 0.73f, z), new Vector3(width + 2f, 0.08f, 0.62f), presentationDarkMaterial);
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
                {
                    PresentationPart(root.transform, "Roof edge zone trim " + zoneSections[i].id,
                        PrimitiveType.Cube, new Vector3((left + right) * .5f, mapConfig.groundLevelY + .775f, edgeZ),
                        new Vector3(stripWidth, .016f, .3f), trim);

                    int markCount = ZoneVisual.IdentityTickCount(zoneSections[i].id);
                    for (int mark = 0; mark < markCount; mark++)
                    {
                        float markOffset = (mark - (markCount - 1) * .5f) * .32f;
                        PresentationPart(root.transform,
                            "Roof edge zone identity mark " + zoneSections[i].id + " " + (mark + 1),
                            PrimitiveType.Cube,
                            new Vector3(zoneSections[i].x + markOffset, mapConfig.groundLevelY + .791f, edgeZ),
                            new Vector3(.18f, .022f, .28f), presentationDarkMaterial);
                    }
                }
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
                    new Vector3(x, facadeY, mapMid.y), new Vector3(4.5f, 24f, depth + 4.5f), presentationFacadeMaterial);
                PresentationPart(root.transform, "Tower facade", PrimitiveType.Cube,
                    new Vector3(mapMid.x, facadeY, z), new Vector3(width + 4.5f, 24f, 4.5f), presentationFacadeMaterial);
                for (float y = mapConfig.groundLevelY - 3f; y >= mapConfig.groundLevelY - 19f; y -= 4f)
                {
                    for (float span = -depth * .5f + 8f; span < depth * .5f - 4f; span += 12f)
                    {
                        float faceX = side == 0 ? west - 4.48f : east + 4.48f;
                        PresentationPart(root.transform, "Warm tower window", PrimitiveType.Cube,
                            new Vector3(faceX, y, mapMid.y + span), new Vector3(0.045f, 1.1f, 3.4f), presentationWindowMaterial);
                    }
                    for (float span = -width * .5f + 8f; span < width * .5f - 4f; span += 12f)
                    {
                        float faceZ = side == 0 ? south - 4.48f : north + 4.48f;
                        PresentationPart(root.transform, "Warm tower window", PrimitiveType.Cube,
                            new Vector3(mapMid.x + span, y, faceZ), new Vector3(3.4f, 1.1f, 0.045f), presentationWindowMaterial);
                    }
                }
            }

            // Placeholder dressing that the CC0 art pass (SwitchyardProps / the sky photo) replaces.
            bool cc0Art = SwitchyardProps.Available;

            // Repeated vent silhouettes on existing spawn roofs, well above players.
            if (!cc0Art)
            foreach (var spawn in new[] { mapConfig.northSpawnCenter, mapConfig.southSpawnCenter })
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 pos = spawn + new Vector3(i * 5f, mapConfig.groundLevelY + 3.95f, 0f);
                    PresentationPart(root.transform, "Roof service vent", PrimitiveType.Cube,
                        pos, new Vector3(2.8f, 0.9f, 2.2f), presentationDarkMaterial);
                    PresentationPart(root.transform, "Vent cover", PrimitiveType.Cube,
                        pos + Vector3.up * 0.49f, new Vector3(3.0f, 0.08f, 2.4f), presentationConcreteMaterial);
                }
            }

            // Fixed, deterministic distant buildings provide the coastal city scale seen in
            // the reference. They are beyond the arena and never enter the navigation mesh.
            for (int i = 0; i < (cc0Art ? 0 : 22); i++)
            {
                float x = mapMid.x - 96f + i * 9f;
                float height = 12f + ((i * 17) % 23);
                float z = i % 2 == 0 ? north + 55f : south - 68f;
                PresentationPart(root.transform, "Distant city block", PrimitiveType.Cube,
                    new Vector3(x, mapConfig.groundLevelY + height * 0.5f - 19f, z),
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
                    // With PBR cover surfaces the dark face panel only hid the metal plate.
                    if (!cc0Art)
                    {
                        var panel = PresentationPart(root.transform, "Equipment face", PrimitiveType.Cube,
                            t.TransformPoint(new Vector3(0, 0, side * .505f)),
                            new Vector3(t.localScale.x * .86f, t.localScale.y * .72f, .035f), presentationDarkMaterial);
                        panel.rotation = t.rotation;
                    }
                    var stripe = PresentationPart(root.transform, "Equipment safety band", PrimitiveType.Cube,
                        t.TransformPoint(new Vector3(0, .33f, side * .52f)),
                        new Vector3(t.localScale.x * .88f, .10f, .04f), presentationSafetyMaterial);
                    stripe.rotation = t.rotation;
                }
                if (cc0Art) AddFabricationTrim(root.transform, t, source.name.StartsWith("SightlineBlocker_"));
            }
            foreach (var platform in mapConfig.platformCenters)
            {
                PresentationPart(root.transform, "Platform landing stripe", PrimitiveType.Cube,
                    platform + new Vector3(0, .015f, 0),
                    new Vector3(mapConfig.platformSize.x - .5f, .018f, .18f), presentationSafetyMaterial);
            }
            if (cc0Art) AddLedgeHazardBands(root.transform);
            // Each material is one mesh rather than a draw call for every seam/panel/building.
            CombineDressing(root.transform);
        }

        /// <summary>
        /// Fabricated-equipment trim on a cover block (07_WORLD_ART): a low breaker gets container
        /// corner castings and top/bottom rails; a tall sightline wall gets frame posts every 3 m
        /// and a cap. Every piece hugs the block within 3 cm and has no collider, so the
        /// silhouette a player reads matches the collision they hit.
        /// </summary>
        void AddFabricationTrim(Transform parent, Transform block, bool wall)
        {
            Vector3 size = block.localScale;
            const float rail = .09f, proud = .03f;
            Transform Piece(string name, Vector3 local, Vector3 worldSize)
            {
                var piece = PresentationPart(parent, name, PrimitiveType.Cube, block.TransformPoint(local), worldSize, presentationDarkMaterial);
                piece.rotation = block.rotation;
                return piece;
            }
            Vector3 L(float x, float y, float z) => new Vector3(x / size.x, y / size.y, z / size.z);
            float hx = size.x * .5f, hy = size.y * .5f, hz = size.z * .5f;
            foreach (float y in new[] { -hy + rail * .5f, hy - rail * .5f })
            {
                Piece("Cover rail", L(0f, y, hz), new Vector3(size.x + proud, rail, proud * 2f));
                Piece("Cover rail", L(0f, y, -hz), new Vector3(size.x + proud, rail, proud * 2f));
                Piece("Cover rail", L(hx, y, 0f), new Vector3(proud * 2f, rail, size.z + proud));
                Piece("Cover rail", L(-hx, y, 0f), new Vector3(proud * 2f, rail, size.z + proud));
            }
            if (wall)
            {
                int posts = Mathf.Max(2, Mathf.RoundToInt(size.x / 3f) + 1);
                for (int i = 0; i < posts; i++)
                {
                    float x = Mathf.Lerp(-hx, hx, i / (float)(posts - 1));
                    Piece("Wall frame post", L(x, 0f, hz), new Vector3(.14f, size.y, proud * 2f));
                    Piece("Wall frame post", L(x, 0f, -hz), new Vector3(.14f, size.y, proud * 2f));
                }
                return;
            }
            foreach (float x in new[] { -hx, hx })
            foreach (float y in new[] { -hy, hy })
            foreach (float z in new[] { -hz, hz })
                Piece("Corner casting", L(x - Mathf.Sign(x) * .07f, y - Mathf.Sign(y) * .07f, z - Mathf.Sign(z) * .07f), new Vector3(.17f, .17f, .17f));
        }

        /// <summary>
        /// Worn hazard paint along raised-platform edges and ramp sides (07_WORLD_ART). Flat bands
        /// 2 cm proud of the walking surface with no collider: they mark a ledge without changing it.
        /// </summary>
        void AddLedgeHazardBands(Transform parent)
        {
            var hazard = Resources.Load<Material>(SwitchyardSurfacing.HazardMaterial);
            if (hazard == null) return;
            Transform Band(Vector3 position, Quaternion rotation, Vector3 size)
            {
                var band = PresentationPart(parent, "Ledge hazard band", PrimitiveType.Cube, position, size, hazard);
                band.rotation = rotation;
                band.GetComponent<MeshFilter>().sharedMesh = SwitchyardSurfacing.MetricBox(size, 1f);
                return band;
            }
            const float width = .22f, thick = .02f;
            foreach (var platform in mapConfig.platformCenters)
            {
                var size = mapConfig.platformSize;
                float y = platform.y + thick * .5f + .002f;
                foreach (float side in new[] { -1f, 1f })
                {
                    Band(new Vector3(platform.x, y, platform.z + side * (size.y * .5f - width * .5f)), Quaternion.identity, new Vector3(size.x, thick, width));
                    Band(new Vector3(platform.x + side * (size.x * .5f - width * .5f), y, platform.z), Quaternion.Euler(0f, 90f, 0f), new Vector3(size.y - 2f * width, thick, width));
                }
            }
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name != "Ramp") continue;
                var t = renderer.transform;
                foreach (float side in new[] { -1f, 1f })
                    Band(t.TransformPoint(new Vector3(side * (.5f - width * .5f / t.lossyScale.x), .5f + (thick * .5f + .002f) / t.lossyScale.y, 0f)),
                        t.rotation * Quaternion.Euler(0f, 90f, 0f), new Vector3(t.lossyScale.z, thick, width));
            }
        }

        /// <summary>
        /// Triangular lattice relay mast (07_WORLD_ART), collider-free and inside a 0.45 m footprint
        /// so it cannot hide a player standing in the objective. Two emissive rings near the top
        /// give Center a beacon that reads from both spawns and blooms on the rich tiers.
        /// </summary>
        void BuildLatticeMast(Transform parent, Vector3 center)
        {
            const float radius = .22f, height = 8f;
            var corners = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                corners[i] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                PresentationPart(parent, "Mast post", PrimitiveType.Cube,
                    center + corners[i] + Vector3.up * height * .5f, new Vector3(.05f, height, .05f), presentationDarkMaterial);
            }
            for (float y = .5f; y < height - .2f; y += 1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    var from = corners[i] + Vector3.up * y;
                    var to = corners[(i + 1) % 3] + Vector3.up * (y + (i % 2 == 0 ? .9f : 0f));
                    var brace = PresentationPart(parent, "Mast brace", PrimitiveType.Cube,
                        center + (from + to) * .5f, new Vector3(.025f, .025f, (to - from).magnitude), presentationDarkMaterial);
                    brace.rotation = Quaternion.LookRotation(to - from);
                }
            }
            foreach (float y in new[] { 7.3f, 7.75f })
                PresentationPart(parent, "Mast beacon ring", PrimitiveType.Cylinder,
                    center + Vector3.up * y, new Vector3(radius * 2.4f, .03f, radius * 2.4f), BeaconMaterial());
            PresentationPart(parent, "Mast base housing", PrimitiveType.Cube,
                center + Vector3.up * .3f, new Vector3(.45f, .6f, .45f), presentationDarkMaterial);
        }

        Material beaconMaterial;

        /// <summary>Emissive objective gold for the relay beacon; HDR so it blooms on the rich tiers.</summary>
        Material BeaconMaterial()
        {
            if (beaconMaterial != null) return beaconMaterial;
            if (presentationSafetyMaterial == null || !Application.isPlaying) return presentationSafetyMaterial;
            beaconMaterial = new Material(presentationSafetyMaterial) { name = "Switchyard objective beacon (emissive)" };
            beaconMaterial.EnableKeyword("_EMISSION");
            beaconMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (beaconMaterial.HasProperty("_EmissionColor")) beaconMaterial.SetColor("_EmissionColor", new Color(1f, .72f, .25f) * 3.5f);
            return beaconMaterial;
        }

        Material ZoneTrimMaterial(ZoneId zone) => zone switch
        {
            ZoneId.West => presentationZoneWestMaterial,
            ZoneId.East => presentationZoneEastMaterial,
            _ => presentationZoneCenterMaterial,
        };

        void CombineDressing(Transform root, UnityEngine.Rendering.ShadowCastingMode shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On)
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
                var mesh = new Mesh { name = "Batched presentation " + group.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray()); presentationMeshes.Add(mesh);
                var obj = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(root, false);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = group.Key;
                // Rooftop dressing casts shadows; distant presentation landmarks can opt out.
                renderer.shadowCastingMode = shadowCastingMode;
            }
        }

        void LateUpdate() => instancedRenderer.RenderFrame();

        void OnDestroy()
        {
            UiPresentationProfile.Changed -= ApplyWorldPresentationProfile;
            instancedRenderer.Clear();
            SwitchyardSurfacing.Release(transform);
            foreach (var mesh in presentationMeshes) if (mesh != null)
            { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            if (roofDeckMaterial != null)
            { if (Application.isPlaying) Destroy(roofDeckMaterial); else DestroyImmediate(roofDeckMaterial); }
            if (spawnRoomFloorMaterial != null)
            { if (Application.isPlaying) Destroy(spawnRoomFloorMaterial); else DestroyImmediate(spawnRoomFloorMaterial); }
            if (roofCladdingMaterial != null)
            { if (Application.isPlaying) Destroy(roofCladdingMaterial); else DestroyImmediate(roofCladdingMaterial); }
            if (presentationGlassMaterial != null)
            { if (Application.isPlaying) Destroy(presentationGlassMaterial); else DestroyImmediate(presentationGlassMaterial); }
            if (beaconMaterial != null) Destroy(beaconMaterial);
        }

        Material DeckMaterial()
        {
            // Keep the large arena floor on the standalone repeating texture. Its 6x5
            // footprint is the worst case for the atlas's 64 px gutters at distant mips.
            var surface = Resources.Load<Material>("Materials/Surface_Structure");
            if (surface == null && groundMaterial != null)
                Debug.LogWarning("Switchyard: Materials/Surface_Structure is missing; the arena ground falls back to the flat graybox material.");
            if (surface == null || groundMaterial == null) return groundMaterial;
            if (roofDeckMaterial != null) return roofDeckMaterial;
            roofDeckMaterial = new Material(surface) { name = "Surface_Rooftop Concrete" };
            SetSurfaceTiling(roofDeckMaterial, new Vector2(6f, 5f));
            return roofDeckMaterial;
        }

        Material SpawnRoomFloorMaterial(Vector3 size)
        {
            if (geometryOnlyGeneration) return null;
            if (spawnRoomFloorMaterial != null) return spawnRoomFloorMaterial;
            bool atlas = Application.isPlaying && SwitchyardSurfacing.HasCompleteAtlasSet;
            var surface = atlas
                ? Resources.Load<Material>("Materials/Surface_Structure_Atlas")
                : Resources.Load<Material>("Materials/Surface_Structure");
            if (surface == null) return DeckMaterial();
            spawnRoomFloorMaterial = new Material(surface) { name = "Surface_Rooftop Concrete Spawn Floor" };
            // Atlas materials already carry the packed region transform. Source materials use
            // unit scale with metric render meshes at runtime. In edit mode the primitive cube
            // remains in place, so its material carries the same physical repeat directly.
            if (!atlas)
            {
                var tiling = Application.isPlaying
                    ? Vector2.one
                    : new Vector2(size.x / SwitchyardSurfacing.StructureTile, size.z / SwitchyardSurfacing.StructureTile);
                SetSurfaceTiling(spawnRoomFloorMaterial, tiling);
            }
            return spawnRoomFloorMaterial;
        }

        Material CladdingMaterial()
        {
            var surface = Application.isPlaying && SwitchyardSurfacing.HasCompleteAtlasSet
                ? Resources.Load<Material>("Materials/Surface_Wall_Atlas")
                : null;
            if (surface == null) surface = Resources.Load<Material>("Materials/Surface_Wall");
            if (roofCladdingMaterial != null) return roofCladdingMaterial;
            var source = surface != null ? surface : coverMaterial;
            if (source == null) return coverMaterial;
            roofCladdingMaterial = new Material(source) { name = "Surface_Rooftop Cladding" };
            return roofCladdingMaterial;
        }

        static void SetSurfaceTiling(Material material, Vector2 tiling)
        {
            foreach (var property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
            {
                if (material.HasProperty(property)) material.SetTextureScale(property, tiling);
            }
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

            if (SwitchyardProps.Available) BuildLatticeMast(root.transform, center);
            else
                PresentationPart(root.transform, "Tower mast", PrimitiveType.Cylinder,
                    center + Vector3.up * 4.0f, new Vector3(0.22f, 4f, 0.22f), presentationDarkMaterial);
            PresentationPart(root.transform, "Signal cap", PrimitiveType.Sphere,
                center + Vector3.up * 8.05f, new Vector3(0.5f, 0.5f, 0.5f), BeaconMaterial());

            // Four suspended antenna arms make Center identifiable from both spawn sides.
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                Vector3 radial = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var arm = PresentationPart(root.transform, "Antenna arm", PrimitiveType.Cube,
                    center + Vector3.up * 6.7f + radial * 1.05f, new Vector3(0.12f, 0.12f, 2.1f), presentationDarkMaterial);
                arm.rotation = Quaternion.Euler(0f, angle, 0f);
                PresentationPart(root.transform, "Antenna beacon", PrimitiveType.Sphere,
                    center + Vector3.up * 6.7f + radial * 2.05f, new Vector3(0.25f, 0.25f, 0.25f), BeaconMaterial());
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
            // The jib's right tip extends 14 m from the mast; keep its whole silhouette outside the map edge.
            float craneX = west - 15f;
            var craneRoot = new GameObject("Rotating tower crane landmark (visual only)").transform;
            craneRoot.SetParent(transform, false);
            PresentationPart(craneRoot, "Crane mast", PrimitiveType.Cylinder,
                new Vector3(craneX, ground + 6f, centerZ), new Vector3(.5f, 6f, .5f), presentationDarkMaterial);
            PresentationPart(craneRoot, "Crane mast safety band", PrimitiveType.Cube,
                new Vector3(craneX, ground + 3.5f, centerZ), new Vector3(.65f, .22f, .65f), presentationSafetyMaterial);
            var rotor = new GameObject("Crane rotating boom").transform;
            rotor.SetParent(craneRoot, false);
            rotor.position = new Vector3(craneX, ground + 12f, centerZ);
            const float craneBoomLength = 22f;
            const float craneBoomCenter = 3f;
            const float craneBoomHalfHeight = .22f;
            const int craneTrussSections = 8;
            float craneSectionWidth = craneBoomLength / craneTrussSections;
            PresentationPart(rotor, "Crane boom top chord", PrimitiveType.Cube,
                rotor.position + new Vector3(craneBoomCenter, craneBoomHalfHeight, 0f),
                new Vector3(craneBoomLength, .14f, .14f), presentationSafetyMaterial);
            PresentationPart(rotor, "Crane boom bottom chord", PrimitiveType.Cube,
                rotor.position + new Vector3(craneBoomCenter, -craneBoomHalfHeight, 0f),
                new Vector3(craneBoomLength, .14f, .14f), presentationSafetyMaterial);
            for (int side = -1; side <= 1; side += 2)
            {
                float trussZ = side * .18f;
                for (int section = 0; section <= craneTrussSections; section++)
                {
                    float x = craneBoomCenter - craneBoomLength * .5f + craneSectionWidth * section;
                    PresentationPart(rotor, "Crane boom truss upright", PrimitiveType.Cube,
                        rotor.position + new Vector3(x, 0f, trussZ), new Vector3(.1f, .44f, .08f), presentationDarkMaterial);
                }
                for (int section = 0; section < craneTrussSections; section++)
                {
                    float x = craneBoomCenter - craneBoomLength * .5f + craneSectionWidth * (section + .5f);
                    float angle = Mathf.Atan2(craneBoomHalfHeight * 2f, craneSectionWidth) * Mathf.Rad2Deg;
                    if (section % 2 == 0) angle = -angle;
                    var brace = PresentationPart(rotor, "Crane boom truss diagonal", PrimitiveType.Cube,
                        rotor.position + new Vector3(x, 0f, trussZ),
                        new Vector3(Mathf.Sqrt(craneSectionWidth * craneSectionWidth + craneBoomHalfHeight * craneBoomHalfHeight * 4f), .08f, .08f),
                        presentationDarkMaterial);
                    brace.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
            PresentationPart(rotor, "Crane counterweight", PrimitiveType.Cube,
                rotor.position + Vector3.left * 7f, new Vector3(3f, 1.2f, 1.1f), presentationDarkMaterial);
            PresentationPart(rotor, "Crane hanging cable", PrimitiveType.Cylinder,
                rotor.position + Vector3.right * 14f + Vector3.down * 2.2f, new Vector3(.035f, 2.2f, .035f), presentationDarkMaterial);
            PresentationPart(rotor, "Crane hook", PrimitiveType.Cube,
                rotor.position + Vector3.right * 14f + Vector3.down * 4.5f, new Vector3(.5f, .16f, .16f), presentationSafetyMaterial);
            CombineDressing(rotor, UnityEngine.Rendering.ShadowCastingMode.Off);
            rotor.gameObject.AddComponent<SwitchyardCraneRotor>();

            // Translucent skybridge silhouette stays beyond the north map boundary.
            float bridgeZ = north + 9f;
            float bridgeY = ground + 10f;
            float bridgeWidth = east - west + 20f;
            float supportHeight = bridgeY - ground - .11f;
            var bridgeRoot = new GameObject("Enclosed glass skybridge landmark (visual only)").transform;
            bridgeRoot.SetParent(transform, false);
            foreach (float x in new[] { west - 6f, east + 6f })
                PresentationPart(bridgeRoot, "Skybridge support (visual only)", PrimitiveType.Cube,
                    new Vector3(x, ground + supportHeight * .5f, bridgeZ), new Vector3(1.2f, supportHeight, 2f), presentationDarkMaterial);
            PresentationPart(bridgeRoot, "Glass skybridge deck (visual only)", PrimitiveType.Cube,
                new Vector3((west + east) * .5f, bridgeY, bridgeZ), new Vector3(bridgeWidth, .22f, 2.6f), presentationGlassMaterial);
            float paneWidth = bridgeWidth / 7f - .18f;
            float sideOffset = 1.18f;
            for (int i = 0; i < 7; i++)
            {
                float x = west - 10f + bridgeWidth * (i + .5f) / 7f;
                foreach (float side in new[] { -1f, 1f })
                {
                    float z = bridgeZ + side * sideOffset;
                    PresentationPart(bridgeRoot, "Glass skybridge side panel (visual only)", PrimitiveType.Cube,
                        new Vector3(x, bridgeY + 1.6f, z), new Vector3(paneWidth, 2.8f, .12f), presentationGlassMaterial);
                }
            }
            for (int i = 0; i <= 7; i++)
            {
                float x = west - 10f + bridgeWidth * i / 7f;
                foreach (float side in new[] { -1f, 1f })
                {
                    float z = bridgeZ + side * sideOffset;
                    PresentationPart(bridgeRoot, "Skybridge side mullion", PrimitiveType.Cube,
                        new Vector3(x, bridgeY + 1.6f, z), new Vector3(.16f, 3f, .18f), presentationDarkMaterial);
                }
            }
            foreach (float side in new[] { -1f, 1f })
            {
                float z = bridgeZ + side * sideOffset;
                PresentationPart(bridgeRoot, "Skybridge top rail", PrimitiveType.Cube,
                    new Vector3((west + east) * .5f, bridgeY + 3.08f, z), new Vector3(bridgeWidth, .16f, .2f), presentationDarkMaterial);
            }
            CombineDressing(bridgeRoot, UnityEngine.Rendering.ShadowCastingMode.Off);

            // Sign faces the arena. The lettering is a temporary TMP prototype, not final art.
            var signCenter = new Vector3((west + east) * .5f, bridgeY + 1.7f, bridgeZ - 1.45f);
            PresentationPart(bridgeRoot, "SWITCHYARD sign panel", PrimitiveType.Cube,
                signCenter, new Vector3(24f, 3.4f, .22f), presentationDarkMaterial);
            var sign = new GameObject("SWITCHYARD neon landmark");
            sign.transform.SetParent(bridgeRoot, false);
            sign.transform.position = signCenter + Vector3.back * .13f;
            sign.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var text = sign.AddComponent<TextMeshPro>();
            text.text = UiDraw.L("world.sign.switchyard", "SWITCHYARD");
            text.fontSize = 3.1f;
            text.alignment = TextAlignmentOptions.Center;
            ApplyNeonLandmarkStyle(text, UiPresentationProfile.Palette.WorldWindow);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = new Vector2(23f, 3.2f);
            var signRenderer = sign.GetComponent<MeshRenderer>();
            if (signRenderer != null) signRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void ApplyNeonLandmarkStyle(TMP_Text text, Color color)
        {
            if (text == null) return;
            text.color = color;
            var material = text.fontMaterial;
            if (material == null || !material.HasProperty("_GlowColor")) return;
            color.a = .8f;
            material.SetColor("_GlowColor", color);
            if (material.HasProperty("_GlowInner")) material.SetFloat("_GlowInner", 0f);
            if (material.HasProperty("_GlowOuter")) material.SetFloat("_GlowOuter", .18f);
            if (material.HasProperty("_GlowPower")) material.SetFloat("_GlowPower", .75f);
            material.EnableKeyword("GLOW_ON");
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
            renderer.receiveShadows = true;
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
            // Sky, ambient, fog, shadows and grading have one owner (07_WORLD_ART).
            // Runtime only: an edit-mode bake must not serialize a runtime volume profile into the scene.
            if (!Application.isPlaying) return;
            if (atmosphere == null) atmosphere = GetComponent<SwitchyardAtmosphere>();
            if (atmosphere == null) atmosphere = gameObject.AddComponent<SwitchyardAtmosphere>();
            atmosphere.Initialize(presentationSun);
        }

        void ApplyWorldPresentationProfile()
        {
            var palette = UiPresentationProfile.Palette;
            if (presentationSun != null)
            {
                presentationSun.color = palette.WorldSun;
                presentationSun.intensity = palette.WorldSunIntensity;
            }
            if (atmosphere != null) atmosphere.Apply();

            if (profileBlock == null) profileBlock = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                string name = material.name.ToLowerInvariant();
                bool propRenderer = SwitchyardProps.IsPropRenderer(transform, renderer);
                Color tint;
                if (propRenderer && name.Contains("glass"))
                {
                    tint = palette.WorldWindow;
                    int colorId = material.HasProperty(BaseColorId) ? BaseColorId
                        : material.HasProperty(LegacyColorId) ? LegacyColorId : 0;
                    if (colorId != 0) tint.a = material.GetColor(colorId).a;
                }
                else if (propRenderer) tint = palette.WorldEquipment;
                else if (name.Contains("friendly")) tint = palette.Ally;
                else if (name.Contains("enemy")) tint = palette.Enemy;
                else if (name.Contains("zonewest")) tint = palette.WorldZoneWest;
                else if (name.Contains("zonecenter")) tint = palette.WorldZoneCenter;
                else if (name.Contains("zoneeast")) tint = palette.WorldZoneEast;
                else if (name.Contains("objective") || name.Contains("safety")) tint = palette.WorldSafety;
                else if (name.Contains("landmark glass")) { tint = palette.WorldWindow; tint.a = .28f; }
                else if (name.Contains("window")) tint = palette.WorldWindow;
                else if (name.Contains("equipment")) tint = palette.WorldEquipment;
                else if (name.Contains("facade")) tint = palette.WorldFacade;
                else if (name.Contains("wall") || name.Contains("cladding")) tint = palette.WorldFacade;
                else if (name.Contains("cover")) tint = palette.WorldCover;
                else if (name.Contains("concrete") || name.Contains("roof")) tint = palette.WorldConcrete;
                else if (name.Contains("structure") || name.Contains("seam") || name.Contains("deck")) tint = palette.WorldStructure;
                else continue;
                // Textured PBR surfaces carry their own value; a full tint would only darken them.
                if (name.StartsWith("surface") || propRenderer)
                {
                    float alpha = tint.a;
                    tint = Color.Lerp(Color.white, tint, .45f);
                    tint.a = alpha;
                }

                if (name.Contains("(gpu instanced surfaces)") || name.Contains("(gpu instanced props)"))
                {
                    // Repeated atlas surface groups share one material and one profile tint,
                    // keeping URP/Lit material values uniform for GPU instancing.
                    if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, tint);
                }
                else
                {
                    renderer.GetPropertyBlock(profileBlock);
                    if (material.HasProperty(BaseColorId)) profileBlock.SetColor(BaseColorId, tint);
                    if (material.HasProperty(LegacyColorId)) profileBlock.SetColor(LegacyColorId, tint);
                    renderer.SetPropertyBlock(profileBlock);
                }
                profileBlock.Clear();
            }
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name.Contains("SWITCHYARD neon")) ApplyNeonLandmarkStyle(text, palette.WorldWindow);
        }

        void GenerateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.parent = transform;
            Vector2 boundsCenter = mapConfig.BoundsCenter;
            ground.transform.position = new Vector3(boundsCenter.x, mapConfig.groundLevelY - 0.25f, boundsCenter.y);
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
            CreateSpawnRoom("NorthSpawnRoom", mapConfig.northSpawnCenter, 0);

            // South spawn room (Team B)
            CreateSpawnRoom("SouthSpawnRoom", mapConfig.southSpawnCenter, 1);
        }

        void CreateSpawnRoom(string name, Vector3 center, int teamId)
        {
            if (!mapConfig.TryGetSwitchyardSpawnRoomGeometry(teamId, out SwitchyardSpawnBoxGeometry[] boxes, out _))
                throw new System.InvalidOperationException($"Spawn-room geometry for team {teamId} is invalid.");
            GameObject room = new GameObject(name);
            room.transform.parent = transform;
            Material teamMaterial = teamId == 0 ? friendlyMaterial : enemyMaterial;
            foreach (SwitchyardSpawnBoxGeometry geometry in boxes)
            {
                Material material = geometry.Role switch
                {
                    SwitchyardSpawnBoxRole.Floor => SpawnRoomFloorMaterial(geometry.Size),
                    SwitchyardSpawnBoxRole.TeamStripe => teamMaterial,
                    SwitchyardSpawnBoxRole.ExitHeader => teamMaterial,
                    _ => platformMaterial,
                };
                GameObject box;
                if (geometry.Role == SwitchyardSpawnBoxRole.Floor)
                {
                    box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = geometry.Name;
                    box.transform.SetParent(room.transform, false);
                    box.transform.localPosition = geometry.LocalPosition;
                    box.transform.localScale = geometry.Size;
                    box.GetComponent<Renderer>().sharedMaterial = material;
                }
                else box = CreateSpawnBox(room, geometry.Name, geometry.LocalPosition, geometry.Size, material);

                if (geometry.Role == SwitchyardSpawnBoxRole.TeamStripe)
                {
                    var stripeCollider = box.GetComponent<Collider>();
                    if (Application.isPlaying) Destroy(stripeCollider); else DestroyImmediate(stripeCollider);
                }
                else if (geometry.Role == SwitchyardSpawnBoxRole.ExitBarrier)
                {
                    box.GetComponent<Renderer>().enabled = false;
                    if (!geometryOnlyGeneration) box.AddComponent<SpawnExitBarrier>().OwnerTeam = teamId;
                }
            }
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
                if (!mapConfig.TryGetSwitchyardDeckGeometry(center, out Vector3 position, out Vector3 size, out _))
                    throw new System.InvalidOperationException($"Platform geometry for platformCenters[{i}] is invalid.");
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = $"Platform_{i}";
                platform.transform.parent = transform;
                platform.transform.position = position;
                platform.transform.localScale = size;

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
                // Ramp 1: X side facing the centre line
                CreateRamp(platformCenter, alongX: true);

                // Ramp 2: Z side facing midfield
                CreateRamp(platformCenter, alongX: false);
            }
        }

        void CreateRamp(Vector3 platformCenter, bool alongX)
        {
            if (!mapConfig.TryGetSwitchyardRampGeometry(platformCenter, alongX, out _, out _,
                    out Vector3 position, out Quaternion rotation, out Vector3 size, out _))
                throw new System.InvalidOperationException($"Ramp geometry for platform center {platformCenter} is invalid.");
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.parent = transform;
            ramp.transform.position = position;
            ramp.transform.localScale = size;
            ramp.transform.rotation = rotation;

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
            marker.transform.localScale = new Vector3(MapConfig.SwitchyardZoneCenterMarkerDiameterMeters, 0.2f,
                MapConfig.SwitchyardZoneCenterMarkerDiameterMeters);
            if (zoneMaterial != null) marker.GetComponent<Renderer>().material = zoneMaterial;
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
            line.startWidth = line.endWidth = MapConfig.SwitchyardZoneRingLineWidthMeters;
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
            // Central sightline blocker per spawn (Z = +/-20). It covers each exit opening interval
            // with a margin inside its 18 m width (spec 9: no spawn-to-spawn shot).
            // North spawn blocker (between north spawn and center)
            if (!mapConfig.TryGetSwitchyardSightlineBlockerGeometry(0, out Vector3 northPosition, out Vector3 northSize, out _))
                throw new System.InvalidOperationException("North sightline-blocker geometry is invalid.");
            CreateBlocker("SightlineBlocker_North", northPosition, northSize);

            // South spawn blocker
            if (!mapConfig.TryGetSwitchyardSightlineBlockerGeometry(1, out Vector3 southPosition, out Vector3 southSize, out _))
                throw new System.InvalidOperationException("South sightline-blocker geometry is invalid.");
            CreateBlocker("SightlineBlocker_South", southPosition, southSize);
        }

        void CreateBlocker(string name, Vector3 position, Vector3 size)
        {
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = name;
            blocker.transform.parent = transform;
            blocker.transform.position = position;
            blocker.transform.localScale = size;

            var renderer = blocker.GetComponent<Renderer>();
            if (coverMaterial != null) renderer.sharedMaterial = CladdingMaterial();

            blocker.layer = LayerMask.NameToLayer("Cover");
        }

        void GenerateOuterLaneBreakers()
        {
            // Short cover segments around each zone perimeter
            float breakerHeight = MapConfig.OuterLaneBreakerHeightMeters;
            float breakerWidth = MapConfig.OuterLaneBreakerWidthMeters;
            float breakerDepth = MapConfig.OuterLaneBreakerDepthMeters;
            int segmentsPerZone = MapConfig.SwitchyardOuterBreakersPerZone;

            Vector3[] zones = { mapConfig.centerZone, mapConfig.westZone, mapConfig.eastZone };

            foreach (Vector3 zoneCenter in zones)
            {
                for (int i = 0; i < segmentsPerZone; i++)
                {
                    if (!mapConfig.TryGetSwitchyardOuterLaneBreakerGeometry(zoneCenter, i,
                            out Vector3 pos, out Quaternion rot, out _))
                        throw new System.InvalidOperationException($"Outer breaker geometry for zone {zoneCenter} segment {i} is invalid.");

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


