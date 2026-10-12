using UnityEditor;

namespace Hotdrop.Editor
{
    /// <summary>
    /// Art rasterised from the UIUX HTML is drawn at its reference size, so it must not be
    /// rescaled to a power of two or block-compressed: either blurs hairline strokes and type.
    /// </summary>
    public class UiuxArtImporter : AssetPostprocessor
    {
        const string Folder = "Assets/Resources/UIUX/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;

            // Weapon portraits are transparent silhouettes drawn in small HUD cards and on
            // touch controls. Mips soften their antialiased alpha edges and waste memory.
            if (assetPath.StartsWith(Folder + "WeaponPortraits/"))
            {
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.filterMode = UnityEngine.FilterMode.Bilinear;
                return;
            }

            importer.mipmapEnabled = true;
            importer.maxTextureSize = 4096;
        }
    }
}
