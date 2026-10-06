using UnityEngine;

namespace Hotdrop.Config
{
    public enum WeaponId { Arc, Zip, Brick }

    // Spec section 8: weapon stats. All values are the initial authoritative
    // configuration; balance changes must happen here, not in scattered code.
    [CreateAssetMenu(fileName = "WeaponDef", menuName = "Hotdrop/Config/Weapon Definition")]
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
        [Tooltip("Authored vertical recoil multiplier across the magazine. One is the configured base recoil.")]
        public AnimationCurve recoilPattern = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public float GetVerticalRecoilDegrees(int shotInMagazine)
        {
            if (recoilPattern == null || recoilPattern.length == 0)
                return verticalRecoilDegreesPerShot;
            int patternLength = Mathf.Max(1, recoilPattern.length);
            int index = Mathf.Max(0, shotInMagazine - 1) % patternLength;
            float t = patternLength <= 1 ? 0f : index / (float)(patternLength - 1);
            return verticalRecoilDegreesPerShot * Mathf.Max(0f, recoilPattern.Evaluate(t));
        }

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


