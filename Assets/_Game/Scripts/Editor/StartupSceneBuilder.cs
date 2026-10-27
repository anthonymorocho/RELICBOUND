using BeforeItWasYou.Boot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>L_Startup: primera escena de la build (aviso de contenido + carga del episodio).</summary>
    public static class StartupSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/L_Startup.unity";

        [MenuItem("BEFORE IT WAS YOU/Generar L_Startup")]
        public static void Build()
        {
            EditorUtil.EnsureFolder("Assets/_Game/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var panel = UiAssets.Prepare();

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraGo.AddComponent<UniversalAdditionalCameraData>();

            var directorGo = new GameObject("Startup");
            var document = directorGo.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssets.StartupPath);
            var director = directorGo.AddComponent<StartupDirector>();
            EditorUtil.Set(director, "document", document);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.skybox = null;
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
