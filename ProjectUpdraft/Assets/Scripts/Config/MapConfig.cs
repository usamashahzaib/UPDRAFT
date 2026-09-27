using UnityEngine;

namespace Updraft.Config
{
    // Spec section 10: Switchyard graybox coordinates. One Unity unit = one meter.
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Updraft/Config/Map Config")]
    public class MapConfig : ScriptableObject
    {
        public string mapName = "Switchyard";

        [Header("Footprint")]
        public Vector2 boundsMin = new Vector2(-48f, -40f); // X, Z
        public Vector2 boundsMax = new Vector2(48f, 40f);
        public float groundLevelY = 0f;

        [Header("Objectives")]
        public Vector3 centerZone = new Vector3(0f, 0f, 0f);
        public Vector3 westZone = new Vector3(-22f, 0f, 0f);
        public Vector3 eastZone = new Vector3(22f, 0f, 0f);
        public float zoneRadius = 6f;

        [Header("Spawns")]
        public Vector3 northSpawnCenter = new Vector3(0f, 0f, 35f);
        public Vector2 spawnRoomSize = new Vector2(20f, 8f);
        public Vector3 southSpawnCenter = new Vector3(0f, 0f, -35f);
        public int exitsPerSpawn = 3;

        [Header("Raised Platforms")]
        public float platformHeight = 3f;
        public Vector2 platformSize = new Vector2(10f, 8f);
        public Vector3[] platformCenters = new[]
        {
            new Vector3(20f, 3f, 14f),
            new Vector3(-20f, 3f, 14f),
            new Vector3(20f, 3f, -14f),
            new Vector3(-20f, 3f, -14f),
        };

        [Header("Routes")]
        public float minimumMainRouteWidth = 4f;

        [Header("Sightline Blockers")]
        public float sightlineBlockerZ = 20f; // placed at Z = +/-20 between each spawn and Center
    }
}
