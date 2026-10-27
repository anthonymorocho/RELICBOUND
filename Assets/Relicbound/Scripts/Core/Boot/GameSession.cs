using System;
using Relicbound.Core.Economy;
using Relicbound.Core.Profile;
using Relicbound.Core.Progression;
using Relicbound.Core.Save;
using Relicbound.Core.Settings;

namespace Relicbound.Core.Boot
{
    /// <summary>
    /// Estado vivo de la partida, construido desde el guardado. Es lo que consume el HUD del castillo
    /// (nombre, nivel, barra de XP, recursos) y lo que se vuelve a convertir en <see cref="SaveGame"/>.
    /// </summary>
    public sealed class GameSession
    {
        private GameSession(PlayerProfile profile, PlayerProgress progress, ResourceWallet wallet, GameSettings settings, TutorialData tutorial)
        {
            Profile = profile;
            Progress = progress;
            Wallet = wallet;
            Settings = settings;
            Tutorial = tutorial;
        }

        public PlayerProfile Profile { get; }

        public PlayerProgress Progress { get; }

        public ResourceWallet Wallet { get; }

        public GameSettings Settings { get; }

        public TutorialData Tutorial { get; }

        public static GameSession StartNew(PlayerProfile profile, StartingRules rules, LevelCurve curve)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var wallet = new ResourceWallet();
            foreach (var type in ResourceTypes.All) wallet.SetCapacity(type, rules.StartingCapacity);
            wallet.Restore(rules.StartingResources);
            return new GameSession(profile, new PlayerProgress(curve), wallet, new GameSettings(), new TutorialData());
        }

        public static GameSession FromSave(SaveGame save, StartingRules rules, LevelCurve curve)
        {
            if (save?.Profile == null) throw new ArgumentException("El guardado no tiene perfil.", nameof(save));
            var p = save.Profile;
            var profile = new PlayerProfile(p.ProfileId, p.PlayerName, p.CastleName, p.BannerId, p.CreatedUtc);
            var wallet = new ResourceWallet();
            // Hasta P3 la capacidad no depende de edificios; entonces la calculará el almacén.
            foreach (var type in ResourceTypes.All) wallet.SetCapacity(type, rules.StartingCapacity);
            wallet.Restore(save.Resources);
            var progress = new PlayerProgress(curve, save.Progress.Level, save.Progress.XpIntoLevel);
            return new GameSession(profile, progress, wallet, save.Settings, save.Tutorial);
        }

        public SaveGame ToSave()
        {
            return new SaveGame
            {
                Profile = new ProfileData
                {
                    ProfileId = Profile.ProfileId,
                    PlayerName = Profile.PlayerName,
                    CastleName = Profile.CastleName,
                    BannerId = Profile.BannerId,
                    CreatedUtc = Profile.CreatedUtc,
                },
                Progress = new ProgressData { Level = Progress.Level, XpIntoLevel = Progress.XpIntoLevel },
                Resources = Wallet.Snapshot(),
                Settings = Settings,
                Tutorial = Tutorial,
            };
        }
    }
}
