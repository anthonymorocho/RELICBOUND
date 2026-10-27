using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeforeItWasYou.Tests
{
    /// <summary>
    /// Captura completa (escena 3D + interfaz) sin ventana: la cámara activa se renderiza a una textura y la
    /// interfaz se redirige temporalmente a otra; después se componen.
    /// </summary>
    public static class JourneyCapture
    {
        public const string Folder = "Evidence/journey";

        public static IEnumerator Capture(string name, int width = 1920, int height = 1080)
        {
            Directory.CreateDirectory(Folder);
            var document = Object.FindFirstObjectByType<UIDocument>();
            var panel = document != null ? document.panelSettings : null;
            var ui = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            ui.Create();
            if (panel != null)
            {
                panel.targetTexture = ui;
                panel.clearColor = true;
                panel.colorClearValue = Color.clear;
            }
            yield return null;
            yield return null;

            var world = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            world.Create();
            var camera = ActiveCamera();
            if (camera != null)
            {
                camera.targetTexture = world;
                camera.Render();
                camera.targetTexture = null;
            }

            var worldPixels = Read(world);
            var uiPixels = Read(ui);
            var output = new Color32[worldPixels.Length];
            for (var i = 0; i < output.Length; i++)
            {
                // La interfaz se pinta sobre transparente con color premultiplicado.
                var u = uiPixels[i];
                var w = worldPixels[i];
                var inverse = 255 - u.a;
                output[i] = new Color32(
                    (byte)Mathf.Min(255, u.r + w.r * inverse / 255),
                    (byte)Mathf.Min(255, u.g + w.g * inverse / 255),
                    (byte)Mathf.Min(255, u.b + w.b * inverse / 255),
                    255);
            }
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.SetPixels32(output);
            result.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), result.EncodeToPNG());
            Debug.Log($"[BIWY] Captura del recorrido: {name}");

            if (panel != null)
            {
                panel.targetTexture = null;
                panel.clearColor = false;
            }
            Object.Destroy(result);
            world.Release();
            ui.Release();
            yield return null;
        }

        private static Camera ActiveCamera()
        {
            Camera best = null;
            foreach (var camera in Camera.allCameras)
            {
                if (!camera.isActiveAndEnabled) continue;
                if (best == null || camera.depth > best.depth) best = camera;
            }
            return best;
        }

        private static Color32[] Read(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            var pixels = image.GetPixels32();
            Object.Destroy(image);
            return pixels;
        }
    }
}
