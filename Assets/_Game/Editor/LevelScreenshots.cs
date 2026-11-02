using System.IO;
using GoldenMend.Data;
using GoldenMend.Flow;
using GoldenMend.Meta;
using GoldenMend.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Renders every screen of the game flow at three device shapes (phone, tall phone, tablet) to PNG in
    /// Logs/Screens, without entering play mode, to check layout and anchoring.
    /// Batch mode: -executeMethod GoldenMend.EditorTools.LevelScreenshots.CaptureAll (needs graphics, not -nographics).
    /// </summary>
    public static class LevelScreenshots
    {
        static readonly (string name, int width, int height)[] Devices =
        {
            ("phone", 1080, 1920),
            ("tall", 1080, 2400),
            ("tablet", 1536, 2048),
        };

        [MenuItem("Golden Mend/Capture Screen Screenshots")]
        public static void CaptureAll()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Screens"));
            Directory.CreateDirectory(folder);

            foreach (var device in Devices) CaptureDevice(folder, device.name, device.width, device.height);
        }

        static void CaptureDevice(string folder, string device, int width, int height)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Capture Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(10.4f, 6.1f / (width / (float)height));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.095f, 0.085f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            var serialized = new SerializedObject(light);
            serialized.FindProperty("m_LightType").intValue = (int)Light2D.LightType.Global;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            var readback = new Texture2D(width, height, TextureFormat.RGB24, false);

            // A player midway through: three vessels restored, the fourth (a hard one) next.
            var store = new MemorySaveStore();
            var save = new SaveData { Coins = 260, Unlocked = 3 };
            save.Levels.Add(new LevelRecord { Index = 0, Stars = 3, BestPeak = 0, Clears = 1 });
            save.Levels.Add(new LevelRecord { Index = 1, Stars = 2, BestPeak = 2, Clears = 1 });
            save.Levels.Add(new LevelRecord { Index = 2, Stars = 3, BestPeak = 1, Clears = 2 });
            store.Save(save);

            var root = new GameObject("Game").transform;
            var flow = GameComposition.Compose(GameSceneBuilder.BuildTheme(), camera, store, root);
            var scaler = Object.FindAnyObjectByType<AdaptiveCanvasScaler>();

            void Capture(string screen)
            {
                scaler.Apply();
                Canvas.ForceUpdateCanvases();
                foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
                {
                    if (rect.GetComponent<UnityEngine.UI.LayoutGroup>() != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readback.Apply();
                RenderTexture.active = null;

                string file = Path.Combine(folder, $"{screen}-{device}.png");
                File.WriteAllBytes(file, readback.EncodeToPNG());
                Debug.Log($"Golden Mend: captured {file}");
            }

            flow.ShowHome();
            Capture("1-home");
            flow.ShowMuseum();
            Capture("2-museum");
            flow.StartLevel(0);
            Capture("3-level1-tip");
            flow.StartLevel(3);
            flow.PreviewBanner();
            Capture("4-gameplay");
            flow.PreviewPause();
            Capture("5-pause");
            flow.StartLevel(3);
            flow.PreviewSettings();
            Capture("6-settings");
            flow.StartLevel(3);
            flow.PreviewFail();
            Capture("7-workbench-full");
            flow.StartLevel(3);
            flow.PreviewWin(stars: 2, coins: 40);
            Capture("8-level-complete");

            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(readback);
        }
    }
}
