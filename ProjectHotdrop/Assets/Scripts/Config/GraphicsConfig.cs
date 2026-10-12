using UnityEngine;

namespace Hotdrop.Config
{
    public enum QualityPreset { Performance, Balanced, Cinematic }

    // Spec section 24: Android performance budgets.
    [CreateAssetMenu(fileName = "GraphicsConfig", menuName = "Hotdrop/Config/Graphics Config")]
    public class GraphicsConfig : ScriptableObject
    {
        public const int DefaultPerformanceTargetFps = 60;
        public const int DefaultBalancedTargetFps = 60;
        public const int DefaultCinematicTargetFps = 30;
        public const int DefaultPerformanceTargetDrawCalls = 120;
        public const float DefaultPerformanceRenderScale = 0.8f;
        public const float DefaultBalancedRenderScale = 1f;
        public const float DefaultCinematicRenderScale = 1f;

        [Header("Frame Targets")]
        [UnityEngine.Serialization.FormerlySerializedAs("lowPresetTargetFps")]
        public int performanceTargetFps = DefaultPerformanceTargetFps;
        [UnityEngine.Serialization.FormerlySerializedAs("mediumHighPresetTargetFps")]
        public int balancedTargetFps = DefaultBalancedTargetFps;
        public int cinematicPresetTargetFps = DefaultCinematicTargetFps;
        [Range(0.5f, 1f)] public float performanceRenderScale = DefaultPerformanceRenderScale;
        [Range(0.5f, 1f)] public float balancedRenderScale = DefaultBalancedRenderScale;
        [Range(0.5f, 1f)] public float cinematicRenderScale = DefaultCinematicRenderScale;

        [Header("Low Preset Scene Budget")]
        [Tooltip("Measured draw-call target for the Performance preset. Reported by the development probe; not enforced as a runtime cap.")]
        public int lowPresetMaxDrawCalls = DefaultPerformanceTargetDrawCalls;
        [Tooltip("Measured rendered-triangle target for the Performance preset. Reported by the development probe; not enforced as a runtime cap.")]
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

        [Tooltip("Keep false until adaptive resolution has a runtime policy and is accepted. Enabling this flag alone does not change render resolution.")]
        public bool dynamicResolution = false;
        public bool bakedLightingOnLow = true;
        public bool realtimeShadowsOnLow = false;
        public bool bloomOnLow = false;
        public bool motionBlurOnLow = false;
        public bool depthOfFieldOnLow = false;
    }
}


