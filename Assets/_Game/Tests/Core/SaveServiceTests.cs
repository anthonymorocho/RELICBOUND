using System;
using System.IO;
using BeforeItWasYou.Core.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BeforeItWasYou.Core.Tests
{
    public sealed class SaveServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 27, 21, 0, 0, DateTimeKind.Utc);

        private static Snapshot SampleSnapshot()
        {
            var snapshot = new Snapshot
            {
                CheckpointId = "CP02",
                PlayerPosition = new Vector3Data(4.2f, 0f, 12.5f),
                PlayerYaw = 135f,
                Objective = "Buscar la carpeta azul",
                Branch = "prudente",
                EpisodeSeconds = 412.5f,
                RandomSeed = 77,
            };
            snapshot.Items.Add("carpeta_azul");
            snapshot.Doors["puerta_lavadero"] = "Ajar";
            snapshot.Drawers["comoda_elena_2"] = true;
            snapshot.MessagesRead.Add("lucia_01");
            snapshot.Flags["vio_colchon"] = true;
            snapshot.Intruder.State = "Shed";
            snapshot.Intruder.HasBag = false;
            return snapshot;
        }

        [Test]
        public void SinGuardado_NoHayContinuar()
        {
            var service = new SaveService(new MemorySaveStorage());
            Assert.AreEqual(LoadStatus.NoSave, service.Load().Status);
            Assert.IsFalse(service.HasContinuableSave());
        }

        [Test]
        public void GuardarYCargar_ConservaTodoElEstadoDelMundo()
        {
            var service = new SaveService(new MemorySaveStorage());
            service.Save(SampleSnapshot(), Now);

            var result = service.Load();
            Assert.AreEqual(LoadStatus.Loaded, result.Status);
            var loaded = result.Snapshot;
            Assert.AreEqual("CP02", loaded.CheckpointId);
            Assert.AreEqual(12.5f, loaded.PlayerPosition.Z);
            Assert.AreEqual(135f, loaded.PlayerYaw);
            Assert.AreEqual("Buscar la carpeta azul", loaded.Objective);
            CollectionAssert.AreEqual(new[] { "carpeta_azul" }, loaded.Items);
            Assert.AreEqual("Ajar", loaded.Doors["puerta_lavadero"]);
            Assert.IsTrue(loaded.Drawers["comoda_elena_2"]);
            Assert.IsTrue(loaded.Flags["vio_colchon"]);
            Assert.AreEqual("Shed", loaded.Intruder.State);
            Assert.AreEqual(77, loaded.RandomSeed);
            Assert.AreEqual(Now, loaded.SavedUtc);
            Assert.IsTrue(service.HasContinuableSave());
        }

        [Test]
        public void ArchivoAlterado_SeRecuperaDelRespaldo()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            var first = SampleSnapshot();
            first.CheckpointId = "CP01";
            service.Save(first, Now);
            service.Save(SampleSnapshot(), Now.AddMinutes(3));

            var tampered = JObject.Parse(storage.Read(SaveService.MainKey));
            tampered["payload"] = tampered.Value<string>("payload").Replace("CP02", "CP05");
            storage.Write(SaveService.MainKey, tampered.ToString());

            var result = service.Load();
            Assert.AreEqual(LoadStatus.RecoveredFromBackup, result.Status);
            Assert.AreEqual("CP01", result.Snapshot.CheckpointId);
            Assert.IsNotNull(storage.Read(SaveService.QuarantineKey), "El archivo dañado debe quedar en cuarentena.");
        }

        [Test]
        public void ArchivoIlegibleSinRespaldo_EsCorrupto()
        {
            var storage = new MemorySaveStorage();
            storage.Write(SaveService.MainKey, "{ esto no es json");
            var service = new SaveService(storage);
            Assert.AreEqual(LoadStatus.Corrupted, service.Load().Status);
            Assert.IsFalse(service.HasContinuableSave());
        }

        [Test]
        public void UnGuardadoCorruptoNoPisaElRespaldoValido()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            service.Save(SampleSnapshot(), Now);
            var valid = storage.Read(SaveService.MainKey);
            service.Save(SampleSnapshot(), Now.AddMinutes(1));
            storage.Write(SaveService.MainKey, "basura");
            service.Save(SampleSnapshot(), Now.AddMinutes(2));
            Assert.AreEqual(valid, storage.Read(SaveService.BackupKey));
        }

        [Test]
        public void VersionMasNueva_NoSeCargaNiSeBorra()
        {
            var storage = new MemorySaveStorage();
            new SaveService(storage, 2).Save(SampleSnapshot(), Now);
            var result = new SaveService(storage).Load();
            Assert.AreEqual(LoadStatus.NewerVersion, result.Status);
            Assert.IsNotNull(storage.Read(SaveService.MainKey));
        }

        [Test]
        public void MigracionDeEsquema_SeAplicaAlCargar()
        {
            var storage = new MemorySaveStorage();
            new SaveService(storage, 1).Save(SampleSnapshot(), Now);
            var service = new SaveService(storage, 2).RegisterMigration(1, data => data["Objective"] = "Migrado");
            var result = service.Load();
            Assert.AreEqual(LoadStatus.Loaded, result.Status);
            Assert.AreEqual("Migrado", result.Snapshot.Objective);
            Assert.AreEqual(2, result.Snapshot.SchemaVersion);
        }

        [Test]
        public void BorrarTodo_QuitaContinuar()
        {
            var service = new SaveService(new MemorySaveStorage());
            service.Save(SampleSnapshot(), Now);
            service.DeleteAll();
            Assert.IsFalse(service.HasContinuableSave());
        }

        [Test]
        public void AlmacenEnDisco_EscribeDeFormaAtomicaYRechazaClavesRaras()
        {
            var directory = Path.Combine(Path.GetTempPath(), "biwy-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new FileSaveStorage(directory);
                var service = new SaveService(storage);
                service.Save(SampleSnapshot(), Now);
                service.Save(SampleSnapshot(), Now.AddSeconds(5));
                Assert.AreEqual(LoadStatus.Loaded, service.Load().Status);
                Assert.IsTrue(File.Exists(Path.Combine(directory, SaveService.BackupKey + ".json")));
                Assert.IsEmpty(Directory.GetFiles(directory, "*.tmp"));
                Assert.Throws<ArgumentException>(() => storage.Write("../fuera", "x"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
