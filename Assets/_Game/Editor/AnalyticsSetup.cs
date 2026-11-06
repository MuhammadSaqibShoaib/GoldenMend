using System.Collections.Generic;
using System.IO;
using GameAnalyticsSDK.Setup;
using GoldenMend.Meta;
using UnityEditor;
using UnityEngine;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Prepares the GameAnalytics settings asset the SDK loads at runtime: makes sure it exists, has an Android
    /// platform entry, and registers the currencies and item types the game sends (GameAnalytics rejects resource
    /// events whose currency or item type is not registered). It never touches the keys: those are entered in
    /// Window > GameAnalytics > Select Settings. The asset is git-ignored because it holds the secret key.
    /// </summary>
    public static class AnalyticsSetup
    {
        public const string SettingsPath = "Assets/Resources/GameAnalytics/Settings.asset";

        [MenuItem("Golden Mend/Prepare Analytics Settings")]
        public static Settings Prepare()
        {
            var settings = AssetDatabase.LoadAssetAtPath<Settings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                settings = ScriptableObject.CreateInstance<Settings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            if (!settings.Platforms.Contains(RuntimePlatform.Android)) settings.AddPlatform(RuntimePlatform.Android);
            AddMissing(settings.ResourceCurrencies, LevelAnalytics.Currencies);
            AddMissing(settings.ResourceItemTypes, LevelAnalytics.ItemTypes);
            settings.InfoLogBuild = true;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            int android = settings.Platforms.IndexOf(RuntimePlatform.Android);

            // Command-line builds can pass the keys in (GA_GAME_KEY / GA_SECRET_KEY) instead of the settings window.
            string gameKey = System.Environment.GetEnvironmentVariable("GA_GAME_KEY");
            string secretKey = System.Environment.GetEnvironmentVariable("GA_SECRET_KEY");
            if (!string.IsNullOrEmpty(gameKey)) settings.UpdateGameKey(android, gameKey.Trim());
            if (!string.IsNullOrEmpty(secretKey)) settings.UpdateSecretKey(android, secretKey.Trim());
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            if (string.IsNullOrEmpty(settings.GetGameKey(android)) || string.IsNullOrEmpty(settings.GetSecretKey(android)))
                Debug.LogWarning("Golden Mend: GameAnalytics has no Android keys yet, so builds will not send events. " +
                                 "Add them in Window > GameAnalytics > Select Settings.");
            return settings;
        }

        static void AddMissing(List<string> list, IEnumerable<string> values)
        {
            foreach (var v in values)
            {
                if (!list.Contains(v)) list.Add(v);
            }
        }
    }
}
