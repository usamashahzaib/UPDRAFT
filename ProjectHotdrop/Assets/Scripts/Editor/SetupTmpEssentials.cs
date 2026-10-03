using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hotdrop.Editor
{
    /// <summary>
    /// TextMeshPro shows an interactive "Import TMP Essential Resources?" popup
    /// the first time a TMP component is used if those resources aren't already
    /// imported. That popup needs a graphics device to draw, so it throws
    /// "No graphic device is available to initialize the view" under
    /// -batchmode -nographics — exactly the environment automated builds and
    /// CI run in. Importing the essentials package non-interactively up front
    /// avoids the popup ever being scheduled.
    /// </summary>
    public static class SetupTmpEssentials
    {
        /// <summary>
        /// Callable via -executeMethod. Deliberately does NOT combine with -quit
        /// on the command line: AssetDatabase.ImportPackage is asynchronous, so
        /// this hooks the completion callbacks and exits the process itself once
        /// the import actually finishes (or immediately if already imported).
        /// </summary>
        [MenuItem("Tools/Hotdrop/Setup TMP Essential Resources")]
        public static void EnsureImported()
        {
            if (Directory.Exists("Assets/TextMesh Pro/Resources"))
            {
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }

            string packagePath = FindEssentialsPackage();
            if (packagePath == null)
            {
                Debug.LogError("SetupTmpEssentials: could not find 'TMP Essential Resources.unitypackage' under Library/PackageCache.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            if (Application.isBatchMode)
            {
                AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
                AssetDatabase.importPackageFailed += (_, err) =>
                {
                    Debug.LogError($"SetupTmpEssentials: import failed: {err}");
                    EditorApplication.Exit(1);
                };
                AssetDatabase.importPackageCancelled += _ => EditorApplication.Exit(1);
            }

            AssetDatabase.ImportPackage(packagePath, false);
        }

        static string FindEssentialsPackage()
        {
            const string packageCacheRoot = "Library/PackageCache";
            if (!Directory.Exists(packageCacheRoot)) return null;

            foreach (var dir in Directory.GetDirectories(packageCacheRoot, "com.unity.ugui@*"))
            {
                string candidate = Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }
    }
}


