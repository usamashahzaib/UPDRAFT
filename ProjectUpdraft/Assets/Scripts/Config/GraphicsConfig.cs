using UnityEngine;

namespace Updraft.Config
{
    public enum QualityPreset { Low, Medium, High }

    // Spec section 24: Android performance budgets.
    [CreateAssetMenu(fileName = "GraphicsConfig", menuName = "Updraft/Config/Graphics Config")]
    public class GraphicsConfig : ScriptableObject
    {
        [Header("Frame Targets")]
        public int lowPresetTargetFps = 30;
        public int mediumHighPresetTargetFps = 60;

        [Header("Low Preset Scene Budget")]
        public int lowPresetMaxDrawCalls = 150;
        public int lowPresetMaxVisibleTriangles = 250000;

        [Header("Footprint Targets")]
        public int targetDownloadMb = 250;
        public int targetInstalledMb = 600;
        public int targetClientMemoryMbLow = 750;

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
        public bool ssaoOnLow = false;
    }
}
