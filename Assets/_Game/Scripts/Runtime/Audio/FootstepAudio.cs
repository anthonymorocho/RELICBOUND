using BeforeItWasYou.Player;
using UnityEngine;

namespace BeforeItWasYou.Audio
{
    /// <summary>Pasos de Daniel: un clip al azar de la superficie pisada, sin repetir el anterior, con leve variación.</summary>
    public sealed class FootstepAudio : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Bank
        {
            public Surface surface;
            public AudioClip[] clips = new AudioClip[0];
        }

        [SerializeField] private PlayerController player;
        [SerializeField] private AudioSource source;
        [SerializeField] private Bank[] banks = new Bank[0];

        private int _last = -1;

        public void Configure(PlayerController controller, AudioSource audioSource, Bank[] surfaceBanks)
        {
            player = controller;
            source = audioSource;
            banks = surfaceBanks;
        }

        private void OnEnable() => player.Footstep += Play;

        private void OnDisable() => player.Footstep -= Play;

        private void Play(Surface surface, float loudness)
        {
            Bank bank = null;
            foreach (var b in banks)
            {
                if (b.surface == surface) bank = b;
            }
            if (bank == null && banks.Length > 0) bank = banks[0];
            if (bank == null || bank.clips.Length == 0) return;
            var index = Random.Range(0, bank.clips.Length);
            if (index == _last && bank.clips.Length > 1) index = (index + 1) % bank.clips.Length;
            _last = index;
            source.pitch = Random.Range(0.93f, 1.06f);
            source.PlayOneShot(bank.clips[index], Mathf.Clamp(0.35f + loudness * 0.45f, 0.15f, 1f) * AudioBuses.Get(AudioBus.Effects));
        }
    }
}
