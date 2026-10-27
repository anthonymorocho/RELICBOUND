using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Relicbound.EditorTools
{
    /// <summary>
    /// Punto de entrada de la CI: configura el proyecto, importa el arte, genera la escena Castle y captura evidencias.
    /// -executeMethod Relicbound.EditorTools.CiPipeline.RunFromCommandLine
    /// </summary>
    public static class CiPipeline
    {
        public static void RunFromCommandLine()
        {
            try
            {
                ProjectBootstrap.Run();
                ArtImportSetup.Run();
                // Castle primero: deja creados y cargados los assets compartidos (PanelSettings) antes que Boot.
                CastleSceneBuilder.Build();
                BootSceneBuilder.Build();
                EvidenceCapture.CaptureCastle();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Build jugable para la plataforma activa (Windows .exe o Android .apk) en Builds/&lt;plataforma&gt;.
        /// -executeMethod Relicbound.EditorTools.CiPipeline.BuildFromCommandLine
        /// </summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                ProjectBootstrap.Run();
                ArtImportSetup.Run();
                CastleSceneBuilder.Build();
                BootSceneBuilder.Build();

                var target = EditorUserBuildSettings.activeBuildTarget;
                var run = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
                PlayerSettings.bundleVersion = "0.1." + (string.IsNullOrEmpty(run) ? "0" : run);
                if (int.TryParse(run, out var code)) PlayerSettings.Android.bundleVersionCode = code;

                var folder = Path.Combine("Builds", target.ToString());
                Directory.CreateDirectory(folder);
                var file = target switch
                {
                    BuildTarget.StandaloneWindows64 => "RELICBOUND.exe",
                    BuildTarget.Android => "RELICBOUND.apk",
                    _ => "RELICBOUND",
                };
                if (target == BuildTarget.Android)
                {
                    // APK instalable directamente (firma de depuración). La firma de publicación es de P14.
                    EditorUserBuildSettings.buildAppBundle = false;
                    PlayerSettings.Android.useCustomKeystore = false;
                }
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { BootSceneBuilder.ScenePath, CastleSceneBuilder.ScenePath },
                    locationPathName = Path.Combine(folder, file),
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    options = BuildOptions.None,
                });
                Debug.Log($"[Relicbound] Build {target}: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime}");
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
