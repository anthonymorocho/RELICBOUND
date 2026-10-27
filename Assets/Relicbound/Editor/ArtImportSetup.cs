using System.IO;
using UnityEditor;
using UnityEngine;

namespace Relicbound.EditorTools
{
    /// <summary>
    /// Configura la importación del arte generado en Blender (Tools/Blender): texturas, materiales URP Lit
    /// y FBX con remapeo de materiales por nombre. Idempotente.
    /// </summary>
    public static class ArtImportSetup
    {
        public const string TexturesFolder = "Assets/Relicbound/Art/Textures";
        public const string MaterialsFolder = "Assets/Relicbound/Art/Materials";
        public const string CastleFolder = "Assets/Relicbound/Art/Castle";
        public const string NatureFolder = "Assets/Relicbound/Art/Nature";

        [MenuItem("Relicbound/Configurar importación de arte")]
        public static void Run()
        {
            ConfigureTextures();
            CreateMaterials();
            ConfigureModels();
            AssetDatabase.SaveAssets();
            Debug.Log("[Relicbound] Arte configurado.");
        }

        private static void ConfigureTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var isNormal = path.EndsWith("_normal.png");
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !isNormal;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.maxTextureSize = 1024;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = 1024,
                    format = TextureImporterFormat.ASTC_6x6,
                });
                importer.SaveAndReimport();
            }
        }

        private static void CreateMaterials()
        {
            EnsureFolder(MaterialsFolder);
            // Nombres iguales a los materiales de rb_castle_kit.py para que el FBX los remapee.
            EnsureLit("M_StoneWall", "StoneWall", 0.15f, 0f);
            EnsureLit("M_RoofBlue", "RoofBlue", 0.4f, 0f);
            EnsureLit("M_RoofRed", "RoofRed", 0.4f, 0f);
            EnsureLit("M_Wood", "Wood", 0.25f, 0f);
            EnsureLit("M_Iron", "Iron", 0.5f, 0.7f);

            // Vegetación y rocas: color plano estilizado (mismos nombres que rb_nature.py).
            EnsureFlat("M_Bark", "#5A3E2B", 0.1f);
            EnsureFlat("M_Leaf", "#557F30", 0.15f);
            EnsureFlat("M_LeafLight", "#648E36", 0.15f);
            EnsureFlat("M_Pine", "#2F5A3A", 0.15f);
            EnsureFlat("M_PineLight", "#3D6E45", 0.15f);
            EnsureFlat("M_Rock", "#8C877C", 0.1f);
        }

        public static Material EnsureFlat(string name, string hex, float smoothness)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material EnsureLit(string name, string family, float smoothness, float metallic)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", LoadTexture(family, "albedo"));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", LoadTexture(family, "normal"));
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Texture2D LoadTexture(string family, string kind) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_{family}_{kind}.png");

        private static void ConfigureModels()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { CastleFolder, NatureFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var isCastle = path.StartsWith(CastleFolder);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.addCollider = isCastle; // Selección por raycast (docs/04 §Selección).
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.Import;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
                importer.SaveAndReimport();
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
