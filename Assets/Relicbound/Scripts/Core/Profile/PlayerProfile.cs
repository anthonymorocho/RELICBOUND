using System;

namespace Relicbound.Core.Profile
{
    /// <summary>Identidad local del jugador. Se crea en la primera apertura (docs/01, paso 3-4).</summary>
    public sealed class PlayerProfile
    {
        public const string DefaultBannerId = "banner_default";

        public PlayerProfile(string profileId, string playerName, string castleName, string bannerId, DateTime createdUtc)
        {
            if (string.IsNullOrEmpty(profileId)) throw new ArgumentException("Falta el identificador.", nameof(profileId));
            ProfileId = profileId;
            PlayerName = Require(playerName, NameValidator.ValidatePlayerName(playerName), nameof(playerName));
            CastleName = Require(castleName, NameValidator.ValidateCastleName(castleName), nameof(castleName));
            BannerId = string.IsNullOrEmpty(bannerId) ? DefaultBannerId : bannerId;
            CreatedUtc = createdUtc;
        }

        public string ProfileId { get; }

        public string PlayerName { get; }

        public string CastleName { get; }

        public string BannerId { get; }

        public DateTime CreatedUtc { get; }

        public static PlayerProfile CreateNew(string playerName, string castleName, string bannerId, DateTime nowUtc) =>
            new PlayerProfile(Guid.NewGuid().ToString("N"), playerName, castleName, bannerId, nowUtc);

        private static string Require(string raw, NameError error, string parameter)
        {
            if (error != NameError.None) throw new ArgumentException("Nombre no válido: " + error, parameter);
            return NameValidator.Normalize(raw);
        }
    }
}
