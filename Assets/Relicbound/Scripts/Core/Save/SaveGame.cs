using System;
using System.Collections.Generic;
using Relicbound.Core.Economy;
using Relicbound.Core.Settings;

namespace Relicbound.Core.Save
{
    /// <summary>
    /// Estado persistente completo (docs/15 §Guardado). Solo datos planos: nunca referencias
    /// a objetos de escena. Los sistemas de fases posteriores añaden campos aquí y suben
    /// <see cref="CurrentSchemaVersion"/> junto con una migración.
    /// </summary>
    public sealed class SaveGame
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public ProfileData Profile;
        public ProgressData Progress = new ProgressData();
        public Dictionary<ResourceType, long> Resources = new Dictionary<ResourceType, long>();
        public GameSettings Settings = new GameSettings();
        public TutorialData Tutorial = new TutorialData();
        public DateTime LastSavedUtc;
    }

    public sealed class ProfileData
    {
        public string ProfileId;
        public string PlayerName;
        public string CastleName;
        public string BannerId;
        public DateTime CreatedUtc;
    }

    public sealed class ProgressData
    {
        public int Level = 1;
        public long XpIntoLevel;
    }

    public sealed class TutorialData
    {
        public bool IntroSeen;
        public List<string> CompletedSteps = new List<string>();
    }
}
