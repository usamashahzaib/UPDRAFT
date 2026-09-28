using UnityEngine;

namespace Updraft.Config
{
    // Spec section 7: launch pad signature mechanic.
    [CreateAssetMenu(fileName = "PadConfig", menuName = "Updraft/Config/Pad Config")]
    public class PadConfig : ScriptableObject
    {
        [Header("Charges")]
        public int chargeCapacity = 1;
        public float rechargeSeconds = 16f;

        [Header("Lifecycle")]
        public float lifetimeSeconds = 18f;
        public float armingSeconds = 0.75f;
        public float health = 60f;
        public float diameter = 1.4f;

        [Header("Launch")]
        public float launchHorizontalVelocity = 14f;
        public float launchUpwardVelocity = 12f;
        public int maxActivePadsPerOwner = 1;
        [UnityEngine.Serialization.FormerlySerializedAs("maxPlacementsPerMatch")]
        public int maxActivePadsPerMatch = 6;
        public float perPlayerActivationLockoutSeconds = 1f;

        [Header("Placement")]
        public float placementForwardOffset = 1.6f;
        public float previewHoldThresholdSeconds = 0.18f;
        public float maxSurfaceTiltDegrees = 5f;
        public float noPlacementNearSpawnExitRadius = 5f;
        public float noPlacementInsideCaptureRadius = 6f;

        [Header("Flight")]
        public float airSteerDegreesPerSecond = 30f;
    }
}
