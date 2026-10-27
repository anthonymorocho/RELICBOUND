using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relicbound.EditorTools
{
    /// <summary>PanelSettings e importación de texturas de interfaz (docs/04 §Adaptación).</summary>
    public static class UiSetup
    {
        public const string UiFolder = "Assets/Relicbound/UI";
        public const string PanelSettingsPath = UiFolder + "/PanelSettings.asset";

        public static PanelSettings Run()
        {
            foreach (var folder in new[] { UiFolder + "/Textures", UiFolder + "/Icons" })
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.GUI;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }

            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiFolder + "/RuntimeTheme.tss");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.clearColor = false;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            // Al crearse por primera vez, Unity recarga el asset al guardar: la instancia anterior deja de ser válida
            // y las escenas guardarían una referencia vacía. Se devuelve siempre la versión cargada de disco.
            return AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        }
    }
}
