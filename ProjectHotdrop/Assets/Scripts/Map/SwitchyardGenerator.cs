using UnityEngine;
using Hotdrop.Config;

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
        readonly System.Collections.Generic.List<Mesh> presentationMeshes = new System.Collections.Generic.List<Mesh>();

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
            SetupLighting();
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
            foreach (var mesh in presentationMeshes) if (mesh != null)
            { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            if (roofDeckMaterial != null)
            { if (Application.isPlaying) Destroy(roofDeckMaterial); else DestroyImmediate(roofDeckMaterial); }
            if (roofCladdingMaterial != null)
            { if (Application.isPlaying) Destroy(roofCladdingMaterial); else DestroyImmediate(roofCladdingMaterial); }
        }

        Material DeckMaterial()
        {
            var texture = Resources.Load<Texture2D>("Environment/RooftopConcrete");
            if (texture == null || groundMaterial == null) return groundMaterial;
            if (roofDeckMaterial != null) return roofDeckMaterial;
            roofDeckMaterial = new Material(groundMaterial) { name = "Weathered rooftop concrete" };
            roofDeckMaterial.color = new Color(.69f, .72f, .75f);
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
            RenderSettings.ambientLight = new Color(0.45f, 0.50f, 0.57f);
            if (FindFirstObjectByType<Light>() == null)
            {
                var sun = new GameObject("Switchyard sunlight").AddComponent<Light>();
                sun.transform.SetParent(transform, false);
                sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(50, -35, 0);
                sun.intensity = 0.9f;
                sun.shadows = LightShadows.None;
            }
            var camera = UnityEngine.Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.61f, 0.77f, 0.88f);
            }
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
}


