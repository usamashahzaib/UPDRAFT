using UnityEngine;
using UnityEngine.Profiling;

namespace Hotdrop.Core
{
    /// <summary>Development-build frame evidence after a ten-second warmup.</summary>
    public class DevicePerformanceProbe : MonoBehaviour
    {
#if DEVELOPMENT_BUILD
        float warmup = 10f, elapsed;
        int frames, slowFrames;
        void OnApplicationPause(bool paused)
        {
            warmup = 10f; elapsed = 0; frames = slowFrames = 0;
        }
        void Update()
        {
            if (warmup > 0) { warmup -= Time.unscaledDeltaTime; return; }
            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (Time.unscaledDeltaTime > 0.05f) slowFrames++;
            if (elapsed < 30f) return;
            Debug.Log($"HOTDROP_PERF duration={elapsed:F1}s frames={frames} average_fps={frames / elapsed:F1} frames_over_50ms={slowFrames} unity_allocated_mb={Profiler.GetTotalAllocatedMemoryLong() / 1048576} model={SystemInfo.deviceModel}");
            elapsed = 0; frames = slowFrames = 0;
        }
#endif
    }
}

