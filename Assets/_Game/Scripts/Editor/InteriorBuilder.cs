using BeforeItWasYou.Audio;
using BeforeItWasYou.Interaction;
using BeforeItWasYou.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Interior de la casa de Elena (Tools/Blender/biwy_interior.py + muebles CC0). La casa está a oscuras al
    /// llegar: cada plafón tiene su interruptor junto a la puerta y empieza apagado (ADR-011).
    /// </summary>
    public static class InteriorBuilder
    {
        public const string Model = ArtImport.ModelsFolder + "/Interior/SM_Interior.fbx";
        private const float Floor = 0.45f;
        private const float Laundry = 0.27f;
        private const float Ceil = 3.05f;

        private static readonly (string room, Vector3 lamp, Vector3 plate, float plateYaw, float intensity, bool shadows)[] Rooms =
        {
            ("Sala", new Vector3(7.1f, Ceil - 0.25f, 8.25f), new Vector3(5.95f, Floor + 1.1f, 6.2f), 0f, 1.5f, true),
            ("Comedor", new Vector3(12.1f, Ceil - 0.25f, 8.25f), new Vector3(11.95f, Floor + 1.1f, 10.44f), 180f, 1.3f, false),
            ("Pasillo", new Vector3(9.1f, Ceil - 0.25f, 14.2f), new Vector3(8.56f, Floor + 1.1f, 10.75f), 90f, 1.0f, true),
            ("Dormitorio2", new Vector3(6.5f, Ceil - 0.25f, 12.4f), new Vector3(8.44f, Floor + 1.1f, 12.55f), 270f, 1.1f, false),
            ("DormitorioElena", new Vector3(6.5f, Ceil - 0.25f, 16.15f), new Vector3(8.44f, Floor + 1.1f, 15.35f), 270f, 1.2f, true),
            ("Cocina", new Vector3(12.1f, Ceil - 0.25f, 12.75f), new Vector3(9.76f, Floor + 1.1f, 12.15f), 90f, 1.6f, true),
            ("Bano", new Vector3(10.8f, Ceil - 0.25f, 16.4f), new Vector3(9.76f, Floor + 1.1f, 16.98f), 90f, 1.0f, false),
            ("Lavadero", new Vector3(13.2f, Ceil - 0.25f, 16.5f), new Vector3(13.5f, Laundry + 1.28f, 15.06f), 0f, 1.1f, true),
        };

        public static void Build(Transform root)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            if (asset == null) return;
            var interior = new GameObject("Interior").transform;
            interior.SetParent(root, false);
            foreach (var mf in asset.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.StartsWith("Lamp_")) continue;
                var go = EnvironmentBuilder.MeshCopy(asset, mf.name, interior, Vector3.zero, 0f, 1f);
                if (mf.name == "Ceiling" || mf.name.StartsWith("Lamp")) continue;
                go.AddComponent<MeshCollider>();
                if (mf.name.StartsWith("Floor_"))
                {
                    var surface = mf.name.Contains("Wood") ? Surface.Wood : Surface.Tile;
                    go.AddComponent<SurfaceType>().Value = surface;
                }
            }
            BuildLights(asset, interior);
            BuildDoors(interior);
            Furnish(interior);
        }

        private static void BuildLights(GameObject asset, Transform interior)
        {
            var plateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ArtImport.ModelsFolder + "/Interior/SM_Switch.fbx");
            foreach (var (room, lampPos, platePos, plateYaw, intensity, shadows) in Rooms)
            {
                var shade = EnvironmentBuilder.MeshCopy(asset, "Lamp_" + room, interior, Vector3.zero, 0f, 1f);
                var light = new GameObject("Light_" + room).AddComponent<Light>();
                light.transform.SetParent(interior, false);
                light.transform.position = lampPos;
                light.type = LightType.Point;
                light.color = new Color(1f, 0.76f, 0.52f);
                light.intensity = intensity;
                light.range = 6.5f;
                light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
                light.shadowBias = 0.02f;
                light.shadowNormalBias = 0.25f;
                light.gameObject.AddComponent<UniversalAdditionalLightData>();

                var plate = plateAsset != null ? EnvironmentBuilder.MeshCopy(plateAsset, "Switch", interior, platePos, plateYaw, 1f) : new GameObject();
                plate.name = "Switch_" + room;
                plate.layer = Layers.Interactable;
                var box = plate.AddComponent<BoxCollider>();
                box.size = new Vector3(0.14f, 0.14f, 0.06f);
                var toggle = new GameObject("Rocker").transform;
                toggle.SetParent(plate.transform, false);
                var sw = plate.AddComponent<LightSwitch>();
                sw.Id = "luz_" + room.ToLowerInvariant();
                sw.Configure(new[] { light }, shade != null ? new[] { shade.GetComponent<Renderer>() } : new Renderer[0], toggle, false);
                var source = plate.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.maxDistance = 8f;
                source.playOnAwake = false;
                plate.AddComponent<BusVolume>().Configure(AudioBus.Effects, 0.6f);
                sw.SetAudio(source, AudioAssets.Clip("Props/SWITCH_On"));
                light.enabled = false;
            }
        }

        private static void BuildDoors(Transform interior)
        {
            // (nombre, bisagra, yaw, estado inicial). Las hojas miden 0.78 en huecos de 0.80.
            var doors = new (string, Vector3, float, DoorState)[]
            {
                ("Door_Dormitorio2", new Vector3(8.52f, Floor + 0.01f, 12.39f), 90f, DoorState.Closed),
                ("Door_DormitorioElena", new Vector3(8.52f, Floor + 0.01f, 16.29f), 90f, DoorState.Ajar),
                ("Door_Cocina", new Vector3(9.68f, Floor + 0.01f, 11.21f), 270f, DoorState.Open),
                ("Door_Bano", new Vector3(9.68f, Floor + 0.01f, 16.11f), 270f, DoorState.Ajar),
                ("Door_Lavadero", new Vector3(12.61f, Laundry + 0.01f, 15.02f), 0f, DoorState.Open),
            };
            foreach (var (name, hinge, yaw, state) in doors)
            {
                EnvironmentBuilder.Door(interior, name, "Interior/SM_Door_Interior_Leaf", hinge, yaw, 0.78f, 2.03f, 0.04f,
                    name.Replace("Door_", "puerta_").ToLowerInvariant(), state);
            }
        }

        private static void Furnish(Transform interior)
        {
            var f = new GameObject("Furniture").transform;
            f.SetParent(interior, false);
            // Sala: sofá contra el dormitorio 2, televisor de tubo frente a él, butaca y mecedora.
            Item(f, "sofa_03", new Vector3(6.85f, Floor, 9.98f), 180f, 0.85f, true);
            Item(f, "throw_pillows_01", new Vector3(6.85f, Floor + 0.42f, 10.02f), 180f, 0.8f, false);
            Item(f, "CoffeeTable_01", new Vector3(6.85f, Floor, 8.65f), 0f, 0.72f, true);
            Item(f, "side_table_01", new Vector3(5.25f, Floor, 6.6f), 0f, 1f, true);
            Item(f, "television_02", new Vector3(5.25f, Floor + 0.55f, 6.62f), 0f, 1.15f, false);
            Item(f, "ArmChair_01", new Vector3(8.95f, Floor, 8.0f), 250f, 0.95f, true);
            Item(f, "Rockingchair_01", new Vector3(5.15f, Floor, 9.05f), 110f, 1f, true);
            Item(f, "hanging_picture_frame_01", new Vector3(6.85f, Floor + 1.45f, 10.43f), 180f, 0.8f, false);
            Item(f, "ornate_mirror_01", new Vector3(7.75f, Floor + 1.35f, 6.21f), 0f, 1f, false);
            Item(f, "standing_picture_frame_01", new Vector3(5.05f, Floor + 0.55f, 6.5f), 20f, 1f, false);
            // Comedor.
            Item(f, "dining_table", new Vector3(12.1f, Floor, 8.25f), 0f, 0.8f, true);
            Item(f, "painted_wooden_chair_01", new Vector3(11.5f, Floor, 7.55f), 0f, 1f, true);
            Item(f, "painted_wooden_chair_01", new Vector3(12.7f, Floor, 7.6f), 5f, 1f, true);
            Item(f, "painted_wooden_chair_01", new Vector3(11.5f, Floor, 8.95f), 180f, 1f, true);
            Item(f, "painted_wooden_chair_01", new Vector3(12.75f, Floor, 9.1f), 168f, 1f, true);
            Item(f, "drawer_cabinet", new Vector3(14.04f, Floor, 8.3f), 270f, 0.95f, true);
            Item(f, "wall_clock", new Vector3(12.1f, Floor + 1.95f, 6.21f), 0f, 1f, false);
            Item(f, "jug_01", new Vector3(12.3f, Floor + 0.71f, 8.2f), 30f, 1f, false);
            // Cocina.
            Item(f, "electric_stove", new Vector3(13.97f, Floor, 11.51f), 270f, 1.02f, true);
            Item(f, "vintage_microwave", new Vector3(14.02f, Floor + 0.9f, 14.35f), 270f, 0.48f, false);
            Item(f, "vintage_electric_kettle", new Vector3(14.02f, Floor + 0.9f, 11.0f), 250f, 0.75f, false);
            Item(f, "painted_wooden_cabinet", new Vector3(10.08f, Floor, 13.4f), 90f, 1f, true);
            // Lavadero: estantería metálica, productos, caja apartada de su sitio, escoba y botas.
            Item(f, "steel_frame_shelves_01", new Vector3(12.22f, Laundry, 16.65f), 90f, 0.1f, true);
            Item(f, "multi_cleaner_bottle", new Vector3(12.18f, Laundry + 0.98f, 16.4f), 30f, 1f, false);
            Item(f, "bleach_bottle", new Vector3(12.2f, Laundry + 0.98f, 16.85f), 200f, 1f, false);
            Item(f, "cardboard_box_01", new Vector3(12.55f, Laundry, 17.55f), 23f, 1f, true);
            Item(f, "wicker_basket_01", new Vector3(14.03f, Laundry + 0.85f, 15.42f), 10f, 1f, false);
            Item(f, "wooden_broom", new Vector3(14.38f, Laundry, 17.7f), 270f, 1f, false);
            Item(f, "rubber_boots", new Vector3(13.95f, Laundry, 17.75f), 200f, 1f, false, "rubber_boots_A");
            // Dormitorio de Elena: cama de hierro, mesita (cargador), cómoda (carpeta azul).
            Item(f, "old_bed_frame", new Vector3(5.75f, Floor, 16.99f), 90f, 1f, true);
            Item(f, "ClassicNightstand_01", new Vector3(4.98f, Floor, 16.05f), 90f, 0.9f, true);
            Item(f, "painted_wooden_cabinet", new Vector3(6.3f, Floor, 14.68f), 0f, 0.95f, true);
            Item(f, "standing_picture_frame_02", new Vector3(6.75f, Floor + 1.12f, 14.6f), 340f, 1f, false);
            Item(f, "potted_plant_04", new Vector3(5.85f, Floor + 1.12f, 14.6f), 0f, 1f, false);
            // Dormitorio 2 (costura y trastos).
            Item(f, "drawer_cabinet", new Vector3(4.82f, Floor, 12.6f), 90f, 0.95f, true);
            Item(f, "cardboard_box_01", new Vector3(7.6f, Floor, 13.75f), 8f, 1.1f, true);
            Item(f, "cardboard_box_01", new Vector3(7.62f, Floor + 0.34f, 13.72f), 352f, 1f, false);
            Item(f, "book_encyclopedia_set_01", new Vector3(4.85f, Floor + 1.79f, 12.6f), 90f, 1f, false);
            // Escondite: el armario de Elena (cabe una persona de pie).
            var hide = new GameObject("HideSpot_Armario") { layer = Layers.Interactable };
            hide.transform.SetParent(f, false);
            hide.transform.position = new Vector3(7.8f, Floor + 1.1f, 17.235f);
            hide.AddComponent<BoxCollider>().size = new Vector3(0.06f, 2.0f, 1.3f);
            var inside = new GameObject("Inside").transform;
            inside.SetParent(hide.transform, false);
            inside.SetPositionAndRotation(new Vector3(7.93f, Floor + 0.02f, 17.235f), Quaternion.Euler(0f, 270f, 0f));
            var exit = new GameObject("Exit").transform;
            exit.SetParent(hide.transform, false);
            exit.SetPositionAndRotation(new Vector3(7.3f, Floor + 0.02f, 17.235f), Quaternion.Euler(0f, 270f, 0f));
            var spot = hide.AddComponent<HideSpot>();
            spot.Id = "armario_elena";
            spot.Configure(inside, exit);
            // Objetos de misión (A2).
            MissionItem(f, "SM_Prop_BlueFolder", "carpeta_azul", "Coger la carpeta azul", new Vector3(6.02f, Floor + 1.123f, 14.72f), 12f);
            MissionItem(f, "SM_Prop_Charger", "cargador", "Coger el cargador", new Vector3(4.96f, Floor + 0.632f, 16.12f), 205f);
            MissionItem(f, "SM_Prop_ClothesBag", "ropa", "Llevar la bolsa de ropa", new Vector3(6.25f, Floor + 0.67f, 16.82f), 72f);
            // Pasillo.
            Item(f, "hanging_picture_frame_02", new Vector3(8.57f, Floor + 1.5f, 13.6f), 90f, 0.8f, false);
        }

        private static void MissionItem(Transform parent, string model, string item, string prompt, Vector3 position, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImport.ModelsFolder}/Props/{model}.fbx");
            if (asset == null) return;
            var go = EnvironmentBuilder.MeshCopy(asset, model.Replace("SM_", string.Empty), parent, position, yaw, 1f);
            go.name = "Pickup_" + item;
            go.layer = Layers.Interactable;
            var bounds = go.GetComponent<MeshFilter>().sharedMesh.bounds;
            var box = go.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size + Vector3.one * 0.04f;
            var pickup = go.AddComponent<Pickup>();
            pickup.Id = item;
            pickup.Configure(item, prompt, null, null);
        }

        private static void Item(Transform parent, string model, Vector3 position, float yaw, float scale, bool collide, string variant = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImport.ModelsFolder}/PolyHaven/{model}/{model}.fbx");
            if (asset == null)
            {
                Debug.LogWarning($"[BIWY] Falta el modelo {model}.");
                return;
            }
            var go = EnvironmentBuilder.MeshCopy(asset, variant ?? model, parent, position, yaw, scale);
            if (go == null) return;
            if (collide)
            {
                var box = go.AddComponent<BoxCollider>();
                var bounds = go.GetComponent<MeshFilter>().sharedMesh.bounds;
                box.center = bounds.center;
                box.size = bounds.size;
            }
        }
    }
}
