using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeforeItWasYou.Audio
{
    public enum AudioBus
    {
        Effects,
        Ambience,
        Music,
        Voice,
    }

    /// <summary>Volumen por categoría (Opciones → Audio). El volumen general va en AudioListener.</summary>
    public static class AudioBuses
    {
        private static readonly Dictionary<AudioBus, float> Volumes = new Dictionary<AudioBus, float>();

        public static event Action Changed;

        public static float Get(AudioBus bus) => Volumes.TryGetValue(bus, out var value) ? value : 1f;

        public static void Set(AudioBus bus, float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Volumes.TryGetValue(bus, out var current) && Mathf.Approximately(current, volume)) return;
            Volumes[bus] = volume;
            Changed?.Invoke();
        }
    }

    /// <summary>Asigna una fuente a un bus: su volumen final es el volumen base por el del bus.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BusVolume : MonoBehaviour
    {
        [SerializeField] private AudioBus bus = AudioBus.Effects;
        [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
        private AudioSource _source;

        public AudioBus Bus => bus;

        public float BaseVolume
        {
            get => baseVolume;
            set
            {
                baseVolume = Mathf.Clamp01(value);
                Refresh();
            }
        }

        public void Configure(AudioBus targetBus, float volume)
        {
            bus = targetBus;
            BaseVolume = volume;
        }

        private void Awake() => _source = GetComponent<AudioSource>();

        private void OnEnable()
        {
            AudioBuses.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => AudioBuses.Changed -= Refresh;

        private void Refresh()
        {
            if (_source == null) _source = GetComponent<AudioSource>();
            _source.volume = baseVolume * AudioBuses.Get(bus);
        }
    }
}
