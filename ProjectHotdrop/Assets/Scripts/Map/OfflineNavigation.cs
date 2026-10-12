using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Hotdrop.Map
{
    /// <summary>Builds arena navigation for bots that still drive the human input interface.</summary>
    public class OfflineNavigation : MonoBehaviour
    {
        NavMeshDataInstance instance;
        NavMeshData data;
        public void Build()
        {
            var sources = new List<NavMeshBuildSource>();
            foreach (var collider in GetComponentsInChildren<BoxCollider>())
            {
                if (!collider.enabled || collider.isTrigger || collider.GetComponent<SpawnExitBarrier>() != null) continue;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = collider.transform.localToWorldMatrix,
                    size = collider.size,
                    area = 0,
                });
            }
            // The configured map rectangle plus a 2 m border on each side (96 x 80 m becomes 100 x 84 m),
            // centred on it with the vertical window following groundLevelY. Without a map, the original
            // origin-centred 100 x 84 x 20 region is kept.
            var region = new Bounds(Vector3.zero, new Vector3(100, 20, 84));
            var generator = GetComponent<SwitchyardGenerator>();
            var map = generator != null ? generator.mapConfig : null;
            if (map != null)
            {
                const float borderMeters = 2f;
                Vector2 mid = map.BoundsCenter;
                region = new Bounds(new Vector3(mid.x, map.groundLevelY, mid.y),
                    new Vector3(map.boundsMax.x - map.boundsMin.x + borderMeters * 2f, 20f, map.boundsMax.y - map.boundsMin.y + borderMeters * 2f));
            }
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = 0.35f; settings.agentHeight = 1.8f;
            settings.agentClimb = 0.35f; settings.agentSlope = 45f;
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, region, Vector3.zero, Quaternion.identity);
            if (data != null) instance = NavMesh.AddNavMeshData(data);
        }
        void OnDestroy()
        {
            if (instance.valid) instance.Remove();
            if (data != null) Destroy(data);
        }
    }
}

