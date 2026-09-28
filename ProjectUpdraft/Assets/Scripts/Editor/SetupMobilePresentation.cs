using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Updraft.Editor
{
    public static class SetupMobilePresentation
    {
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Rendering")) AssetDatabase.CreateFolder("Assets", "Rendering");
            const string path = "Assets/Rendering/UpdraftLow.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/Rendering/UpdraftForward.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, path);
            }
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            var settings = new SerializedObject(pipeline);
            settings.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 0;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(0, false);
            EditorUtility.SetDirty(pipeline);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.productName = "UPDRAFT";
            // Persist original palette materials so mobile shader stripping retains them.
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Materials")) AssetDatabase.CreateFolder("Assets/Resources", "Materials");
            string[] names = { "Concrete", "Structure", "Cover", "Friendly", "Enemy", "Objective", "Equipment" };
            string[] colors = { "#C3B59E", "#49627B", "#7A8D9D", "#30D5E8", "#FF6376", "#FFD166", "#24374B" };
            for (int i = 0; i < names.Length; i++)
            {
                string materialPath = "Assets/Resources/Materials/" + names[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                ColorUtility.TryParseHtmlString(colors[i], out var color);
                material.color = color; material.SetFloat("_Smoothness", 0.08f); material.enableInstancing = true;
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
