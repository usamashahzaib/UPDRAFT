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

        /// <summary>
        /// QA-006: every player-facing rejection, indexed so the server can replicate a one-byte
        /// code to the placing client. Index 0 means "no rejection".
        /// </summary>
        public static readonly string[] RejectReasons =
        {
            null, "PADS DISABLED IN THIS VERSION", "NOT DURING THIS PHASE", "RECHARGING", "MUST BE GROUNDED",
            "NO FLOOR", "ARENA PAD LIMIT", "TOO STEEP", "OVERLAPS A PAD", "SPAWN ROOM", "TOO CLOSE TO SPAWN EXIT",
            "INSIDE A CAPTURE ZONE", "DOESN'T FIT", "NO HEADROOM", "LAUNCH PATH BLOCKED", "CAN'T PLACE HERE",
        };

        public static byte CodeOf(string reason)
        {
            if (reason == null) return 0;
            int index = System.Array.IndexOf(RejectReasons, reason);
            return (byte)(index > 0 ? index : RejectReasons.Length - 1);
        }

        public static string ReasonOf(byte code) =>
            code == 0 ? null : RejectReasons[Mathf.Min(code, RejectReasons.Length - 1)];

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
            // The rim of a disc on a legal (<= max tilt) plane sits up to r*tan(tilt) off the centre height.
            float rimTolerance = pad.diameter * 0.5f * Mathf.Tan(pad.maxSurfaceTiltDegrees * Mathf.Deg2Rad) + 0.02f;
            // QA-005: sample the whole disc, not just its rim. Rings at full, 2/3 and 1/3 radius keep
            // every gap between samples under ~0.28 m — narrower than the 0.7 m player capsule —
            // so any notch or hole large enough to matter to a player is found.
            float radius = pad.diameter * 0.5f;
            for (int ring = 0; ring < SupportRings.Length; ring++)
            {
                float r = radius * SupportRings[ring].fraction;
                int count = SupportRings[ring].samples;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i + ring * 0.5f) * Mathf.PI * 2f / count;
                    Vector3 sample = position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * r;
                    if (!physics.Raycast(sample + Vector3.up * 0.5f, Vector3.down, out var support, 1f, groundMask, QueryTriggerInteraction.Ignore) ||
                        Mathf.Abs(support.point.y - hit.point.y) > rimTolerance) return "DOESN'T FIT";
                }
            }

            // QA-005: clearance for the full standing player (not a small sphere starting 0.5 m up):
            // the capsule must fit on the pad, then sweep clear along the first 0.3 s of the launch.
            int blockMask = LayerMask.GetMask("Ground", "Cover", "SpawnBarrier");
            float capsuleR = movement.capsuleRadius;
            Vector3 bottom = position + Vector3.up * (capsuleR + 0.05f);
            Vector3 top = position + Vector3.up * Mathf.Max(capsuleR + 0.05f, movement.capsuleHeight - capsuleR);
            if (physics.OverlapCapsule(bottom, top, capsuleR, overlap, blockMask, QueryTriggerInteraction.Ignore) > 0)
                return "NO HEADROOM";
            Vector3 launch = forward * pad.launchHorizontalVelocity + Vector3.up * pad.launchUpwardVelocity;
            if (physics.CapsuleCast(bottom, top, capsuleR, launch.normalized, out _, launch.magnitude * 0.3f,
                blockMask, QueryTriggerInteraction.Ignore))
                return "LAUNCH PATH BLOCKED";

            return null;
        }

        static readonly (float fraction, int samples)[] SupportRings = { (1f, 16), (0.66f, 10), (0.33f, 6) };
    }
}
