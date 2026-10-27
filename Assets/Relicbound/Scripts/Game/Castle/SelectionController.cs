using System;
using UnityEngine;

namespace Relicbound.Game.Castle
{
    /// <summary>
    /// Selección de edificios (docs/04 §Selección): brillo dorado pulsante y anillo en el suelo.
    /// El panel de información lo gestiona la UI a través de <see cref="SelectionChanged"/>.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Highlight = new Color(0.79f, 0.65f, 0.30f);

        [SerializeField] private Material ringMaterial;
        [SerializeField] private float pulseSpeed = 3.2f;

        private MaterialPropertyBlock _block;
        private GameObject _ring;

        public BuildingView Selected { get; private set; }

        public event Action<BuildingView> SelectionChanged;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _ring = new GameObject("SelectionRing");
            _ring.transform.SetParent(transform, false);
            _ring.AddComponent<MeshFilter>().sharedMesh = BuildRing(64);
            var renderer = _ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ringMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.SetActive(false);
        }

        /// <summary>Selecciona lo que haya bajo el punto de pantalla; si no hay edificio, deselecciona.</summary>
        public BuildingView PickAt(Camera camera, Vector2 screen)
        {
            var ray = camera.ScreenPointToRay(screen);
            BuildingView view = null;
            if (Physics.Raycast(ray, out var hit, 500f)) view = hit.collider.GetComponentInParent<BuildingView>();
            Select(view);
            return view;
        }

        public void Select(BuildingView view)
        {
            if (view == Selected) return;
            SetHighlight(Selected, Color.black);
            Selected = view;
            if (view != null)
            {
                var bounds = view.WorldBounds;
                var radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.8f;
                _ring.transform.position = new Vector3(bounds.center.x, 0.06f, bounds.center.z);
                _ring.transform.localScale = new Vector3(radius, 1f, radius);
                _ring.SetActive(true);
            }
            else
            {
                _ring.SetActive(false);
            }
            SelectionChanged?.Invoke(view);
        }

        private void Update()
        {
            if (Selected == null) return;
            var pulse = 0.18f + 0.1f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            SetHighlight(Selected, Highlight * pulse);
        }

        private void SetHighlight(BuildingView view, Color emission)
        {
            if (view == null) return;
            foreach (var renderer in view.Renderers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, emission);
                renderer.SetPropertyBlock(_block);
            }
        }

        /// <summary>Anillo plano de radio 1 (exterior) con UV.x = 0 en el borde interior y 1 en el exterior.</summary>
        private static Mesh BuildRing(int segments)
        {
            const float inner = 0.86f;
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (var i = 0; i <= segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * inner;
                vertices[i * 2 + 1] = direction;
                uvs[i * 2] = new Vector2(0f, i / (float)segments);
                uvs[i * 2 + 1] = new Vector2(1f, i / (float)segments);
                if (i == segments) break;
                var t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 2;
                triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2 + 1;
                triangles[t + 4] = i * 2 + 2;
                triangles[t + 5] = i * 2 + 3;
            }
            var mesh = new Mesh { name = "SelectionRing", vertices = vertices, uv = uvs, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
