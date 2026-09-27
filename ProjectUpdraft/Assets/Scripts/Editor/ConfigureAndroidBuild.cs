using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Updraft.Editor
{
    /// <summary>
    /// Applies the Android constraints from the master specification. The
    /// publisher-owned application identifier and signing credentials remain
    /// intentionally unset until the publisher supplies them.
    /// </summary>
    public static class ConfigureAndroidBuild
    {
        [MenuItem("Tools/Updraft/Configure Android Build")]
        public static void Run()
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = true;

            // A build-time placeholder only — Unity refuses to build Android
            // without SOME applicationIdentifier set. The real publisher-owned
            // package name (spec section 24: "Package Name: com.[publisher].updraft")
            // must replace this before any store submission or signed release.
            if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.updraft.placeholder");

            AssetDatabase.SaveAssets();
            Debug.Log("Configured Android for API 28 minimum, current installed target API, IL2CPP, ARM64, and AAB output. Application ID is a placeholder (com.updraft.placeholder) and signing still requires publisher-owned values.");
        }
    }
}
