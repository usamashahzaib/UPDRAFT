using UnityEngine;
using UnityEditor;
using Hotdrop.Config;
using Hotdrop.Player;
using Hotdrop.Bots;
using Hotdrop.Pads;
using Hotdrop.Camera;
using Hotdrop.Input;
using Hotdrop.UI;
using Hotdrop.Match;
using Hotdrop.Tutorial;
using Hotdrop.Map;
using Hotdrop.Core;

namespace Hotdrop.Editor
{
    /// <summary>
    /// Creates player and bot prefabs with all necessary components.
    /// Run via Tools/Hotdrop/Create Prefabs
    /// </summary>
    public class CreatePrefabs
    {
        const string PrefabPath = "Assets/Resources/Prefabs";

        [MenuItem("Tools/Hotdrop/Create Prefabs")]
        public static void CreateAllPrefabs()
        {
            EnsureAvatarMaterials();
            CreatePlayerPrefab();
            CreateBotPrefab();
            CreateTutorialTargetPrefab();
            CreateTutorialMarkerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("All Hotdrop prefabs created at " + PrefabPath);
        }

        static GameObject CreatePlayerPrefab()
        {
            GameObject player = new GameObject("PlayerPrefab");

            // CharacterController - spec 15 (Amendment v2.1): one capsule shared by both body rigs.
            var gameplay = BodyRigs.SharedGameplay;
            var cc = player.AddComponent<CharacterController>();
            cc.height = gameplay.CapsuleHeight;
            cc.radius = gameplay.CapsuleRadius;
            cc.stepOffset = 0.35f;
            cc.center = gameplay.CapsuleCenter;

            // AudioSource
            var audio = player.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.minDistance = 1f;
            audio.maxDistance = 50f;
            audio.rolloffMode = AudioRolloffMode.Linear;

            // PlayerController
            var pc = player.AddComponent<PlayerController>();

            // Body visual (capsule primitive for graybox)
            GameObject body = CreateSportAvatar(player.transform);

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
                weapon.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Equipment.mat");
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
            var bodyRenderer = body.GetComponentInChildren<Renderer>();
            bodyRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Friendly.mat");

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
            so.FindProperty("friendlyMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Friendly.mat");
            so.FindProperty("enemyMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Enemy.mat");
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

            // CharacterController - bots share the player capsule and therefore both rigs.
            var gameplay = BodyRigs.SharedGameplay;
            var cc = bot.AddComponent<CharacterController>();
            cc.height = gameplay.CapsuleHeight;
            cc.radius = gameplay.CapsuleRadius;
            cc.stepOffset = 0.35f;
            cc.center = gameplay.CapsuleCenter;

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
            GameObject body = CreateSportAvatar(bot.transform);

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
                weapon.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Equipment.mat");
            }

            // Muzzle
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(bot.transform, false);
            muzzle.transform.localPosition = new Vector3(0.3f, 1.5f, 1f);

            // Camera pivot (not used for bots but needed for PlayerController)
            GameObject camPivot = new GameObject("CameraPivot");
            camPivot.transform.SetParent(bot.transform, false);
            camPivot.transform.localPosition = new Vector3(0, 1.45f, 0);

            var bodyRenderer = body.GetComponentInChildren<Renderer>();
            bodyRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Friendly.mat");

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
            pcSo.FindProperty("friendlyMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Friendly.mat");
            pcSo.FindProperty("enemyMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Enemy.mat");
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

        static GameObject CreateSportAvatar(Transform parent)
        {
            // Amendment 2: the shared procedural humanoid (spec 15, v2.1). The prefab carries
            // the default rig; the local player's stored rig is applied at spawn by
            // PlayerController.ApplyStoredBodyRig, so gameplay geometry is never duplicated.
            var root = new GameObject("Body");
            root.transform.SetParent(parent, false);
            var motion = root.AddComponent<SportAvatarAnimation>();
            var parts = Hotdrop.Player.AvatarRigVisual.Build(root, Hotdrop.Player.BodyRigs.Default,
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Friendly.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Equipment.mat"),
                includeGun: false);
            Hotdrop.Player.AvatarRigVisual.Wire(motion, parts);
            return root;
        }

        /// <summary>The humanoid's shared skin/dark materials as assets so prefabs can reference them.</summary>
        static void EnsureAvatarMaterials()
        {
            EnsureMaterial("AvatarSkin", new Color(0.78f, 0.60f, 0.46f), 0.25f);
            EnsureMaterial("AvatarDark", new Color(0.14f, 0.16f, 0.19f), 0.3f);
        }

        static void EnsureMaterial(string name, Color colour, float smoothness)
        {
            const string folder = "Assets/Resources/Materials";
            string path = folder + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            material.color = colour;
            material.SetFloat("_Smoothness", smoothness);
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "Materials");
            AssetDatabase.CreateAsset(material, path);
        }

        static Transform Part(Transform parent, string name, Vector3 position, Vector3 size, string material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/" + material + ".mat");
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part.transform;
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
            visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Enemy.mat");

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
            visual.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Objective.mat");
            
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





