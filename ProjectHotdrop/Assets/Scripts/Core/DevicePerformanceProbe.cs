using UnityEngine;
using UnityEngine.Profiling;
#if DEVELOPMENT_BUILD
using System.Runtime.InteropServices;
using System.Text;
using Unity.Profiling;
using Hotdrop.Config;
#endif

namespace Hotdrop.Core
{
    /// <summary>Development-build frame evidence after a ten-second warmup.</summary>
    public class DevicePerformanceProbe : MonoBehaviour
    {
#if DEVELOPMENT_BUILD
        float warmup = 10f, elapsed;
        float processMemorySampleElapsed;
        readonly StringBuilder thermalTimeline = new StringBuilder(96);
        int thermalSampleCount;
        bool thermalApiAvailable;
        string thermalPlatform = "unsupported";
        int frames, slowFrames;
        long peakDrawCalls, peakTriangles;
        bool sawDrawCalls, sawTriangles;
        int peakProcessPssMb;
        bool sawProcessPss;
        int drawCallTarget, triangleTarget;
        int configMemoryTargetMb;
        ProfilerRecorder drawCallsRecorder, trianglesRecorder;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass androidDebug;
        AndroidJavaObject androidPowerManager;
        bool processPssUnavailable;
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int HotdropGetThermalState();
#endif

        void Awake()
        {
            var config = GameBootstrap.Instance != null ? GameBootstrap.Instance.graphicsConfig : null;
            if (config == null) config = Resources.Load<GraphicsConfig>("Config/GraphicsConfig");
            drawCallTarget = config != null
                ? config.lowPresetMaxDrawCalls
                : GraphicsConfig.DefaultPerformanceTargetDrawCalls;
            triangleTarget = config != null ? config.lowPresetMaxVisibleTriangles : 250000;
            configMemoryTargetMb = config != null ? config.targetClientMemoryMbLow : 0;
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 16);
            trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 16);
#if UNITY_ANDROID && !UNITY_EDITOR
            try { androidDebug = new AndroidJavaClass("android.os.Debug"); }
            catch (System.Exception) { processPssUnavailable = true; }
            InitializeAndroidThermal();
#elif UNITY_IOS && !UNITY_EDITOR
            thermalPlatform = "ios";
            thermalApiAvailable = true;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            thermalPlatform = "android";
#endif
        }

        void OnDestroy()
        {
            drawCallsRecorder.Dispose();
            trianglesRecorder.Dispose();
#if UNITY_ANDROID && !UNITY_EDITOR
            androidDebug?.Dispose();
            androidPowerManager?.Dispose();
#endif
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) ResetWindow(rewarm: true);
        }

        void Update()
        {
            if (warmup > 0) { warmup -= Time.unscaledDeltaTime; return; }
            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (Time.unscaledDeltaTime > 0.05f) slowFrames++;
            processMemorySampleElapsed += Time.unscaledDeltaTime;
            if (processMemorySampleElapsed >= 1f)
            {
                processMemorySampleElapsed = 0f;
                SampleProcessPss();
                SampleThermalState();
            }
            if (drawCallsRecorder.Valid)
            {
                sawDrawCalls = true;
                peakDrawCalls = System.Math.Max(peakDrawCalls, drawCallsRecorder.LastValue);
            }
            if (trianglesRecorder.Valid)
            {
                sawTriangles = true;
                peakTriangles = System.Math.Max(peakTriangles, trianglesRecorder.LastValue);
            }
            if (elapsed < 30f) return;
            int quality = Mathf.Clamp(PlayerPrefs.GetInt(OfflinePauseMenu.GraphicsQualityKey, 0), 0, 2);
            string preset = quality == 0 ? "performance" : quality == 1 ? "balanced" : "cinematic";
            string drawBudget = quality == 0 ? Budget(peakDrawCalls, sawDrawCalls, drawCallTarget) : "not_applicable";
            string triangleBudget = quality == 0 ? Budget(peakTriangles, sawTriangles, triangleTarget) : "not_applicable";
            string memoryBudget = quality == 0 ? Budget(peakProcessPssMb, sawProcessPss, configMemoryTargetMb) : "not_applicable";
            Debug.Log($"HOTDROP_PERF duration={elapsed:F1}s frames={frames} average_fps={frames / elapsed:F1} frames_over_50ms={slowFrames} unity_allocated_mb={Profiler.GetTotalAllocatedMemoryLong() / 1048576} peak_sampled_process_pss_mb={Metric(peakProcessPssMb, sawProcessPss)} process_memory_budget={memoryBudget} thermal_platform={thermalPlatform} thermal_available={thermalApiAvailable.ToString().ToLowerInvariant()} thermal_samples={thermalSampleCount} thermal_timeline={ThermalTimeline()} preset={preset} peak_draw_calls={Metric(peakDrawCalls, sawDrawCalls)} draw_calls_budget={drawBudget} peak_triangles={Metric(peakTriangles, sawTriangles)} triangles_budget={triangleBudget} model={SystemInfo.deviceModel}");
            ResetWindow(rewarm: false);
        }

        static string Metric(long peak, bool available) => available ? peak.ToString() : "unavailable";

        static string Metric(int peak, bool available) => available ? peak.ToString() : "unavailable";

        string ThermalTimeline() => thermalSampleCount == 0 ? "unavailable" : thermalTimeline.ToString();

        void InitializeAndroidThermal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            thermalPlatform = "android";
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") < 29) return;
                }
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    androidPowerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
                thermalApiAvailable = androidPowerManager != null;
            }
            catch (System.Exception)
            {
                thermalApiAvailable = false;
                androidPowerManager?.Dispose();
                androidPowerManager = null;
            }
#endif
        }

        void SampleThermalState()
        {
            int state = -1;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (thermalApiAvailable && androidPowerManager != null)
            {
                try { state = androidPowerManager.Call<int>("getCurrentThermalStatus"); }
                catch (System.Exception) { thermalApiAvailable = false; }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (thermalApiAvailable)
            {
                try
                {
                    state = HotdropGetThermalState();
                    if (state < 0) thermalApiAvailable = false;
                }
                catch (System.Exception) { thermalApiAvailable = false; }
            }
#endif
            if (thermalSampleCount > 0) thermalTimeline.Append(',');
            thermalTimeline.Append(state);
            thermalSampleCount++;
        }

        void SampleProcessPss()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (processPssUnavailable || androidDebug == null) return;
            try
            {
                long pssKb = androidDebug.CallStatic<long>("getPss");
                if (pssKb <= 0) return;
                int pssMb = (int)System.Math.Min(int.MaxValue, (pssKb + 1023) / 1024);
                sawProcessPss = true;
                peakProcessPssMb = System.Math.Max(peakProcessPssMb, pssMb);
            }
            catch (System.Exception)
            {
                processPssUnavailable = true;
            }
#endif
        }

        static string Budget(long peak, bool available, int target)
        {
            if (!available) return "unavailable";
            if (target <= 0) return "not_configured";
            return $"{(peak <= target ? "within" : "over")}_{target}";
        }

        void ResetWindow(bool rewarm)
        {
            warmup = rewarm ? 10f : 0f;
            elapsed = 0f;
            frames = slowFrames = 0;
            peakDrawCalls = peakTriangles = 0;
            peakProcessPssMb = 0;
            processMemorySampleElapsed = 0f;
            sawDrawCalls = sawTriangles = false;
            sawProcessPss = false;
            thermalTimeline.Length = 0;
            thermalSampleCount = 0;
        }
#endif
    }
}

