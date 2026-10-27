using BeforeItWasYou.Audio;
using UnityEngine;

namespace BeforeItWasYou.UI
{
    public enum UiSound
    {
        Move,
        Confirm,
        Back,
        Adjust,
    }

    /// <summary>Sonidos de interfaz suaves. Si faltan clips, la interfaz funciona en silencio.</summary>
    public sealed class UiSounds : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip move;
        [SerializeField] private AudioClip confirm;
        [SerializeField] private AudioClip back;

        private static UiSounds _instance;

        public void Configure(AudioSource audioSource, AudioClip moveClip, AudioClip confirmClip, AudioClip backClip)
        {
            source = audioSource;
            move = moveClip;
            confirm = confirmClip;
            back = backClip;
        }

        private void OnEnable() => _instance = this;

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
        }

        public static void Play(UiSound sound)
        {
            if (_instance == null || _instance.source == null) return;
            var clip = sound switch
            {
                UiSound.Confirm => _instance.confirm,
                UiSound.Back => _instance.back,
                _ => _instance.move,
            };
            if (clip == null) return;
            var volume = sound == UiSound.Adjust ? 0.5f : 1f;
            _instance.source.PlayOneShot(clip, volume * AudioBuses.Get(AudioBus.Effects));
        }
    }
}
