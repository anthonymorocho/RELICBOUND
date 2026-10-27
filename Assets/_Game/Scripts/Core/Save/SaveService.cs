using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeforeItWasYou.Core.Save
{
    public enum LoadStatus
    {
        NoSave,
        Loaded,
        RecoveredFromBackup,
        Corrupted,
        NewerVersion,
    }

    public sealed class LoadResult
    {
        public LoadResult(LoadStatus status, Snapshot snapshot, string detail)
        {
            Status = status;
            Snapshot = snapshot;
            Detail = detail;
        }

        public LoadStatus Status { get; }

        public Snapshot Snapshot { get; }

        public string Detail { get; }

        public bool HasSnapshot => Snapshot != null;
    }

    /// <summary>
    /// Slot de partida con sobre de integridad (formato, versión y SHA-256), copia de respaldo del último
    /// checkpoint válido, cuarentena del archivo dañado y migraciones de esquema. Nunca se sobrescribe un
    /// guardado válido con uno corrupto.
    /// </summary>
    public sealed class SaveService
    {
        public const string Format = "biwy-save";
        public const string MainKey = "slot_main";
        public const string BackupKey = "slot_backup";
        public const string QuarantineKey = "slot_corrupt";

        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        private readonly ISaveStorage _storage;
        private readonly int _targetVersion;
        private readonly Dictionary<int, Action<JObject>> _migrations = new Dictionary<int, Action<JObject>>();

        public SaveService(ISaveStorage storage, int targetVersion = Snapshot.CurrentSchemaVersion)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _targetVersion = targetVersion;
        }

        /// <summary>Migración de la versión <paramref name="from"/> a la siguiente.</summary>
        public SaveService RegisterMigration(int from, Action<JObject> step)
        {
            _migrations[from] = step;
            return this;
        }

        public void Save(Snapshot snapshot, DateTime nowUtc)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            snapshot.SchemaVersion = _targetVersion;
            snapshot.SavedUtc = nowUtc;
            var previous = _storage.Read(MainKey);
            if (previous != null && TryOpen(previous, out _, out _) == null) _storage.Write(BackupKey, previous);
            var payload = JsonConvert.SerializeObject(snapshot, Json);
            var envelope = new JObject
            {
                ["format"] = Format,
                ["version"] = _targetVersion,
                ["checksum"] = Checksum(payload),
                ["payload"] = payload,
            };
            _storage.Write(MainKey, envelope.ToString(Formatting.Indented));
        }

        public LoadResult Load()
        {
            var main = _storage.Read(MainKey);
            var backup = _storage.Read(BackupKey);
            if (main == null && backup == null) return new LoadResult(LoadStatus.NoSave, null, null);
            if (main != null)
            {
                var error = TryOpen(main, out var snapshot, out var newer);
                if (error == null) return new LoadResult(LoadStatus.Loaded, snapshot, null);
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

        /// <summary>¿Hay una partida que se pueda continuar? (El menú solo muestra Continuar si es así.)</summary>
        public bool HasContinuableSave()
        {
            var result = Load();
            return result.Status == LoadStatus.Loaded || result.Status == LoadStatus.RecoveredFromBackup;
        }

        public void DeleteAll()
        {
            _storage.Delete(MainKey);
            _storage.Delete(BackupKey);
        }

        private string TryOpen(string raw, out Snapshot snapshot, out bool newer)
        {
            snapshot = null;
            newer = false;
            try
            {
                var envelope = JObject.Parse(raw);
                if (envelope.Value<string>("format") != Format) return "Formato desconocido.";
                var payload = envelope.Value<string>("payload");
                if (payload == null) return "Sin contenido.";
                if (envelope.Value<string>("checksum") != Checksum(payload)) return "Checksum incorrecto.";
                var data = JObject.Parse(payload);
                var version = data.Value<int?>("SchemaVersion") ?? 0;
                if (version > _targetVersion)
                {
                    newer = true;
                    return "Partida de una versión más nueva (" + version + ").";
                }
                while (version < _targetVersion)
                {
                    if (!_migrations.TryGetValue(version, out var step)) return "Sin migración desde la versión " + version + ".";
                    step(data);
                    version++;
                    data["SchemaVersion"] = version;
                }
                snapshot = data.ToObject<Snapshot>(JsonSerializer.Create(Json));
                return snapshot == null ? "Contenido vacío." : null;
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
