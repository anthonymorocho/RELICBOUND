using System;

namespace Relicbound.Core.Settings
{
    public enum QualityPreset
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>Ajustes del jugador. Los grupos de volumen coinciden con el AudioMixer (docs/14).</summary>
    public sealed class GameSettings
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.7f;
        public float AmbienceVolume = 0.8f;
        public float EffectsVolume = 1f;
        public float InterfaceVolume = 0.8f;
        public float VoiceVolume = 1f;
        public QualityPreset Quality = QualityPreset.Medium;
        public string Language = "es";
        public bool Haptics = true;

        /// <summary>Corrige valores fuera de rango que puedan venir de un guardado manipulado o antiguo.</summary>
        public void Sanitize()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume = Clamp01(MusicVolume);
            AmbienceVolume = Clamp01(AmbienceVolume);
            EffectsVolume = Clamp01(EffectsVolume);
            InterfaceVolume = Clamp01(InterfaceVolume);
            VoiceVolume = Clamp01(VoiceVolume);
            if (!Enum.IsDefined(typeof(QualityPreset), Quality)) Quality = QualityPreset.Medium;
            if (string.IsNullOrEmpty(Language)) Language = "es";
        }

        private static float Clamp01(float value) => float.IsNaN(value) ? 1f : Math.Max(0f, Math.Min(1f, value));
    }
}
