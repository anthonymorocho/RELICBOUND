using System;
using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    /// <summary>Interacción genérica con texto y condición (subir al coche, coger la mochila...).</summary>
    public sealed class ActionPoint : Interactable
    {
        [SerializeField] private string prompt = "Usar";

        public Func<bool> Available { get; set; }
        public Func<string> DynamicPrompt { get; set; }

        public event Action<ActionPoint> Used;

        public override string Prompt => DynamicPrompt != null ? DynamicPrompt() : prompt;

        public override bool CanInteract => base.CanInteract && (Available == null || Available());

        public void Configure(string label) => prompt = label;

        public override void Interact(Interactor interactor) => Used?.Invoke(this);
    }
}
