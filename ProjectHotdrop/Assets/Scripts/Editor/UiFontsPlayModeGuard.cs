using UnityEditor;

namespace Hotdrop.Editor
{
    /// <summary>
    /// The runtime installs a generated font asset as TMP's default; this puts the project's own
    /// default back when Play Mode ends so the TMP settings asset never keeps a transient reference.
    /// </summary>
    [InitializeOnLoad]
    static class UiFontsPlayModeGuard
    {
        static UiFontsPlayModeGuard()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    Hotdrop.UI.UIFonts.RestoreShippedDefault();
            };
        }
    }
}
