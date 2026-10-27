using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Capturas reales del motor para revisión visual (1920x1080) desde puntos de vista fijos. Se guardan en
    /// Evidence/ (fuera de Assets) y la CI las publica como artefacto.
    /// </summary>
    public static class EvidenceCapture
    {
        public const string OutputFolder = "Evidence/editor";

        [MenuItem("BEFORE IT WAS YOU/Capturar evidencias del entorno")]
        public static void CaptureEnvironment()
        {
            EditorSceneManager.OpenScene(ArrivalSceneBuilder.ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);
            var views = GameObject.Find("Environment/Markers");
            if (views == null) return;
            foreach (Transform view in views.transform)
            {
                if (!view.name.StartsWith("View_") && !view.name.StartsWith("Menu_")) continue;
                // Interiores: con las luces de la casa encendidas (al llegar están apagadas).
                var interiorLights = view.name.StartsWith("View_Int_") ? Object.FindObjectsByType<Light>(FindObjectsSortMode.None) : new Light[0];
                var toggled = new System.Collections.Generic.List<Light>();
                foreach (var l in interiorLights)
                {
                    if (!l.enabled && l.name.StartsWith("Light_") && l.transform.parent != null && l.transform.parent.name == "Interior")
                    {
                        l.enabled = true;
                        toggled.Add(l);
                    }
                }
                var go = new GameObject("EvidenceCamera");
                go.transform.SetPositionAndRotation(view.position, view.rotation);
                var camera = go.AddComponent<Camera>();
                camera.fieldOfView = 64f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 400f;
                var data = go.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                Capture(camera, 1920, 1080, Path.Combine(OutputFolder, view.name.ToLowerInvariant() + ".png"));
                Object.DestroyImmediate(go);
                foreach (var l in toggled) l.enabled = false;
            }
        }

        public static void Capture(Camera camera, int width, int height, string path)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
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
            Debug.Log($"[BIWY] Captura {width}x{height} -> {path}");
        }
    }
}
