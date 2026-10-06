using UnityEngine;

namespace Hotdrop.Config
{
    public enum QualityPreset { Performance, Balanced, Cinematic }

    // Spec section 24: Android performance budgets.
    [CreateAssetMenu(fileName = "GraphicsConfig", menuName = "Hotdrop/Config/Graphics Config")]
    public class GraphicsConfig : ScriptableObject
    {
        [Header("Frame Targets")]
        [UnityEngine.Serialization.FormerlySerializedAs("lowPresetTargetFps")]
        public int performanceTargetFps = 60;
        [UnityEngine.Serialization.FormerlySerializedAs("mediumHighPresetTargetFps")]
        public int balancedTargetFps = 60;
        public int cinematicPresetTargetFps = 30;
        [Range(0.5f, 1f)] public float performanceRenderScale = 0.8f;
        [Range(0.5f, 1f)] public float balancedRenderScale = 1f;
        [Range(0.5f, 1f)] public float cinematicRenderScale = 1f;

        [Header("Low Preset Scene Budget")]
        public int lowPresetMaxDrawCalls = 150;
        public int lowPresetMaxVisibleTriangles = 250000;
        [Range(0, 3)] public int performanceTextureMipmapLimit = 1;
        [Range(0.1f, 2f)] public float performanceLodBias = 0.7f;
        [Range(0, 3)] public int performanceMaximumLodLevel = 1;
        [Range(8, 2048)] public int performanceMaximumParticlesPerEffect = 256;
        [Range(0.25f, 1f)] public float performanceParticleLifetimeScale = 0.75f;
        [Range(0.25f, 1f)] public float performanceParticleSizeScale = 0.9f;

        [Header("Footprint Targets (memory values are estimates until real-device profiling)")]
        public int targetDownloadMb = 250;
        public int targetInstalledMb = 600;
        public int targetClientMemoryMbLow = 1536;
        public int targetClientMemoryMbStandard = 2500;

        [Header("Timing Targets")]
        public float targetReturningStartupSeconds = 8f;
        public float targetArenaLoadSeconds = 10f;

        [Header("Networking Target")]
        public float targetKbPerSecondPerClient = 100f;

        public bool dynamicResolution = true;
        public bool bakedLightingOnLow = true;
        public bool realtimeShadowsOnLow = false;
        public bool bloomOnLow = false;
        public bool motionBlurOnLow = false;
        public bool depthOfFieldOnLow = false;
    }
}


