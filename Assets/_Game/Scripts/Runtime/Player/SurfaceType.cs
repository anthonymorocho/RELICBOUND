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

    /// <summary>Superficie de un suelo: decide el sonido de los pasos y cuánto se oyen.</summary>
    public sealed class SurfaceType : MonoBehaviour
    {
        [SerializeField] private Surface surface = Surface.Tile;

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
