using System;

namespace Relicbound.Core.CameraControl
{
    /// <summary>
    /// Límites de la cámara del castillo (docs/04 §Cámara). El pivote se mueve sobre el plano XZ
    /// y no puede salir del terreno más un margen; el zoom es la distancia al pivote.
    /// </summary>
    public sealed class CameraLimits
    {
        public CameraLimits(float halfExtentX, float halfExtentZ, float minDistance, float maxDistance)
        {
            if (halfExtentX <= 0 || halfExtentZ <= 0) throw new ArgumentOutOfRangeException(nameof(halfExtentX));
            if (minDistance <= 0 || maxDistance < minDistance) throw new ArgumentOutOfRangeException(nameof(minDistance));
            HalfExtentX = halfExtentX;
            HalfExtentZ = halfExtentZ;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
        }

        public float HalfExtentX { get; }

        public float HalfExtentZ { get; }

        public float MinDistance { get; }

        public float MaxDistance { get; }

        /// <summary>
        /// Al alejar, la vista abarca más terreno, así que el pivote debe quedar más cerca del centro
        /// para no enseñar el vacío fuera del mapa. Con zoom máximo el pivote queda a la mitad del límite.
        /// </summary>
        public void ClampPivot(ref float x, ref float z, float distance)
        {
            var zoomT = ZoomFraction(distance);
            var shrink = 1f - 0.5f * zoomT;
            var limitX = HalfExtentX * shrink;
            var limitZ = HalfExtentZ * shrink;
            x = Math.Max(-limitX, Math.Min(limitX, x));
            z = Math.Max(-limitZ, Math.Min(limitZ, z));
        }

        public float ClampDistance(float distance) => Math.Max(MinDistance, Math.Min(MaxDistance, distance));

        /// <summary>0 = lo más cerca posible, 1 = lo más lejos.</summary>
        public float ZoomFraction(float distance) => (ClampDistance(distance) - MinDistance) / Math.Max(0.0001f, MaxDistance - MinDistance);

        /// <summary>Límites para un terreno de N×N celdas (docs/05) con un margen visual en celdas.</summary>
        public static CameraLimits ForGrid(int cells, float cellSize, float marginCells, float minDistance, float maxDistance)
        {
            var half = (cells * 0.5f + marginCells) * cellSize;
            return new CameraLimits(half, half, minDistance, maxDistance);
        }
    }
}
