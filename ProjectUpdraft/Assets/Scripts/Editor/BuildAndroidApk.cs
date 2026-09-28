using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Updraft.Editor
{
    /// <summary>
    /// Local device playtest build: a directly installable development APK
    /// (not the AAB store artifact from BuildAndroidTest). Callable via
    /// `-executeMethod Updraft.Editor.BuildAndroidApk.Run`.
    /// </summary>
    public static class BuildAndroidApk
    {
        [MenuItem("Tools/Updraft/Build Android Dev APK")]
        public static void Run()
        {
            // Android's SDK manager rejects present-but-empty proxy values.
            foreach (string proxy in new[] { "HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY" })
                if (string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable(proxy)))
                    System.Environment.SetEnvironmentVariable(proxy, null);
            // Set this inside the editor: Windows GUI launches can lose shell
            // environment overrides before Unity spawns its Java child process.
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string project = System.IO.Directory.GetParent(Application.dataPath).FullName;
                string socketFallback = System.IO.Path.Combine(project, "Builds", "JavaSocketFallback");
                string prior = System.Environment.GetEnvironmentVariable("JAVA_TOOL_OPTIONS") ?? "";
                if (!prior.Contains("jdk.net.unixdomain.tmpdir"))
                    System.Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS", prior + " -Djdk.net.unixdomain.tmpdir=\"" + socketFallback + "\"");
            }
            ConfigureAndroidBuild.Run();
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Android/UpdraftDev.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            Debug.Log($"Build result: {report.summary.result}, " +
                      $"errors: {report.summary.totalErrors}, warnings: {report.summary.totalWarnings}, " +
                      $"size: {report.summary.totalSize} bytes, time: {report.summary.totalTime}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Android APK build failed. See the build log for the actual cause.");
        }
    }
}
