using UnityEngine;
using UnityEditor;
using Updraft.Config;
using Updraft.Player;
using Updraft.Bots;
using Updraft.Pads;
using Updraft.Camera;
using Updraft.Input;
using Updraft.UI;
using Updraft.Match;
using Updraft.Tutorial;
using Updraft.Map;
using Updraft.Core;

namespace Updraft.Editor
{
    /// <summary>
    /// Creates player and bot prefabs with all necessary components.
    /// Run via Tools/Updraft/Create Prefabs
    /// </summary>
    public class CreatePrefabs
    {
        const string PrefabPath = "Assets/Resources/Prefabs";

        [MenuItem("Tools/Updraft/Create Prefabs")]
        public static void CreateAllPrefabs()
        {
            CreatePlayerPrefab();
            CreateBotPrefab();
            CreateTutorialTargetPrefab();
            CreateTutorialMarkerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("All Updraft prefabs created at " + PrefabPath);
        }

        static GameObject CreatePlayerPrefab()
        {
            GameObject player = new GameObject("PlayerPrefab");

            // CharacterController
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.stepOffset = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            // AudioSource
            var audio = player.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.minDistance = 1f;
            audio.maxDistance = 50f;
            audio.rolloffMode = AudioRolloffMode.Linear;

            // PlayerController
            var pc = player.AddComponent<PlayerController>();

            // Body visual (capsule primitive for graybox)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.7f, 1.8f, 0.7f);
            // Remove collider from visual (CharacterController handles collision)
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            // Weapon attachment points (3 weapons)
            GameObject[] weaponMeshes = new GameObject[3];
            string[] weaponNames = { "Weapon_ARC", "Weapon_ZIP", "Weapon_BRICK" };
            for (int i = 0; i < 3; i++)
            {
                GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weapon.name = weaponNames[i];
                weapon.transform.SetParent(player.transform, false);
                weapon.transform.localPosition = new Vector3(0.3f, 1.5f, 0.5f);
                weapon.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
                weapon.SetActive(i == 0); // Only ARC visible by default
                Object.DestroyImmediate(weapon.GetComponent<BoxCollider>());
                weaponMeshes[i] = weapon;
            }

            // Muzzle transform
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(player.transform, false);
            muzzle.transform.localPosition = new Vector3(0.3f, 1.5f, 1f);

            // Camera pivot (upper torso)
            GameObject camPivot = new GameObject("CameraPivot");
            camPivot.transform.SetParent(player.transform, false);
            camPivot.transform.localPosition = new Vector3(0, 1.45f, 0);

            // Materials (will be assigned at runtime)
            var bodyRenderer = body.GetComponent<Renderer>();

            // Set up PlayerController references via SerializedObject
            var so = new SerializedObject(pc);
            so.FindProperty("movementConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Resources/Config/MovementConfig.asset");
            so.FindProperty("weaponDefinitions").arraySize = 3;
            so.FindProperty("weaponDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_ARC.asset");
            so.FindProperty("weaponDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_ZIP.asset");
            so.FindProperty("weaponDefinitions").GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_BRICK.asset");
            so.FindProperty("padConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PadConfig>("Assets/Resources/Config/PadConfig.asset");
            so.FindProperty("mapConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Resources/Config/MapConfig.asset");
            so.FindProperty("playerCamera").objectReferenceValue = null; // Assigned at runtime
            so.FindProperty("touchInput").objectReferenceValue = null; // Assigned at runtime
            so.FindProperty("muzzleTransform").objectReferenceValue = muzzle.transform;
            so.FindProperty("cameraPivot").objectReferenceValue = camPivot.transform;
            so.FindProperty("bodyMesh").objectReferenceValue = body;
            so.FindProperty("weaponMeshes").arraySize = 3;
            for (int i = 0; i < 3; i++)
                so.FindProperty("weaponMeshes").GetArrayElementAtIndex(i).objectReferenceValue = weaponMeshes[i];
            so.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
            so.ApplyModifiedProperties();

            // Tag and layer
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(player, PrefabPath + "/PlayerPrefab.prefab");
            Object.DestroyImmediate(player);
            return null;
        }

        static GameObject CreateBotPrefab()
        {
            GameObject bot = new GameObject("BotPrefab");

            // CharacterController
            var cc = bot.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.stepOffset = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

            // AudioSource
            var audio = bot.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.minDistance = 1f;
            audio.maxDistance = 50f;
            audio.rolloffMode = AudioRolloffMode.Linear;

            // PlayerController (bots use same controller)
            var pc = bot.AddComponent<PlayerController>();

            // BotController
            var bc = bot.AddComponent<BotController>();

            // Body visual
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(bot.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.7f, 1.8f, 0.7f);
            Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

            // Weapon meshes
            GameObject[] weaponMeshes = new GameObject[3];
            string[] weaponNames = { "Weapon_ARC", "Weapon_ZIP", "Weapon_BRICK" };
            for (int i = 0; i < 3; i++)
            {
                GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weapon.name = weaponNames[i];
                weapon.transform.SetParent(bot.transform, false);
                weapon.transform.localPosition = new Vector3(0.3f, 1.5f, 0.5f);
                weapon.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
                weapon.SetActive(i == 0);
                Object.DestroyImmediate(weapon.GetComponent<BoxCollider>());
                weaponMeshes[i] = weapon;
            }

            // Muzzle
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(bot.transform, false);
            muzzle.transform.localPosition = new Vector3(0.3f, 1.5f, 1f);

            // Camera pivot (not used for bots but needed for PlayerController)
            GameObject camPivot = new GameObject("CameraPivot");
            camPivot.transform.SetParent(bot.transform, false);
            camPivot.transform.localPosition = new Vector3(0, 1.45f, 0);

            var bodyRenderer = body.GetComponent<Renderer>();

            // Set up PlayerController
            var pcSo = new SerializedObject(pc);
            pcSo.FindProperty("movementConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Resources/Config/MovementConfig.asset");
            pcSo.FindProperty("weaponDefinitions").arraySize = 3;
            pcSo.FindProperty("weaponDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_ARC.asset");
            pcSo.FindProperty("weaponDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_ZIP.asset");
            pcSo.FindProperty("weaponDefinitions").GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_BRICK.asset");
            pcSo.FindProperty("padConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PadConfig>("Assets/Resources/Config/PadConfig.asset");
            pcSo.FindProperty("mapConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Resources/Config/MapConfig.asset");
            pcSo.FindProperty("playerCamera").objectReferenceValue = null;
            pcSo.FindProperty("touchInput").objectReferenceValue = null;
            pcSo.FindProperty("muzzleTransform").objectReferenceValue = muzzle.transform;
            pcSo.FindProperty("cameraPivot").objectReferenceValue = camPivot.transform;
            pcSo.FindProperty("bodyMesh").objectReferenceValue = body;
            pcSo.FindProperty("weaponMeshes").arraySize = 3;
            for (int i = 0; i < 3; i++)
                pcSo.FindProperty("weaponMeshes").GetArrayElementAtIndex(i).objectReferenceValue = weaponMeshes[i];
            pcSo.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
            pcSo.ApplyModifiedProperties();

            // Set up BotController
            var bcSo = new SerializedObject(bc);
            bcSo.FindProperty("botConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BotConfig>("Assets/Resources/Config/BotConfig.asset");
            bcSo.FindProperty("movementConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Resources/Config/MovementConfig.asset");
            bcSo.FindProperty("weaponDefinitions").arraySize = 3;
            bcSo.FindProperty("weaponDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_ARC.asset");
            bcSo.FindProperty("weaponDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_ZIP.asset");
            bcSo.FindProperty("weaponDefinitions").GetArrayElementAtIndex(2).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Resources/Config/Weapon_BRICK.asset");
            bcSo.FindProperty("padConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PadConfig>("Assets/Resources/Config/PadConfig.asset");
            bcSo.FindProperty("matchRules").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MatchRulesConfig>("Assets/Resources/Config/MatchRulesConfig.asset");
            bcSo.FindProperty("mapConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Resources/Config/MapConfig.asset");
            bcSo.FindProperty("playerController").objectReferenceValue = pc;
            bcSo.FindProperty("aimTarget").objectReferenceValue = null;
            bcSo.ApplyModifiedProperties();

            // Tag and layer
            bot.tag = "Player";
            bot.layer = LayerMask.NameToLayer("Player");

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(bot, PrefabPath + "/BotPrefab.prefab");
            Object.DestroyImmediate(bot);
            return null;
        }

        static GameObject CreateTutorialTargetPrefab()
        {
            GameObject target = new GameObject("TutorialTarget");
            target.layer = LayerMask.NameToLayer("Default");

            // Visual - simple cube
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(target.transform, false);
            visual.transform.localScale = new Vector3(1f, 2f, 0.5f);
            visual.transform.localPosition = new Vector3(0, 1f, 0);

            // TutorialTarget script
            var tt = target.AddComponent<TutorialTarget>();
            tt.health = 50f; // Low health for quick tutorial kill

            // AudioSource
            var audio = target.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;

            PrefabUtility.SaveAsPrefabAsset(target, PrefabPath + "/TutorialTarget.prefab");
            Object.DestroyImmediate(target);
            return null;
        }

        static GameObject CreateTutorialMarkerPrefab()
        {
            GameObject marker = new GameObject("TutorialMarker");
            
            // Visual - cylinder with transparent material
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(marker.transform, false);
            visual.transform.localScale = new Vector3(2f, 0.1f, 2f);
            visual.transform.localPosition = new Vector3(0, 0.05f, 0);
            
            // Make it a trigger
            var col = visual.GetComponent<Collider>();
            if (col) col.isTrigger = true;

            // Pulsing animation would be added here
            
            PrefabUtility.SaveAsPrefabAsset(marker, PrefabPath + "/TutorialMarker.prefab");
            Object.DestroyImmediate(marker);
            return null;
        }
    }
}