#if DEVELOPMENT_BUILD
using System.IO;
using UnityEngine;

namespace Hotdrop.Core
{
    /// <summary>
    /// Development builds only: mirrors every Unity log message (including exceptions and
    /// their stack traces) to persistentDataPath/dev_log.txt so on-device failures can be
    /// read with adb even when the device's logcat does not carry managed output.
    /// </summary>
    static class DevFileLog
    {
        static readonly object gate = new object();
        static string path;
        // logMessageReceivedThreaded fires from whatever thread logged (Photon's networking
        // threads included), and Time.realtimeSinceStartup is main-thread only; a Stopwatch
        // started here (main thread, at install) is safe to read from any thread.
        static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Install()
        {
            path = Path.Combine(Application.persistentDataPath, "dev_log.txt");
            try { File.WriteAllText(path, $"--- session {System.DateTime.UtcNow:o} {Application.version}\n"); } catch { return; }
            Application.logMessageReceivedThreaded += (condition, stack, type) =>
            {
                double t = clock.Elapsed.TotalSeconds;
                string line = type == LogType.Log || type == LogType.Warning
                    ? $"{t:0.00} {type}: {condition}\n"
                    : $"{t:0.00} {type}: {condition}\n{stack}\n";
                lock (gate)
                {
                    try { File.AppendAllText(path, line); } catch { }
                }
            };
        }
    }
}
#endif

