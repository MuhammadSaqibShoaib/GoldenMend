using UnityEditor;
using UnityEngine;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Import settings for the Kenney UI Pack and Game Icons: every PNG becomes an uncompressed UI sprite, and buttons,
    /// inputs and checkboxes get 9-slice borders so they stretch cleanly.
    /// </summary>
    public sealed class KenneyUiImport : AssetPostprocessor
    {
        const string Folder = "Assets/ThirdParty/Kenney/";

        // Bump when the settings below change so Unity reimports the pack.
        public override uint GetVersion() => 3;

        // Short effects decompress on load for zero-latency playback; everything is mono to halve memory.
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = assetPath.Contains("/Jingles/") ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = BorderFor(System.IO.Path.GetFileNameWithoutExtension(assetPath));
        }

        // x = left, y = bottom, z = right, w = top. "depth" variants carry a darker lip at the bottom.
        static Vector4 BorderFor(string name)
        {
            bool depth = name.Contains("_depth_");
            if (name.StartsWith("button_") || name.StartsWith("input_") || name.StartsWith("check_square"))
                return new Vector4(16f, depth ? 22f : 16f, 16f, 16f);
            if (name.StartsWith("slide_horizontal")) return new Vector4(12f, 0f, 12f, 0f);
            if (name.StartsWith("slide_vertical")) return new Vector4(0f, 12f, 0f, 12f);
            return Vector4.zero;
        }
    }
}
