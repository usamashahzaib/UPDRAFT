using UnityEngine;

namespace Hotdrop.Config
{
    // Spec section 6: Make movement simple and dependable.
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Hotdrop/Config/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        [Header("Health")]
        public float maxHealth = 100f;

        [Header("Ground Movement")]
        public float groundSpeed = 6f;
        public float speedWhileFiring = 5.1f;
        public float groundAcceleration = 35f;
        public float gravity = 20f;
        [Tooltip("Ground speed at which weapon spread switches from stationary to moving.")]
        [Min(0f)] public float weaponMovingGroundSpeedThreshold = 0.5f;

        [Header("Jump")]
        public float jumpUpwardVelocity = 6f;
        // Approximately 0.9 m at the above gravity/velocity; derived, not authoritative.

        [Header("Capsule")]
        public float capsuleHeight = 1.8f;
        public float capsuleRadius = 0.35f;
        public float stepHeight = 0.35f;

        [Header("Respawn")]
        public float respawnDelaySeconds = 4f;

        [Header("Health Recovery")]
        public float recoveryDelayAfterDamage = 5f;
        public float recoveryPerSecond = 25f;

        [Header("Spawn Protection")]
        public float respawnProtectionSeconds = 1.5f;
    }
}


