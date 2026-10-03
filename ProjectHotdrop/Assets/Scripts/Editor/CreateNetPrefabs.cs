using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEditor;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Network;
using Hotdrop.Player;

namespace Hotdrop.Editor
{
    /// <summary>
    /// Creates the three network prefabs the Fusion session loads from Resources
    /// (NetSession.SpawnMatch/SpawnAvatar, NetMatch.SpawnPad) plus the
    /// BackendConfig resource asset if absent. QA-001 fix: these assets were
    /// missing from the checkout, so any online session failed at spawn.
    /// Run via Tools/Hotdrop/Create Net Prefabs, or batch:
    ///   Unity.exe -batchmode -projectPath ProjectHotdrop
    ///     -executeMethod Hotdrop.Editor.CreateNetPrefabs.Run -logFile log.txt
    /// </summary>
    public static class CreateNetPrefabs
    {
        const string PrefabFolder = "Assets/Resources/NetPrefabs";
        const string ConfigFolder = "Assets/Resources/Config";

        [MenuItem("Tools/Hotdrop/Create Net Prefabs")]
        public static void CreateAll()
        {
            SetupProjectLayers.EnsureLayersExist();

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "NetPrefabs");

            CreateMatchPrefab();
            CreatePlayerPrefab();
            CreatePadPrefab();
            EnsureBackendConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Net prefabs created at " + PrefabFolder);
        }

        /// <summary>Batchmode entry point; exits explicitly so failures return non-zero.</summary>
        public static void Run()
        {
            try
            {
                CreateAll();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void CreateMatchPrefab()
        {
            var go = new GameObject("NetMatch");
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetMatch>();
            SaveAndDestroy(go, PrefabFolder + "/NetMatch.prefab");
        }

        static void CreatePlayerPrefab()
        {
            var go = new GameObject("NetPlayer");
            go.layer = LayerMask.NameToLayer("Player");
            go.AddComponent<NetworkObject>();

            // PhysX shape: hit detection fallback (aim assist LOS, pings, IncludePhysX
            // raycasts) and the shape SimpleKCC resolves collision with. Spec 15
            // (Amendment v2.1): one capsule shared by both body rigs, never a per-rig shape.
            var gameplay = BodyRigs.SharedGameplay;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = gameplay.CapsuleCenter;
            capsule.height = gameplay.CapsuleHeight;
            capsule.radius = gameplay.CapsuleRadius;
            capsule.direction = 1;

            go.AddComponent<SimpleKCC>();

            // Authoritative lag-compensated hit geometry (spec 7/21): body + head.
            // Head hitbox is what makes the 1.25x multiplier reachable online.
            go.AddComponent<HitboxRoot>();

            // Spec 15 (Amendment v2.1): the same hitbox set serves both body rigs.
            var body = new GameObject(gameplay.BodyHitboxName);
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = gameplay.BodyHitboxOffset;
            body.layer = LayerMask.NameToLayer("Player");
            var bodyHitbox = body.AddComponent<Hitbox>();
            bodyHitbox.Type = HitboxTypes.Box;
            bodyHitbox.BoxExtents = gameplay.BodyHitboxExtents;

            var head = new GameObject(gameplay.HeadHitboxName); // name is read by NetMatch.ResolveShots
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = gameplay.HeadHitboxOffset;
            head.layer = LayerMask.NameToLayer("Player");
            var headHitbox = head.AddComponent<Hitbox>();
            headHitbox.Type = HitboxTypes.Sphere;
            headHitbox.SphereRadius = gameplay.HeadHitboxRadius;

            go.AddComponent<NetPlayer>();

            SaveAndDestroy(go, PrefabFolder + "/NetPlayer.prefab");
        }

        static void CreatePadPrefab()
        {
            var go = new GameObject("NetPad");
            go.layer = LayerMask.NameToLayer("Pad");
            go.AddComponent<NetworkObject>();

            // Flat collider so enemy fire can reach the pad (NetMatch.ResolveShots
            // pad branch); movement ignores it via GameBootstrap layer matrix.
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.08f, 0f);
            box.size = new Vector3(1.4f, 0.16f, 1.4f);

            go.AddComponent<NetPad>();

            SaveAndDestroy(go, PrefabFolder + "/NetPad.prefab");
        }

        static void EnsureBackendConfig()
        {
            const string path = ConfigFolder + "/BackendConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<BackendConfig>(path) != null) return;
            var cfg = ScriptableObject.CreateInstance<BackendConfig>();
            cfg.environmentName = "dev";
            cfg.baseUrl = "http://localhost:5000";
            AssetDatabase.CreateAsset(cfg, path);
        }

        static void SaveAndDestroy(GameObject go, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) Object.DestroyImmediate(existing);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }
}





