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
        const string Fonts = "Assets/ThirdParty/Fonts/ZenMaruGothic/";

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
            camera.backgroundColor = new Color(0.11f, 0.095f, 0.085f);
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

        /// <summary>Creates or refreshes the UI theme from the Kenney UI Pack, Kenney Game Icons and Zen Maru Gothic.</summary>
        [MenuItem("Golden Mend/Rebuild UI Theme")]
        public static UiTheme BuildTheme()
        {
            AssetDatabase.ImportAsset("Assets/ThirdParty/Kenney", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ThemePath));
                theme = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            theme.BodyFont = Load<Font>(Fonts + "ZenMaruGothic-Regular.ttf");
            theme.TitleFont = Load<Font>(Fonts + "ZenMaruGothic-Bold.ttf");
            theme.DisplayFont = Load<Font>(Fonts + "ZenMaruGothic-Black.ttf");

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
            theme.IconRestart = Load<Sprite>(Icons + "return.png");
            theme.IconHint = Load<Sprite>(Icons + "question.png");
            theme.IconPrevious = Load<Sprite>(Icons + "arrowLeft.png");
            theme.IconNext = Load<Sprite>(Icons + "arrowRight.png");

            theme.Click = Load<AudioClip>(Kenney + "Sounds/click-a.ogg");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
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
        const string Audio = "Assets/ThirdParty/Kenney/Audio/";

        /// <summary>Creates or refreshes the sound bank from Kenney's CC0 audio packs.</summary>
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

            bank.Pick = Clips("Casino/chips-handle-1", "Casino/chips-handle-2", "Casino/chips-handle-3");
            bank.Park = Clips("Casino/chip-lay-1", "Casino/chip-lay-2", "Casino/chip-lay-3");
            bank.Lock = Clips("Interface/glass_001", "Interface/glass_002", "Interface/glass_003");
            bank.Uncover = Clips("Interface/pluck_001", "Interface/pluck_002");
            bank.Reject = Load<AudioClip>(Audio + "Interface/tick_002.ogg");
            bank.Expand = Load<AudioClip>(Audio + "Interface/maximize_006.ogg");
            bank.Coins = Clips("Casino/chips-stack-1", "Casino/chips-stack-2");
            bank.WinJingle = Load<AudioClip>(Audio + "Jingles/jingles_STEEL02.ogg");
            bank.FailJingle = Load<AudioClip>(Audio + "Jingles/jingles_STEEL14.ogg");

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
