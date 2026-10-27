using System;
using UnityEngine;

namespace BeforeItWasYou.Player
{
    /// <summary>
    /// Ruidos del mundo que la IA puede oír: pasos, puertas, objetos. El radio ya incluye el volumen y la
    /// superficie; la IA aplica la oclusión por paredes.
    /// </summary>
    public static class NoiseEvents
    {
        public static event Action<Vector3, float, GameObject> Emitted;

        public static void Emit(Vector3 position, float radius, GameObject source) => Emitted?.Invoke(position, radius, source);
    }
}
