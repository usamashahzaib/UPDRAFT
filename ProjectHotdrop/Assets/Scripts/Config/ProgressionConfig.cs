using UnityEngine;

namespace Hotdrop.Config
{
    // Spec section 19: XP and cosmetic-credit progression. Never changes combat power.
    [CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Hotdrop/Config/Progression Config")]
    public class ProgressionConfig : ScriptableObject
    {
        public const float DefaultDisconnectBotSubstitutionSeconds = 5f;

        [Header("XP")]
        public int xpPerEligibleMatch = 120;
        public int xpWinBonus = 30;
        public int xpDrawBonus = 15;
        public int xpPerLevel = 400;
        public int maxLevel = 20;

        [Header("Eligibility")]
        [Range(0f, 1f)] public float minPresenceFraction = 0.75f;
        public float afkWarningSeconds = 30f;
        public float afkRemovalAfterWarningSeconds = 15f;
        [Min(0f)] public float disconnectBotSubstitutionSeconds = DefaultDisconnectBotSubstitutionSeconds;

        [Header("Cosmetic Credits")]
        public int creditsPerEligibleMatch = 20;
        public int creditsWinBonus = 5;
        public int creditsPerLevelUp = 20;

        [Header("Initial Credit Shop Prices")]
        public int priceBodyColorStyle = 300;
        public int priceWeaponWrap = 200;
        public int priceProfileBadge = 100;

        [Header("Unlock Levels")]
        public int[] appearanceUnlockLevels = { 3, 6, 10, 15, 20 };
        public int appearanceUnlockAfterMatches = 3; // additional appearance after 3 eligible completed matches
    }
}


