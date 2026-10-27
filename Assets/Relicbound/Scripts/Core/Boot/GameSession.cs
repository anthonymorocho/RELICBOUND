using System;
using System.Collections.Generic;
using Relicbound.Core.Castle;
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

        public PlayerProfile Profile { get; private set; }

        public event Action<PlayerProfile> ProfileChanged;

        public void UpdateProfile(PlayerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.ProfileId != Profile.ProfileId) throw new ArgumentException("No se puede cambiar de perfil en la misma sesión.");
            Profile = profile;
            ProfileChanged?.Invoke(profile);
        }

        public PlayerProgress Progress { get; }

        public ResourceWallet Wallet { get; }

        public GameSettings Settings { get; }

        public TutorialData Tutorial { get; }

        public CastleLayout Castle { get; private set; }

        public CastleEconomy Economy { get; private set; }

        private List<PlacedBuilding> _savedCastle = new List<PlacedBuilding>();

        /// <summary>
        /// Monta el castillo editable (P3) con el catálogo. Si la partida no tiene castillo, usa el inicial.
        /// </summary>
        public void AttachCastle(BuildingCatalog catalog, List<PlacedBuilding> defaultLayout, long baseCapacity)
        {
            var buildings = _savedCastle != null && _savedCastle.Count > 0 ? _savedCastle : defaultLayout;
            Castle = new CastleLayout(catalog, buildings);
            Economy = new CastleEconomy(Castle, Wallet, Progress, baseCapacity);
        }

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
            return new GameSession(profile, progress, wallet, save.Settings, save.Tutorial)
            {
                _savedCastle = save.Castle ?? new List<PlacedBuilding>(),
            };
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
                Castle = Castle != null ? new List<PlacedBuilding>(Castle.Buildings) : _savedCastle,
            };
        }
    }
}
