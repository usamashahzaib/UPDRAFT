using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Hotdrop.Editor
{
    /// <summary>
    /// Applies the Android constraints from the master specification. The
    /// publisher-owned application identifier is fixed here. Release signing
    /// credentials remain external and must never be committed.
    /// </summary>
    public static class ConfigureAndroidBuild
    {
        [MenuItem("Tools/Hotdrop/Configure Android Build")]
        public static void Run()
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.companyName = "Techspirex";
            PlayerSettings.productName = "HOTDROP";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.techspirex.hotdrop");

            AssetDatabase.SaveAssets();
            Debug.Log("Configured Android for API 28 minimum, current installed target API, IL2CPP, ARM64, AAB output, Techspirex publisher branding, and application ID com.techspirex.hotdrop. Release signing still requires publisher-controlled values.");
        }
    }
}




