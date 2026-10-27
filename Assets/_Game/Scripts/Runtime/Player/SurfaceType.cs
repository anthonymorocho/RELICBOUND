using UnityEngine;

namespace BeforeItWasYou.Player
{
    public enum Surface
    {
        Tile,
        Wood,
        Carpet,
        Concrete,
        Grass,
        Gravel,
        Metal,
    }

    /// <summary>
    /// Superficie de un suelo: decide el sonido de los pasos y cuánto se oyen. Con byMaterial, la superficie se
    /// deduce del material del triángulo pisado (una sola malla con césped, hormigón y mantillo).
    /// </summary>
    public sealed class SurfaceType : MonoBehaviour
    {
        [SerializeField] private Surface surface = Surface.Tile;
        [SerializeField] private bool byMaterial;

        public bool ByMaterial
        {
            get => byMaterial;
            set => byMaterial = value;
        }

        public static Surface Resolve(RaycastHit hit)
        {
            var type = hit.collider.GetComponentInParent<SurfaceType>();
            if (type == null) return Surface.Concrete;
            if (!type.byMaterial || !(hit.collider is MeshCollider mc) || mc.sharedMesh == null || hit.triangleIndex < 0) return type.surface;
            var mesh = mc.sharedMesh;
            var renderer = hit.collider.GetComponent<Renderer>();
            var triangle = hit.triangleIndex * 3;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var desc = mesh.GetSubMesh(sub);
                if (triangle < desc.indexStart || triangle >= desc.indexStart + desc.indexCount) continue;
                var name = renderer != null && sub < renderer.sharedMaterials.Length && renderer.sharedMaterials[sub] != null ? renderer.sharedMaterials[sub].name : string.Empty;
                if (name.Contains("Lawn") || name.Contains("Mulch")) return Surface.Grass;
                if (name.Contains("Gravel")) return Surface.Gravel;
                return Surface.Concrete;
            }
            return type.surface;
        }

        public Surface Value
        {
            get => surface;
            set => surface = value;
        }

        public static float Loudness(Surface surface) => surface switch
        {
            Surface.Carpet => 0.45f,
            Surface.Grass => 0.55f,
            Surface.Wood => 1.1f,
            Surface.Gravel => 1.25f,
            Surface.Metal => 1.3f,
            _ => 1f,
        };
    }
}
