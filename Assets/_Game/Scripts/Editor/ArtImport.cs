using UnityEditor;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>Ajustes de importación del arte (modelos, texturas PBR, audio). Se amplía con cada lote de assets.</summary>
    public static class ArtImport
    {
        public static void Run()
        {
            AssetDatabase.Refresh();
        }
    }
}
