using Relicbound.Game.Boot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Relicbound.EditorTools
{
    /// <summary>Escena Boot: marca y carga. Es la primera escena de la build.</summary>
    public static class BootSceneBuilder
    {
        public const string ScenePath = "Assets/Relicbound/Scenes/Boot.unity";

        [MenuItem("Relicbound/Generar escena Boot")]
        public static void Build()
        {
            var panelSettings = UiSetup.Run();
            ArtImportSetup.EnsureFolder("Assets/Relicbound/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.043f, 0.075f, 0.141f);
            cameraGo.AddComponent<UniversalAdditionalCameraData>();

            var directorGo = new GameObject("BootDirector");
            var document = directorGo.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiSetup.UiFolder + "/Splash.uxml");
            var director = directorGo.AddComponent<BootDirector>();
            var serialized = new SerializedObject(director);
            serialized.FindProperty("document").objectReferenceValue = document;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var documentData = new SerializedObject(document);
            documentData.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            documentData.ApplyModifiedPropertiesWithoutUndo();
            if (document.panelSettings == null) throw new System.InvalidOperationException("Boot: UIDocument sin PanelSettings.");

            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
