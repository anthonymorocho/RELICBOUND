using System.IO;
using BeforeItWasYou.Core.Flow;
using BeforeItWasYou.Core.Save;
using BeforeItWasYou.Core.Settings;
using UnityEngine;

namespace BeforeItWasYou.Services
{
    /// <summary>
    /// Servicios globales del juego. Se inicializan antes de cargar cualquier escena, así que funcionan igual
    /// arrancando desde L_Startup que abriendo L_E01_Arrival directamente en el editor.
    /// </summary>
    public static class GameServices
    {
        public static GameFlow Flow { get; private set; }

        public static SaveService Saves { get; private set; }

        public static SettingsStore SettingsStore { get; private set; }

        public static GameSettings Settings { get; private set; }

        public static bool IsReady => Flow != null;

        /// <summary>Carpeta de datos. Los tests la redirigen para no tocar las partidas del jugador.</summary>
        public static string DataFolder { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Flow = null;
            Saves = null;
            SettingsStore = null;
            Settings = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (!IsReady) Initialize(Path.Combine(Application.persistentDataPath, "Saves"));
        }

        public static void Initialize(string dataFolder)
        {
            if (IsReady && DataFolder == dataFolder) return;
            DataFolder = dataFolder;
            var storage = new FileSaveStorage(dataFolder);
            Flow = new GameFlow();
            Saves = new SaveService(storage);
            SettingsStore = new SettingsStore(storage);
            Settings = SettingsStore.Load();
            SettingsApplier.Apply(Settings);
        }

        public static void SaveSettings(GameSettings settings)
        {
            Settings = settings.Clone();
            SettingsStore.Save(Settings);
            SettingsApplier.Apply(Settings);
        }
    }
}
