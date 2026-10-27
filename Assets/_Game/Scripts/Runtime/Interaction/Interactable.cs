using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    /// <summary>
    /// Algo con lo que Daniel puede interactuar apuntando y pulsando E. La etiqueta es breve y contextual
    /// (Abrir, Cerrar, Encender luz) y solo aparece al mirar el objeto.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private float reach = 1.9f;

        /// <summary>Identificador estable para guardado y narrativa (no depende del nombre del GameObject).</summary>
        public string Id
        {
            get => string.IsNullOrEmpty(id) ? name : id;
            set => id = value;
        }

        public float Reach => reach;

        public abstract string Prompt { get; }

        public virtual bool CanInteract => isActiveAndEnabled;

        public abstract void Interact(Interactor interactor);
    }
}
