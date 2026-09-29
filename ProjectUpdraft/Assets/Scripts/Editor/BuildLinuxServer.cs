using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Updraft.Editor
{
    /// <summary>
    /// Dedicated Linux headless server binary for Gate C acceptance. Callable via
    /// `-executeMethod Updraft.Editor.BuildLinuxServer.Run`. Requires the
    /// "Linux Build Support (Server)" module. Development flags on: acceptance needs
    /// logs, not store hardening.
    /// </summary>
    public static class BuildLinuxServer
    {
        [MenuItem("Tools/Updraft/Build Linux Dedicated Server")]
        public static void Run()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Linux/UpdraftServer.x86_64",
                target = BuildTarget.StandaloneLinux64,
                subtarget = StandaloneBuildSubtarget.Server,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            Debug.Log($"Linux server build: {report.summary.result}, " +
                      $"size: {report.summary.totalSize} bytes, time: {report.summary.totalTime}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Linux server build failed. See the build log for the actual cause.");

            EditorApplication.isPlaying = false;
            Application.Quit(0);
        }
    }
}
