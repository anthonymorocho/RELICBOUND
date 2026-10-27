using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Punto de entrada de la CI: configura el proyecto, genera las escenas y captura evidencias.
    /// -executeMethod BeforeItWasYou.EditorTools.CiPipeline.RunFromCommandLine
    /// </summary>
    public static class CiPipeline
    {
        public static void RunFromCommandLine()
        {
            try
            {
                GenerateAll();
                EvidenceCapture.CaptureEnvironment();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("BEFORE IT WAS YOU/Generar todo")]
        public static void GenerateAll()
        {
            ProjectBootstrap.Run();
            ArtImport.Run();
            ArrivalSceneBuilder.Build();
            StartupSceneBuilder.Build();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(StartupSceneBuilder.ScenePath, true),
                new EditorBuildSettingsScene(ArrivalSceneBuilder.ScenePath, true),
            };
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Build de Windows en Builds/StandaloneWindows64.
        /// -executeMethod BeforeItWasYou.EditorTools.CiPipeline.BuildFromCommandLine
        /// </summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                GenerateAll();
                var run = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
                PlayerSettings.bundleVersion = ProjectBootstrap.Version + (string.IsNullOrEmpty(run) ? string.Empty : "." + run);
                var target = EditorUserBuildSettings.activeBuildTarget;
                var folder = Path.Combine("Builds", target.ToString());
                Directory.CreateDirectory(folder);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { StartupSceneBuilder.ScenePath, ArrivalSceneBuilder.ScenePath },
                    locationPathName = Path.Combine(folder, target == BuildTarget.StandaloneWindows64 ? "BeforeItWasYou.exe" : "BeforeItWasYou"),
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    options = BuildOptions.None,
                });
                Debug.Log($"[BIWY] Build {target}: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
