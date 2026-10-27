using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>PanelSettings compartido e importación de texturas de interfaz.</summary>
    public static class UiAssets
    {
        public const string UiFolder = "Assets/_Game/UI";
        public const string PanelSettingsPath = ProjectBootstrap.SettingsFolder + "/UI_PanelSettings.asset";
        public const string FrontEndPath = UiFolder + "/Screens/FrontEnd.uxml";
        public const string StartupPath = UiFolder + "/Screens/Startup.uxml";

        /// <summary>Llamar DESPUÉS de crear la escena: NewScene descarga los assets sin referencias.</summary>
        public static PanelSettings Prepare()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiFolder + "/Textures" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.textureType == TextureImporterType.GUI && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.textureType = TextureImporterType.GUI;
                importer.mipmapEnabled = false;
                importer.wrapMode = path.EndsWith("ui_grain.png") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
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
            settings.match = 1f;
            settings.clearColor = false;
            settings.sortingOrder = 10;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            // Al crearse, Unity recarga el asset al guardar: se devuelve siempre la versión de disco.
            var loaded = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (loaded == null)
            {
                AssetDatabase.ImportAsset(PanelSettingsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                loaded = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            }
            if (loaded == null) throw new System.InvalidOperationException("No se pudo cargar UI_PanelSettings.");
            return loaded;
        }
    }
}
