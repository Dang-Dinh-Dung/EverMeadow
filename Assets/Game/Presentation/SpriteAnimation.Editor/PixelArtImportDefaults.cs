using UnityEditor;
using UnityEngine;

namespace Evermeadow.Presentation.SpriteAnimation.Editor
{
    /// <summary>
    /// Pixel-art defaults for PNGs dropped under Assets/Game/Rigs, applied on their first import only (art bible §8.1, §8.8):
    /// Sprite, Multiple (ready to slice in the Sprite Editor), PPU 16, Point, Clamp, no mipmaps, uncompressed. Slicing and pivots
    /// stay yours; settings you change afterwards are never overwritten.
    /// </summary>
    public sealed class PixelArtImportDefaults : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing) return; // only the very first import
            if (!assetPath.StartsWith(AnimationAssetMenu.Root + "/") || !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
