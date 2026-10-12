using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hotdrop.Editor
{
    public static class SetupMobilePresentation
    {
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Rendering")) AssetDatabase.CreateFolder("Assets", "Rendering");
            const string path = "Assets/Rendering/HotdropLow.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/Rendering/HotdropForward.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, path);
            }
            // The runtime Performance preset disables HDR on its camera and post volume.
            // Keep the shared pipeline capable of HDR so Balanced/Cinematic can opt in.
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            var settings = new SerializedObject(pipeline);
            settings.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            settings.FindProperty("m_MainLightShadowmapResolution").intValue = (int)UnityEngine.Rendering.Universal.ShadowResolution._1024;
            settings.FindProperty("m_ShadowCascadeCount").intValue = 2;
            settings.FindProperty("m_SoftShadowsSupported").boolValue = true;
            settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureCinematicSsaoVariants(pipeline);
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 45f;
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
            PlayerSettings.productName = "HOTDROP";
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

        /// <summary>
        /// Keep both the SSAO-off variant (Performance/Balanced) and the configured Cinematic
        /// variant in player builds. Strip only SSAO keywords that the Cinematic feature does not use.
        /// </summary>
        public static void ConfigureCinematicSsaoVariants(UniversalRenderPipelineAsset pipeline)
        {
            if (pipeline == null) return;
            var settings = new SerializedObject(pipeline);
            void Set(string name, System.Action<SerializedProperty> apply)
            {
                var property = settings.FindProperty(name);
                if (property != null) apply(property);
                else Debug.LogWarning("URP SSAO variant setting missing: " + name);
            }

            // Enum order in URP 17.3: Remove=0, Select=1, SelectOnly=2.
            Set("m_PrefilteringModeScreenSpaceOcclusion", p => p.enumValueIndex = 1);
            Set("m_PrefilterSSAODepthNormals", p => p.boolValue = true);
            Set("m_PrefilterSSAOSourceDepthLow", p => p.boolValue = true);
            Set("m_PrefilterSSAOSourceDepthMedium", p => p.boolValue = false);
            Set("m_PrefilterSSAOSourceDepthHigh", p => p.boolValue = true);
            Set("m_PrefilterSSAOInterleaved", p => p.boolValue = false);
            Set("m_PrefilterSSAOBlueNoise", p => p.boolValue = true);
            Set("m_PrefilterSSAOSampleCountLow", p => p.boolValue = false);
            Set("m_PrefilterSSAOSampleCountMedium", p => p.boolValue = true);
            Set("m_PrefilterSSAOSampleCountHigh", p => p.boolValue = true);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }
    }
}

