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

        void Awake()
        {
            GenerateMap();
        }

        public void GenerateMap()
        {
            // Clear existing children
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            GenerateGround();
            GenerateSpawnRooms();
            GeneratePlatforms();
            GenerateRamps();
            GenerateZones();
            GenerateSightlineBlockers();
            GenerateOuterLaneBreakers();
            GenerateSpawnBarriers();
        }

        void GenerateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.parent = transform;
            ground.transform.position = new Vector3(0, mapConfig.groundLevelY, 0);
            ground.transform.localScale = new Vector3(
                (mapConfig.boundsMax.x - mapConfig.boundsMin.x) / 10f, // Plane is 10x10 by default
                1f,
                (mapConfig.boundsMax.y - mapConfig.boundsMin.y) / 10f
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
            floor.transform.localPosition = new Vector3(0, mapConfig.groundLevelY + 0.25f, 0);
            floor.transform.localScale = new Vector3(size.x, 0.5f, size.y);
            var floorRenderer = floor.GetComponent<Renderer>();
            floorRenderer.material = teamId == 0 ? friendlyMaterial : enemyMaterial;

            // Walls (3 exits = 3 gaps in perimeter)
            float wallHeight = 3f;
            float wallThickness = 0.5f;
            float exitWidth = 4f; // per spec: main routes >= 4m wide
            int exitsPerSpawn = mapConfig.exitsPerSpawn;

            // Calculate wall segments
            float perimeter = 2 * (size.x + size.y);
            float exitTotalWidth = exitsPerSpawn * exitWidth;
            float wallTotalWidth = perimeter - exitTotalWidth;
            float segmentWidth = wallTotalWidth / (exitsPerSpawn * 2); // walls between exits

            // Build walls along perimeter
            Vector3[] corners = new Vector3[4]
            {
                new Vector3(-size.x * 0.5f, 0, -size.y * 0.5f),
                new Vector3(size.x * 0.5f, 0, -size.y * 0.5f),
                new Vector3(size.x * 0.5f, 0, size.y * 0.5f),
                new Vector3(-size.x * 0.5f, 0, size.y * 0.5f)
            };

            // Create wall segments with gaps for exits
            for (int side = 0; side < 4; side++)
            {
                Vector3 start = corners[side];
                Vector3 end = corners[(side + 1) % 4];
                float sideLength = Vector3.Distance(start, end);
                Vector3 direction = (end - start).normalized;

                // Place exits evenly along this side
                int exitsOnThisSide = exitsPerSpawn / 4 + (side < (exitsPerSpawn % 4) ? 1 : 0);

                float currentPos = 0f;
                for (int e = 0; e <= exitsOnThisSide; e++)
                {
                    float exitStart = (sideLength / (exitsOnThisSide + 1)) * (e + 0.5f) - exitWidth * 0.5f;
                    float wallStart = currentPos;
                    float wallEnd = exitStart;

                    if (wallEnd > wallStart)
                    {
                        CreateWallSegment(room, start + direction * wallStart, direction, wallEnd - wallStart, wallHeight, wallThickness, teamId);
                    }

                    currentPos = exitStart + exitWidth;
                }

                // Final wall segment
                if (currentPos < sideLength)
                {
                    CreateWallSegment(room, start + direction * currentPos, direction, sideLength - currentPos, wallHeight, wallThickness, teamId);
                }
            }

            // Roof (optional, for spawn protection cover)
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.parent = room.transform;
            roof.transform.localPosition = new Vector3(0, wallHeight + 0.25f, 0);
            roof.transform.localScale = new Vector3(size.x, 0.5f, size.y);
            var roofRenderer = roof.GetComponent<Renderer>();
            roofRenderer.material = teamId == 0 ? friendlyMaterial : enemyMaterial;

            room.transform.position = center;
        }

        void CreateWallSegment(GameObject parent, Vector3 start, Vector3 direction, float length, float height, float thickness, int teamId)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "WallSegment";
            wall.transform.parent = parent.transform;
            wall.transform.localPosition = start + direction * (length * 0.5f) + Vector3.up * (height * 0.5f);
            wall.transform.rotation = Quaternion.LookRotation(direction);
            wall.transform.localScale = new Vector3(thickness, height, length);

            var renderer = wall.GetComponent<Renderer>();
            renderer.material = teamId == 0 ? friendlyMaterial : enemyMaterial;

            // Spawn barriers block shots both ways per spec
            wall.layer = LayerMask.NameToLayer("SpawnBarrier");
        }

        void GeneratePlatforms()
        {
            for (int i = 0; i < mapConfig.platformCenters.Length; i++)
            {
                Vector3 center = mapConfig.platformCenters[i];
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = $"Platform_{i}";
                platform.transform.parent = transform;
                platform.transform.position = center;
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
                CreateRamp(platformCenter + new Vector3(mapConfig.platformSize.x * 0.5f + 3f, 0, 0),
                    platformCenter + new Vector3(mapConfig.platformSize.x * 0.5f, mapConfig.platformHeight * 0.5f, 0),
                    new Vector3(6f, mapConfig.platformHeight, 4f));

                // Ramp 2: from ground to platform on -Z side
                CreateRamp(platformCenter + new Vector3(0, 0, -mapConfig.platformSize.y * 0.5f - 3f),
                    platformCenter + new Vector3(0, mapConfig.platformHeight * 0.5f, -mapConfig.platformSize.y * 0.5f),
                    new Vector3(4f, mapConfig.platformHeight, 6f));
            }
        }

        void CreateRamp(Vector3 groundPos, Vector3 platformEdge, Vector3 size)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.parent = transform;
            ramp.transform.position = (groundPos + platformEdge) * 0.5f;
            ramp.transform.localScale = size;

            // Rotate to slope
            Vector3 dir = (platformEdge - groundPos).normalized;
            float angle = Mathf.Atan2(platformEdge.y - groundPos.y, Vector3.Distance(new Vector3(groundPos.x, 0, groundPos.z), new Vector3(platformEdge.x, 0, platformEdge.z))) * Mathf.Rad2Deg;
            ramp.transform.rotation = Quaternion.Euler(-angle, 0, 0);

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

            // Zone center marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "CenterMarker";
            marker.transform.parent = zone.transform;
            marker.transform.position = center + Vector3.up * 0.1f;
            marker.transform.localScale = new Vector3(1f, 0.2f, 1f);
            marker.GetComponent<Renderer>().material = zoneMaterial;

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
            // Create a ring using a cylinder with a hole (approximated)
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.position = center + Vector3.up * 0.05f;
            ring.transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);
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