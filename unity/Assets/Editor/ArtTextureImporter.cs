#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Reveal.EditorTools
{
    /// <summary>
    /// Forces sane, crisp import settings for every image under
    /// Assets/Resources/Art/. Without this, Unity's default texture import
    /// (lossy block compression + auto-generated mipmaps) visibly blurs and
    /// discolors gradient-heavy art (the logo in particular) once it's
    /// scaled down for UI — this fixes that at the source.
    ///
    /// Only applies to NEW imports; if an asset already has a .meta with
    /// stale settings, use Assets > Reimport on the Art folder once after
    /// pulling this change.
    /// </summary>
    public class ArtTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("Resources/Art/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }
    }
}
#endif
