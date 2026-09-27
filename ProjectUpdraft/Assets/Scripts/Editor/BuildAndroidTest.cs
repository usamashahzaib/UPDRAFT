using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Updraft.Editor
{
    /// <summary>
    /// Milestone A acceptance check: does the toolchain actually produce an
    /// Android build, not just compile in the Editor? Callable via
    /// `-executeMethod Updraft.Editor.BuildAndroidTest.Run`.
    /// </summary>
    public static class BuildAndroidTest
    {
        [MenuItem("Tools/Updraft/Build Android Test AAB")]
        public static void Run()
        {
            ConfigureAndroidBuild.Run();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Android/UpdraftTest.aab",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            Debug.Log($"Build result: {report.summary.result}, " +
                      $"errors: {report.summary.totalErrors}, warnings: {report.summary.totalWarnings}, " +
                      $"size: {report.summary.totalSize} bytes, time: {report.summary.totalTime}");
        }
    }
}
