using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Updraft.Editor
{
    /// <summary>
    /// The Play Store artifact: a signed, non-development AAB. Refuses to build while any
    /// value that would be permanent or rejected by Play is still a placeholder — a package
    /// name can never change after the first upload, and a debug-signed bundle is rejected.
    ///
    /// Callable via `-executeMethod Updraft.Editor.BuildAndroidRelease.Run` with:
    ///   UPDRAFT_ANDROID_PACKAGE   e.g. com.yourstudio.updraft (permanent once uploaded)
    ///   UPDRAFT_VERSION_CODE      integer, higher than every earlier upload
    ///   UPDRAFT_VERSION_NAME      optional, e.g. 1.0.0
    ///   UPDRAFT_KEYSTORE_PATH, UPDRAFT_KEYSTORE_PASS, UPDRAFT_KEY_ALIAS, UPDRAFT_KEY_PASS
    /// Passwords are applied for this editor session only and are not written to ProjectSettings.
    /// </summary>
    public static class BuildAndroidRelease
    {
        const int PlayTargetApi = 36;
        const string OutputPath = "Builds/Android/Updraft.aab";

        [MenuItem("Tools/Updraft/Build Android Release AAB")]
        public static void Run()
        {
            var problems = new List<string>();
            string package = Env("UPDRAFT_ANDROID_PACKAGE");
            if (string.IsNullOrEmpty(package)) problems.Add("UPDRAFT_ANDROID_PACKAGE is not set.");
            else if (IsPlaceholderPackage(package)) problems.Add($"'{package}' is a placeholder package name; choose the permanent publisher-owned id.");

            if (!int.TryParse(Env("UPDRAFT_VERSION_CODE"), out int versionCode) || versionCode < 1)
                problems.Add("UPDRAFT_VERSION_CODE must be a positive integer higher than every earlier upload.");

            string keystore = Env("UPDRAFT_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(keystore) || !System.IO.File.Exists(keystore)) problems.Add("UPDRAFT_KEYSTORE_PATH must point to the upload keystore.");
            foreach (var name in new[] { "UPDRAFT_KEYSTORE_PASS", "UPDRAFT_KEY_ALIAS", "UPDRAFT_KEY_PASS" })
                if (string.IsNullOrEmpty(Env(name))) problems.Add($"{name} is not set.");

            if (problems.Count > 0)
                throw new InvalidOperationException("Release build refused:\n - " + string.Join("\n - ", problems));

            ConfigureAndroidBuild.Run();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, package);
            PlayerSettings.Android.bundleVersionCode = versionCode;
            string versionName = Env("UPDRAFT_VERSION_NAME");
            if (!string.IsNullOrEmpty(versionName)) PlayerSettings.bundleVersion = versionName;
            // Pin the target API instead of "highest installed": a preview SDK on the build
            // machine must not silently become the store target.
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)PlayTargetApi;

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = Env("UPDRAFT_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Env("UPDRAFT_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = Env("UPDRAFT_KEY_PASS");
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;

            var backend = Updraft.Config.BackendConfig.Load();
            bool online = backend != null && !string.IsNullOrWhiteSpace(backend.baseUrl) &&
                          !string.Equals(backend.environmentName, "dev", StringComparison.OrdinalIgnoreCase) &&
                          backend.baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            Debug.Log(online
                ? $"[Release] Online play enabled against {backend.baseUrl} ({backend.environmentName})."
                : "[Release] BackendConfig is dev/loopback/HTTP: this build is OFFLINE-ONLY (Play Online shows as unavailable).");

            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Main.unity" },
                    locationPathName = OutputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                });
                Debug.Log($"[Release] {report.summary.result}: {package} v{PlayerSettings.bundleVersion} ({versionCode}), " +
                          $"target API {PlayTargetApi}, {report.summary.totalSize} bytes → {OutputPath}");
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Android release build failed. See the build log for the actual cause.");
            }
            finally
            {
                // Keep secrets out of any later SaveAssets in this editor session.
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
            }
        }

        public static bool IsPlaceholderPackage(string package) =>
            package.StartsWith("com.DefaultCompany.", StringComparison.OrdinalIgnoreCase) ||
            package.StartsWith("com.updraft.placeholder", StringComparison.OrdinalIgnoreCase) ||
            package.StartsWith("com.example.", StringComparison.OrdinalIgnoreCase) ||
            package.StartsWith("com.unity.", StringComparison.OrdinalIgnoreCase);

        static string Env(string name) => (Environment.GetEnvironmentVariable(name) ?? "").Trim();
    }
}
