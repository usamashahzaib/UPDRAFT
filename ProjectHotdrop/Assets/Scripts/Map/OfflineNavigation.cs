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
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = 0.35f; settings.agentHeight = 1.8f;
            settings.agentClimb = 0.35f; settings.agentSlope = 45f;
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, new Vector3(100, 20, 84)), Vector3.zero, Quaternion.identity);
            if (data != null) instance = NavMesh.AddNavMeshData(data);
        }
        void OnDestroy()
        {
            if (instance.valid) instance.Remove();
            if (data != null) Destroy(data);
        }
    }
}

