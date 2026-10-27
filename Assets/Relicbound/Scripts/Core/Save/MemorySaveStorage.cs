using System.Collections.Generic;

namespace Relicbound.Core.Save
{
    /// <summary>Almacenamiento en memoria para pruebas y para el modo editor sin disco.</summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>();

        public string Read(string key) => _entries.TryGetValue(key, out var value) ? value : null;

        public void Write(string key, string content) => _entries[key] = content;

        public void Delete(string key) => _entries.Remove(key);
    }
}
