using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Match
{
    /// <summary>
    /// Spec section 4, "Valid occupation" rules. Pure function so both the
    /// dedicated server and offline practice evaluate it identically.
    /// </summary>
    public static class ZoneOccupancy
    {
        public static bool IsValid(
            Vector3 feetPosition,
            Vector3 zoneCenter,
            float zoneFloorY,
            MatchRulesConfig rules,
            bool isAlive,
            bool isSpawnProtected)
        {
            if (!isAlive || isSpawnProtected) return false;

            float horizontalDist = new Vector2(feetPosition.x - zoneCenter.x, feetPosition.z - zoneCenter.z).magnitude;
            if (horizontalDist > rules.zoneCaptureRadius) return false;

            float heightAboveFloor = feetPosition.y - zoneFloorY;
            if (heightAboveFloor > rules.zoneMaxFeetHeight) return false;

            return true;
        }
    }
}


