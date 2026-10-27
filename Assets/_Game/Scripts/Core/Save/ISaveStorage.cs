using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeforeItWasYou.Core.Save
{
    /// <summary>Almacenamiento de texto por clave. Las implementaciones escriben de forma atómica.</summary>
    public interface ISaveStorage
    {
        string Read(string key);

        void Write(string key, string content);

        void Delete(string key);
    }

    /// <summary>Disco: escribe en un temporal y lo sustituye (no deja archivos a medias si se corta el juego).</summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _directory;

        public FileSaveStorage(string directory)
        {
            _directory = directory;
            Directory.CreateDirectory(_directory);
        }

        public string Read(string key)
        {
            var path = PathFor(key);
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
        }

        public void Write(string key, string content)
        {
            var path = PathFor(key);
            var temp = path + ".tmp";
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }

        public void Delete(string key)
        {
            var path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
        }

        private string PathFor(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new System.ArgumentException("Clave no válida: " + key, nameof(key));
            }
            return Path.Combine(_directory, key + ".json");
        }
    }

    public sealed class MemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>();

        public string Read(string key) => _entries.TryGetValue(key, out var value) ? value : null;

        public void Write(string key, string content) => _entries[key] = content;

        public void Delete(string key) => _entries.Remove(key);
    }
}
