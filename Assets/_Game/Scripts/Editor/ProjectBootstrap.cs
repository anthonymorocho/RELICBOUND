using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Configuración del proyecto según el contrato técnico: URP con PC_RPAsset, Windows DirectX 12, Input System,
    /// calidad Baja/Media/Alta/Ultra, capas físicas e identidad. Idempotente.
    /// -executeMethod BeforeItWasYou.EditorTools.ProjectBootstrap.RunFromCommandLine
    /// </summary>
    public static class ProjectBootstrap
    {
        public const string SettingsFolder = "Assets/_Game/Settings";
        public const string RendererPath = SettingsFolder + "/PC_Renderer.asset";
        public const string PipelinePath = SettingsFolder + "/PC_RPAsset.asset";
        public const string Version = "0.1.0";

        private static readonly string[] QualityNames = { "Low", "Medium", "High", "Ultra" };

        [MenuItem("BEFORE IT WAS YOU/Configurar proyecto")]
        public static void Run()
        {
            ConfigureRendering();
            ConfigurePlayer();
            ConfigureInput();
            ConfigureLayers();
            AssetDatabase.SaveAssets();
            Debug.Log("[BIWY] Proyecto configurado.");
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

        // ---------- Render ----------

        private static void ConfigureRendering()
        {
            EditorUtil.EnsureFolder(SettingsFolder);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                AssetDatabase.SaveAssets();
                renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            }
            renderer.renderingMode = RenderingMode.ForwardPlus;
            EditorUtility.SetDirty(renderer);
            EnsureAmbientOcclusion(renderer);

            var assets = new UniversalRenderPipelineAsset[QualityNames.Length];
            for (var i = 0; i < QualityNames.Length; i++)
            {
                var path = QualityNames[i] == "High" ? PipelinePath : SettingsFolder + "/PC_RPAsset_" + QualityNames[i] + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null)
                {
                    asset = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                    asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                }
                ConfigurePipeline(asset, i);
                assets[i] = asset;
            }

            GraphicsSettings.defaultRenderPipeline = assets[2];
            ConfigureQualityLevels(assets);
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void ConfigurePipeline(UniversalRenderPipelineAsset asset, int level)
        {
            var data = new SerializedObject(asset);
            EditorUtil.Set(data, "m_SupportsHDR", true);
            EditorUtil.Set(data, "m_HDRColorBufferPrecision", 0);
            EditorUtil.Set(data, "m_MSAA", new[] { 1, 2, 4, 4 }[level]);
            EditorUtil.Set(data, "m_RenderScale", 1f);
            EditorUtil.Set(data, "m_MainLightRenderingMode", 1);
            EditorUtil.Set(data, "m_MainLightShadowsSupported", true);
            EditorUtil.Set(data, "m_MainLightShadowmapResolution", new[] { 1024, 2048, 2048, 4096 }[level]);
            EditorUtil.Set(data, "m_AdditionalLightsRenderingMode", 1);
            EditorUtil.Set(data, "m_AdditionalLightsPerObjectLimit", new[] { 4, 6, 8, 8 }[level]);
            EditorUtil.Set(data, "m_AdditionalLightShadowsSupported", level > 0);
            EditorUtil.Set(data, "m_AdditionalLightsShadowmapResolution", new[] { 512, 1024, 2048, 4096 }[level]);
            EditorUtil.Set(data, "m_ShadowDistance", new[] { 22f, 32f, 42f, 60f }[level]);
            EditorUtil.Set(data, "m_ShadowCascadeCount", new[] { 1, 2, 3, 4 }[level]);
            EditorUtil.Set(data, "m_SoftShadowsSupported", level > 0);
            EditorUtil.Set(data, "m_SoftShadowQuality", new[] { 1, 1, 2, 3 }[level]);
            EditorUtil.Set(data, "m_SupportsCameraDepthTexture", true);
            EditorUtil.Set(data, "m_SupportsCameraOpaqueTexture", false);
            EditorUtil.Set(data, "m_ColorGradingMode", 1);
            EditorUtil.Set(data, "m_ColorGradingLutSize", 32);
            EditorUtil.Set(data, "m_UseFastSRGBLinearConversion", false);
            EditorUtil.Set(data, "m_SupportsLightLayers", true);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// SSAO en el renderer. La clase del feature no es pública en URP 17: se crea por reflexión y se registra en el
        /// mapa de features para que Unity la serialice.
        /// </summary>
        private static void EnsureAmbientOcclusion(UniversalRendererData renderer)
        {
            var type = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
            if (type == null) return;
            ScriptableRendererFeature feature = null;
            foreach (var existing in renderer.rendererFeatures)
            {
                if (existing != null && existing.GetType() == type) feature = existing;
            }
            if (feature == null)
            {
                feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
                feature.name = "ScreenSpaceAmbientOcclusion";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                AssetDatabase.SaveAssets();
                var data = new SerializedObject(renderer);
                var map = data.FindProperty("m_RendererFeatureMap");
                if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId))
                {
                    map.arraySize = renderer.rendererFeatures.Count;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            var settings = new SerializedObject(feature);
            EditorUtil.Set(settings, "m_Settings.Intensity", 1.6f);
            EditorUtil.Set(settings, "m_Settings.Radius", 0.45f);
            EditorUtil.Set(settings, "m_Settings.DirectLightingStrength", 0.2f);
            EditorUtil.Set(settings, "m_Settings.Falloff", 40f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }

        private static void ConfigureQualityLevels(UniversalRenderPipelineAsset[] assets)
        {
            var quality = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (quality.Length == 0) return;
            var data = new SerializedObject(quality[0]);
            var levels = data.FindProperty("m_QualitySettings");
            levels.arraySize = QualityNames.Length;
            for (var i = 0; i < QualityNames.Length; i++)
            {
                var level = levels.GetArrayElementAtIndex(i);
                level.FindPropertyRelative("name").stringValue = QualityNames[i];
                SetRelative(level, "customRenderPipeline", assets[i]);
                SetRelative(level, "vSyncCount", 1);
                SetRelative(level, "lodBias", new[] { 0.7f, 1f, 1.5f, 2f }[i]);
                SetRelative(level, "anisotropicTextures", i == 0 ? 1 : 2);
                SetRelative(level, "globalTextureMipmapLimit", i == 0 ? 1 : 0);
                SetRelative(level, "realtimeReflectionProbes", i >= 2);
                SetRelative(level, "skinWeights", i == 0 ? 2 : 4);
            }
            var current = data.FindProperty("m_CurrentQuality");
            if (current != null) current.intValue = 2;
            var defaults = data.FindProperty("m_PerPlatformDefaultQuality");
            if (defaults != null)
            {
                for (var i = 0; i < defaults.arraySize; i++)
                {
                    var entry = defaults.GetArrayElementAtIndex(i);
                    var second = entry.FindPropertyRelative("second");
                    if (second != null) second.intValue = 2;
                }
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.SetQualityLevel(2, true);
        }

        private static void SetRelative(SerializedProperty parent, string name, object value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property == null) return;
            switch (value)
            {
                case int i when property.propertyType == SerializedPropertyType.Boolean:
                    property.boolValue = i != 0;
                    break;
                case int i:
                    property.intValue = i;
                    break;
                case bool b when property.propertyType == SerializedPropertyType.Integer:
                    property.intValue = b ? 1 : 0;
                    break;
                case bool b:
                    property.boolValue = b;
                    break;
                case float f:
                    property.floatValue = f;
                    break;
                case UnityEngine.Object o:
                    property.objectReferenceValue = o;
                    break;
            }
        }

        // ---------- Jugador, plataforma y entrada ----------

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "BeforeItWasYou";
            PlayerSettings.productName = "BEFORE IT WAS YOU";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.beforeitwasyou.ep01");
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            // DirectX 12 como API principal; DirectX 11 solo como reserva si el equipo no admite DX12.
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 });
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.visibleInBackground = false;
            PlayerSettings.SplashScreen.backgroundColor = Color.black;
        }

        private static void ConfigureInput()
        {
            // 1 = solo Input System (ningún script usa UnityEngine.Input).
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length == 0) return;
            var data = new SerializedObject(settings[0]);
            var handler = data.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue != 1)
            {
                handler.intValue = 1;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureLayers()
        {
            var tags = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tags.Length == 0) return;
            var data = new SerializedObject(tags[0]);
            var layers = data.FindProperty("layers");
            void SetLayer(int index, string name)
            {
                if (layers != null && index < layers.arraySize) layers.GetArrayElementAtIndex(index).stringValue = name;
            }
            SetLayer(Layers.Player, "Player");
            SetLayer(Layers.Interactable, "Interactable");
            SetLayer(Layers.Intruder, "Intruder");
            SetLayer(Layers.Glass, "Glass");
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static class EditorUtil
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static void Set(SerializedObject data, string path, object value)
        {
            var property = data.FindProperty(path);
            if (property == null) return;
            switch (value)
            {
                case bool b when property.propertyType == SerializedPropertyType.Integer:
                    property.intValue = b ? 1 : 0;
                    break;
                case bool b:
                    property.boolValue = b;
                    break;
                case int i when property.propertyType == SerializedPropertyType.Float:
                    property.floatValue = i;
                    break;
                case int i:
                    // En propiedades enum, intValue fija el valor subyacente (p. ej. MSAA 4 = 4x), no el índice.
                    property.intValue = i;
                    break;
                case float f:
                    property.floatValue = f;
                    break;
                case UnityEngine.Object o:
                    property.objectReferenceValue = o;
                    break;
            }
        }

        public static void Set(UnityEngine.Object target, string path, object value)
        {
            var data = new SerializedObject(target);
            Set(data, path, value);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
