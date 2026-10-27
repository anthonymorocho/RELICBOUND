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

            var terrain = BuildTerrain();
            var castle = new GameObject("Castle");
            BuildCastleLayout(castle.transform);
            ScatterNature(terrain, new GameObject("Nature").transform);
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

        private static Terrain BuildTerrain()
        {
            var grass = TerrainLayerFor("Grass", 9f);
            var dry = TerrainLayerFor("GrassDry", 11f);
            var dirt = TerrainLayerFor("Dirt", 6f);

            // El asset se crea antes de pintar: si se crea después, Unity pierde los mapas de mezcla.
            var dataPath = $"{EnvironmentFolder}/TD_CastleGround.asset";
            AssetDatabase.DeleteAsset(dataPath);
            var data = new TerrainData();
            AssetDatabase.CreateAsset(data, dataPath);
            data.heightmapResolution = 257;
            data.alphamapResolution = 512;
            data.size = new Vector3(TerrainSize, 12f, TerrainSize);
            data.terrainLayers = new[] { grass, dry, dirt };

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
            var maps = new float[ares, ares, 3];
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
                    // Variación a gran escala (no se repite): rompe el patrón del tile de césped.
                    var macro = Mathf.PerlinNoise(wx * 0.018f + 70f, wz * 0.018f + 40f) * 0.7f
                                + Mathf.PerlinNoise(wx * 0.06f + 11f, wz * 0.06f + 5f) * 0.3f;
                    var dryWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.7f, macro)) * 0.85f;
                    maps[y, x, 0] = (1f - d) * (1f - dryWeight);
                    maps[y, x, 1] = (1f - d) * dryWeight;
                    maps[y, x, 2] = d;
                }
            }
            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Ground";
            terrainGo.transform.position = new Vector3(-TerrainSize / 2f, -0.02f, -TerrainSize / 2f);
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainMaterial();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;
            terrainGo.isStatic = true;
            return terrain;
        }

        /// <summary>
        /// Bosquecillos alrededor del castillo con densidad por ruido (semilla fija): deja libres el patio,
        /// un margen de 3 m junto a las murallas, el camino y el campo abierto frente a la puerta.
        /// </summary>
        private static void ScatterNature(Terrain terrain, Transform root)
        {
            var random = new System.Random(20261027);
            var broadleaf = Models("SM_Tree_Broadleaf_01", "SM_Tree_Broadleaf_02", "SM_Tree_Broadleaf_03");
            var pines = Models("SM_Tree_Pine_01", "SM_Tree_Pine_02");
            var bushes = Models("SM_Bush_01", "SM_Bush_02");
            var rocks = Models("SM_Rock_01", "SM_Rock_02", "SM_Rock_03");

            var placed = new System.Collections.Generic.List<Vector3>();
            for (var attempt = 0; attempt < 6000 && placed.Count < 260; attempt++)
            {
                var x = (float)(random.NextDouble() - 0.5) * (TerrainSize - 8f);
                var z = (float)(random.NextDouble() - 0.5) * (TerrainSize - 8f);
                var ring = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                if (ring < 16f) continue;                                   // castillo y margen de murallas
                if (Mathf.Abs(x + Mathf.Sin(z * 0.08f) * 1.5f) < 5f && z < 0f) continue; // camino
                if (z < -14f && z > -40f && Mathf.Abs(x) < 16f) continue;   // explanada frente a la puerta (despliegue)

                var density = Mathf.PerlinNoise(x * 0.04f + 50f, z * 0.04f + 20f);
                var nearCastle = ring < 28f;
                var roll = (float)random.NextDouble();
                if (roll > density * (nearCastle ? 0.6f : 1.1f)) continue;

                var position = new Vector3(x, 0f, z);
                var tooClose = false;
                foreach (var other in placed)
                {
                    if ((other - position).sqrMagnitude < 9f) { tooClose = true; break; }
                }
                if (tooClose) continue;
                placed.Add(position);

                GameObject[] pool;
                var kind = random.NextDouble();
                if (nearCastle) pool = kind < 0.55 ? bushes : kind < 0.85 ? rocks : broadleaf;
                else pool = kind < 0.45 ? broadleaf : kind < 0.7 ? pines : kind < 0.88 ? bushes : rocks;

                var model = pool[random.Next(pool.Length)];
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root);
                position.y = terrain.SampleHeight(position) + terrain.transform.position.y - 0.05f;
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                instance.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.2f, (float)random.NextDouble());
                instance.isStatic = true;
            }
        }

        private static GameObject[] Models(params string[] names)
        {
            var result = new GameObject[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtImportSetup.NatureFolder}/{names[i]}.fbx");
            }
            return result;
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
            color.postExposure.Override(0f);
            color.contrast.Override(10f);
            color.saturation.Override(-8f);
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
