using UnityEditor;
using UnityEngine;
using Fusion.Photon.Realtime;

namespace Hotdrop.Editor
{
    public static class ConfigureFusionDependencies
    {
        public static void Run()
        {
            var settings = PhotonAppSettings.Global;
            settings.AppSettings.AppIdFusion = "a91a9f6e-7470-44fb-a32d-de6db0a910ef";
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("User-supplied Fusion App ID saved. No online match or dedicated-server acceptance implied.");
        }
    }
}

