using UnityEngine;
using Updraft.Config;

namespace Updraft.Pads
{
    /// <summary>
    /// Spec section 6 placement rules, shared by offline play and the network
    /// server. Takes an explicit PhysicsScene so each network runner validates
    /// against its own scene. Returns null when valid, otherwise a player-facing
    /// reason; an invalid placement never costs the charge.
    /// </summary>
    public static class PadPlacementRules
    {
        static readonly Collider[] overlap = new Collider[8];

        /// <summary>Where a pad would go ~1.6 m ahead, snapped to the supporting surface.</summary>
        public static string ResolvePosition(PhysicsScene physics, Vector3 feet, Vector3 forward, PadConfig pad, out Vector3 position)
        {
            position = feet + forward * pad.placementForwardOffset;
            if (!physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out var surface, 1.2f,
                LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore)) return "NO FLOOR";
            position = surface.point + Vector3.up * 0.05f;
            return null;
        }

        public static string Validate(PhysicsScene physics, Vector3 position, Vector3 forward, int activeOtherPads,
            PadConfig pad, MapConfig map, MovementConfig movement)
        {
            if (activeOtherPads >= pad.maxActivePadsPerMatch) return "ARENA PAD LIMIT";
            if (!physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1f, ~0, QueryTriggerInteraction.Ignore)) return "NO FLOOR";
            if (Vector3.Angle(hit.normal, Vector3.up) > pad.maxSurfaceTiltDegrees) return "TOO STEEP";

            int padMask = LayerMask.GetMask("Pad");
            if (padMask != 0 && physics.OverlapSphere(position + Vector3.up * 0.1f, pad.diameter * 0.5f, overlap, padMask, QueryTriggerInteraction.Ignore) > 0)
                return "OVERLAPS A PAD";

            for (int spawnTeam = 0; spawnTeam < 2; spawnTeam++)
            {
                Vector3 center = spawnTeam == 0 ? map.northSpawnCenter : map.southSpawnCenter;
                if (Mathf.Abs(position.x - center.x) <= map.spawnRoomSize.x * 0.5f + pad.diameter * 0.5f &&
                    Mathf.Abs(position.z - center.z) <= map.spawnRoomSize.y * 0.5f + pad.diameter * 0.5f) return "SPAWN ROOM";
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 exit = center + new Vector3(i * 6f, 0, (spawnTeam == 0 ? -1 : 1) * map.spawnRoomSize.y * 0.5f);
                    if (Vector2.Distance(new Vector2(position.x, position.z), new Vector2(exit.x, exit.z)) < pad.noPlacementNearSpawnExitRadius)
                        return "TOO CLOSE TO SPAWN EXIT";
                }
            }

            Vector3[] zones = { map.centerZone, map.westZone, map.eastZone };
            foreach (var zone in zones)
                if (Vector2.Distance(new Vector2(position.x, position.z), new Vector2(zone.x, zone.z)) < map.zoneRadius + pad.diameter * 0.5f)
                    return "INSIDE A CAPTURE ZONE";

            int groundMask = LayerMask.GetMask("Ground");
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector3 edge = position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * pad.diameter * 0.5f;
                if (!physics.Raycast(edge + Vector3.up * 0.5f, Vector3.down, out var support, 1f, groundMask, QueryTriggerInteraction.Ignore) ||
                    Mathf.Abs(support.point.y - hit.point.y) > 0.05f) return "DOESN'T FIT";
            }

            Vector3 launch = forward * pad.launchHorizontalVelocity + Vector3.up * pad.launchUpwardVelocity;
            if (physics.SphereCast(position + Vector3.up * 0.5f, movement.capsuleRadius, launch.normalized, out _, launch.magnitude * 0.3f,
                LayerMask.GetMask("Ground", "Cover", "SpawnBarrier"), QueryTriggerInteraction.Ignore))
                return "LAUNCH PATH BLOCKED";

            return null;
        }
    }
}
