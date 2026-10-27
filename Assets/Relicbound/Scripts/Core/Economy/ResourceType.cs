namespace Relicbound.Core.Economy
{
    /// <summary>
    /// Recursos del juego. Los valores numéricos se persisten: no reordenar ni reutilizar.
    /// </summary>
    public enum ResourceType
    {
        Gold = 0,
        Wood = 1,
        Stone = 2,
        Iron = 3,
        /// <summary>Moneda premium. Solo cosméticos (docs/12, docs/13).</summary>
        Gems = 4,
    }

    public static class ResourceTypes
    {
        public static readonly ResourceType[] All =
        {
            ResourceType.Gold, ResourceType.Wood, ResourceType.Stone, ResourceType.Iron, ResourceType.Gems,
        };

        /// <summary>Las gemas no tienen límite de almacén; los recursos de juego sí.</summary>
        public static bool IsStorageCapped(ResourceType type) => type != ResourceType.Gems;
    }
}
