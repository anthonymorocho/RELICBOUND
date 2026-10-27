using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Relicbound.Core.Boot;
using Relicbound.Core.Economy;
using Relicbound.Core.Profile;
using Relicbound.Core.Progression;
using Relicbound.Core.Save;

namespace Relicbound.Core.Tests
{
    public class SaveTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        private static SaveGame SampleSave(long gold = 1234)
        {
            var session = GameSession.StartNew(
                PlayerProfile.CreateNew("Patricia", "Bastión Real", null, Now),
                StartingRules.CreateDefault(),
                LevelCurve.CreateDefault());
            session.Wallet.Restore(new System.Collections.Generic.Dictionary<ResourceType, long> { { ResourceType.Gold, gold } });
            session.Progress.AddXp(150);
            session.Settings.MusicVolume = 0.25f;
            return session.ToSave();
        }

        [Test]
        public void NoFiles_ReportsNoSave()
        {
            Assert.That(new SaveService(new MemorySaveStorage()).Load().Status, Is.EqualTo(LoadStatus.NoSave));
        }

        [Test]
        public void RoundTrip_RestoresTheSession()
        {
            var service = new SaveService(new MemorySaveStorage());
            service.Save(SampleSave(), Now);

            var result = service.Load();
            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            var session = GameSession.FromSave(result.Save, StartingRules.CreateDefault(), LevelCurve.CreateDefault());
            Assert.That(session.Profile.PlayerName, Is.EqualTo("Patricia"));
            Assert.That(session.Profile.CastleName, Is.EqualTo("Bastión Real"));
            Assert.That(session.Wallet.Get(ResourceType.Gold), Is.EqualTo(1234));
            Assert.That(session.Progress.Level, Is.EqualTo(2));
            Assert.That(session.Progress.XpIntoLevel, Is.EqualTo(50));
            Assert.That(session.Settings.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(result.Save.LastSavedUtc, Is.EqualTo(Now));
        }

        [Test]
        public void TamperedMain_RecoversPreviousValidSave()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            service.Save(SampleSave(gold: 100), Now);
            service.Save(SampleSave(gold: 200), Now.AddMinutes(1));

            storage.Write(SaveService.MainKey, storage.Read(SaveService.MainKey).Replace("200", "999999"));

            var result = service.Load();
            Assert.That(result.Status, Is.EqualTo(LoadStatus.RecoveredFromBackup));
            Assert.That(result.Save.Resources[ResourceType.Gold], Is.EqualTo(100));
            Assert.That(storage.Read(SaveService.QuarantineKey), Is.Not.Null, "El archivo dañado se conserva para diagnóstico.");
            Assert.That(service.Load().Status, Is.EqualTo(LoadStatus.Loaded), "La copia recuperada pasa a ser la principal.");
        }

        [Test]
        public void TruncatedMainWithoutBackup_IsCorrupted()
        {
            var storage = new MemorySaveStorage();
            storage.Write(SaveService.MainKey, "{\"format\":\"relicbound-sa");
            Assert.That(new SaveService(storage).Load().Status, Is.EqualTo(LoadStatus.Corrupted));
        }

        [Test]
        public void CorruptedSave_DoesNotOverwriteBackupOnNextSave()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            service.Save(SampleSave(gold: 100), Now);
            service.Save(SampleSave(gold: 200), Now);
            var goodBackup = storage.Read(SaveService.BackupKey);

            storage.Write(SaveService.MainKey, "basura");
            service.Save(SampleSave(gold: 300), Now);

            Assert.That(storage.Read(SaveService.BackupKey), Is.EqualTo(goodBackup));
        }

        [Test]
        public void SaveFromNewerGame_IsNotTouched()
        {
            var storage = new MemorySaveStorage();
            new SaveService(storage, new SaveMigrator(3).Register(1, _ => { }).Register(2, _ => { })).Save(SampleSave(), Now);

            var result = new SaveService(storage).Load();
            Assert.That(result.Status, Is.EqualTo(LoadStatus.NewerVersion));
            Assert.That(storage.Read(SaveService.QuarantineKey), Is.Null);
        }

        [Test]
        public void Migrations_RunInOrder()
        {
            var storage = new MemorySaveStorage();
            new SaveService(storage).Save(SampleSave(gold: 10), Now);

            // Simula una v3 futura que duplica el oro (p. ej. un cambio de escala de economía).
            var migrator = new SaveMigrator(3)
                .Register(1, save => save["Castle"] = new JArray())
                .Register(2, save => save["Resources"]["Gold"] = save["Resources"].Value<long>("Gold") * 2);
            var result = new SaveService(storage, migrator).Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Save.SchemaVersion, Is.EqualTo(3));
            Assert.That(result.Save.Resources[ResourceType.Gold], Is.EqualTo(20));
        }

        [Test]
        public void VersionOneSave_MigratesToEditableCastle()
        {
            var storage = new MemorySaveStorage();
            new SaveService(storage, new SaveMigrator(1)).Save(SampleSave(), Now);
            var raw = JObject.Parse(storage.Read(SaveService.MainKey));
            Assert.That(raw.Value<int>("version"), Is.EqualTo(1));

            var result = new SaveService(storage).Load();
            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Save.SchemaVersion, Is.EqualTo(SaveGame.CurrentSchemaVersion));
            Assert.That(result.Save.Castle, Is.Empty, "Castillo vacío = castillo inicial del catálogo.");
        }

        [Test]
        public void OutOfRangeSettings_AreSanitizedOnLoad()
        {
            var storage = new MemorySaveStorage();
            var save = SampleSave();
            save.Settings.MasterVolume = 7f;
            new SaveService(storage).Save(save, Now);
            Assert.That(new SaveService(storage).Load().Save.Settings.MasterVolume, Is.EqualTo(1f));
        }

        [Test]
        public void FileStorage_WritesAndReplaces()
        {
            var directory = Path.Combine(Path.GetTempPath(), "relicbound-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new SaveService(new FileSaveStorage(directory));
                service.Save(SampleSave(gold: 1), Now);
                service.Save(SampleSave(gold: 2), Now);
                Assert.That(service.Load().Save.Resources[ResourceType.Gold], Is.EqualTo(2));
                Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
                Assert.That(JObject.Parse(File.ReadAllText(Path.Combine(directory, "save_backup.json")))["format"].ToString(), Is.EqualTo("relicbound-save"));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
