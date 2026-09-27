using UnityEngine;

namespace Updraft.Config
{
    // Final spec section 19: rewarded ads only, never interstitial, never
    // forced, always skippable and capped. Disabled pre-Milestone-D like the
    // rest of monetization (see CatalogConfig.shopEnabled).
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Updraft/Config/Ad Config")]
    public class AdConfig : ScriptableObject
    {
        public bool rewardedAdsEnabled = false; // stays false until Milestone D passes, same gate as the shop

        [Header("Extra Crate Roll")]
        public int extraCrateRollMaxPerDay = 1;

        [Header("Credit Bonus")]
        public int creditBonusAmount = 20;
        public int creditBonusMaxPerDay = 3;

        [Header("Cosmetic Trial")]
        public float cosmeticTrialHours = 24f;
        public int cosmeticTrialMaxPerDay = 1;

        // Hard rules, not tunables — kept as constants rather than inspector
        // fields so nobody can accidentally toggle them off in an asset.
        public const bool RewardedOnly = true;
        public const bool Skippable = true;
        public const bool NoForcedInterstitials = true;
        public const bool NoMidMatchAds = true;
    }
}
