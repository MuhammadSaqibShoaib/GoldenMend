using System;
using UnityEditor;
using UnityEditor.SceneManagement;
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

            new GameObject("Game").AddComponent(bootstrap);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Golden Mend";
            PlayerSettings.companyName = "Portfolio Games";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.portfoliogames.goldenmend");

            AssetDatabase.SaveAssets();
            Debug.Log($"Golden Mend: game scene saved to {ScenePath}");
        }
    }
}
