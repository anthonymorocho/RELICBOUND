using System;
using System.IO;
using System.Text;

namespace Relicbound.Core.Save
{
    /// <summary>
    /// Guardado en disco. En Unity se construye con Application.persistentDataPath.
    /// Escribe en un temporal y lo sustituye para no dejar archivos a medias si se corta la app.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _directory;

        public FileSaveStorage(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Falta el directorio.", nameof(directory));
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
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
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
                throw new ArgumentException("Clave de guardado no válida: " + key, nameof(key));
            }
            return Path.Combine(_directory, key + ".json");
        }
    }
}
