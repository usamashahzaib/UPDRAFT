using System.Collections.Generic;
using UnityEngine;
using Updraft.Config;

namespace Updraft.Pads
{
    public struct PlacementSurfaceInfo
    {
        public bool grounded;
        public Vector3 point;
        public Vector3 normal;
        public bool fitsOnSurface;
        public bool overheadObstructed;
    }

    /// <summary>
    /// Spec section 7, "Placement restrictions". Pure decision logic; the
    /// caller (client preview + server authority) supplies geometry query
    /// results (raycasts against the real scene) so this class stays testable
    /// without a live Unity scene.
    /// </summary>
    public static class PadPlacementValidator
    {
        public static bool IsValid(
            PlacementSurfaceInfo surface,
            Vector3 candidatePosition,
            IEnumerable<Vector3> existingPadPositions,
            IEnumerable<(Vector3 exitPoint, float radius)> spawnExits,
            IEnumerable<(Vector3 center, float radius)> spawnRoomBounds,
            IEnumerable<(Vector3 center, float radius)> captureCircles,
            PadConfig config)
        {
            if (!surface.grounded) return false;

            float tiltDegrees = Vector3.Angle(surface.normal, Vector3.up);
            if (tiltDegrees > config.maxSurfaceTiltDegrees) return false;

            if (!surface.fitsOnSurface) return false;
            if (surface.overheadObstructed) return false;

            foreach (var (center, radius) in spawnRoomBounds)
                if (Vector3.Distance(candidatePosition, center) <= radius) return false;

            foreach (var (exitPoint, _) in spawnExits)
                if (Vector3.Distance(candidatePosition, exitPoint) <= config.noPlacementNearSpawnExitRadius) return false;

            foreach (var (center, _) in captureCircles)
                if (Vector3.Distance(candidatePosition, center) <= config.noPlacementInsideCaptureRadius) return false;

            foreach (var existing in existingPadPositions)
                if (Vector3.Distance(candidatePosition, existing) <= config.diameter) return false;

            return true;
        }
    }
}
