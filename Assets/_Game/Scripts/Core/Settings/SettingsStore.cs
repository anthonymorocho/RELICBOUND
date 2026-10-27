using System;
using BeforeItWasYou.Core.Save;
using Newtonsoft.Json;

namespace BeforeItWasYou.Core.Settings
{
    /// <summary>Persistencia de opciones en su propio archivo; un archivo dañado vuelve a los valores por defecto.</summary>
    public sealed class SettingsStore
    {
        public const string Key = "settings";
        private readonly ISaveStorage _storage;

        public SettingsStore(ISaveStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public GameSettings Load()
        {
            var raw = _storage.Read(Key);
            GameSettings settings = null;
            if (raw != null)
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<GameSettings>(raw);
                }
                catch (JsonException)
                {
                    settings = null;
                }
            }
            settings = settings ?? new GameSettings();
            settings.Sanitize();
            return settings;
        }

        public void Save(GameSettings settings)
        {
            settings.Sanitize();
            _storage.Write(Key, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}
