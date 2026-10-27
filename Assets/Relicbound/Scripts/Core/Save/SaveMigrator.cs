using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Relicbound.Core.Save
{
    /// <summary>
    /// Aplica migraciones en cadena sobre el JSON crudo: v1→v2→...→actual. Cada paso modifica
    /// el objeto y debe dejar "SchemaVersion" en la versión de destino.
    /// </summary>
    public sealed class SaveMigrator
    {
        private readonly Dictionary<int, Action<JObject>> _steps = new Dictionary<int, Action<JObject>>();

        public int TargetVersion { get; }

        public SaveMigrator(int targetVersion)
        {
            TargetVersion = targetVersion;
        }

        /// <summary>Registra la migración desde <paramref name="fromVersion"/> a fromVersion + 1.</summary>
        public SaveMigrator Register(int fromVersion, Action<JObject> step)
        {
            _steps[fromVersion] = step ?? throw new ArgumentNullException(nameof(step));
            return this;
        }

        public static SaveMigrator CreateDefault() => new SaveMigrator(SaveGame.CurrentSchemaVersion);

        /// <exception cref="SaveFormatException">Versión futura o sin ruta de migración.</exception>
        public void Migrate(JObject save)
        {
            var version = save.Value<int?>("SchemaVersion") ?? 0;
            if (version > TargetVersion)
            {
                throw new SaveFormatException("Guardado de una versión más nueva del juego (" + version + ").");
            }
            while (version < TargetVersion)
            {
                if (!_steps.TryGetValue(version, out var step))
                {
                    throw new SaveFormatException("No hay migración desde la versión " + version + ".");
                }
                step(save);
                version++;
                save["SchemaVersion"] = version;
            }
        }
    }

    public sealed class SaveFormatException : Exception
    {
        public SaveFormatException(string message) : base(message)
        {
        }
    }
}
