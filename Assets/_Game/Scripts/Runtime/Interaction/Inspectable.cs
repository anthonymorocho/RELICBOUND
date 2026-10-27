using BeforeItWasYou.Episode;
using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    /// <summary>
    /// Algo que Daniel puede mirar de cerca: una foto, una lata abierta, una manta. Muestra lo que piensa en voz
    /// baja (una línea corta) y deja un hecho narrativo. Sin brillos ni marcadores: solo la etiqueta al apuntar.
    /// </summary>
    public sealed class Inspectable : Interactable
    {
        [SerializeField] private string prompt = "Mirar";
        [SerializeField] private string thought;
        [SerializeField] private string flag;
        [SerializeField] private bool once = true;

        private bool _seen;

        public string Flag => flag;

        public override string Prompt => prompt;

        public override bool CanInteract => base.CanInteract && !(once && _seen);

        public void Configure(string label, string line, string factFlag, bool singleUse = true)
        {
            prompt = label;
            thought = line;
            flag = factFlag;
            once = singleUse;
        }

        public override void Interact(Interactor interactor)
        {
            _seen = true;
            if (!string.IsNullOrEmpty(thought) && EpisodeDirector.Current != null)
            {
                EpisodeDirector.Current.Ui.ShowSubtitle(new UI.DialogueLine(string.Empty, thought));
            }
            if (!string.IsNullOrEmpty(flag)) EpisodeState.Current?.SetFlag(flag);
        }
    }
}
