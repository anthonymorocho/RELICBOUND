using System.Text;

namespace Relicbound.Core.Profile
{
    public enum NameError
    {
        None,
        Empty,
        TooShort,
        TooLong,
        InvalidCharacter,
    }

    /// <summary>
    /// Reglas de nombre de jugador y de castillo (docs/04, pantalla de perfil). Admite letras
    /// Unicode (tildes, ñ), dígitos, espacio simple, guion y apóstrofo.
    /// </summary>
    public static class NameValidator
    {
        public const int PlayerMin = 3;
        public const int PlayerMax = 16;
        public const int CastleMin = 3;
        public const int CastleMax = 24;

        /// <summary>Recorta extremos y colapsa espacios repetidos.</summary>
        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var builder = new StringBuilder(raw.Length);
            var previousSpace = false;
            foreach (var c in raw.Trim())
            {
                var isSpace = char.IsWhiteSpace(c);
                if (isSpace && previousSpace) continue;
                builder.Append(isSpace ? ' ' : c);
                previousSpace = isSpace;
            }
            return builder.ToString();
        }

        public static NameError ValidatePlayerName(string raw) => Validate(raw, PlayerMin, PlayerMax);

        public static NameError ValidateCastleName(string raw) => Validate(raw, CastleMin, CastleMax);

        private static NameError Validate(string raw, int min, int max)
        {
            var name = Normalize(raw);
            if (name.Length == 0) return NameError.Empty;
            if (name.Length < min) return NameError.TooShort;
            if (name.Length > max) return NameError.TooLong;
            foreach (var c in name)
            {
                if (!(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '\'')) return NameError.InvalidCharacter;
            }
            return NameError.None;
        }
    }
}
