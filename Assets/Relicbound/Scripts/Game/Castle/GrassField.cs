using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Relicbound.Game.Castle
{
    /// <summary>
    /// Hierba y flores instanciadas por GPU sobre el terreno (docs/02: vegetación coherente, sin terrenos lisos).
    /// Se dibujan en beginCameraRendering para que aparezcan también en capturas del editor y en cada cámara URP.
    /// </summary>
    [ExecuteAlways]
    public sealed class GrassField : MonoBehaviour
    {
        [SerializeField] private Terrain terrain;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material[] flowerMaterials;
        [SerializeField] private float radius = 72f;
        [SerializeField] private float spacing = 0.75f;
        [SerializeField] private float keepOutHalfSize = 13.5f;
        [SerializeField] private int seed = 1871;

        private Mesh _grassMesh;
        private Mesh _flowerMesh;
        private readonly List<Matrix4x4> _grass = new List<Matrix4x4>();
        private readonly List<Matrix4x4>[] _flowers = { new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>() };
        private Matrix4x4[] _batch = new Matrix4x4[1023];

        /// <summary>Fracción de densidad según la calidad gráfica (el director la ajusta: móvil &lt; PC).</summary>
        public static float DensityScale { get; set; } = 1f;

        private void OnEnable()
        {
            _grassMesh = BuildTuft(7, 0.55f, false);
            _flowerMesh = BuildTuft(3, 0.42f, true);
            Scatter();
            RenderPipelineManager.beginCameraRendering += Draw;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Draw;
        }

        public void Scatter()
        {
            _grass.Clear();
            foreach (var list in _flowers) list.Clear();
            if (terrain == null) return;
            var random = new System.Random(seed);
            var data = terrain.terrainData;
            var origin = terrain.transform.position;
            var alpha = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            var step = spacing / Mathf.Sqrt(Mathf.Max(0.1f, DensityScale));

            for (var z = -radius; z < radius; z += step)
            {
                for (var x = -radius; x < radius; x += step)
                {
                    var px = x + ((float)random.NextDouble() - 0.5f) * step;
                    var pz = z + ((float)random.NextDouble() - 0.5f) * step;
                    if (Mathf.Max(Mathf.Abs(px), Mathf.Abs(pz)) < keepOutHalfSize) continue;
                    if (px * px + pz * pz > radius * radius) continue;

                    var u = Mathf.Clamp01((px - origin.x) / data.size.x);
                    var v = Mathf.Clamp01((pz - origin.z) / data.size.z);
                    var ax = Mathf.Min(data.alphamapWidth - 1, (int)(u * data.alphamapWidth));
                    var az = Mathf.Min(data.alphamapHeight - 1, (int)(v * data.alphamapHeight));
                    var dirt = alpha[az, ax, alpha.GetLength(2) - 1];
                    if (dirt > 0.35f) continue;

                    // Manchas: más densidad en zonas del ruido, claros entre ellas.
                    var clump = Mathf.PerlinNoise(px * 0.09f + 3f, pz * 0.09f + 7f);
                    if (random.NextDouble() > Mathf.SmoothStep(0.15f, 0.85f, clump) * (1f - dirt * 2f)) continue;

                    var position = new Vector3(px, terrain.SampleHeight(new Vector3(px, 0f, pz)) + origin.y - 0.02f, pz);
                    var rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                    var scale = Mathf.Lerp(0.7f, 1.35f, (float)random.NextDouble()) * Mathf.Lerp(0.8f, 1.15f, clump);
                    var matrix = Matrix4x4.TRS(position, rotation, new Vector3(scale, scale * Mathf.Lerp(0.85f, 1.3f, clump), scale));

                    var flowerRoll = random.NextDouble();
                    var flowerPatch = Mathf.PerlinNoise(px * 0.05f + 40f, pz * 0.05f + 11f);
                    if (flowerMaterials != null && flowerMaterials.Length > 0 && flowerRoll < 0.06 * Mathf.SmoothStep(0.45f, 0.8f, flowerPatch) * 4f)
                    {
                        _flowers[random.Next(Mathf.Min(_flowers.Length, flowerMaterials.Length))].Add(matrix);
                    }
                    else
                    {
                        _grass.Add(matrix);
                    }
                }
            }
        }

        private void Draw(ScriptableRenderContext context, Camera camera)
        {
            if (grassMaterial == null) return;
            Submit(_grassMesh, grassMaterial, _grass, camera);
            for (var i = 0; i < _flowers.Length && flowerMaterials != null && i < flowerMaterials.Length; i++)
            {
                Submit(_flowerMesh, flowerMaterials[i], _flowers[i], camera);
            }
        }

        private void Submit(Mesh mesh, Material material, List<Matrix4x4> instances, Camera camera)
        {
            if (material == null || instances.Count == 0) return;
            var parameters = new RenderParams(material)
            {
                camera = camera,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.Off,
                worldBounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f),
            };
            for (var start = 0; start < instances.Count; start += _batch.Length)
            {
                var count = Mathf.Min(_batch.Length, instances.Count - start);
                instances.CopyTo(start, _batch, 0, count);
                Graphics.RenderMeshInstanced(parameters, mesh, 0, _batch, count);
            }
        }

        /// <summary>
        /// Mata de briznas curvas y estrechadas (3 segmentos). uv.y = altura relativa; en flores, la punta lleva
        /// una cabeza (uv.x = 1) que el shader colorea.
        /// </summary>
        private static Mesh BuildTuft(int blades, float height, bool flowers)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var random = new System.Random(flowers ? 7 : 3);
            for (var b = 0; b < blades; b++)
            {
                var angle = b / (float)blades * Mathf.PI * 2f + (float)random.NextDouble() * 0.6f;
                var lean = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (0.12f + (float)random.NextDouble() * 0.14f);
                var root = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.06f;
                var side = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var h = height * (0.75f + (float)random.NextDouble() * 0.5f);
                var width = flowers ? 0.025f : 0.06f;
                const int segments = 3;
                var baseIndex = vertices.Count;
                for (var s = 0; s <= segments; s++)
                {
                    var t = s / (float)segments;
                    var center = root + lean * t * t + Vector3.up * h * t;
                    var w = width * (1f - t * 0.85f);
                    vertices.Add(center - side * w);
                    vertices.Add(center + side * w);
                    uvs.Add(new Vector2(0f, t));
                    uvs.Add(new Vector2(0f, t));
                }
                for (var s = 0; s < segments; s++)
                {
                    var i = baseIndex + s * 2;
                    triangles.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
                }
                if (flowers)
                {
                    // Cabeza: dos quads cruzados en la punta.
                    var top = root + lean + Vector3.up * h;
                    for (var q = 0; q < 2; q++)
                    {
                        var axis = q == 0 ? side : Vector3.Cross(side, Vector3.up);
                        var start = vertices.Count;
                        const float size = 0.07f;
                        vertices.Add(top - axis * size + Vector3.up * -size * 0.4f);
                        vertices.Add(top + axis * size + Vector3.up * -size * 0.4f);
                        vertices.Add(top - axis * size + Vector3.up * size);
                        vertices.Add(top + axis * size + Vector3.up * size);
                        for (var k = 0; k < 4; k++) uvs.Add(new Vector2(1f, 1f));
                        triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 });
                    }
                }
            }
            var mesh = new Mesh { name = flowers ? "FlowerTuft" : "GrassTuft" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
