using System;
using System.Collections.Generic;

namespace Relicbound.Core.Economy
{
    /// <summary>
    /// Recursos del jugador con límites de almacén. Toda modificación pasa por aquí para que
    /// el HUD y el guardado tengan una única fuente de verdad.
    /// </summary>
    public sealed class ResourceWallet
    {
        private readonly Dictionary<ResourceType, long> _amounts = new Dictionary<ResourceType, long>();
        private readonly Dictionary<ResourceType, long> _capacities = new Dictionary<ResourceType, long>();

        /// <summary>Se dispara con (tipo, cantidad nueva) cada vez que cambia un recurso.</summary>
        public event Action<ResourceType, long> Changed;

        public long Get(ResourceType type) => _amounts.TryGetValue(type, out var value) ? value : 0;

        /// <summary>Capacidad del almacén; <see cref="long.MaxValue"/> si el recurso no tiene límite.</summary>
        public long GetCapacity(ResourceType type)
        {
            if (!ResourceTypes.IsStorageCapped(type)) return long.MaxValue;
            return _capacities.TryGetValue(type, out var value) ? value : 0;
        }

        /// <summary>
        /// Cambia la capacidad. Bajarla no destruye recursos ya almacenados (docs/12: nunca
        /// se quita progreso al jugador); solo impide seguir acumulando.
        /// </summary>
        public void SetCapacity(ResourceType type, long capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (!ResourceTypes.IsStorageCapped(type)) return;
            _capacities[type] = capacity;
        }

        /// <summary>Añade hasta llenar el almacén. Devuelve lo realmente añadido.</summary>
        public long Add(ResourceType type, long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Usa TrySpend para restar.");
            var current = Get(type);
            var room = Math.Max(0, GetCapacity(type) - current);
            var added = Math.Min(amount, room);
            if (added == 0) return 0;
            Set(type, current + added);
            return added;
        }

        public bool CanAfford(ResourceCost cost)
        {
            foreach (var entry in cost.Entries)
            {
                if (Get(entry.Key) < entry.Value) return false;
            }
            return true;
        }

        /// <summary>Cobra el coste completo o nada.</summary>
        public bool TrySpend(ResourceCost cost)
        {
            if (!CanAfford(cost)) return false;
            foreach (var entry in cost.Entries)
            {
                Set(entry.Key, Get(entry.Key) - entry.Value);
            }
            return true;
        }

        /// <summary>Restaura cantidades desde guardado, sin recortar por capacidad.</summary>
        public void Restore(IDictionary<ResourceType, long> amounts)
        {
            foreach (var type in ResourceTypes.All)
            {
                amounts.TryGetValue(type, out var value);
                Set(type, Math.Max(0, value));
            }
        }

        public Dictionary<ResourceType, long> Snapshot()
        {
            var copy = new Dictionary<ResourceType, long>();
            foreach (var type in ResourceTypes.All) copy[type] = Get(type);
            return copy;
        }

        private void Set(ResourceType type, long value)
        {
            if (Get(type) == value && _amounts.ContainsKey(type)) return;
            _amounts[type] = value;
            Changed?.Invoke(type, value);
        }
    }
}
