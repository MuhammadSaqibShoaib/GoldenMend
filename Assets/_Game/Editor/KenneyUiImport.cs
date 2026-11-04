using UnityEditor;
using UnityEngine;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Import settings for the Kenney UI Pack and Game Icons: every PNG becomes an uncompressed UI sprite, and buttons,
    /// inputs and checkboxes get 9-slice borders so they stretch cleanly. Also sets up the game's sounds and music.
    /// </summary>
    public sealed class KenneyUiImport : AssetPostprocessor
    {
        const string Folder = "Assets/ThirdParty/Kenney/";

        // Bump when the settings below change so Unity reimports the pack.
        public override uint GetVersion() => 5;

        // The game's own sounds: short effects decompress on load for zero-latency playback, the music streams;
        // everything is mono to halve memory.
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/_Game/Content/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            bool music = assetPath.Contains("/Music/");
            bool jingle = assetPath.EndsWith("/win.ogg") || assetPath.EndsWith("/fail.ogg");
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : jingle ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.5f : 0.7f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music;
        }

        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/_Game/Content/Art/"))
            {
                // Paintings: sampled at many sizes, so keep mipmaps; 1024 is plenty for a phone vessel.
                var art = (TextureImporter)assetImporter;
                art.textureType = TextureImporterType.Default;
                art.mipmapEnabled = true;
                art.wrapMode = TextureWrapMode.Clamp;
                art.filterMode = FilterMode.Trilinear;
                art.maxTextureSize = 1024;
                art.textureCompression = TextureImporterCompression.CompressedHQ;
                art.npotScale = TextureImporterNPOTScale.None;
                return;
            }

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
