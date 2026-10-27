using BeforeItWasYou.AI;
using BeforeItWasYou.Audio;
using BeforeItWasYou.Episode;
using BeforeItWasYou.Interaction;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Víctor, su refugio en el cobertizo, las pistas, las zonas de la parcela, la NavMesh y el director de la historia.
    /// </summary>
    public static class StoryBuilder
    {
        private const float Floor = 0.45f;
        private const float Laundry = 0.27f;
        public const string NavMeshPath = "Assets/_Game/Scenes/NavMesh_E01.asset";

        public static StoryDirector Build(Transform environment, EpisodeDirector director)
        {
            var root = new GameObject("Story").transform;
            var nodes = new IntruderAI.Nodes
            {
                behindShed = Node(root, "Node_BehindShed", new Vector3(15.7f, 0.02f, 25.25f), 270f),
                shedInside = Node(root, "Node_ShedInside", new Vector3(15.6f, 0.13f, 23.0f), 270f),
                shedDoorway = Node(root, "Node_ShedDoorway", new Vector3(13.75f, 0.03f, 23.02f), 90f),
                serviceDoorOutside = Node(root, "Node_ServiceOutside", new Vector3(13.1f, 0.15f, 19.0f), 180f),
                laundryInside = Node(root, "Node_LaundryInside", new Vector3(13.05f, Laundry, 16.5f), 90f),
                gateInside = Node(root, "Node_GateInside", new Vector3(2.2f, 0.02f, 14.2f), 180f),
                street = Node(root, "Node_Street", new Vector3(-14f, -0.15f, -6f), 270f),
            };

            // ---------- Refugio de Víctor ----------
            var refuge = new GameObject("Refuge").transform;
            refuge.SetParent(root, false);
            var bed = Model(refuge, "Props/SM_Prop_Refuge", new Vector3(16.35f, 0.125f, 23.15f), 0f, 1f);
            AddInspect(bed, "Mirar", "Cartones, una manta doblada. Alguien duerme aquí.", "manta_cobertizo");
            PolyHaven(refuge, "russian_food_cans_01", new Vector3(15.75f, 0.125f, 24.2f), 30f);
            PolyHaven(refuge, "long_life_food", new Vector3(15.6f, 0.125f, 24.3f), 300f, "long_life_food_B");
            PolyHaven(refuge, "plastic_bottle_gallon", new Vector3(16.95f, 0.125f, 24.25f), 80f);
            PolyHaven(refuge, "trashbag", new Vector3(16.85f, 0.125f, 21.95f), 200f);
            var bag = Model(refuge, "Props/SM_Prop_Backpack", new Vector3(15.55f, 0.125f, 22.3f), 115f, 1f);
            bag.name = "Backpack_Victor";
            bag.layer = Layers.Interactable;
            AddBox(bag, 0.06f);
            var bagAction = bag.AddComponent<ActionPoint>();
            bagAction.Id = "mochila_victor";
            bagAction.Configure("Mirar la mochila");
            var shedLight = new GameObject("Light_ShedCandle").AddComponent<Light>();
            shedLight.transform.SetParent(refuge, false);
            shedLight.transform.position = new Vector3(16.9f, 0.4f, 24.1f);
            shedLight.type = LightType.Point;
            shedLight.color = new Color(1f, 0.6f, 0.3f);
            shedLight.intensity = 0f;
            shedLight.range = 3f;

            // ---------- Pistas de la casa ----------
            var clues = new GameObject("Clues").transform;
            clues.SetParent(root, false);
            var can = Model(clues, "Props/SM_Prop_OpenCan", new Vector3(14.0f, Floor + 0.9f, 13.62f), 40f, 1f);
            AddInspect(can, "Mirar", "Lentejas de lata, abiertas. Mamá no come de lata.", "lata_abierta");
            var mug = Model(clues, "Props/SM_Prop_Mug", new Vector3(13.9f, Laundry + 0.82f, 16.25f), 160f, 1f);
            AddInspect(mug, "Mirar", "Esta taza no es de casa.", "taza_ajena");
            var photo = GameObject.Find("Environment/Interior/Furniture/standing_picture_frame_01");
            if (photo != null) AddInspect(photo, "Mirar la foto", "Navidad. La última con papá.", null);
            var box = Model(clues, "Props/SM_Prop_LaundryBox", new Vector3(14.0f, Laundry + 0.851f, 15.45f), 8f, 1f);
            AddBox(box, 0f);
            var body = box.AddComponent<Rigidbody>();
            body.mass = 3f;
            body.isKinematic = true;
            var thud = box.AddComponent<AudioSource>();
            thud.clip = AudioAssets.Clip("Props/BOX_Fall");
            thud.playOnAwake = false;
            thud.spatialBlend = 1f;
            thud.maxDistance = 25f;
            thud.rolloffMode = AudioRolloffMode.Linear;
            box.AddComponent<BusVolume>().Configure(AudioBus.Effects, 1f);

            // ---------- Víctor ----------
            var victor = BuildVictor(root, nodes);
            var doors = environment.Find("Doors");
            var service = doors.Find("Door_Service").GetComponent<Door>();
            var shed = doors.Find("Door_Shed").GetComponent<Door>();
            var gate = doors.Find("Gate_Side").GetComponent<Door>();
            var front = doors.Find("Door_Front").GetComponent<Door>();
            front.ConfigureKey(AudioAssets.Clip("Doors/DOOR_Unlock"));
            EditorUtil.Set(front, "state", (int)DoorState.Locked);
            service.ConfigureFaultyLatch();
            var ai = victor.GetComponent<IntruderAI>();
            ai.Configure(nodes, victor.GetComponentInChildren<Animator>(), FindHead(victor.transform), service, shed, gate, bag, body, victor.GetComponent<AudioSource>());

            // ---------- Zonas y coche ----------
            var zones = new GameObject("Zones").transform;
            zones.SetParent(root, false);
            var house = Zone(zones, "Zone_House", new Vector3(9.5f, 1.6f, 12f), new Vector3(9.6f, 3f, 11.6f));
            var shedZone = Zone(zones, "Zone_Shed", new Vector3(15.7f, 1.2f, 23.1f), new Vector3(2.8f, 2.2f, 2.8f));
            var street = Zone(zones, "Zone_Street", new Vector3(9f, 1f, -9f), new Vector3(90f, 4f, 13f));
            var yard = Zone(zones, "Zone_Backyard", new Vector3(9f, 1.2f, 22f), new Vector3(18f, 3f, 8f));
            var carGo = new GameObject("Car_DriverDoor") { layer = Layers.Interactable };
            carGo.transform.SetParent(zones, false);
            carGo.transform.position = new Vector3(1.28f, 1.0f, 5.55f);
            carGo.AddComponent<BoxCollider>().size = new Vector3(0.15f, 0.9f, 1.0f);
            var car = carGo.AddComponent<ActionPoint>();
            car.Id = "coche";
            car.Configure("Subir al coche");

            // ---------- NavMesh (las puertas no cuentan: Víctor las abre al pasar) ----------
            BakeNavMesh(environment.gameObject);

            var story = new GameObject("StoryDirector").AddComponent<StoryDirector>();
            story.transform.SetParent(root, false);
            story.Configure(director, ai, house, shedZone, street, yard, front, service, car, bagAction);
            return story;
        }

        private static GameObject BuildVictor(Transform root, IntruderAI.Nodes nodes)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterImport.VictorModel);
            var victor = asset != null ? (GameObject)PrefabUtility.InstantiatePrefab(asset) : new GameObject("Victor");
            if (asset != null) PrefabUtility.UnpackPrefabInstance(victor, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            victor.name = "Victor";
            victor.transform.SetParent(root, false);
            victor.transform.SetPositionAndRotation(nodes.behindShed.position, nodes.behindShed.rotation);
            foreach (var t in victor.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Intruder;
            var animator = victor.GetComponentInChildren<Animator>();
            if (animator == null) animator = victor.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CharacterImport.ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var agent = victor.AddComponent<NavMeshAgent>();
            agent.radius = 0.25f;
            agent.height = 1.75f;
            agent.baseOffset = 0f;
            var capsule = victor.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.88f, 0f);
            capsule.height = 1.76f;
            capsule.radius = 0.25f;
            var voice = victor.AddComponent<AudioSource>();
            voice.spatialBlend = 1f;
            voice.playOnAwake = false;
            victor.AddComponent<IntruderAI>();
            victor.AddComponent<IntruderDebug>();
            return victor;
        }

        private static Transform FindHead(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLowerInvariant() == "head") return t;
            }
            var eyes = new GameObject("Eyes").transform;
            eyes.SetParent(root, false);
            eyes.localPosition = new Vector3(0f, 1.62f, 0.08f);
            return eyes;
        }

        private static void BakeNavMesh(GameObject environment)
        {
            var surface = environment.GetComponent<NavMeshSurface>() ?? environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~((1 << Layers.Interactable) | (1 << Layers.Player) | (1 << Layers.Intruder));
            surface.BuildNavMesh();
            if (surface.navMeshData != null)
            {
                AssetDatabase.DeleteAsset(NavMeshPath);
                AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            }
        }

        private static Transform Node(Transform parent, string name, Vector3 position, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return t;
        }

        private static ZoneTrigger Zone(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>();
            var zone = go.AddComponent<ZoneTrigger>();
            zone.Configure(name.Replace("Zone_", string.Empty).ToLowerInvariant(), size);
            return zone;
        }

        private static GameObject Model(Transform parent, string relative, Vector3 position, float yaw, float scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImport.ModelsFolder}/{relative}.fbx");
            if (asset == null) return new GameObject(relative);
            var name = System.IO.Path.GetFileName(relative).Replace("SM_", string.Empty);
            return EnvironmentBuilder.MeshCopy(asset, name, parent, position, yaw, scale) ?? new GameObject(name);
        }

        private static GameObject PolyHaven(Transform parent, string model, Vector3 position, float yaw, string variant = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImport.ModelsFolder}/PolyHaven/{model}/{model}.fbx");
            return asset == null ? null : EnvironmentBuilder.MeshCopy(asset, variant ?? model, parent, position, yaw, 1f);
        }

        private static void AddBox(GameObject go, float padding)
        {
            var filter = go.GetComponent<MeshFilter>();
            var box = go.AddComponent<BoxCollider>();
            if (filter == null || filter.sharedMesh == null) return;
            box.center = filter.sharedMesh.bounds.center;
            box.size = filter.sharedMesh.bounds.size + Vector3.one * padding;
        }

        private static void AddInspect(GameObject go, string prompt, string thought, string flag)
        {
            go.layer = Layers.Interactable;
            if (go.GetComponent<Collider>() == null) AddBox(go, 0.08f);
            var inspect = go.AddComponent<Inspectable>();
            inspect.Configure(prompt, thought, flag);
        }
    }
}
