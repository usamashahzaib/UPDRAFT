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

        /// <summary>Spawn point just inside exit 0..2 of a team's spawn room, facing the arena (team 0 = north).</summary>
        public Pose SpawnExit(int team, int exit)
        {
            Vector3 center = team == 0 ? northSpawnCenter : southSpawnCenter;
            float facing = team == 0 ? -1f : 1f;
            var position = center + new Vector3((exit % 3 - 1) * 6f, groundLevelY + 0.55f, facing * (spawnRoomSize.y * 0.5f - 2f));
            return new Pose(position, Quaternion.Euler(0, team == 0 ? 180f : 0f, 0));
        }

        public Vector3 ZoneCenter(ZoneId zone) => zone switch
        {
            ZoneId.West => westZone,
            ZoneId.East => eastZone,
            _ => centerZone,
        };

        public bool IsInsideSpawnRoom(int team, Vector3 position)
        {
            Vector3 center = team == 0 ? northSpawnCenter : southSpawnCenter;
            return Mathf.Abs(position.x - center.x) <= spawnRoomSize.x * 0.5f && Mathf.Abs(position.z - center.z) <= spawnRoomSize.y * 0.5f;
        }
    }
}
