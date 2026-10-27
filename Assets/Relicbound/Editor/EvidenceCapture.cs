using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Relicbound.EditorTools
{
    /// <summary>
    /// Capturas reales del motor para revisión (docs/18 §Evidencias): PC 1920x1080 y Android 2400x1080.
    /// Se guardan en Evidence/ (fuera de Assets) y la CI las publica como artefacto.
    /// </summary>
    public static class EvidenceCapture
    {
        public const string OutputFolder = "Evidence";

        [MenuItem("Relicbound/Capturar evidencias del castillo")]
        public static void CaptureCastle()
        {
            EditorSceneManager.OpenScene(CastleSceneBuilder.ScenePath, OpenSceneMode.Single);
            var camera = Camera.main;
            if (camera == null) throw new System.InvalidOperationException("La escena Castle no tiene Main Camera.");
            Directory.CreateDirectory(OutputFolder);
            Capture(camera, 1920, 1080, Path.Combine(OutputFolder, "castle_pc_1920x1080.png"));
            Capture(camera, 2400, 1080, Path.Combine(OutputFolder, "castle_android_2400x1080.png"));
        }

        public static void Capture(Camera camera, int width, int height, string path)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = 4,
            };
            target.Create();
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            Debug.Log($"[Relicbound] Captura {width}x{height} -> {path}");
        }
    }
}
