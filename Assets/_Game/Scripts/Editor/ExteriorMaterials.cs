using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Materiales de la arquitectura modelada en Tools/Blender/biwy_exterior.py. Los nombres coinciden con los del FBX
    /// para el remapeo automático. Las UV del modelo están en metros: tiling = 1 / tamaño físico de la textura.
    /// </summary>
    public static class ExteriorMaterials
    {
        public const string Folder = ArtImport.MaterialsFolder + "/Exterior";

        private sealed class Spec
        {
            public string Name;
            public string Texture;
            public float SizeMeters = 1f;
            public Color Tint = Color.white;
            public float Smoothness = 1f;
            public float Metallic;
            public float NormalScale = 1f;
            public float Alpha = 1f;
            public bool Transparent;
            public Color Emission = Color.black;
            public bool DoubleSided;
        }

        private static Spec S(string name, string texture, float size, Color tint, float smoothness = 1f) =>
            new Spec { Name = name, Texture = texture, SizeMeters = size, Tint = tint, Smoothness = smoothness };

        private static Spec Plain(string name, Color color, float smoothness, float metallic = 0f) =>
            new Spec { Name = name, Tint = color, Smoothness = smoothness, Metallic = metallic };

        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static readonly Spec[] Specs =
        {
            // Envolvente de la casa de Elena: revestimiento beige cálido y molduras blancas algo envejecidas.
            S("M_Siding", "exterior_wall_cladding_03", 1.96f, C(0.86f, 0.79f, 0.67f)),
            S("M_SidingNeighborA", "exterior_wall_cladding_03", 1.96f, C(0.58f, 0.63f, 0.66f)),
            S("M_SidingNeighborB", "painted_plaster_wall", 2f, C(0.84f, 0.8f, 0.72f)),
            S("M_Trim", "white_plaster_02", 1f, C(0.9f, 0.89f, 0.85f), 0.8f),
            Plain("M_TrimShadow", C(0.06f, 0.06f, 0.06f), 0.2f),
            S("M_Soffit", "white_plaster_02", 1f, C(0.8f, 0.79f, 0.75f), 0.6f),
            S("M_Roof", "grey_roof_01", 4f, C(0.78f, 0.78f, 0.8f)),
            S("M_RoofRidge", "grey_roof_01", 2f, C(0.55f, 0.55f, 0.57f)),
            S("M_Foundation", "concrete_floor_02", 2f, C(0.66f, 0.66f, 0.64f)),
            S("M_InteriorWall", "white_plaster_02", 1f, C(0.78f, 0.75f, 0.68f), 0.5f),
            S("M_Brick", "brick_wall_02", 2f, C(0.82f, 0.74f, 0.7f)),
            Plain("M_WindowFrame", C(0.86f, 0.86f, 0.83f), 0.45f),
            S("M_DoorFrame", "white_plaster_02", 1f, C(0.88f, 0.87f, 0.83f), 0.7f),
            S("M_DoorPaint", "dark_wood", 1.2f, C(0.78f, 0.62f, 0.52f)),
            S("M_DoorServicePaint", "weathered_plank_siding", 1.57f, C(0.62f, 0.66f, 0.6f)),
            Plain("M_MetalBrushed", C(0.62f, 0.55f, 0.42f), 0.62f, 1f),
            Plain("M_MetalDark", C(0.05f, 0.05f, 0.05f), 0.4f, 0.8f),
            Plain("M_MetalGrey", C(0.42f, 0.43f, 0.44f), 0.45f, 0.75f),
            Plain("M_Gutter", C(0.84f, 0.84f, 0.82f), 0.55f, 0.15f),
            Plain("M_GutterInner", C(0.2f, 0.2f, 0.2f), 0.3f),
            S("M_PorchConcrete", "concrete_floor_02", 2f, C(0.76f, 0.75f, 0.72f)),
            S("M_PorchCeiling", "white_planks_clean", 1.8f, C(0.9f, 0.89f, 0.85f), 0.6f),
            // Parcela y calle.
            S("M_Asphalt", "asphalt_02", 3f, C(0.82f, 0.82f, 0.84f)),
            S("M_Curb", "concrete_floor_02", 2f, C(0.82f, 0.82f, 0.8f)),
            S("M_Sidewalk", "concrete_pavement_02", 1.8f, C(0.86f, 0.85f, 0.82f)),
            S("M_Driveway", "brushed_concrete", 2.5f, C(0.86f, 0.85f, 0.82f)),
            S("M_Path", "concrete_pavement_02", 1.2f, C(0.8f, 0.78f, 0.74f)),
            S("M_Lawn", "leafy_grass", 2f, C(0.78f, 0.82f, 0.7f)),
            S("M_LawnNeighbor", "sparse_grass", 2f, C(0.85f, 0.9f, 0.8f)),
            S("M_Mulch", "brown_mud_leaves_01", 1.3f, C(0.8f, 0.72f, 0.65f)),
            S("M_Edging", "concrete_floor_02", 1f, C(0.6f, 0.6f, 0.58f)),
            S("M_Fence", "weathered_plank_siding", 1.57f, C(0.78f, 0.7f, 0.62f)),
            S("M_ShedWall", "weathered_plank_siding", 1.57f, C(0.72f, 0.66f, 0.58f)),
            S("M_ShedWallIn", "weathered_plank_siding", 1.57f, C(0.45f, 0.4f, 0.35f)),
            S("M_ShedTrim", "weathered_plank_siding", 1.57f, C(0.55f, 0.5f, 0.44f)),
            S("M_ShedRoof", "rusty_metal_02", 1f, C(0.75f, 0.72f, 0.7f)),
            Plain("M_Mailbox", C(0.07f, 0.07f, 0.07f), 0.45f, 0.6f),
            Plain("M_MailFlag", C(0.55f, 0.07f, 0.05f), 0.4f),
            S("M_PoleWood", "dark_wood", 1.5f, C(0.5f, 0.42f, 0.36f)),
            Plain("M_Insulator", C(0.22f, 0.3f, 0.27f), 0.85f),
            // Cristales y luces.
            new Spec { Name = "M_Glass", Tint = C(0.02f, 0.025f, 0.03f), Smoothness = 0.97f, Transparent = true, Alpha = 0.42f },
            new Spec { Name = "M_GlassDoor", Tint = C(0.03f, 0.03f, 0.03f), Smoothness = 0.95f, Transparent = true, Alpha = 0.5f },
            new Spec { Name = "M_GlassFrosted", Tint = C(0.7f, 0.72f, 0.74f), Smoothness = 0.55f, Transparent = true, Alpha = 0.88f },
            new Spec { Name = "M_GlassDirty", Tint = C(0.16f, 0.16f, 0.14f), Smoothness = 0.4f, Transparent = true, Alpha = 0.75f },
            new Spec { Name = "M_Curtain", Tint = C(0.33f, 0.3f, 0.26f), Smoothness = 0.1f, DoubleSided = true },
            new Spec { Name = "M_CurtainLit", Tint = C(0.55f, 0.45f, 0.35f), Smoothness = 0.1f, Emission = C(1f, 0.62f, 0.32f) * 1.4f },
            new Spec { Name = "M_LampGlass", Tint = C(0.95f, 0.85f, 0.65f), Smoothness = 0.9f, Emission = C(1f, 0.7f, 0.38f) * 6f },
            new Spec { Name = "M_LampLens", Tint = C(0.9f, 0.92f, 0.95f), Smoothness = 0.9f, Emission = C(0.85f, 0.9f, 1f) * 9f },
        };

        public static void CreateAll()
        {
            EditorUtil.EnsureFolder(Folder);
            CreateCar();
            foreach (var spec in Specs)
            {
                Texture2D baseMap = null, normal = null, mask = null;
                if (!string.IsNullOrEmpty(spec.Texture))
                {
                    var dir = $"{ArtImport.TexturesFolder}/{spec.Texture}/{spec.Texture}";
                    baseMap = ArtImport.Texture(dir + "_BaseColor.jpg");
                    normal = ArtImport.Texture(dir + "_Normal.jpg");
                    mask = ArtImport.Texture(dir + "_Mask.png");
                    if (baseMap == null) Debug.LogWarning($"[BIWY] Falta la textura {spec.Texture} para {spec.Name}.");
                }
                var tiling = Vector2.one / Mathf.Max(0.1f, spec.SizeMeters);
                var material = ArtImport.LitMaterial($"{Folder}/{spec.Name}.mat", baseMap, normal, mask, spec.Tint, tiling,
                    spec.Smoothness, spec.Metallic, spec.NormalScale);
                if (spec.Transparent) ArtImport.MakeTransparent(material, spec.Alpha);
                if (spec.Emission.maxColorComponent > 0f) ArtImport.MakeEmissive(material, spec.Emission);
                if (spec.DoubleSided) material.SetFloat("_Cull", 0f);
            }
        }

        /// <summary>Sedán de Daniel (CC BY 4.0): una sola textura de color; brillo de pintura algo envejecida.</summary>
        private static void CreateCar()
        {
            var baseMap = ArtImport.Texture(ArtImport.ModelsFolder + "/Vehicles/M_CarSedan_BaseColor.png");
            if (baseMap == null) return;
            ArtImport.LitMaterial($"{Folder}/M_CarSedan.mat", baseMap, null, null, Color.white, Vector2.one, 0.62f, 0.35f);
        }

        public static Material Get(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{name}.mat");
    }
}
