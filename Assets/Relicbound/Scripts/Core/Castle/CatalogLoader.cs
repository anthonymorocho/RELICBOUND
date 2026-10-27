using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Relicbound.Core.Economy;

namespace Relicbound.Core.Castle
{
    /// <summary>Lee el catálogo de edificios desde JSON (Assets/Relicbound/Data/catalog.json).</summary>
    public static class CatalogLoader
    {
        public static BuildingCatalog FromJson(string json)
        {
            var root = JObject.Parse(json);
            var catalog = new BuildingCatalog();
            if (root["gridSizeByKeepLevel"] is JArray grid) catalog.GridSizeByKeepLevel = grid.ToObject<int[]>();
            foreach (var entry in root["buildings"])
            {
                var type = new BuildingType
                {
                    Id = (string)entry["id"],
                    Name = (string)entry["name"],
                    Role = (string)entry["role"] ?? string.Empty,
                    Description = (string)entry["description"] ?? string.Empty,
                    Model = (string)entry["model"] ?? (string)entry["id"],
                    Category = (BuildingCategory)Enum.Parse(typeof(BuildingCategory), (string)entry["category"], true),
                    Width = (int)entry["width"],
                    Depth = (int)entry["depth"],
                    Passable = (bool?)entry["passable"] ?? false,
                    IsFortification = (bool?)entry["fortification"] ?? false,
                    Movable = (bool?)entry["movable"] ?? true,
                    UnlockPlayerLevel = (int?)entry["unlockPlayerLevel"] ?? 1,
                    MaxCountByKeepLevel = entry["maxCountByKeepLevel"]?.ToObject<int[]>() ?? Array.Empty<int>(),
                };
                var number = 1;
                foreach (var levelEntry in entry["levels"])
                {
                    var level = new BuildingLevel
                    {
                        Level = number++,
                        Cost = ParseCost(levelEntry["cost"] as JObject),
                        RequiredKeepLevel = (int?)levelEntry["keep"] ?? 1,
                        RequiredPlayerLevel = (int?)levelEntry["player"] ?? 1,
                        Hitpoints = (int?)levelEntry["hp"] ?? 0,
                        XpReward = (long?)levelEntry["xp"] ?? 0,
                        ProductionPerHour = (long?)levelEntry["perHour"] ?? 0,
                        ProductionCapacity = (long?)levelEntry["capacity"] ?? 0,
                        StorageBonus = (long?)levelEntry["storage"] ?? 0,
                    };
                    var produces = (string)entry["produces"];
                    if (!string.IsNullOrEmpty(produces)) level.Produces = (ResourceType)Enum.Parse(typeof(ResourceType), produces, true);
                    type.Levels.Add(level);
                }
                catalog.Add(type);
            }
            return catalog;
        }

        public static List<PlacedBuilding> DefaultLayout(string json, DateTime nowUtc)
        {
            var root = JObject.Parse(json);
            var result = new List<PlacedBuilding>();
            var index = 0;
            foreach (var entry in root["startingLayout"])
            {
                result.Add(new PlacedBuilding
                {
                    InstanceId = "start_" + index++,
                    TypeId = (string)entry["type"],
                    Level = 1,
                    X = (int)entry["x"],
                    Z = (int)entry["z"],
                    Rotation = (int?)entry["rot"] ?? 0,
                    LastCollectedUtc = nowUtc,
                });
            }
            return result;
        }

        private static ResourceCost ParseCost(JObject cost)
        {
            if (cost == null) return ResourceCost.Free;
            var amounts = new Dictionary<ResourceType, long>();
            foreach (var pair in cost)
            {
                amounts[(ResourceType)Enum.Parse(typeof(ResourceType), pair.Key, true)] = (long)pair.Value;
            }
            return new ResourceCost(amounts);
        }
    }
}
