using System;
using System.Collections.Generic;
using Relicbound.Core.Economy;

namespace Relicbound.Core.Castle
{
    public enum BuildingCategory
    {
        Core,
        Fortification,
        Production,
        Storage,
        Military,
        Decoration,
    }

    /// <summary>Datos de un nivel de edificio (docs/06, docs/12). Valores de balance: hipótesis.</summary>
    public sealed class BuildingLevel
    {
        public int Level;
        public ResourceCost Cost = ResourceCost.Free;
        /// <summary>Nivel mínimo del torreón para construir/mejorar a este nivel (el torreón usa nivel de jugador).</summary>
        public int RequiredKeepLevel = 1;
        public int RequiredPlayerLevel = 1;
        public int Hitpoints;
        public long XpReward;
        public ResourceType? Produces;
        public long ProductionPerHour;
        public long ProductionCapacity;
        public long StorageBonus;
    }

    public sealed class BuildingType
    {
        public string Id;
        public string Name;
        public string Role;
        public string Description;
        /// <summary>Nombre base del modelo (SM_&lt;Model&gt;_L&lt;nivel&gt;).</summary>
        public string Model;
        public BuildingCategory Category;
        public int Width = 1;
        public int Depth = 1;
        /// <summary>Paso para las unidades (puertas): no bloquea el camino al torreón.</summary>
        public bool Passable;
        /// <summary>Muralla: bloquea el paso pero puede tocar otras piezas de fortificación sin pasillo.</summary>
        public bool IsFortification;
        public bool Movable = true;
        public int UnlockPlayerLevel = 1;
        /// <summary>Máximo de unidades por nivel de torreón (índice 0 = torreón nivel 1). Vacío = sin límite.</summary>
        public int[] MaxCountByKeepLevel = Array.Empty<int>();
        public List<BuildingLevel> Levels = new List<BuildingLevel>();

        public int MaxLevel => Levels.Count;

        public BuildingLevel GetLevel(int level)
        {
            if (level < 1 || level > Levels.Count) throw new ArgumentOutOfRangeException(nameof(level));
            return Levels[level - 1];
        }

        public int MaxCount(int keepLevel)
        {
            if (MaxCountByKeepLevel.Length == 0) return int.MaxValue;
            var index = Math.Min(Math.Max(keepLevel, 1), MaxCountByKeepLevel.Length) - 1;
            return MaxCountByKeepLevel[index];
        }

        /// <summary>Huella en celdas según la rotación (0, 90, 180, 270).</summary>
        public (int width, int depth) Footprint(int rotation) => rotation % 180 == 0 ? (Width, Depth) : (Depth, Width);
    }

    /// <summary>Catálogo de edificios. Se rellena desde datos (JSON en Unity) y es la única fuente de cifras.</summary>
    public sealed class BuildingCatalog
    {
        public const string KeepId = "keep";
        private readonly Dictionary<string, BuildingType> _types = new Dictionary<string, BuildingType>();

        /// <summary>Lado del terreno construible por nivel de torreón (docs/05). Índice 0 = nivel 1.</summary>
        public int[] GridSizeByKeepLevel = { 40 };

        public void Add(BuildingType type) => _types[type.Id] = type;

        public BuildingType Get(string id) =>
            _types.TryGetValue(id, out var type) ? type : throw new KeyNotFoundException("Edificio desconocido: " + id);

        public bool TryGet(string id, out BuildingType type) => _types.TryGetValue(id, out type);

        public IEnumerable<BuildingType> All => _types.Values;

        public int GridSize(int keepLevel)
        {
            var index = Math.Min(Math.Max(keepLevel, 1), GridSizeByKeepLevel.Length) - 1;
            return GridSizeByKeepLevel[index];
        }
    }
}
