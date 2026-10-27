using System;
using System.Collections.Generic;

namespace Relicbound.Core.Castle
{
    /// <summary>Edificio colocado. Coordenadas en celdas de 1 m; (0,0) es la esquina suroeste del terreno.</summary>
    public sealed class PlacedBuilding
    {
        public string InstanceId;
        public string TypeId;
        public int Level = 1;
        public int X;
        public int Z;
        /// <summary>0, 90, 180 o 270 grados.</summary>
        public int Rotation;
        /// <summary>Última recolecta de producción (UTC).</summary>
        public DateTime LastCollectedUtc;

        public PlacedBuilding Clone() => (PlacedBuilding)MemberwiseClone();
    }

    public enum PlacementError
    {
        None,
        OutOfBounds,
        Overlap,
        BlocksPath,
        LimitReached,
        Locked,
        InvalidRotation,
        NotMovable,
    }

    /// <summary>
    /// Distribución del castillo sobre la cuadrícula (docs/05): límites, solapes, pasillo alrededor de edificios
    /// grandes, límites por nivel del torreón y conectividad (siempre debe existir un camino desde el borde
    /// del terreno hasta el torreón atravesando solo celdas libres o puertas).
    /// </summary>
    public sealed class CastleLayout
    {
        private readonly BuildingCatalog _catalog;
        private readonly List<PlacedBuilding> _buildings;

        public CastleLayout(BuildingCatalog catalog, List<PlacedBuilding> buildings)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _buildings = buildings ?? new List<PlacedBuilding>();
        }

        public IReadOnlyList<PlacedBuilding> Buildings => _buildings;

        public BuildingCatalog Catalog => _catalog;

        public PlacedBuilding Keep => _buildings.Find(b => b.TypeId == BuildingCatalog.KeepId);

        public int KeepLevel => Keep?.Level ?? 1;

        public int GridSize => _catalog.GridSize(KeepLevel);

        public int Count(string typeId) => _buildings.FindAll(b => b.TypeId == typeId).Count;

        public PlacedBuilding Find(string instanceId) => _buildings.Find(b => b.InstanceId == instanceId);

        public PlacementError CanPlace(string typeId, int x, int z, int rotation, int playerLevel, string ignoreInstance = null)
        {
            var type = _catalog.Get(typeId);
            if (rotation % 90 != 0 || rotation < 0 || rotation >= 360) return PlacementError.InvalidRotation;
            if (ignoreInstance == null)
            {
                if (playerLevel < type.UnlockPlayerLevel) return PlacementError.Locked;
                if (Count(typeId) >= type.MaxCount(KeepLevel)) return PlacementError.LimitReached;
            }
            var (w, d) = type.Footprint(rotation);
            if (!InsideGrid(x, z, w, d)) return PlacementError.OutOfBounds;

            foreach (var other in _buildings)
            {
                if (other.InstanceId == ignoreInstance) continue;
                var otherType = _catalog.Get(other.TypeId);
                var (ow, od) = otherType.Footprint(other.Rotation);
                // Pasillo de 1 celda entre edificios que no son fortificación (docs/05 regla 3).
                var gap = type.IsFortification || otherType.IsFortification ? 0 : 1;
                if (x < other.X + ow + gap && other.X < x + w + gap && z < other.Z + od + gap && other.Z < z + d + gap)
                {
                    return PlacementError.Overlap;
                }
            }

            var candidate = new PlacedBuilding { InstanceId = "__candidate", TypeId = typeId, X = x, Z = z, Rotation = rotation };
            if (!HasPathToKeep(candidate, ignoreInstance)) return PlacementError.BlocksPath;
            return PlacementError.None;
        }

        /// <summary>Límites del terreno en celdas: con torreón de nivel 1 de 0 a 40; al crecer se amplía alrededor.</summary>
        public bool InsideGrid(int x, int z, int w, int d)
        {
            var extra = (GridSize - 40) / 2;
            return x >= -extra && z >= -extra && x + w <= 40 + extra && z + d <= 40 + extra;
        }

