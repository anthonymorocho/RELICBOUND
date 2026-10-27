using BeforeItWasYou.Audio;
using BeforeItWasYou.Interaction;
using BeforeItWasYou.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Compone el entorno del episodio (calle, parcela, casa, patio, cobertizo, vecinos) con el arte de
    /// Tools/Blender/biwy_exterior.py y los modelos CC0 de Poly Haven, y devuelve los puntos clave.
    /// Coordenadas del plano técnico (Docs/Proyecto/PLANO_TECNICO.md): x este, z norte, calle al sur.
    /// </summary>
    public static class EnvironmentBuilder
    {
        public const string ExteriorModel = ArtImport.ModelsFolder + "/Exterior/SM_Exterior.fbx";
        public const float Floor = 0.45f;

        public sealed class Result
        {
            public Transform Root;
            public Transform ArrivalSpawn;
            public Transform MenuFrom;
            public Transform MenuTo;
        }

        public static Result Build(Transform moon)
        {
            var root = new GameObject("Environment").transform;
            var markers = Child(root, "Markers");
            SetupSky(moon);

            var exterior = AssetDatabase.LoadAssetAtPath<GameObject>(ExteriorModel);
            if (exterior != null)
            {
                var architecture = Child(root, "Architecture");
                foreach (var name in new[] { "House_Elena", "Site", "Fences", "Shed", "Neighbor_W", "Neighbor_E" })
                {
                    var go = MeshCopy(exterior, name, architecture, Vector3.zero, 0f, 1f);
                    if (go == null) continue;
                    go.AddComponent<MeshCollider>();
                    if (name == "Site") go.AddComponent<SurfaceType>().Value = Surface.Concrete;
                }
                MeshCopy(exterior, "Mailbox", architecture, new Vector3(4.3f, 0f, -1.75f), 0f, 1f)?.AddComponent<MeshCollider>();
                BuildDoors(root);
                var car = AssetDatabase.LoadAssetAtPath<GameObject>(ArtImport.ModelsFolder + "/Vehicles/SM_Car_Daniel.fbx");
                if (car != null)
                {
                    // Recién aparcado en la entrada, algo torcido, con el morro hacia el portón.
                    var parked = MeshCopy(car, "Car_Daniel", architecture, new Vector3(2.15f, 0.02f, 4.75f), -2.5f, 1f);
                    if (parked == null) throw new System.InvalidOperationException("SM_Car_Daniel.fbx no contiene malla.");
                    var collider = parked.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0f, 0.7f, 0f);
                    collider.size = new Vector3(1.8f, 1.4f, 4.72f);
                }
                BuildStreetLights(exterior, root);
                BuildVegetationAndProps(root);
                BuildLights(root);
            }
            else
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.SetParent(root, false);
                ground.transform.localScale = new Vector3(8f, 1f, 8f);
            }

            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                if (t.GetComponent<Door>() != null || t.GetComponentInParent<Door>() != null) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }

            Marker(markers, "View_Street", new Vector3(9.4f, 1.62f, -6.2f), new Vector3(2f, -6f, 0f));
            Marker(markers, "View_Porch", new Vector3(6.55f, 1.62f, 2.2f), new Vector3(3f, 0f, 0f));
            Marker(markers, "View_Driveway", new Vector3(2.4f, 1.62f, 0.6f), new Vector3(4f, 12f, 0f));
            Marker(markers, "View_Backyard", new Vector3(7.2f, 1.62f, 23.8f), new Vector3(4f, 118f, 0f));
            Marker(markers, "View_Gate", new Vector3(2.2f, 1.62f, 20.5f), new Vector3(3f, 180f, 0f));
            return new Result
            {
                Root = root,
                ArrivalSpawn = Marker(markers, "Spawn_Arrival", new Vector3(0.45f, 0.05f, 5.9f), new Vector3(0f, 92f, 0f)),
                MenuFrom = Marker(markers, "Menu_From", ArrivalSceneBuilder.MenuFrom, ArrivalSceneBuilder.MenuFromEuler),
                MenuTo = Marker(markers, "Menu_To", ArrivalSceneBuilder.MenuTo, ArrivalSceneBuilder.MenuToEuler),
            };
        }

        // ---------- Cielo, ambiente y niebla ----------

        private static void SetupSky(Transform moon)
        {
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(ArtImport.SkyFolder + "/moonlit_golf_2k.hdr");
            var skyPath = ArtImport.MaterialsFolder + "/Exterior/M_Sky_Night.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Cubemap"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.SetTexture("_Tex", cube);
            sky.SetFloat("_Exposure", 0.85f);
            sky.SetFloat("_Rotation", 200f);
            sky.SetColor("_Tint", new Color(0.46f, 0.5f, 0.58f));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.sun = moon.GetComponent<Light>();
            // Ambiente explícito (sin depender de un horneado): cielo azul oscuro, horizonte apagado, suelo casi negro.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.075f, 0.092f, 0.135f);
            RenderSettings.ambientEquatorColor = new Color(0.05f, 0.055f, 0.068f);
            RenderSettings.ambientGroundColor = new Color(0.022f, 0.022f, 0.024f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = 0.55f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.052f, 0.062f, 0.082f);
            RenderSettings.fogDensity = 0.016f;
        }

        // ---------- Puertas y portón ----------

        private static void BuildDoors(Transform root)
        {
            var doors = Child(root, "Doors");
            Door(doors, "Door_Front", "SM_Door_Front_Leaf", new Vector3(6.14f, Floor + 0.012f, 6.05f), 0f, 0.86f, 2.06f, 0.045f, "puerta_principal", DoorState.Closed);
            Door(doors, "Door_Service", "SM_Door_Service_Leaf", new Vector3(13.46f, 0.282f, 17.95f), 180f, 0.8f, 2.0f, 0.04f, "puerta_servicio", DoorState.Ajar);
            Door(doors, "Door_Shed", "SM_Door_Shed_Leaf", new Vector3(14.2f, 0.13f, 23.5f), 90f, 0.88f, 1.86f, 0.035f, "puerta_cobertizo", DoorState.Closed);
            Door(doors, "Gate_Side", "SM_Gate_Side_Leaf", new Vector3(1.62f, 0f, 13.0f), 0f, 1.16f, 1.75f, 0.07f, "porton", DoorState.Closed);
        }

        private static void Door(Transform parent, string name, string model, Vector3 hinge, float yaw, float width, float height, float thickness, string id, DoorState state)
        {
            var go = new GameObject(name) { layer = Layers.Interactable };
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(hinge, Quaternion.Euler(0f, yaw, 0f));
            var leafAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImport.ModelsFolder}/Exterior/{model}.fbx");
            var leaf = new GameObject("Leaf") { layer = Layers.Interactable };
            leaf.transform.SetParent(go.transform, false);
            if (leafAsset != null)
            {
                var source = leafAsset.GetComponentInChildren<MeshFilter>();
                leaf.transform.localRotation = source.transform.localRotation;
                leaf.transform.localScale = source.transform.localScale;
                leaf.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                leaf.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            }
            var box = leaf.AddComponent<BoxCollider>();
            box.center = new Vector3(width / 2f, height / 2f, thickness / 2f - (name == "Gate_Side" ? 0.06f : 0f));
            box.size = new Vector3(width, height, Mathf.Max(0.04f, thickness));
            var door = go.AddComponent<Door>();
            door.Id = id;
            door.Configure(leaf.transform, name == "Gate_Side" ? 95f : 100f, name != "Gate_Side");
            EditorUtil.Set(door, "state", (int)state);
            var source3d = go.AddComponent<AudioSource>();
            source3d.spatialBlend = 1f;
            source3d.minDistance = 1f;
            source3d.maxDistance = 18f;
            source3d.playOnAwake = false;
            go.AddComponent<BusVolume>().Configure(AudioBus.Effects, 0.9f);
            door.SetAudio(source3d, null, null);
        }

        // ---------- Calle: postes con luminaria ----------

        private static void BuildStreetLights(GameObject exterior, Transform root)
        {
            var street = Child(root, "Street");
            var positions = new[] { new Vector3(-7.5f, 0f, -1.35f), new Vector3(18.6f, 0f, -1.35f), new Vector3(44.5f, 0f, -1.35f) };
            for (var i = 0; i < positions.Length; i++)
            {
                var pole = MeshCopy(exterior, "StreetLight", street, positions[i], 180f, 1f);
                if (pole == null) continue;
                pole.name = "StreetLight_" + i;
                pole.AddComponent<CapsuleCollider>().height = 8f;
                // Luminaria LED fría: cono hacia la calzada y la acera.
                var light = new GameObject("Lamp").AddComponent<Light>();
                light.transform.SetParent(pole.transform, false);
                light.transform.localPosition = new Vector3(0f, 6.82f, 2.1f);
                light.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                light.type = LightType.Spot;
                light.spotAngle = 128f;
                light.innerSpotAngle = 70f;
                light.range = 19f;
                light.intensity = i == 1 ? 9f : 7f;
                light.color = new Color(0.82f, 0.88f, 1f);
                light.shadows = i == 1 ? LightShadows.Soft : LightShadows.None;
                light.shadowBias = 0.02f;
                light.gameObject.AddComponent<UniversalAdditionalLightData>();
            }
            // Cables entre postes (catenaria suave).
            for (var i = 0; i < positions.Length - 1; i++)
            {
                for (var w = 0; w < 3; w++)
                {
                    var offset = new Vector3(-0.85f + w * 0.85f, 7.95f, 0f);
                    Cable(street, positions[i] + offset, positions[i + 1] + offset, 0.55f + w * 0.05f);
                }
            }
        }

        private static void Cable(Transform parent, Vector3 a, Vector3 b, float sag)
        {
            var go = new GameObject("Cable");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            const int segments = 24;
            line.positionCount = segments + 1;
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                line.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t)));
            }
            line.widthMultiplier = 0.018f;
            line.sharedMaterial = ExteriorMaterials.Get("M_MetalDark");
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.numCapVertices = 0;
        }

        // ---------- Vegetación y objetos CC0 ----------

        private static void BuildVegetationAndProps(Transform root)
        {
            var plants = Child(root, "Vegetation");
            // Parterre de la fachada: arbustos variados, nunca en fila perfecta.
            Prop(plants, "shrub_02", "shrub_02_A", new Vector3(8.65f, 0.04f, 5.4f), 20f, 0.62f);
            Prop(plants, "shrub_02", "shrub_02_D", new Vector3(10.1f, 0.04f, 5.45f), 160f, 0.72f);
            Prop(plants, "shrub_02", "shrub_02_B", new Vector3(12.3f, 0.04f, 5.45f), 75f, 0.66f);
            Prop(plants, "shrub_02", "shrub_02_C", new Vector3(14.05f, 0.04f, 5.35f), 300f, 0.52f);
            Prop(plants, "shrub_02", "shrub_02_D", new Vector3(5.1f, 0.04f, 5.35f), 10f, 0.55f);
            Prop(plants, "shrub_03", "shrub_03_A", new Vector3(11.25f, 0.04f, 5.15f), 200f, 1.6f);
            Prop(plants, "shrub_04", "shrub_04_C", new Vector3(9.35f, 0.04f, 5.2f), 40f, 2.2f);
            Prop(plants, "shrub_03", "shrub_03_C", new Vector3(13.2f, 0.04f, 5.2f), 120f, 1.8f);
            Prop(plants, "shrub_02", "shrub_02_A", new Vector3(-0.4f, 0f, 8.5f), 80f, 0.9f);
            Prop(plants, "shrub_02", "shrub_02_C", new Vector3(-0.5f, 0f, 10.6f), 140f, 0.8f);
            // Árbol grande del jardín delantero (lado este) y otro en el del vecino oeste.
            Prop(plants, "jacaranda_tree", "jacaranda_tree", new Vector3(15.6f, 0f, 2.2f), 35f, 0.52f);
            Prop(plants, "jacaranda_tree", "jacaranda_tree", new Vector3(-6.0f, 0f, 3.5f), 210f, 0.46f);
            Prop(plants, "jacaranda_tree", "jacaranda_tree", new Vector3(9.0f, 0f, 30.5f), 120f, 0.6f);

            var props = Child(root, "Props");
            Prop(props, "potted_plant_02", "potted_plant_02_A", new Vector3(5.45f, Floor, 4.85f), 30f, 1f);
            Prop(props, "planter_pot_clay", "planter_pot_clay", new Vector3(7.75f, Floor, 4.8f), 0f, 1.2f);
            Prop(props, "metal_trash_can", "metal_trash_can_A", new Vector3(4.05f, 0.02f, 10.2f), 95f, 1f);
            Prop(props, "metal_trash_can", "metal_trash_can_B", new Vector3(4.0f, 0.02f, 11.0f), 260f, 1f);
            Prop(props, "exterior_aircon_unit", "exterior_aircon_unit_A", new Vector3(14.92f, 0.03f, 13.2f), 270f, 1f);
            Prop(props, "garden_hose_wall_mounted_01", "garden_hose_wall_mounted_01", new Vector3(4.47f, 0.55f, 9.2f), 270f, 1f);
            Prop(props, "utility_box_01", "utility_box_01", new Vector3(-2.2f, 0f, -0.9f), 180f, 1f);
            Prop(props, "fire_hydrant", "fire_hydrant_A", new Vector3(19.6f, 0f, -1.55f), 200f, 1f);
            Prop(props, "water_manhole_cover", "water_manhole_cover", new Vector3(8.6f, -0.148f, -5.4f), 0f, 1f);
            Prop(props, "outdoor_table_chair_set_01", "outdoor_table_chair_set_01_A", new Vector3(12.4f, 0.03f, 19.4f), 15f, 1f);
            Prop(props, "outdoor_table_chair_set_01", "outdoor_table_chair_set_01_B", new Vector3(13.3f, 0.03f, 19.1f), 200f, 1f);
            Prop(props, "security_light", "security_light", new Vector3(14.32f, 2.2f, 21.5f), 180f, 0.8f);
        }

        private static GameObject Prop(Transform parent, string model, string variant, Vector3 position, float yaw, float scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImport.ModelsFolder}/PolyHaven/{model}/{model}.fbx");
            if (asset == null)
            {
                Debug.LogWarning($"[BIWY] Falta el modelo {model}.");
                return null;
            }
            var go = MeshCopy(asset, variant, parent, position, yaw, scale) ?? MeshCopy(asset, model, parent, position, yaw, scale);
            if (go == null) Debug.LogWarning($"[BIWY] {model}: no existe la variante {variant}.");
            return go;
        }

        // ---------- Luces prácticas ----------

        private static void BuildLights(Transform root)
        {
            var lights = Child(root, "Lights");
            // Farol del porche (fotocélula: siempre encendido de noche).
            Point(lights, "Light_Porch", new Vector3(7.33f, Floor + 1.62f, 5.83f), new Color(1f, 0.7f, 0.42f), 2.4f, 7.5f, LightShadows.Soft);
            // Rebote cálido bajo el techo del porche.
            Point(lights, "Light_PorchFill", new Vector3(6.55f, 2.7f, 5.1f), new Color(1f, 0.72f, 0.48f), 0.35f, 4f, LightShadows.None);
            // Vecinos: porche y alguna habitación con luz (la calle está viva; la casa de Elena, no).
            Point(lights, "Light_NeighborW_Porch", new Vector3(-10.45f, 2.3f, 6.6f), new Color(1f, 0.74f, 0.45f), 1.4f, 6f, LightShadows.None);
            Point(lights, "Light_NeighborW_Room", new Vector3(-7.8f, 2.0f, 7.6f), new Color(1f, 0.66f, 0.38f), 0.9f, 4f, LightShadows.None);
            Point(lights, "Light_NeighborE_Room", new Vector3(27.2f, 2.0f, 7.1f), new Color(1f, 0.66f, 0.38f), 0.9f, 4f, LightShadows.None);
            Point(lights, "Light_Backyard_Security", new Vector3(14.1f, 2.15f, 21.35f), new Color(0.95f, 0.9f, 0.8f), 0.0f, 9f, LightShadows.Soft);
        }

        private static Light Point(Transform parent, string name, Vector3 position, Color color, float intensity, float range, LightShadows shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows;
            light.shadowBias = 0.02f;
            light.shadowNormalBias = 0.3f;
            light.enabled = intensity > 0f;
            go.AddComponent<UniversalAdditionalLightData>();
            return light;
        }

        // ---------- Utilidades ----------

        /// <summary>Copia la malla de un hijo del FBX en un GameObject propio (sin depender de la jerarquía del modelo).</summary>
        private static GameObject MeshCopy(GameObject asset, string childName, Transform parent, Vector3 position, float yaw, float scale)
        {
            Transform source = null;
            foreach (var t in asset.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == childName && t.GetComponent<MeshFilter>() != null) source = t;
            }
            // Un FBX de un solo objeto se importa con la malla en la raíz, que toma el nombre del archivo.
            if (source == null)
            {
                var filters = asset.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length == 1) source = filters[0].transform;
            }
            if (source == null) return null;
            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * source.localRotation);
            go.transform.localScale = source.localScale * scale;
            go.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            return go;
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Marker(Transform parent, string name, Vector3 position, Vector3 euler)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.SetPositionAndRotation(position, Quaternion.Euler(euler));
            return marker;
        }
    }
}
