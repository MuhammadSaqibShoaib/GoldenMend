using System.IO;
using GoldenMend.Data;
using GoldenMend.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GoldenMend.EditorTools
{
    /// <summary>
    /// Renders the opening board of every level to PNG (Logs/Screens) without entering play mode.
    /// Batch mode: -executeMethod GoldenMend.EditorTools.LevelScreenshots.CaptureAll (needs graphics, not -nographics).
    /// </summary>
    public static class LevelScreenshots
    {
        const int Width = 1080;
        const int Height = 1920;

        [MenuItem("Golden Mend/Capture Level Screenshots")]
        public static void CaptureAll()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Screens"));
            Directory.CreateDirectory(folder);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Capture Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(10.4f, 6.1f / (Width / (float)Height));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.095f, 0.085f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            var serialized = new SerializedObject(light);
            serialized.FindProperty("m_LightType").intValue = (int)Light2D.LightType.Global;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var boardObject = new GameObject("Board");
            boardObject.AddComponent<AnimationQueue>();
            boardObject.AddComponent<FeedbackService>();
            var board = boardObject.AddComponent<BoardPresenter>();

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            var readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);

            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                board.Load(LevelCatalog.Build(i));
                camera.Render();

                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                readback.Apply();
                RenderTexture.active = null;

                string file = Path.Combine(folder, $"level{i + 1}.png");
                File.WriteAllBytes(file, readback.EncodeToPNG());
                Debug.Log($"Golden Mend: captured {file}");

                foreach (Transform child in boardObject.transform) Object.DestroyImmediate(child.gameObject);
            }

            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(readback);
        }
    }
}
