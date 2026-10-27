using System;
using BeforeItWasYou.Audio;
using BeforeItWasYou.Core.Settings;
using UnityEngine;

namespace BeforeItWasYou.Services
{
    /// <summary>Lleva las opciones guardadas al motor: pantalla, calidad, audio por buses y cámara.</summary>
    public static class SettingsApplier
    {
        public static event Action<GameSettings> Applied;

        public static void Apply(GameSettings settings)
        {
            if (!Application.isBatchMode && !Application.isEditor) ApplyDisplay(settings);
            var names = QualitySettings.names;
            var index = Array.IndexOf(names, settings.Quality.ToString());
            if (index >= 0 && index != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(index, true);
            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;
            Application.targetFrameRate = settings.VSync ? -1 : 144;
            AudioListener.volume = settings.MasterVolume;
            AudioBuses.Set(AudioBus.Music, settings.MusicVolume);
            AudioBuses.Set(AudioBus.Effects, settings.EffectsVolume);
            AudioBuses.Set(AudioBus.Ambience, settings.AmbienceVolume);
            AudioBuses.Set(AudioBus.Voice, 1f);
            Applied?.Invoke(settings);
        }

        private static void ApplyDisplay(GameSettings settings)
        {
            var mode = settings.Display switch
            {
                DisplayMode.Windowed => FullScreenMode.Windowed,
                DisplayMode.Borderless => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.ExclusiveFullScreen,
            };
            var width = settings.ResolutionWidth > 0 ? settings.ResolutionWidth : Screen.currentResolution.width;
            var height = settings.ResolutionHeight > 0 ? settings.ResolutionHeight : Screen.currentResolution.height;
            if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != mode)
            {
                Screen.SetResolution(width, height, mode);
            }
        }
    }
}
