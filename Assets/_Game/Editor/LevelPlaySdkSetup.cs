using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Command-line version of what the Ads Mediation package does when it is first added in an open Editor: turns on
    /// its Network Manager (the LEVELPLAY_DEPENDENCIES_INSTALLED define) and installs the latest compatible LevelPlay
    /// native SDK plus the Unity Ads adapter into Assets/LevelPlay/Editor. In an open Editor the same is done from
    /// Ads Mediation > Network Manager. Run in two passes, because the define needs a recompile:
    ///   -executeMethod GoldenMend.EditorTools.LevelPlaySdkSetup.EnableNetworkManager -quit
    ///   -executeMethod GoldenMend.EditorTools.LevelPlaySdkSetup.InstallSdk      (no -quit; it exits when done)
    /// </summary>
    public static class LevelPlaySdkSetup
    {
        const string Define = "LEVELPLAY_DEPENDENCIES_INSTALLED";

        public static void EnableNetworkManager()
        {
            foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(target);
                if (defines.Split(';').Contains(Define)) continue;
                PlayerSettings.SetScriptingDefineSymbols(target, string.IsNullOrEmpty(defines) ? Define : defines + ";" + Define);
            }
            AssetDatabase.SaveAssets();
        }

        public static void InstallSdk()
        {
            Run().ContinueWith(t =>
            {
                if (t.Exception != null) Debug.LogError("Golden Mend: LevelPlay SDK install failed: " + t.Exception);
                EditorApplication.delayCall += () => EditorApplication.Exit(t.Exception == null ? 0 : 1);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        static async Task Run()
        {
            var editorAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Unity.LevelPlay.Editor");
            const BindingFlags any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var servicesType = editorAssembly.GetType("Unity.Services.LevelPlay.Editor.EditorServices", true);
            var services = servicesType.GetProperty("Instance", any).GetValue(null);
            var logger = servicesType.GetProperty("LevelPlayLogger", any).GetValue(services);
            var networkManager = servicesType.GetProperty("LevelPlayNetworkManager", any).GetValue(services);

            var installerType = editorAssembly.GetType("Unity.Services.LevelPlay.Editor.SdkInstaller", true);
            var installer = installerType.GetConstructors(any).First().Invoke(new[] { logger, networkManager });

            foreach (var step in new[] { "PreInstallAsync", "InstallLatestIronSourceSdkAsync", "InstallUnityAdsAdapterAsync" })
            {
                var method = installerType.GetMethod(step, any);
                var args = method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
                Debug.Log("Golden Mend: LevelPlay " + step);
                await (Task)method.Invoke(installer, args);
            }
            AssetDatabase.Refresh();
            Debug.Log("Golden Mend: LevelPlay native SDK files: " +
                      string.Join(", ", System.IO.Directory.Exists("Assets/LevelPlay/Editor")
                          ? System.IO.Directory.GetFiles("Assets/LevelPlay/Editor", "*.xml").Select(System.IO.Path.GetFileName)
                          : new string[0]));
        }
    }
}
