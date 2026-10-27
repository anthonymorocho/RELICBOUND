using System;
using NUnit.Framework;
using Relicbound.Core.Profile;

namespace Relicbound.Core.Tests
{
    public class ProfileTests
    {
        [TestCase("Íñigo", NameError.None)]
        [TestCase("  Ana   María  ", NameError.None)]
        [TestCase("O'Neil-2", NameError.None)]
        [TestCase("", NameError.Empty)]
        [TestCase("   ", NameError.Empty)]
        [TestCase("Al", NameError.TooShort)]
        [TestCase("NombreDemasiadoLargo", NameError.TooLong)]
        [TestCase("Rey<script>", NameError.InvalidCharacter)]
        [TestCase("Rey😀", NameError.InvalidCharacter)]
        public void PlayerNameRules(string input, NameError expected)
        {
            Assert.That(NameValidator.ValidatePlayerName(input), Is.EqualTo(expected));
        }

        [Test]
        public void Profile_StoresNormalizedNames()
        {
            var profile = PlayerProfile.CreateNew("  Ana   María ", "Bastión  del Norte", null, DateTime.UtcNow);
            Assert.That(profile.PlayerName, Is.EqualTo("Ana María"));
            Assert.That(profile.CastleName, Is.EqualTo("Bastión del Norte"));
            Assert.That(profile.BannerId, Is.EqualTo(PlayerProfile.DefaultBannerId));
        }

        [Test]
        public void Profile_RejectsInvalidName()
        {
            Assert.Throws<ArgumentException>(() => PlayerProfile.CreateNew("A", "Bastión", null, DateTime.UtcNow));
        }
    
        [Test]
        public void Profile_CanChangeCastleNameAndBanner_KeepingIdentity()
        {
            var profile = PlayerProfile.CreateNew("Patricia", "Bastión", "banner_azure", DateTime.UtcNow);
            var renamed = profile.WithCastleName("  Fortaleza  Gris ").WithBanner("banner_verdant");
            Assert.That(renamed.ProfileId, Is.EqualTo(profile.ProfileId));
            Assert.That(renamed.CastleName, Is.EqualTo("Fortaleza Gris"));
            Assert.That(renamed.BannerId, Is.EqualTo("banner_verdant"));
            Assert.Throws<ArgumentException>(() => profile.WithCastleName("X"));
        }
    }
}
