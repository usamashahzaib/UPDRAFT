using UnityEngine;
using Updraft.Config;

namespace Updraft.Map
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
        [SerializeField] Material platformMaterial;
        [SerializeField] Material coverMaterial;
        [SerializeField] Material spawnBarrierMaterial;
        [SerializeField] Material zoneMaterial;
        [SerializeField] Material friendlyMaterial;
        [SerializeField] Material enemyMaterial;

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
            GenerateSpawnBarriers();
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
            if (groundMaterial != null) renderer.material = groundMaterial;

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
            floor.GetComponent<Renderer>().material = groundMaterial;

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
            // Each platform has two walkable ramps
            foreach (Vector3 platformCenter in mapConfig.platformCenters)
            {
                // Ramp 1: from ground to platform on +X side
                CreateRamp(new Vector3(platformCenter.x + mapConfig.platformSize.x * 0.5f + 7f, mapConfig.groundLevelY, platformCenter.z),
                    platformCenter + new Vector3(mapConfig.platformSize.x * 0.5f, 0, 0),
                    new Vector3(6f, mapConfig.platformHeight, 4f));

                // Ramp 2: from ground to platform on -Z side
                CreateRamp(new Vector3(platformCenter.x, mapConfig.groundLevelY, platformCenter.z - mapConfig.platformSize.y * 0.5f - 7f),
                    platformCenter + new Vector3(0, 0, -mapConfig.platformSize.y * 0.5f),
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
            // Central sightline blocker per spawn (Z = +/-20)
            float blockerHeight = 5f;
            float blockerWidth = 8f;
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
            if (coverMaterial != null) renderer.material = coverMaterial;

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
                    if (coverMaterial != null) renderer.material = coverMaterial;

                    breaker.layer = LayerMask.NameToLayer("Cover");
                }
            }
        }

        void GenerateSpawnBarriers()
        {
            // Barriers at spawn exits that block shots both ways
            // Enemies can't enter; barriers block shots both ways; can't fire out from inside
            // These are created as part of spawn room walls with SpawnBarrier layer
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
