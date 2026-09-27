using UnityEngine;
using UnityEditor;

namespace Updraft.Editor
{
    /// <summary>
    /// The gameplay code assigns objects to custom layers (Ground, Player, Pad,
    /// Zone, Cover, SpawnBarrier) for collision/raycast filtering, and custom
    /// tags (Player, Head) for hit identification — but a fresh project has
    /// neither defined. LayerMask.NameToLayer/CompareTag against an undefined
    /// name logs an error every time (caught by the PlayMode smoke test, which
    /// fails on any logged error). This registers both, once.
    /// </summary>
    public static class SetupProjectLayers
    {
        static readonly string[] RequiredLayers = { "Ground", "Player", "Pad", "Zone", "Cover", "SpawnBarrier" };
        static readonly string[] RequiredTags = { "Player", "Head" };

        [MenuItem("Tools/Updraft/Setup Project Layers")]
        public static void EnsureLayersExist()
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
            {
                Debug.LogError("SetupProjectLayers: could not load ProjectSettings/TagManager.asset");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layersProp = tagManager.FindProperty("layers");

            foreach (var layerName in RequiredLayers)
            {
                if (LayerAlreadyExists(layersProp, layerName)) continue;

                int freeIndex = FindFirstFreeUserLayer(layersProp);
                if (freeIndex < 0)
                {
                    Debug.LogError($"SetupProjectLayers: no free user layer slots left for '{layerName}'.");
                    continue;
                }

                layersProp.GetArrayElementAtIndex(freeIndex).stringValue = layerName;
            }

            var tagsProp = tagManager.FindProperty("tags");
            foreach (var tagName in RequiredTags)
            {
                if (TagAlreadyExists(tagsProp, tagName)) continue;

                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static bool TagAlreadyExists(SerializedProperty tagsProp, string tagName)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return true;
            }
            return false;
        }

        static bool LayerAlreadyExists(SerializedProperty layersProp, string layerName)
        {
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName) return true;
            }
            return false;
        }

        // User-definable layers start at index 8 (0-7 are Unity built-ins).
        static int FindFirstFreeUserLayer(SerializedProperty layersProp)
        {
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layersProp.GetArrayElementAtIndex(i).stringValue)) return i;
            }
            return -1;
        }
    }
}
