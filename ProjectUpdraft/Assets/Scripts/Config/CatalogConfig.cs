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

    // Spec section 20: monetize only after validation. Cosmetic-only, permanent, no premium currency.
    [CreateAssetMenu(fileName = "CatalogConfig", menuName = "Updraft/Config/Catalog Config")]
    public class CatalogConfig : ScriptableObject
    {
        public bool shopEnabled = false; // stays false until the gameplay validation gate (section 26) passes

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
