using UnityEngine;

namespace Hotdrop.Weapons
{
    /// <summary>
    /// QA-012: a ray that starts at the muzzle cannot see a thin wall the muzzle has already
    /// poked through (raycasts ignore colliders they start inside). Before any shot is traced
    /// from the muzzle, the gun itself must be on the shooter's side of the geometry: a line
    /// from the body centre (at muzzle height) to the muzzle must be clear.
    /// Used by offline fire, the server, client prediction and both blocked-shot indicators.
    /// </summary>
    public static class MuzzleObstruction
    {
        static readonly Collider[] overlap = new Collider[4];

        public static int GeometryMask => LayerMask.GetMask("Ground", "Cover", "SpawnBarrier", "Pad");

        /// <summary>True when the muzzle is inside or behind geometry relative to the shooter's body.</summary>
        public static bool IsBuried(PhysicsScene physics, Vector3 bodyCentre, Vector3 muzzle, int mask, out Vector3 blockPoint)
        {
            blockPoint = muzzle;
            Vector3 d = muzzle - bodyCentre;
            float distance = d.magnitude;
            if (distance > 1e-3f &&
                physics.Raycast(bodyCentre, d / distance, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
            {
                blockPoint = hit.point;
                return true;
            }
            return physics.OverlapSphere(muzzle, 0.02f, overlap, mask, QueryTriggerInteraction.Ignore) > 0;
        }
    }
}

