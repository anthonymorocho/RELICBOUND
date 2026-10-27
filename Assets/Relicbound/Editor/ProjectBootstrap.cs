using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Relicbound.EditorTools
{
    /// <summary>
    /// Deja el proyecto con la configuración de docs/15 y docs/16 (URP, identidad, Android, entrada).
    /// Es idempotente: se puede ejecutar desde el menú o en CI con
    /// -executeMethod Relicbound.EditorTools.ProjectBootstrap.RunFromCommandLine
    /// </summary>
    public static class ProjectBootstrap
    {
        private const string SettingsFolder = "Assets/Relicbound/Settings/Rendering";
        private const string RendererPath = SettingsFolder + "/Relicbound_Renderer.asset";
        private const string PipelinePath = SettingsFolder + "/Relicbound_URP.asset";

        [MenuItem("Relicbound/Configurar proyecto")]
        public static void Run()
        {
            ConfigureRendering();
            ConfigurePlayer();
            ConfigureInput();
            AssetDatabase.SaveAssets();
            Debug.Log("[Relicbound] Proyecto configurado.");
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigureRendering()
        {
            EnsureFolder(SettingsFolder);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            // Valores de calidad "Alta" de docs/16. El preset móvil se aplica en tiempo de ejecución (P1.6).
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 120f;
            pipeline.shadowCascadeCount = 4;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var current = QualitySettings.GetQualityLevel();
            for (var level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Relicbound";
            PlayerSettings.productName = "RELICBOUND: Siege Kingdoms";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.relicbound.siegekingdoms");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.relicbound.siegekingdoms");

            // Solo horizontal (docs/01 §Plataformas).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Android: ARM64, IL2CPP, API 26+, Vulkan con alternativa GLES3 (docs/16).
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
        }

        private static void ConfigureInput()
        {
            // 1 = solo Input System nuevo (docs/16: entrada unificada ratón/táctil).
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0) return;
            var serialized = new SerializedObject(settings[0]);
            var handler = serialized.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != 1)
            {
                handler.intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
