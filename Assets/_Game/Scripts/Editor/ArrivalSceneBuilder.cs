using BeforeItWasYou.Audio;
using BeforeItWasYou.Episode;
using BeforeItWasYou.Interaction;
using BeforeItWasYou.Player;
using BeforeItWasYou.Services;
using BeforeItWasYou.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// L_E01_Arrival: menú sobre la casa y episodio. Estructura pedida: Player_Daniel con Main Camera hija y
    /// CharacterController (Height 1.8, Radius 0.3, Center (0,0.9,0), Step Offset 0.3, Slope Limit 45, Skin Width 0.08)
    /// y Directional Light. El entorno lo compone EnvironmentBuilder.
    /// </summary>
    public static class ArrivalSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/L_E01_Arrival.unity";

        // Encuadres del menú (moodboard: fachada en diagonal, coche a la izquierda, porche iluminado).
        public static readonly Vector3 MenuFrom = new Vector3(-1.4f, 1.5f, -1.6f);
        public static readonly Vector3 MenuFromEuler = new Vector3(2.5f, 52f, 0f);
        public static readonly Vector3 MenuTo = new Vector3(-0.4f, 1.45f, -0.9f);
        public static readonly Vector3 MenuToEuler = new Vector3(2.2f, 47f, 0f);

        [MenuItem("BEFORE IT WAS YOU/Generar L_E01_Arrival")]
        public static void Build()
        {
            EditorUtil.EnsureFolder("Assets/_Game/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var panel = UiAssets.Prepare();

            var moon = new GameObject("Directional Light");
            var light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.62f, 0.72f, 0.98f);
            light.intensity = 0.32f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.35f;
            moon.transform.rotation = Quaternion.Euler(38f, -38f, 0f);
            moon.AddComponent<UniversalAdditionalLightData>();

            var environment = EnvironmentBuilder.Build(moon.transform);

            var volumeGo = new GameObject("PostProcess_Night");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0;
            volume.sharedProfile = PostProcessProfiles.Night();
            volumeGo.AddComponent<PostProcessSettings>();

            // ---------- Daniel ----------
            var daniel = new GameObject("Player_Daniel") { layer = Layers.Player };
            daniel.transform.SetPositionAndRotation(environment.ArrivalSpawn.position, environment.ArrivalSpawn.rotation);
            var body = daniel.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.3f;
            body.center = new Vector3(0f, 0.9f, 0f);
            body.stepOffset = 0.3f;
            body.slopeLimit = 45f;
            body.skinWidth = 0.08f;
            body.minMoveDistance = 0f;

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera", layer = Layers.Player };
            cameraGo.transform.SetParent(daniel.transform, false);
            cameraGo.transform.localPosition = new Vector3(0f, 1.64f, 0f);
            var camera = ConfigureCamera(cameraGo);
            cameraGo.AddComponent<AudioListener>();
            var controller = daniel.AddComponent<PlayerController>();
            EditorUtil.Set(controller, "cameraPivot", cameraGo.transform);
            EditorUtil.Set(controller, "playerCamera", camera);
            var interactor = daniel.AddComponent<Interactor>();
            var steps = new GameObject("Footsteps");
            steps.transform.SetParent(daniel.transform, false);
            var stepsSource = steps.AddComponent<AudioSource>();
            stepsSource.playOnAwake = false;
            stepsSource.spatialBlend = 0f;
            steps.AddComponent<BusVolume>().Configure(AudioBus.Effects, 0.7f);
            steps.AddComponent<FootstepAudio>().Configure(controller, stepsSource, AudioAssets.FootstepBanks());

            // ---------- Cámara del menú ----------
            var menuGo = new GameObject("MenuCamera");
            ConfigureCamera(menuGo);
            menuGo.AddComponent<AudioListener>();
            var rig = menuGo.AddComponent<MenuCameraRig>();
            rig.Configure(environment.MenuFrom.position, environment.MenuFrom.eulerAngles, environment.MenuTo.position, environment.MenuTo.eulerAngles, 52f);

            // ---------- Interfaz ----------
            var uiGo = new GameObject("UI");
            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssets.FrontEndPath);
            var ui = uiGo.AddComponent<GameUi>();
            EditorUtil.Set(ui, "credits", CreditsGenerator.Generate());
            var uiSource = uiGo.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.ignoreListenerPause = true;
            uiGo.AddComponent<UiSounds>().Configure(uiSource, AudioAssets.Clip("UI/UI_Select"), AudioAssets.Clip("UI/UI_Confirm"), AudioAssets.Clip("UI/UI_PanelClose"));

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var prologueGo = new GameObject("PrologueAudio");
            var prologueSource = prologueGo.AddComponent<AudioSource>();
            prologueSource.playOnAwake = false;
            prologueSource.loop = true;
            prologueSource.spatialBlend = 0f;
            prologueSource.clip = AudioAssets.Clip("Ambience/AMB_Kitchen_RoomTone");
            prologueGo.AddComponent<BusVolume>().Configure(AudioBus.Ambience, 0.8f);

            var directorGo = new GameObject("EpisodeDirector");
            var director = directorGo.AddComponent<EpisodeDirector>();
            director.Configure(ui, controller, interactor, rig, environment.ArrivalSpawn, prologueSource);
            directorGo.AddComponent<EpisodeState>().Configure(director);
            StoryBuilder.Build(environment.Root, director);
            var phoneSource = directorGo.AddComponent<AudioSource>();
            phoneSource.playOnAwake = false;
            phoneSource.spatialBlend = 0f;
            directorGo.AddComponent<ArrivalNarrative>().Configure(director, phoneSource, AudioAssets.Clip("Props/PHONE_Vibrate"));

            // Ambiente nocturno: grillos y viento, amortiguados dentro de la casa.
            var ambienceGo = new GameObject("Ambience");
            var ambience = new List<AudioSource>();
            foreach (var (clip, volume) in new[] { ("Ambience/AMB_Night_Crickets", 0.55f), ("Ambience/AMB_Night_Wind", 0.7f) })
            {
                var child = new GameObject(clip.Substring(clip.LastIndexOf('/') + 1));
                child.transform.SetParent(ambienceGo.transform, false);
                var src = child.AddComponent<AudioSource>();
                src.clip = AudioAssets.Clip(clip);
                src.loop = true;
                src.playOnAwake = true;
                src.spatialBlend = 0f;
                child.AddComponent<AudioLowPassFilter>().cutoffFrequency = 22000f;
                child.AddComponent<BusVolume>().Configure(AudioBus.Ambience, volume);
                ambience.Add(src);
            }
            ambienceGo.AddComponent<AmbienceZone>().Configure(ambience.ToArray(), new Bounds(new Vector3(9.5f, 1.6f, 12f), new Vector3(10f, 3.4f, 12f)));
            EditorUtility.SetDirty(director);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Camera ConfigureCamera(GameObject go)
        {
            var camera = go.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 400f;
            camera.fieldOfView = 64f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            return camera;
        }
    }
}
