using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Relicbound.Core.Save
{
    public enum LoadStatus
    {
        /// <summary>No hay partida: primera apertura.</summary>
        NoSave,
        Loaded,
        /// <summary>El archivo principal estaba dañado y se recuperó la copia anterior.</summary>
        RecoveredFromBackup,
        /// <summary>Ambas copias dañadas. El dañado se conserva aparte para soporte.</summary>
        Corrupted,
        /// <summary>Partida creada por una versión más nueva del juego: no se sobrescribe.</summary>
        NewerVersion,
    }

    public sealed class LoadResult
    {
        public LoadResult(LoadStatus status, SaveGame save, string detail)
        {
            Status = status;
            Save = save;
            Detail = detail;
        }

        public LoadStatus Status { get; }

        public SaveGame Save { get; }

        public string Detail { get; }
    }

    /// <summary>
    /// Guardado con sobre de integridad (versión + SHA-256 del contenido), copia de respaldo de la
    /// última partida válida y migraciones. El checksum detecta corrupción, no trampas:
    /// la validación competitiva siempre es del servidor (docs/11).
    /// </summary>
    public sealed class SaveService
    {
        public const string MainKey = "save_main";
        public const string BackupKey = "save_backup";
        public const string QuarantineKey = "save_corrupt";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = { new StringEnumConverter() },
        };

        private readonly ISaveStorage _storage;
        private readonly SaveMigrator _migrator;

        public SaveService(ISaveStorage storage, SaveMigrator migrator = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _migrator = migrator ?? SaveMigrator.CreateDefault();
        }

        public void Save(SaveGame save, DateTime nowUtc)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            save.SchemaVersion = _migrator.TargetVersion;
            save.LastSavedUtc = nowUtc;

            var previous = _storage.Read(MainKey);
            if (previous != null && TryOpen(previous, out _, out _) == null)
            {
                _storage.Write(BackupKey, previous);
            }
            _storage.Write(MainKey, Seal(JsonConvert.SerializeObject(save, JsonSettings), save.SchemaVersion));
        }

        public LoadResult Load()
        {
            var main = _storage.Read(MainKey);
            var backup = _storage.Read(BackupKey);
            if (main == null && backup == null) return new LoadResult(LoadStatus.NoSave, null, null);

            if (main != null)
            {
                var error = TryOpen(main, out var save, out var newer);
                if (error == null) return new LoadResult(LoadStatus.Loaded, save, null);
                if (newer) return new LoadResult(LoadStatus.NewerVersion, null, error);

                _storage.Write(QuarantineKey, main);
                if (backup != null && TryOpen(backup, out var recovered, out _) == null)
                {
                    _storage.Write(MainKey, backup);
                    return new LoadResult(LoadStatus.RecoveredFromBackup, recovered, error);
                }
                return new LoadResult(LoadStatus.Corrupted, null, error);
            }

            var backupError = TryOpen(backup, out var fromBackup, out _);
            return backupError == null
                ? new LoadResult(LoadStatus.RecoveredFromBackup, fromBackup, "Faltaba el archivo principal.")
                : new LoadResult(LoadStatus.Corrupted, null, backupError);
        }

        public void DeleteAll()
        {
            _storage.Delete(MainKey);
            _storage.Delete(BackupKey);
        }

        private static string Seal(string payload, int version)
        {
            var envelope = new JObject
            {
                ["format"] = "relicbound-save",
                ["version"] = version,
                ["checksum"] = Checksum(payload),
                ["payload"] = payload,
            };
            return envelope.ToString(Formatting.Indented);
        }

        /// <summary>Devuelve null si todo va bien o el motivo del fallo.</summary>
        private string TryOpen(string raw, out SaveGame save, out bool newerVersion)
        {
            save = null;
            newerVersion = false;
            try
            {
                var envelope = JObject.Parse(raw);
                if (envelope.Value<string>("format") != "relicbound-save") return "Formato desconocido.";
                var payload = envelope.Value<string>("payload");
                if (payload == null) return "Sin contenido.";
                if (envelope.Value<string>("checksum") != Checksum(payload)) return "Checksum incorrecto.";

                var data = JObject.Parse(payload);
                try
                {
                    _migrator.Migrate(data);
                }
                catch (SaveFormatException exception)
                {
                    newerVersion = (data.Value<int?>("SchemaVersion") ?? 0) > _migrator.TargetVersion;
                    return exception.Message;
                }

                save = data.ToObject<SaveGame>(JsonSerializer.Create(JsonSettings));
                if (save == null) return "Contenido vacío.";
                save.Settings = save.Settings ?? new Settings.GameSettings();
                save.Settings.Sanitize();
                save.Progress = save.Progress ?? new ProgressData();
                save.Tutorial = save.Tutorial ?? new TutorialData();
                save.Resources = save.Resources ?? new System.Collections.Generic.Dictionary<Economy.ResourceType, long>();
                save.Castle = save.Castle ?? new System.Collections.Generic.List<Castle.PlacedBuilding>();
                return null;
            }
            catch (JsonException exception)
            {
                return "JSON no válido: " + exception.Message;
            }
        }

        private static string Checksum(string payload)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
