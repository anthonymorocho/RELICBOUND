namespace Relicbound.Core.Save
{
    /// <summary>Almacenamiento de texto por clave. Las implementaciones deben escribir de forma atómica.</summary>
    public interface ISaveStorage
    {
        /// <summary>Devuelve el contenido o null si no existe.</summary>
        string Read(string key);

        void Write(string key, string content);

        void Delete(string key);
    }
}
