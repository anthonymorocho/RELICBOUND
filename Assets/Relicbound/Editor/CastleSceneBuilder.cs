using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Relicbound.EditorTools
{
    /// <summary>
    /// Genera la escena Castle (pantalla principal, docs/04) con el castillo inicial, terreno, luz y postproceso.
    /// Hasta P3 la disposición del castillo es fija; después la dará el guardado del jugador.
    /// Convención del kit: exterior de cada pieza hacia -Z local.
    /// </summary>
    public static class CastleSceneBuilder
    {
        public const string ScenePath = "Assets/Relicbound/Scenes/Castle.unity";
        private const string EnvironmentFolder = "Assets/Relicbound/Art/Environment";
        private const float TowerOffset = 11f;
        private const float Section = 2f;
        private const float TerrainSize = 160f;

        [MenuItem("Relicbound/Generar escena Castle")]
        public static void Build()
        {
            ArtImportSetup.EnsureFolder("Assets/Relicbound/Scenes");
            ArtImportSetup.EnsureFolder(EnvironmentFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildTerrain();
            var castle = new GameObject("Castle");
            BuildCastleLayout(castle.transform);
            var sun = BuildLighting();
            BuildPostProcessing();
            BuildCamera();
            RenderSettings.sun = sun;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Relicbound] Escena Castle generada.");
        }

        private static GameObject Piece(string model, Transform parent, Vector3 position, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImportSetup.CastleFolder}/{model}.fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            instance.isStatic = true;
            return instance;
        }

        private static void BuildCastleLayout(Transform root)
        {
            var t = TowerOffset;
            foreach (var sx in new[] { -1f, 1f })
            {
                foreach (var sz in new[] { -1f, 1f })
                {
                    Piece("SM_Tower_Round", root, new Vector3(sx * t, 0f, sz * t), 0f).name = "Tower";
                }
            }
            for (var i = 0; i < 10; i++)
            {
                var c = -9f + Section * i;
                Piece("SM_Wall_Straight", root, new Vector3(c, 0f, t), 180f).name = "Wall_N";
                if (Mathf.Abs(c) > 4f) Piece("SM_Wall_Straight", root, new Vector3(c, 0f, -t), 0f).name = "Wall_S";
                Piece("SM_Wall_Straight", root, new Vector3(t, 0f, c), -90f).name = "Wall_E";
                Piece("SM_Wall_Straight", root, new Vector3(-t, 0f, c), 90f).name = "Wall_W";
            }
            Piece("SM_Gatehouse", root, new Vector3(0f, 0f, -t), 0f).name = "Gatehouse";
            Piece("SM_Keep_L1", root, new Vector3(0f, 0f, 2.5f), 0f).name = "Keep";
        }

        private static void BuildTerrain()
        {
            var grass = TerrainLayerFor("Grass", 8f);
            var dirt = TerrainLayerFor("Dirt", 6f);

            var data = new TerrainData
            {
                heightmapResolution = 257,
                alphamapResolution = 512,
            };
            data.size = new Vector3(TerrainSize, 12f, TerrainSize);
            data.terrainLayers = new[] { grass, dirt };

            // Llano alrededor del castillo y colinas suaves a partir de 34 m.
            var res = data.heightmapResolution;
            var heights = new float[res, res];
            for (var y = 0; y < res; y++)
            {
                for (var x = 0; x < res; x++)
                {
                    var wx = (x / (float)(res - 1) - 0.5f) * TerrainSize;
                    var wz = (y / (float)(res - 1) - 0.5f) * TerrainSize;
                    var distance = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz));
                    var rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(34f, 70f, distance));
                    var hills = Mathf.PerlinNoise(wx * 0.025f + 10f, wz * 0.025f + 3f) * 0.75f
                                + Mathf.PerlinNoise(wx * 0.07f + 4f, wz * 0.07f + 9f) * 0.25f;
                    heights[y, x] = rise * hills * 0.55f;
                }
            }
            data.SetHeights(0, 0, heights);

            // Patio de tierra y camino desde la puerta, con borde irregular.
            var ares = data.alphamapResolution;
            var maps = new float[ares, ares, 2];
            for (var y = 0; y < ares; y++)
            {
                for (var x = 0; x < ares; x++)
                {
                    var wx = (x / (float)(ares - 1) - 0.5f) * TerrainSize;
                    var wz = (y / (float)(ares - 1) - 0.5f) * TerrainSize;
                    var edgeNoise = (Mathf.PerlinNoise(wx * 0.35f, wz * 0.35f) - 0.5f) * 1.6f;
                    var yard = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9.5f, 12.5f, Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) + edgeNoise));
                    var pathWidth = 1.7f + Mathf.Clamp01((-wz - 12f) / 50f) * 0.8f;
                    var path = wz < -9f ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pathWidth, pathWidth + 1.4f, Mathf.Abs(wx + Mathf.Sin(wz * 0.08f) * 1.5f) + edgeNoise * 0.6f)) : 0f;
                    var worn = Mathf.Clamp01((Mathf.PerlinNoise(wx * 0.12f + 30f, wz * 0.12f) - 0.72f) * 2f);
                    var d = Mathf.Clamp01(Mathf.Max(Mathf.Max(yard, path), worn));
                    maps[y, x, 0] = 1f - d;
                    maps[y, x, 1] = d;
                }
            }
            data.SetAlphamaps(0, 0, maps);
            AssetDatabase.CreateAsset(data, $"{EnvironmentFolder}/TD_CastleGround.asset");

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Ground";
            terrainGo.transform.position = new Vector3(-TerrainSize / 2f, -0.02f, -TerrainSize / 2f);
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainMaterial();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;
            terrainGo.isStatic = true;
        }

        private static TerrainLayer TerrainLayerFor(string family, float tile)
        {
            var path = $"{EnvironmentFolder}/TL_{family}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = ArtImportSetup.LoadTexture(family, "albedo");
            layer.normalMapTexture = ArtImportSetup.LoadTexture(family, "normal");
            layer.tileSize = new Vector2(tile, tile);
            layer.smoothness = 0.05f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static Material TerrainMaterial()
        {
            var path = $"{EnvironmentFolder}/M_Terrain.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        private static Light BuildLighting()
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.82f); // ~5200 K (docs/02)
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sunGo.transform.rotation = Quaternion.Euler(48f, -40f, 0f);

            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0.03f);
            sky.SetFloat("_AtmosphereThickness", 0.75f);
            sky.SetColor("_SkyTint", new Color(0.55f, 0.65f, 0.8f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.45f, 0.36f));
            sky.SetFloat("_Exposure", 1.15f);
            AssetDatabase.CreateAsset(sky, $"{EnvironmentFolder}/M_Sky.mat");
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.6f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.22f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.8f, 0.9f);
            RenderSettings.fogStartDistance = 110f;
            RenderSettings.fogEndDistance = 320f;
            return sun;
        }

        private static void BuildPostProcessing()
        {
            var path = $"{EnvironmentFolder}/VP_Castle.asset";
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.15f);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.25f);
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);

            var volumeGo = new GameObject("PostProcess");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void BuildCamera()
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 600f;
            camera.clearFlags = CameraClearFlags.Skybox;
            var data = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraGo.AddComponent<AudioListener>();

            // Vista inicial de docs/04: tres cuartos desde el suroeste, 45° de inclinación.
            var target = new Vector3(0f, 3f, 0f);
            const float distance = 78f;
            const float pitch = 45f * Mathf.Deg2Rad;
            const float yaw = -35f * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), -Mathf.Cos(yaw) * Mathf.Cos(pitch)) * distance;
            cameraGo.transform.position = target + offset;
            cameraGo.transform.LookAt(target);
        }
    }
}
