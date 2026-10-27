using BeforeItWasYou.Core.Save;
using BeforeItWasYou.Core.Settings;
using NUnit.Framework;

namespace BeforeItWasYou.Core.Tests
{
    public sealed class SettingsTests
    {
        [Test]
        public void ValoresFueraDeRango_SeCorrigen()
        {
            var settings = new GameSettings { MasterVolume = 3f, FieldOfView = 140f, MouseSensitivity = -1f, SubtitleScale = 9f };
            settings.Sanitize();
            Assert.AreEqual(1f, settings.MasterVolume);
            Assert.AreEqual(GameSettings.MaxFov, settings.FieldOfView);
            Assert.Greater(settings.MouseSensitivity, 0f);
            Assert.LessOrEqual(settings.SubtitleScale, 1.6f);
        }

        [Test]
        public void SeGuardanYSeRecuperan()
        {
            var storage = new MemorySaveStorage();
            var store = new SettingsStore(storage);
            var settings = store.Load();
            settings.InvertY = true;
            settings.Quality = QualityLevel.Ultra;
            settings.MusicVolume = 0.25f;
            store.Save(settings);

            var loaded = new SettingsStore(storage).Load();
            Assert.IsTrue(loaded.InvertY);
            Assert.AreEqual(QualityLevel.Ultra, loaded.Quality);
            Assert.AreEqual(0.25f, loaded.MusicVolume);
        }

        [Test]
        public void ArchivoDanado_VuelveALosValoresPorDefecto()
        {
            var storage = new MemorySaveStorage();
            storage.Write(SettingsStore.Key, "{{{");
            var loaded = new SettingsStore(storage).Load();
            Assert.AreEqual(new GameSettings().FieldOfView, loaded.FieldOfView);
        }
    }
}
