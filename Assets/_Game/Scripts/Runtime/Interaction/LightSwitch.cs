using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    /// <summary>Interruptor de pared: enciende luces y materiales emisivos (pantallas de lámpara, bombillas).</summary>
    public sealed class LightSwitch : Interactable
    {
        [SerializeField] private Light[] lights = new Light[0];
        [SerializeField] private Renderer[] emissive = new Renderer[0];
        [SerializeField] private Transform toggle;
        [SerializeField] private bool on;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickClip;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private Color[] _emission = new Color[0];

        public bool On => on;

        public override string Prompt => on ? "Apagar luz" : "Encender luz";

        public void Configure(Light[] targets, Renderer[] glowing, Transform rocker, bool startOn)
        {
            lights = targets;
            emissive = glowing;
            toggle = rocker;
            on = startOn;
        }

        public void SetAudio(AudioSource source, AudioClip click)
        {
            audioSource = source;
            clickClip = click;
        }

        private void Awake()
        {
            _emission = new Color[emissive.Length];
            for (var i = 0; i < emissive.Length; i++)
            {
                if (emissive[i] != null) _emission[i] = emissive[i].sharedMaterial.GetColor(EmissionColor);
            }
            Set(on, true);
        }

        public override void Interact(Interactor interactor) => Set(!on, false);

        public void Set(bool value, bool silent)
        {
            on = value;
            foreach (var target in lights)
            {
                if (target != null) target.enabled = on;
            }
            for (var i = 0; i < emissive.Length; i++)
            {
                if (emissive[i] == null) continue;
                emissive[i].material.SetColor(EmissionColor, on ? _emission[i] : Color.black);
            }
            if (toggle != null) toggle.localRotation = Quaternion.Euler(on ? -8f : 8f, 0f, 0f);
            if (!silent && audioSource != null && clickClip != null) audioSource.PlayOneShot(clickClip);
        }
    }
}
