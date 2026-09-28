using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Updraft.Config;
using Updraft.Core;

namespace Updraft.Editor
{
    /// <summary>
    /// One-shot automation that chains CreateConfigAssets -> CreatePrefabs ->
    /// SceneSetup and saves the result as Assets/Scenes/Main.unity, so the
    /// offline vertical slice (Milestone A/B) exists as an actual openable
    /// scene instead of only as editor-menu instructions in a README.
    /// Callable via `-executeMethod Updraft.Editor.BuildOfflineSlice.Run`.
    /// </summary>
    public static class BuildOfflineSlice
    {
        [MenuItem("Tools/Updraft/Build Offline Slice Scene")]
        public static void Run()
        {
            // Deliberately does NOT call ConfigureAndroidBuild here — this is
            // a content-generation tool (configs/prefabs/scene), and platform
            // build settings are a separate concern with its own menu item
            // (Tools/Updraft/Configure Android Build) and its own caller
            // (BuildAndroidTest.Run). Coupling them meant every scene rebuild
            // silently rewrote ProjectSettings.asset.
            SetupProjectLayers.EnsureLayersExist();
            SetupMobilePresentation.Run();
            CreateConfigAssets.CreateAllConfigs();
            CreatePrefabs.CreateAllPrefabs();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var setupObj = new GameObject("SceneSetup_Temp");
            var setup = setupObj.AddComponent<Updraft.Core.SceneSetup>();

            setup.movementConfig = Load<MovementConfig>("MovementConfig");
            setup.weaponDefinitions = new[]
            {
                Load<WeaponDefinition>("Weapon_ARC"),
                Load<WeaponDefinition>("Weapon_ZIP"),
                Load<WeaponDefinition>("Weapon_BRICK"),
            };
            setup.padConfig = Load<PadConfig>("PadConfig");
            setup.matchRules = Load<MatchRulesConfig>("MatchRulesConfig");
            setup.mapConfig = Load<MapConfig>("MapConfig");
            setup.botConfig = Load<BotConfig>("BotConfig");
            setup.graphicsConfig = Load<GraphicsConfig>("GraphicsConfig");
            setup.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayerPrefab.prefab");
            setup.botPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/BotPrefab.prefab");

            setup.SetupScene();

            Object.DestroyImmediate(setupObj);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            const string scenePath = "Assets/Scenes/Main.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Offline slice scene built at {scenePath}");
        }

        static T Load<T>(string assetName) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"Assets/Resources/Config/{assetName}.asset");
            if (asset == null)
                Debug.LogError($"BuildOfflineSlice: could not load {typeof(T).Name} at Assets/Resources/Config/{assetName}.asset");
            return asset;
        }
    }
}
