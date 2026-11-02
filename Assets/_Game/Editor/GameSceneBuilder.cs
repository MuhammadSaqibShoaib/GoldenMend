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
        public static void Build()
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
            bootstrapData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Golden Mend";
            PlayerSettings.companyName = "Portfolio Games";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.portfoliogames.goldenmend");

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

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"Golden Mend: missing UI asset {path}");
            return asset;
        }
    }
}
