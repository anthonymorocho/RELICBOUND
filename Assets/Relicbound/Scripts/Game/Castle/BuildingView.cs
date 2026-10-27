using UnityEngine;

namespace Relicbound.Game.Castle
{
    /// <summary>Instancia de un edificio en el mundo. El estado persistente vive en el guardado, no aquí.</summary>
    public sealed class BuildingView : MonoBehaviour
    {
        [SerializeField] private BuildingDefinition definition;
        [SerializeField] private int level = 1;

        private Renderer[] _renderers;

        public BuildingDefinition Definition => definition;

        public int Level => level;

        public Renderer[] Renderers => _renderers ??= GetComponentsInChildren<Renderer>();

        public void Configure(BuildingDefinition buildingDefinition, int buildingLevel)
        {
            definition = buildingDefinition;
            level = buildingLevel;
        }

        public Bounds WorldBounds
        {
            get
            {
                var renderers = Renderers;
                if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one);
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                return bounds;
            }
        }
    }
}
