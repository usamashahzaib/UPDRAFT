using NUnit.Framework;
using UnityEngine;
using Updraft.Config;

namespace Updraft.Tests
{
    /// <summary>
    /// QA-001 regression: the Fusion session loads NetPrefabs/NetMatch,
    /// NetPrefabs/NetPlayer and NetPrefabs/NetPad from Resources at runtime, and
    /// BackendClient loads Config/BackendConfig. If any of these assets is missing
    /// from the checkout, online play fails at spawn — this test keeps them present.
    /// </summary>
    public class NetResourceAssetsTests
    {
        static readonly string[] RequiredPrefabs = { "NetMatch", "NetPlayer", "NetPad" };

        [Test]
        public void NetPrefabs_Exist_WithNetworkObjectAndBehaviour()
        {
            foreach (var name in RequiredPrefabs)
            {
                var prefab = Resources.Load<GameObject>("NetPrefabs/" + name);
                Assert.IsNotNull(prefab, "Missing resource prefab NetPrefabs/" + name);
                Assert.IsNotNull(prefab.GetComponent("NetworkObject"),
                    name + " prefab must carry a Fusion NetworkObject");
                Assert.IsNotNull(prefab.GetComponent(name),
                    name + " prefab must carry its " + name + " behaviour");
            }
        }

        [Test]
        public void NetPlayerPrefab_CarriesKccColliderAndHitboxes()
        {
            var prefab = Resources.Load<GameObject>("NetPrefabs/NetPlayer");
            Assert.IsNotNull(prefab, "Missing resource prefab NetPrefabs/NetPlayer");

            Assert.IsNotNull(prefab.GetComponent("SimpleKCC"),
                "NetPlayer requires SimpleKCC (NetPlayer.Spawned caches it)");
            var capsule = prefab.GetComponent<CapsuleCollider>();
            Assert.IsNotNull(capsule, "NetPlayer requires a CapsuleCollider for hit queries");
            Assert.AreEqual(1.8f, capsule.height, 0.001f);
            Assert.AreEqual(0.35f, capsule.radius, 0.001f);

            Assert.IsNotNull(prefab.GetComponent("HitboxRoot"),
                "NetPlayer requires a Fusion HitboxRoot for lag-compensated hits");

            var head = prefab.transform.Find("Head");
            Assert.IsNotNull(head, "NetPlayer requires a Head child object");
            Assert.IsNotNull(head.GetComponent("Hitbox"),
                "Head must carry a Fusion Hitbox (online 1.25x headshots)");

            var body = prefab.transform.Find("Body");
            Assert.IsNotNull(body, "NetPlayer requires a Body hitbox child object");
            Assert.IsNotNull(body.GetComponent("Hitbox"),
                "Body must carry a Fusion Hitbox (lag-compensated body hits)");
        }

        [Test]
        public void NetPadPrefab_HasColliderForEnemyFire()
        {
            var prefab = Resources.Load<GameObject>("NetPrefabs/NetPad");
            Assert.IsNotNull(prefab, "Missing resource prefab NetPrefabs/NetPad");
            Assert.IsNotNull(prefab.GetComponent<Collider>(),
                "NetPad requires a collider so enemy fire can destroy it");
        }

        [Test]
        public void BackendConfigAsset_Exists_And_IsConfigured()
        {
            var cfg = Resources.Load<BackendConfig>("Config/BackendConfig");
            Assert.IsNotNull(cfg, "Missing resource asset Config/BackendConfig");
            Assert.IsFalse(string.IsNullOrWhiteSpace(cfg.baseUrl),
                "BackendConfig.baseUrl must be set for online play (empty = offline-only)");
            Assert.Greater(cfg.requestTimeoutSeconds, 0f);
            Assert.GreaterOrEqual(cfg.clientProtocolVersion, 1);
        }
    }
}
