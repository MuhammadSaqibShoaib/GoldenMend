using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using GoldenMend.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Builds the game scene from code so it can be regenerated at any time:
    /// Golden Mend > Rebuild Game Scene, or in batch mode with
    /// -executeMethod GoldenMend.EditorTools.GameSceneBuilder.Build
    /// </summary>
    public static class GameSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Bootstrap/Game.unity";
        const string BootstrapType = "GoldenMend.Bootstrap.GameBootstrap, Assembly-CSharp";
        const string ThemePath = "Assets/_Game/Content/UI/UiTheme.asset";
        const string Kenney = "Assets/ThirdParty/Kenney/UIPack/";
        const string Fonts = "Assets/ThirdParty/Fonts/";
        const string GameIcons = "Assets/_Game/Content/UI/Icons/";

        [MenuItem("Golden Mend/Rebuild Game Scene")]
        public static void Build() => Build(withPaintings: true);

        /// <summary>Builds the scene with the painting art, or with the generated glazes for the Classic build.</summary>
        public static void Build(bool withPaintings)
        {
            var bootstrap = Type.GetType(BootstrapType);
            if (bootstrap == null) throw new InvalidOperationException($"Could not find {BootstrapType}. Has the project compiled?");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 10.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiTheme.Hex(0x8EC5FF);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            var serialized = new SerializedObject(light);
            serialized.FindProperty("m_LightType").intValue = (int)Light2D.LightType.Global;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var game = new GameObject("Game").AddComponent(bootstrap);
            var bootstrapData = new SerializedObject(game);
            bootstrapData.FindProperty("theme").objectReferenceValue = BuildTheme();
            bootstrapData.FindProperty("sounds").objectReferenceValue = BuildSoundBank();
            bootstrapData.FindProperty("art").objectReferenceValue = withPaintings ? BuildArtCatalog() : null;
            bootstrapData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };



            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AndroidBuild.ApplyPlayerSettings();

            AssetDatabase.SaveAssets();
            Debug.Log($"Golden Mend: game scene saved to {ScenePath}");
        }

        /// <summary>Creates or refreshes the UI theme: Titan One and Baloo 2, Kenney Game Icons, the game's own icons and the palette.</summary>
        [MenuItem("Golden Mend/Rebuild UI Theme")]
        public static UiTheme BuildTheme()
        {
            AssetDatabase.ImportAsset("Assets/ThirdParty/Kenney", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            ImportIcons();

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ThemePath));
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            theme.BodyFont = Load<Font>(Fonts + "Fredoka/Fredoka-Medium.ttf");
            theme.TitleFont = Load<Font>(Fonts + "Fredoka/Fredoka-SemiBold.ttf");
            theme.DisplayFont = Load<Font>(Fonts + "Fredoka/Fredoka-Bold.ttf");

            theme.Panel = Load<Sprite>(Kenney + "Sprites/Grey/button_square_flat.png");
            theme.Button = Load<Sprite>(Kenney + "Sprites/Grey/button_rectangle_depth_flat.png");
            theme.IconButton = Load<Sprite>(Kenney + "Sprites/Grey/button_square_depth_flat.png");
            theme.Badge = Load<Sprite>(Kenney + "Sprites/Grey/button_rectangle_flat.png");
            theme.Divider = Load<Sprite>(Kenney + "Sprites/Extra/divider.png");

            theme.Star = Load<Sprite>(Kenney + "Sprites/Grey/star.png");
            theme.Circle = Load<Sprite>(Kenney + "Sprites/Grey/button_round_flat.png");

            const string Icons = "Assets/ThirdParty/Kenney/GameIcons/Icons/";
            theme.IconPause = Load<Sprite>(Icons + "pause.png");
            theme.IconSettings = Load<Sprite>(Icons + "gear.png");
            theme.IconHome = Load<Sprite>(Icons + "home.png");
            theme.IconMuseum = Load<Sprite>(Icons + "menuGrid.png");
            theme.IconLock = Load<Sprite>(Icons + "locked.png");
            theme.IconClose = Load<Sprite>(Icons + "cross.png");
            theme.IconWarning = Load<Sprite>(Icons + "exclamation.png");
            theme.IconSoundOn = Load<Sprite>(Icons + "audioOn.png");
            theme.IconSoundOff = Load<Sprite>(Icons + "audioOff.png");
            theme.IconPlay = Load<Sprite>(Icons + "arrowRight.png");
            theme.IconTrophy = Load<Sprite>(Icons + "trophy.png");
            theme.IconHaptics = Load<Sprite>(Icons + "phone.png");
            theme.IconMusic = Load<Sprite>(Icons + "musicOn.png");
            theme.IconRestart = Load<Sprite>(Icons + "return.png");
            theme.IconHint = Load<Sprite>(GameIcons + "bulb.png");
            theme.IconPull = Load<Sprite>(GameIcons + "pull.png");
            theme.IconPrevious = Load<Sprite>(Icons + "arrowLeft.png");
            theme.IconNext = Load<Sprite>(Icons + "arrowRight.png");

            theme.Click = Load<AudioClip>(Audio + "Sfx/button.ogg");
            theme.Popup = Load<AudioClip>(Audio + "Sfx/popup.ogg");
            theme.StarSound = Load<AudioClip>(Audio + "Sfx/star.ogg");

            // Sky Breeze palette (see the Figma file "Golden Mend – UI Revamp", page Sky Breeze). The defaults in
            // UiTheme are the source of truth; a fresh instance carries them into the saved asset.
            var defaults = ScriptableObject.CreateInstance<UiTheme>();
            theme.Text = defaults.Text;
            theme.Title = defaults.Title;
            theme.TitleLip = defaults.TitleLip;
            theme.OnAccent = defaults.OnAccent;
            theme.Paper = defaults.Paper;
            theme.Glass = defaults.Glass;
            theme.PanelEdge = defaults.PanelEdge;
            theme.Chip = defaults.Chip;
            theme.Primary = defaults.Primary;
            theme.Secondary = defaults.Secondary;
            theme.Confirm = defaults.Confirm;
            theme.Accent = defaults.Accent;
            theme.Danger = defaults.Danger;
            theme.Ribbon = defaults.Ribbon;
            theme.Hard = defaults.Hard;
            theme.VeryHard = defaults.VeryHard;
            theme.Gold = defaults.Gold;
            theme.Ink = defaults.Ink;
            theme.Muted = defaults.Muted;
            theme.StarEmpty = defaults.StarEmpty;
            theme.SkyTop = defaults.SkyTop;
            theme.SkyBottom = defaults.SkyBottom;
            theme.Well = defaults.Well;
            theme.Scrim = defaults.Scrim;
            UnityEngine.Object.DestroyImmediate(defaults);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        /// <summary>The game's own icons (exported from the Figma file) import as sprites.</summary>
        static void ImportIcons()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { GameIcons.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.textureType == TextureImporterType.Sprite) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        const string ArtCatalogPath = "Assets/_Game/Content/Art/ArtCatalog.asset";
        const string ArtFolder = "Assets/_Game/Content/Art/";

        [System.Serializable]
        sealed class ArtList
        {
            public System.Collections.Generic.List<Artwork> Works;
        }

        /// <summary>Creates or refreshes the painting catalog from Content/Art/artworks.json and the images beside it.</summary>
        [MenuItem("Golden Mend/Rebuild Art Catalog")]
        public static ArtCatalog BuildArtCatalog()
        {
            AssetDatabase.ImportAsset(ArtFolder.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(ArtFolder + "artworks.json");
            if (json == null) throw new InvalidOperationException("Golden Mend: missing Content/Art/artworks.json");
            var list = JsonUtility.FromJson<ArtList>(json.text);

            var catalog = AssetDatabase.LoadAssetAtPath<ArtCatalog>(ArtCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtCatalog>();
                AssetDatabase.CreateAsset(catalog, ArtCatalogPath);
            }
            catalog.Works = list.Works;
            foreach (var work in catalog.Works) work.Image = Load<Texture2D>(ArtFolder + work.Key + ".jpg");

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        const string SoundBankPath = "Assets/_Game/Content/Audio/SoundBank.asset";
        const string Audio = "Assets/_Game/Content/Audio/";

        /// <summary>Creates or refreshes the sound bank from the clips Tools/Audio/make_sounds.py writes.</summary>
        [MenuItem("Golden Mend/Rebuild Sound Bank")]
        public static SoundBank BuildSoundBank()
        {
            AssetDatabase.ImportAsset(Audio.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankPath);
            if (bank == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SoundBankPath));
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, SoundBankPath);
            }

            bank.Pick = Clips("Sfx/pick_1", "Sfx/pick_2", "Sfx/pick_3");
            bank.Lock = Load<AudioClip>(Audio + "Sfx/lock.ogg");
            bank.Uncover = Clips("Sfx/uncover");
            bank.Reject = Load<AudioClip>(Audio + "Sfx/reject.ogg");
            bank.Expand = Load<AudioClip>(Audio + "Sfx/expand.ogg");
            bank.Coins = Clips("Sfx/coins_1", "Sfx/coins_2");
            bank.WinJingle = Load<AudioClip>(Audio + "Sfx/win.ogg");
            bank.FailJingle = Load<AudioClip>(Audio + "Sfx/fail.ogg");
            bank.Music = Load<AudioClip>(Audio + "Music/bright_workshop.ogg");

            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        static AudioClip[] Clips(params string[] names)
        {
            var clips = new AudioClip[names.Length];
            for (int i = 0; i < names.Length; i++) clips[i] = Load<AudioClip>(Audio + names[i] + ".ogg");
            return clips;
        }

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"Golden Mend: missing UI asset {path}");
            return asset;
        }
    }
}
