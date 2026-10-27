using System;

namespace BeforeItWasYou.Core.Settings
{
    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3,
    }

    public enum DisplayMode
    {
        Fullscreen = 0,
        Borderless = 1,
        Windowed = 2,
    }

    /// <summary>
    /// Opciones del jugador (SISTEMAS_Y_CHECKPOINTS §Pausa y opciones, GDD §3 accesibilidad).
    /// Se guardan aparte de la partida: cambiar opciones nunca altera un checkpoint.
    /// </summary>
    public sealed class GameSettings
    {
        public int SchemaVersion = 1;

        // Gráficos
        public int ResolutionWidth;
        public int ResolutionHeight;
        public DisplayMode Display = DisplayMode.Fullscreen;
        public QualityLevel Quality = QualityLevel.High;
        public bool VSync = true;

        // Audio (0..1)
        public float MasterVolume = 0.9f;
        public float MusicVolume = 0.7f;
        public float EffectsVolume = 1f;
        public float AmbienceVolume = 0.85f;

        // Controles
        public float MouseSensitivity = 1f;
        public bool InvertY;

        // Accesibilidad
        public float FieldOfView = 64f;
        public float CameraShake = 1f;
        public bool MotionBlur;
        public float SubtitleScale = 1f;
        public bool ReduceFlashes;

        public const float MinFov = 55f;
        public const float MaxFov = 85f;

        public void Sanitize()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume = Clamp01(MusicVolume);
            EffectsVolume = Clamp01(EffectsVolume);
            AmbienceVolume = Clamp01(AmbienceVolume);
            MouseSensitivity = Clamp(MouseSensitivity, 0.2f, 3f, 1f);
            FieldOfView = Clamp(FieldOfView, MinFov, MaxFov, 64f);
            CameraShake = Clamp01(CameraShake);
            SubtitleScale = Clamp(SubtitleScale, 0.8f, 1.6f, 1f);
            if (!Enum.IsDefined(typeof(QualityLevel), Quality)) Quality = QualityLevel.High;
            if (!Enum.IsDefined(typeof(DisplayMode), Display)) Display = DisplayMode.Fullscreen;
            if (ResolutionWidth < 0 || ResolutionHeight < 0) ResolutionWidth = ResolutionHeight = 0;
        }

        public GameSettings Clone() => (GameSettings)MemberwiseClone();

        private static float Clamp01(float value) => Clamp(value, 0f, 1f, 1f);

        private static float Clamp(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }
}
