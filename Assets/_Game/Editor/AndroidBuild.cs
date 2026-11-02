using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Player settings and the Android build.
    /// Golden Mend > Build Android APK, or in batch mode with
    /// -executeMethod GoldenMend.EditorTools.AndroidBuild.BuildApk
    /// </summary>
    public static class AndroidBuild
    {
        public const string Studio = "Tensei Games";
        public const string Product = "Golden Mend";
        public const string PackageId = "com.tenseigames.goldenmend";
        public const string ClassicPackageId = "com.tenseigames.goldenmend.classic";
        const string ClassicOutputPath = "Builds/Android/GoldenMend-Classic.apk";
        public const string IconPath = "Assets/_Game/Content/Icon/AppIcon.png";
        const string OutputPath = "Builds/Android/GoldenMend.apk";

        [MenuItem("Golden Mend/Apply Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = Studio;
            PlayerSettings.productName = Product;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0") PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.11f, 0.095f, 0.085f);

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
                if (importer.textureType != TextureImporterType.Default || importer.mipmapEnabled)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }
            else
            {
                Debug.LogWarning($"Golden Mend: no app icon at {IconPath}; using the default icon.");
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The Classic build: generated glazes instead of paintings, installed as a separate app
        /// ("Golden Mend Classic") so both can sit on one phone. Restores the main settings and scene afterwards.
        /// </summary>
        [MenuItem("Golden Mend/Build Android APK (Classic glaze)")]
        public static void BuildClassicApk()
        {
            try
            {
                GameSceneBuilder.Build(withPaintings: false);
                ApplyPlayerSettings();
                PlayerSettings.productName = Product + " Classic";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ClassicPackageId);
                Build(ClassicOutputPath);
            }
            finally
            {
                GameSceneBuilder.Build(withPaintings: true);
                ApplyPlayerSettings();
            }
        }

        [MenuItem("Golden Mend/Build Android APK")]
        public static void BuildApk()
        {
            GameSceneBuilder.Build(withPaintings: true);
            ApplyPlayerSettings();
            Build(OutputPath);
        }

        static void Build(string outputPath)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { GameSceneBuilder.ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"Golden Mend: Android build {summary.result}, {summary.totalErrors} errors, at {outputPath}");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
