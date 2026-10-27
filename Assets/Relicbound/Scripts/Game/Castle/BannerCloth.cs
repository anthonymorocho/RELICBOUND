using Relicbound.Game.UI;
using UnityEngine;

namespace Relicbound.Game.Castle
{
    public enum BannerShape
    {
        /// <summary>Estandarte colgado de su borde superior (fachadas).</summary>
        Hanging = 0,
        /// <summary>Gallardete sujeto a un mástil (torres).</summary>
        Pennant = 1,
    }

    /// <summary>
    /// Paño de estandarte generado por código (malla subdividida para el ondeo del shader Relicbound/Banner)
    /// y vestido con la heráldica del jugador.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class BannerCloth : MonoBehaviour
    {
        private static readonly int FieldColor = Shader.PropertyToID("_FieldColor");
        private static readonly int MetalColor = Shader.PropertyToID("_MetalColor");
        private static readonly int Charge = Shader.PropertyToID("_Charge");
        private static readonly int Shape = Shader.PropertyToID("_Shape");
        private static readonly int Amplitude = Shader.PropertyToID("_WaveAmplitude");

        [SerializeField] private BannerShape shape = BannerShape.Hanging;
        [SerializeField] private float width = 1.6f;
        [SerializeField] private float height = 3.2f;
        [SerializeField] private float waveAmplitude = 0.12f;

        private MaterialPropertyBlock _block;

        public void Configure(BannerShape bannerShape, float clothWidth, float clothHeight, float amplitude)
        {
            shape = bannerShape;
            width = clothWidth;
            height = clothHeight;
            waveAmplitude = amplitude;
            Rebuild();
        }

        private void OnEnable()
        {
            Rebuild();
            Dress(Banners.Starting[0]);
        }

        public void Dress(BannerStyle banner)
        {
            _block ??= new MaterialPropertyBlock();
            var renderer = GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(_block);
            _block.SetColor(FieldColor, banner.Field);
            _block.SetColor(MetalColor, banner.Metal);
            // Canales de T_BannerMasks: 0 torre, 1 cheurón, 2 cruz.
            _block.SetFloat(Charge, banner.Charge switch { BannerCharge.Tower => 0f, BannerCharge.Chevron => 1f, _ => 2f });
            _block.SetFloat(Shape, (float)shape);
            _block.SetFloat(Amplitude, waveAmplitude);
            renderer.SetPropertyBlock(_block);
        }

        /// <summary>Viste todos los estandartes de la escena con la heráldica indicada.</summary>
        public static void DressAll(string bannerId)
        {
            var banner = Banners.Get(bannerId);
            foreach (var cloth in FindObjectsByType<BannerCloth>(FindObjectsSortMode.None)) cloth.Dress(banner);
        }

        private void Rebuild()
        {
            const int columns = 10;
            const int rows = 16;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uvs = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[columns * rows * 6];
            for (var y = 0; y <= rows; y++)
            {
                for (var x = 0; x <= columns; x++)
                {
                    var i = y * (columns + 1) + x;
                    var u = x / (float)columns;
                    var v = y / (float)rows;
                    // Colgado: origen en el centro del borde superior. Gallardete: origen en el mástil.
                    vertices[i] = shape == BannerShape.Hanging
                        ? new Vector3((u - 0.5f) * width, (v - 1f) * height, 0f)
                        : new Vector3(u * width, (v - 0.5f) * height, 0f);
                    uvs[i] = new Vector2(u, v);
                    normals[i] = Vector3.back;
                }
            }
            var t = 0;
            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    var i = y * (columns + 1) + x;
                    triangles[t++] = i;
                    triangles[t++] = i + columns + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + columns + 1;
                    triangles[t++] = i + columns + 2;
                }
            }
            var mesh = new Mesh { name = "BannerCloth", vertices = vertices, uv = uvs, normals = normals, triangles = triangles };
            mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one * 1.5f);
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
