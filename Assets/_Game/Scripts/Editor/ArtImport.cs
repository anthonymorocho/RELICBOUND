using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Importación del arte: texturas PBR (BaseColor sRGB, Normal, Mask lineal), HDRI como cubemap, materiales URP Lit
    /// creados desde la tabla de ExteriorMaterials y desde los manifest.json de los modelos de Poly Haven, y FBX con
    /// remapeo de materiales por nombre. Idempotente.
    /// </summary>
    public static class ArtImport
    {
        public const string ArtFolder = "Assets/_Game/Art";
        public const string TexturesFolder = ArtFolder + "/Textures";
        public const string ModelsFolder = ArtFolder + "/Models";
        public const string SkyFolder = ArtFolder + "/Sky";
        public const string MaterialsFolder = "Assets/_Game/Materials";

        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int MetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");
        private static readonly int OcclusionMap = Shader.PropertyToID("_OcclusionMap");

        [MenuItem("BEFORE IT WAS YOU/Importar arte")]
        public static void Run()
        {
            AssetDatabase.Refresh();
            ConfigureTextures(TexturesFolder, 2048);
            ConfigureTextures(ModelsFolder, 1024);
            ConfigureSky();
            ExteriorMaterials.CreateAll();
            CreatePolyHavenMaterials();
            AssetDatabase.SaveAssets();
            ConfigureModels();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureTextures(string folder, int maxSize)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                var isNormal = path.Contains("_Normal");
                var isMask = path.Contains("_Mask");
                var changed = false;
                void Set<T>(T current, T wanted, System.Action<T> apply)
                {
                    if (EqualityComparer<T>.Default.Equals(current, wanted)) return;
                    apply(wanted);
                    changed = true;
                }
                Set(importer.textureType, isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default, v => importer.textureType = v);
                Set(importer.sRGBTexture, !isNormal && !isMask, v => importer.sRGBTexture = v);
                Set(importer.mipmapEnabled, true, v => importer.mipmapEnabled = v);
                Set(importer.anisoLevel, 8, v => importer.anisoLevel = v);
                Set(importer.maxTextureSize, maxSize, v => importer.maxTextureSize = v);
                Set(importer.textureCompression, TextureImporterCompression.CompressedHQ, v => importer.textureCompression = v);
                // Las hojas con alfa deben conservar el contorno al reducir mipmaps.
                if (!isNormal && !isMask && importer.DoesSourceTextureHaveAlpha())
                {
                    Set(importer.alphaIsTransparency, true, v => importer.alphaIsTransparency = v);
                    Set(importer.mipMapsPreserveCoverage, true, v => importer.mipMapsPreserveCoverage = v);
                    Set(importer.alphaTestReferenceValue, 0.5f, v => importer.alphaTestReferenceValue = v);
                }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static void ConfigureSky()
        {
            if (!AssetDatabase.IsValidFolder(SkyFolder)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { SkyFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureShape == TextureImporterShape.TextureCube) continue;
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        public static Texture2D Texture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        /// <summary>Material URP Lit (o lo actualiza si existe) con mapas PBR opcionales.</summary>
        public static Material LitMaterial(string path, Texture2D baseMap, Texture2D normal, Texture2D mask, Color tint, Vector2 tiling,
            float smoothness = 0.5f, float metallic = 0f, float normalScale = 1f)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture(BaseMap, baseMap);
            material.SetTextureScale(BaseMap, tiling);
            material.SetColor(BaseColor, tint);
            material.SetTexture(BumpMap, normal);
            material.SetFloat("_BumpScale", normalScale);
            material.SetTexture(MetallicGlossMap, mask);
            material.SetTexture(OcclusionMap, mask);
            material.SetFloat("_OcclusionStrength", mask != null ? 1f : 0f);
            // Con máscara: el canal A da la suavidad y _Smoothness la escala.
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            Keyword(material, "_NORMALMAP", normal != null);
            Keyword(material, "_METALLICSPECGLOSSMAP", mask != null);
            Keyword(material, "_OCCLUSIONMAP", mask != null);
            material.SetFloat("_EnvironmentReflections", 1f);
            material.SetFloat("_SpecularHighlights", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static void Keyword(Material material, string keyword, bool on)
        {
            if (on) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        public static void MakeTransparent(Material material, float alpha)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            Keyword(material, "_SURFACE_TYPE_TRANSPARENT", true);
            Keyword(material, "_ALPHAPREMULTIPLY_ON", false);
            var color = material.GetColor(BaseColor);
            color.a = alpha;
            material.SetColor(BaseColor, color);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
        }

        public static void MakeCutout(Material material, float cutoff, bool doubleSided)
        {
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", cutoff);
            Keyword(material, "_ALPHATEST_ON", true);
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetOverrideTag("RenderType", "TransparentCutout");
            if (doubleSided) material.SetFloat("_Cull", 0f);
        }

        public static void MakeEmissive(Material material, Color emission)
        {
            material.SetColor("_EmissionColor", emission);
            Keyword(material, "_EMISSION", true);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private static void CreatePolyHavenMaterials()
        {
            var folder = ModelsFolder + "/PolyHaven";
            if (!AssetDatabase.IsValidFolder(folder)) return;
            var output = MaterialsFolder + "/PolyHaven";
            EditorUtil.EnsureFolder(output);
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var manifestPath = Path.Combine(dir, "manifest.json");
                if (!File.Exists(manifestPath)) continue;
                var manifest = JsonUtility.FromJson<ModelManifest>(File.ReadAllText(manifestPath));
                var assetDir = dir.Replace('\\', '/');
                foreach (var entry in manifest.materials)
                {
                    var tint = entry.baseColorFactor != null && entry.baseColorFactor.Length == 4
                        ? new Color(entry.baseColorFactor[0], entry.baseColorFactor[1], entry.baseColorFactor[2], 1f)
                        : Color.white;
                    var material = LitMaterial($"{output}/{entry.name}.mat",
                        string.IsNullOrEmpty(entry.baseColor) ? null : Texture($"{assetDir}/{entry.baseColor}"),
                        string.IsNullOrEmpty(entry.normal) ? null : Texture($"{assetDir}/{entry.normal}"),
                        string.IsNullOrEmpty(entry.mask) ? null : Texture($"{assetDir}/{entry.mask}"),
                        tint, Vector2.one, 1f, 1f);
                    if (entry.alpha == "MASK" || entry.alpha == "BLEND") MakeCutout(material, Mathf.Clamp(entry.cutoff, 0.3f, 0.6f), true);
                    else if (entry.doubleSided) material.SetFloat("_Cull", 0f);
                }
            }
        }

        private static void ConfigureModels()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.isReadable = false;
                importer.addCollider = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.SaveAndReimport();
                importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
                importer.SaveAndReimport();
            }
        }

        [System.Serializable]
        private sealed class ModelManifest
        {
            public string id;
            public MaterialEntry[] materials;
        }

        [System.Serializable]
        private sealed class MaterialEntry
        {
            public string name;
            public string alpha;
            public float cutoff;
            public bool doubleSided;
            public float[] baseColorFactor;
            public string baseColor;
            public string normal;
            public string mask;
        }
    }
}
