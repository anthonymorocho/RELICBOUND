using System;
using System.Collections.Generic;
using Relicbound.Core.Economy;
using Relicbound.Core.Progression;

namespace Relicbound.Core.Castle
{
    public enum UpgradeError
    {
        None,
        MaxLevel,
        KeepTooLow,
        PlayerTooLow,
        NotEnoughResources,
    }

    /// <summary>
    /// Reglas económicas del castillo (docs/06, docs/12): construir, mejorar, producir y almacenar.
    /// Las cifras salen del catálogo; aquí solo hay reglas.
    /// </summary>
    public sealed class CastleEconomy
    {
        /// <summary>Tope de acumulación de producción (docs/12: sin esperas abusivas, 8 h).</summary>
        public const double MaxAccumulationHours = 8.0;

        private readonly CastleLayout _layout;
        private readonly ResourceWallet _wallet;
        private readonly PlayerProgress _progress;
        private readonly long _baseCapacity;

        public CastleEconomy(CastleLayout layout, ResourceWallet wallet, PlayerProgress progress, long baseCapacity)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _baseCapacity = baseCapacity;
            RefreshCapacity();
        }

        public CastleLayout Layout => _layout;

        public ResourceCost BuildCost(string typeId) => _layout.Catalog.Get(typeId).GetLevel(1).Cost;

        public PlacementError CanBuild(string typeId, int x, int z, int rotation, out bool affordable)
        {
            affordable = _wallet.CanAfford(BuildCost(typeId));
            return _layout.CanPlace(typeId, x, z, rotation, _progress.Level);
        }

        /// <summary>Construye pagando el coste del nivel 1. Devuelve null si no se puede.</summary>
        public PlacedBuilding Build(string typeId, int x, int z, int rotation, DateTime nowUtc)
        {
            if (CanBuild(typeId, x, z, rotation, out var affordable) != PlacementError.None || !affordable) return null;
            var type = _layout.Catalog.Get(typeId);
            if (!_wallet.TrySpend(type.GetLevel(1).Cost)) return null;
            var building = _layout.Place(typeId, x, z, rotation, _progress.Level, nowUtc);
            _progress.AddXp(type.GetLevel(1).XpReward);
            RefreshCapacity();
            return building;
        }

        public UpgradeError CanUpgrade(PlacedBuilding building)
        {
            var type = _layout.Catalog.Get(building.TypeId);
            if (building.Level >= type.MaxLevel) return UpgradeError.MaxLevel;
            var next = type.GetLevel(building.Level + 1);
            if (_progress.Level < next.RequiredPlayerLevel) return UpgradeError.PlayerTooLow;
            if (building.TypeId != BuildingCatalog.KeepId && _layout.KeepLevel < next.RequiredKeepLevel) return UpgradeError.KeepTooLow;
            if (!_wallet.CanAfford(next.Cost)) return UpgradeError.NotEnoughResources;
            return UpgradeError.None;
        }

        public bool Upgrade(PlacedBuilding building, DateTime nowUtc)
        {
            if (CanUpgrade(building) != UpgradeError.None) return false;
            var type = _layout.Catalog.Get(building.TypeId);
            var next = type.GetLevel(building.Level + 1);
            // Lo producido antes de mejorar no se pierde.
            Collect(building, nowUtc);
            if (!_wallet.TrySpend(next.Cost)) return false;
            building.Level++;
            _progress.AddXp(next.XpReward);
            RefreshCapacity();
            return true;
        }

        /// <summary>Recursos listos para recoger (con tope por capacidad del edificio y 8 h).</summary>
        public long Pending(PlacedBuilding building, DateTime nowUtc)
        {
            var level = _layout.Catalog.Get(building.TypeId).GetLevel(building.Level);
            if (!level.Produces.HasValue || level.ProductionPerHour <= 0) return 0;
            var hours = Math.Min(MaxAccumulationHours, Math.Max(0.0, (nowUtc - building.LastCollectedUtc).TotalHours));
            var produced = (long)Math.Floor(hours * level.ProductionPerHour);
            return Math.Min(produced, level.ProductionCapacity > 0 ? level.ProductionCapacity : long.MaxValue);
        }

        /// <summary>Recoge lo producido hasta llenar el almacén. Lo que no cabe se queda en el edificio.</summary>
        public long Collect(PlacedBuilding building, DateTime nowUtc)
        {
            var level = _layout.Catalog.Get(building.TypeId).GetLevel(building.Level);
            var pending = Pending(building, nowUtc);
            if (pending <= 0 || !level.Produces.HasValue) return 0;
            var added = _wallet.Add(level.Produces.Value, pending);
            if (added >= pending)
            {
                building.LastCollectedUtc = nowUtc;
            }
            else if (level.ProductionPerHour > 0)
            {
                // Retrasa la marca solo lo cobrado: lo sobrante sigue esperando en el edificio.
                var remaining = pending - added;
                building.LastCollectedUtc = nowUtc - TimeSpan.FromHours((double)remaining / level.ProductionPerHour);
            }
            return added;
        }

        public Dictionary<ResourceType, long> CollectAll(DateTime nowUtc)
        {
            var result = new Dictionary<ResourceType, long>();
            foreach (var building in _layout.Buildings)
            {
                var level = _layout.Catalog.Get(building.TypeId).GetLevel(building.Level);
                if (!level.Produces.HasValue) continue;
                var added = Collect(building, nowUtc);
                if (added <= 0) continue;
                result.TryGetValue(level.Produces.Value, out var current);
                result[level.Produces.Value] = current + added;
            }
            return result;
        }

        /// <summary>Capacidad = base + bonificación de almacenes y torreón (las gemas no tienen límite).</summary>
        public void RefreshCapacity()
        {
            var bonus = 0L;
            foreach (var building in _layout.Buildings)
            {
                bonus += _layout.Catalog.Get(building.TypeId).GetLevel(building.Level).StorageBonus;
            }
            foreach (var type in ResourceTypes.All) _wallet.SetCapacity(type, _baseCapacity + bonus);
        }
    }
}
