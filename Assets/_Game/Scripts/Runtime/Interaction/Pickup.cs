using BeforeItWasYou.Episode;
using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    /// <summary>Objeto que Daniel se lleva (carpeta, cargador, bolsa). Desaparece de la escena y queda en el estado.</summary>
    public sealed class Pickup : Interactable
    {
        [SerializeField] private string item;
        [SerializeField] private string prompt = "Coger";
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clip;

        public string Item => item;

        public override string Prompt => prompt;

        public void Configure(string itemId, string label, AudioSource source, AudioClip sound)
        {
            item = itemId;
            prompt = label;
            audioSource = source;
            clip = sound;
        }

        public override void Interact(Interactor interactor)
        {
            var state = EpisodeState.Current;
            if (state == null) return;
            if (audioSource != null && clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, audioSource.volume);
            state.AddItem(item);
            gameObject.SetActive(false);
        }

        public void SyncWithState(EpisodeState state) => gameObject.SetActive(!state.Has(item));
    }
}
