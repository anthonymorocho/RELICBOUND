using System.Collections.Generic;
using Relicbound.Core.Economy;

namespace Relicbound.Core.Boot
{
    /// <summary>
    /// Recursos y capacidad iniciales. En Unity se rellena desde un ScriptableObject de balance
    /// (docs/12 §Tablas); <see cref="CreateDefault"/> refleja los valores actuales del documento.
    /// </summary>
    public sealed class StartingRules
    {
        public long StartingCapacity = 5000;
        public Dictionary<ResourceType, long> StartingResources = new Dictionary<ResourceType, long>();
        public Dictionary<ResourceType, long> TutorialReward = new Dictionary<ResourceType, long>();

        public static StartingRules CreateDefault()
        {
            return new StartingRules
            {
                StartingCapacity = 5000,
                StartingResources = new Dictionary<ResourceType, long>
                {
                    { ResourceType.Gold, 1000 },
                    { ResourceType.Wood, 800 },
                    { ResourceType.Stone, 600 },
                    { ResourceType.Iron, 0 },
                    { ResourceType.Gems, 50 },
                },
                TutorialReward = new Dictionary<ResourceType, long>
                {
                    { ResourceType.Gold, 250 },
                    { ResourceType.Wood, 150 },
                },
            };
        }
    }
}
