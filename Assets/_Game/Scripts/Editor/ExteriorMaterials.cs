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
            S("M_InteriorWall", "beige_wall_001", 3f, C(0.93f, 0.86f, 0.74f), 0.5f),
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
            // Interior de la casa de Elena.
            S("M_FloorWood", "wood_floor", 1.7f, C(0.86f, 0.8f, 0.74f)),
            S("M_FloorKitchen", "floor_tiles_06", 1.5f, C(0.9f, 0.88f, 0.84f)),
            S("M_FloorBath", "floor_tiles_08", 1.5f, C(0.92f, 0.92f, 0.9f)),
            S("M_FloorLaundry", "grey_tiles", 2.2f, C(0.82f, 0.82f, 0.8f)),
            S("M_StepEdge", "concrete_floor_02", 1f, C(0.6f, 0.6f, 0.58f)),
            Plain("M_Threshold", C(0.55f, 0.5f, 0.4f), 0.55f, 1f),
            S("M_Ceiling", "white_plaster_02", 1f, C(0.88f, 0.87f, 0.83f), 0.3f),
            S("M_WallBath", "beige_wall_001", 3f, C(0.8f, 0.86f, 0.88f), 0.6f),
            S("M_Baseboard", "white_plaster_02", 1f, C(0.9f, 0.89f, 0.85f), 0.7f),
            S("M_CabinetCarcass", "kitchen_wood", 0.6f, C(0.9f, 0.82f, 0.72f)),
            S("M_CabinetDoor", "kitchen_wood", 0.6f, C(0.95f, 0.86f, 0.74f)),
            Plain("M_CabinetDark", C(0.08f, 0.07f, 0.06f), 0.3f),
            S("M_Countertop", "concrete_floor_02", 1f, C(0.42f, 0.4f, 0.38f), 1f),
            Plain("M_Steel", C(0.62f, 0.63f, 0.64f), 0.7f, 1f),
            Plain("M_Chrome", C(0.85f, 0.85f, 0.86f), 0.92f, 1f),
            S("M_Backsplash", "long_white_tiles", 1.27f, C(0.95f, 0.94f, 0.9f)),
            S("M_BathTile", "long_white_tiles", 1.27f, C(0.82f, 0.9f, 0.94f)),
            Plain("M_Fridge", C(0.86f, 0.85f, 0.8f), 0.6f),
            Plain("M_Porcelain", C(0.93f, 0.93f, 0.91f), 0.88f),
            Plain("M_ToiletSeat", C(0.9f, 0.9f, 0.88f), 0.6f),
            Plain("M_Grout", C(0.5f, 0.5f, 0.48f), 0.3f),
            Plain("M_Mirror", C(0.9f, 0.9f, 0.9f), 0.98f, 1f),
            Plain("M_Appliance", C(0.9f, 0.9f, 0.88f), 0.55f),
            Plain("M_ApplianceDark", C(0.12f, 0.12f, 0.13f), 0.5f),
            new Spec { Name = "M_WasherGlass", Tint = C(0.05f, 0.06f, 0.07f), Smoothness = 0.95f, Transparent = true, Alpha = 0.6f },
            S("M_Concrete", "concrete_floor_02", 1f, C(0.72f, 0.71f, 0.68f)),
            S("M_ConcreteDark", "concrete_floor_02", 1f, C(0.45f, 0.44f, 0.42f)),
            S("M_ShelfWood", "kitchen_wood", 0.6f, C(0.75f, 0.65f, 0.55f)),
            Plain("M_Mattress", C(0.82f, 0.8f, 0.74f), 0.15f),
            S("M_Quilt", "floral_jacquard", 0.38f, C(0.9f, 0.78f, 0.78f), 0.6f),
            Plain("M_Pillow", C(0.88f, 0.86f, 0.82f), 0.15f),
            S("M_WardrobeCarcass", "dark_wood", 1.2f, C(0.85f, 0.72f, 0.62f)),
            S("M_WardrobeDoor", "dark_wood", 1.2f, C(0.95f, 0.8f, 0.68f)),
            new Spec { Name = "M_LampShade", Tint = C(0.92f, 0.9f, 0.86f), Smoothness = 0.8f, Emission = C(1f, 0.78f, 0.52f) * 3f },
            Plain("M_SwitchPlate", C(0.9f, 0.89f, 0.85f), 0.6f),
            S("M_DoorInterior", "white_plaster_02", 1f, C(0.9f, 0.89f, 0.85f), 0.75f),
            // Objetos de misión.
            Plain("M_FolderBlue", C(0.06f, 0.16f, 0.42f), 0.45f),
            Plain("M_Paper", C(0.9f, 0.89f, 0.84f), 0.2f),
            Plain("M_Label", C(0.92f, 0.9f, 0.82f), 0.3f),
            Plain("M_PlasticWhite", C(0.88f, 0.88f, 0.86f), 0.55f),
            Plain("M_Canvas", C(0.55f, 0.5f, 0.42f), 0.15f),
            Plain("M_ClothFolded", C(0.42f, 0.5f, 0.6f), 0.1f),
            Plain("M_ClothFolded2", C(0.7f, 0.55f, 0.52f), 0.1f),
            // Refugio de Víctor.
            Plain("M_BackpackFabric", C(0.2f, 0.24f, 0.19f), 0.2f),
            Plain("M_BackpackDark", C(0.07f, 0.075f, 0.07f), 0.25f),
            Plain("M_Cardboard", C(0.52f, 0.4f, 0.27f), 0.15f),
            Plain("M_CardboardTape", C(0.6f, 0.5f, 0.35f), 0.5f),
            S("M_Blanket", "floral_jacquard", 0.38f, C(0.45f, 0.5f, 0.55f), 0.4f),
            Plain("M_Ceramic", C(0.85f, 0.82f, 0.74f), 0.85f),
            Plain("M_CoffeeDregs", C(0.12f, 0.07f, 0.04f), 0.9f),
            Plain("M_Lentils", C(0.35f, 0.24f, 0.14f), 0.5f),
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
