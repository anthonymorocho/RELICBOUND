using System;
using UnityEditor;
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
                CastleSceneBuilder.Build();
                EvidenceCapture.CaptureCastle();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
