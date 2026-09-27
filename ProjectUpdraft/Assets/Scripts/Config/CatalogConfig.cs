using UnityEngine;

namespace Updraft.Config
{
    [System.Serializable]
    public struct CosmeticBundle
    {
        public string id;
        public string displayName;
        public float referenceUsdPrice; // actual localized price comes from Google Play at runtime
        public string[] contents;
    }

    // Final spec section 19: monetize only after Milestone D validation.
    // IAP is cosmetic-only, permanent, no premium currency. Rewarded ads
    // (AdConfig) are the other approved monetization lever — never
    // interstitial/forced ads, never pay-to-win.
    [CreateAssetMenu(fileName = "CatalogConfig", menuName = "Updraft/Config/Catalog Config")]
    public class CatalogConfig : ScriptableObject
    {
        public bool shopEnabled = false; // stays false until Milestone D's gameplay validation gate passes

        public CosmeticBundle[] bundles = new[]
        {
            new CosmeticBundle
            {
                id = "signal",
                displayName = "Signal",
                referenceUsdPrice = 1.99f,
                contents = new[] { "Body style: Signal", "Weapon wrap: Signal (ARC/ZIP/BRICK)" },
            },
            new CosmeticBundle
            {
                id = "foundry",
                displayName = "Foundry",
                referenceUsdPrice = 3.99f,
                contents = new[] { "Body style: Foundry", "Profile badge: Foundry" },
            },
            new CosmeticBundle
            {
                id = "skyline",
                displayName = "Skyline",
                referenceUsdPrice = 5.99f,
                contents = new[] { "Body style: Skyline", "Profile badge: Skyline" },
            },
        };
    }
}
