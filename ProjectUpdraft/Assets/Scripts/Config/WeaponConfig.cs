using UnityEngine;

namespace Updraft.Config
{
    public enum WeaponId { Arc, Zip, Brick }

    // Spec section 8: weapon stats. All values are the initial authoritative
    // configuration; balance changes must happen here, not in scattered code.
    [CreateAssetMenu(fileName = "WeaponDef", menuName = "Updraft/Config/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public WeaponId id;
        public string displayName;
        [TextArea] public string roleDescription;

        [Header("Damage")]
        public float bodyDamage = 18f;
        public int pelletsPerShot = 1;
        public float fullDamageDistance = 20f;
        public float falloffEndpointDistance = 45f;
        public float damageAtFalloffEndpoint = 10f;
        public float maxRange = 55f;
        public float headMultiplier = 1.25f;

        [Header("Firing")]
        public float roundsPerMinute = 480f;
        public int magazineSize = 24;
        public float reloadSeconds = 1.6f;

        [Header("Spread (cone half-angle, degrees)")]
        public float spreadStationaryGrounded = 0.45f;
        public float spreadMovingGrounded = 0.75f;
        public float spreadAirborne = 1.2f;

        [Header("Recoil")]
        public float verticalRecoilDegreesPerShot = 0.55f;

        public float SecondsPerShot => 60f / roundsPerMinute;

        // Damage falls linearly between fullDamageDistance and falloffEndpointDistance.
        // Beyond the endpoint, damageAtFalloffEndpoint applies flat out to maxRange.
        public float GetDamageAtDistance(float distance)
        {
            if (distance <= fullDamageDistance) return bodyDamage;
            if (distance >= maxRange) return 0f;
            if (distance >= falloffEndpointDistance) return damageAtFalloffEndpoint;

            float t = (distance - fullDamageDistance) / (falloffEndpointDistance - fullDamageDistance);
            return Mathf.Lerp(bodyDamage, damageAtFalloffEndpoint, t);
        }
    }
}
