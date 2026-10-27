using System;
using System.Collections.Generic;

namespace Relicbound.Core.Economy
{
    /// <summary>Coste inmutable compuesto por varios recursos.</summary>
    public sealed class ResourceCost
    {
        public static readonly ResourceCost Free = new ResourceCost(new Dictionary<ResourceType, long>());

        private readonly Dictionary<ResourceType, long> _amounts;

        public ResourceCost(IDictionary<ResourceType, long> amounts)
        {
            if (amounts == null) throw new ArgumentNullException(nameof(amounts));
            _amounts = new Dictionary<ResourceType, long>();
            foreach (var pair in amounts)
            {
                if (pair.Value < 0) throw new ArgumentOutOfRangeException(nameof(amounts), "Un coste no puede ser negativo.");
                if (pair.Value > 0) _amounts[pair.Key] = pair.Value;
            }
        }

        public static ResourceCost Of(ResourceType type, long amount) =>
            new ResourceCost(new Dictionary<ResourceType, long> { { type, amount } });

        public long Get(ResourceType type) => _amounts.TryGetValue(type, out var value) ? value : 0;

        public IEnumerable<KeyValuePair<ResourceType, long>> Entries => _amounts;

        public bool IsFree => _amounts.Count == 0;
    }
}