        public PlacedBuilding Place(string typeId, int x, int z, int rotation, int playerLevel, DateTime nowUtc)
        {
            var error = CanPlace(typeId, x, z, rotation, playerLevel);
            if (error != PlacementError.None) throw new InvalidOperationException("Colocación no válida: " + error);
            var building = new PlacedBuilding
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                TypeId = typeId,
                Level = 1,
                X = x,
                Z = z,
                Rotation = rotation,
                LastCollectedUtc = nowUtc,
            };
            _buildings.Add(building);
            return building;
        }

        public PlacementError CanMove(string instanceId, int x, int z, int rotation)
        {
            var building = Find(instanceId) ?? throw new KeyNotFoundException(instanceId);
            if (!_catalog.Get(building.TypeId).Movable) return PlacementError.NotMovable;
            return CanPlace(building.TypeId, x, z, rotation, int.MaxValue, instanceId);
        }

        public void Move(string instanceId, int x, int z, int rotation)
        {
            var error = CanMove(instanceId, x, z, rotation);
            if (error != PlacementError.None) throw new InvalidOperationException("Movimiento no válido: " + error);
            var building = Find(instanceId);
            building.X = x;
            building.Z = z;
            building.Rotation = rotation;
        }

        /// <summary>
        /// Búsqueda en anchura desde todas las celdas del borde hasta cualquier celda adyacente al torreón.
        /// Murallas y edificios bloquean; las puertas son transitables.
        /// </summary>
        private bool HasPathToKeep(PlacedBuilding candidate, string ignoreInstance)
        {
            var keep = candidate.TypeId == BuildingCatalog.KeepId ? candidate : Keep;
            if (keep == null) return true;
            var extra = (GridSize - 40) / 2;
            var min = -extra;
            var size = GridSize;
            var blocked = new bool[size, size];

            void Mark(PlacedBuilding b, bool value)
            {
                var type = _catalog.Get(b.TypeId);
                if (type.Passable || b == keep) return;
                var (w, d) = type.Footprint(b.Rotation);
                for (var i = b.X; i < b.X + w; i++)
                {
                    for (var j = b.Z; j < b.Z + d; j++)
                    {
                        var gi = i - min;
                        var gj = j - min;
                        if (gi >= 0 && gj >= 0 && gi < size && gj < size) blocked[gi, gj] = value;
                    }
                }
            }

            foreach (var b in _buildings)
            {
                if (b.InstanceId == ignoreInstance) continue;
                Mark(b, true);
            }
            Mark(candidate, true);

            var keepType = _catalog.Get(keep.TypeId);
            var (kw, kd) = keepType.Footprint(keep.Rotation);
            bool AdjacentToKeep(int gi, int gj)
            {
                var x = gi + min;
                var z = gj + min;
                return x >= keep.X - 1 && x <= keep.X + kw && z >= keep.Z - 1 && z <= keep.Z + kd
                       && !(x >= keep.X && x < keep.X + kw && z >= keep.Z && z < keep.Z + kd);
            }

            var visited = new bool[size, size];
            var queue = new Queue<(int, int)>();
            for (var i = 0; i < size; i++)
            {
                foreach (var (gi, gj) in new[] { (i, 0), (i, size - 1), (0, i), (size - 1, i) })
                {
                    if (blocked[gi, gj] || visited[gi, gj]) continue;
                    visited[gi, gj] = true;
                    queue.Enqueue((gi, gj));
                }
            }
            while (queue.Count > 0)
            {
                var (ci, cj) = queue.Dequeue();
                if (AdjacentToKeep(ci, cj)) return true;
                foreach (var (ni, nj) in new[] { (ci + 1, cj), (ci - 1, cj), (ci, cj + 1), (ci, cj - 1) })
                {
                    if (ni < 0 || nj < 0 || ni >= size || nj >= size || visited[ni, nj] || blocked[ni, nj]) continue;
                    var x = ni + min;
                    var z = nj + min;
                    if (x >= keep.X && x < keep.X + kw && z >= keep.Z && z < keep.Z + kd) continue;
                    visited[ni, nj] = true;
                    queue.Enqueue((ni, nj));
                }
            }
            return false;
        }
    }
}
