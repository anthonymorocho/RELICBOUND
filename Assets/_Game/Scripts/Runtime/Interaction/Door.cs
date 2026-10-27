using BeforeItWasYou.Player;
using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    public enum DoorState
    {
        Closed,
        Open,
        Ajar,
        Locked,
    }

    /// <summary>
    /// Puerta con bisagra real: gira alrededor de su pivote, se abre alejándose de quien la empuja y suena.
    /// Locked solo por un motivo narrativo explícito (SISTEMAS §Interacciones).
    /// </summary>
    public sealed class Door : Interactable
    {
        [SerializeField] private Transform leaf;
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float ajarAngle = 14f;
        [SerializeField] private float swingSpeed = 2.4f;
        [SerializeField] private bool opensBothWays = true;
        [SerializeField] private string lockedReason = "Cerrada con llave";
        [SerializeField] private DoorState state = DoorState.Closed;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;

        private float _current;
        private float _target;
        private float _direction = 1f;

        public DoorState State => state;

        public override string Prompt => state switch
        {
            DoorState.Locked => lockedReason,
            DoorState.Open => "Cerrar",
            _ => "Abrir",
        };

        public void Configure(Transform doorLeaf, float angle, bool bothWays)
        {
            leaf = doorLeaf;
            openAngle = angle;
            opensBothWays = bothWays;
        }

        public void SetAudio(AudioSource source, AudioClip open, AudioClip close)
        {
            audioSource = source;
            openClip = open;
            closeClip = close;
        }

        private void Awake() => SetState(state, true);

        public override void Interact(Interactor interactor)
        {
            if (state == DoorState.Locked) return;
            if (state == DoorState.Open)
            {
                SetState(DoorState.Closed, false);
                return;
            }
            if (opensBothWays && interactor != null)
            {
                // Abre hacia el lado contrario al jugador, como al empujar.
                var local = transform.InverseTransformPoint(interactor.transform.position);
                _direction = local.z > 0f ? -1f : 1f;
            }
            SetState(DoorState.Open, false);
            if (interactor != null) NoiseEvents.Emit(transform.position, 5f, interactor.gameObject);
        }

        public void SetState(DoorState next, bool instant)
        {
            var wasOpen = _target > 1f;
            state = next;
            _target = next switch
            {
                DoorState.Open => openAngle * _direction,
                DoorState.Ajar => ajarAngle * _direction,
                _ => 0f,
            };
            if (instant)
            {
                _current = _target;
                Apply();
            }
            else if (audioSource != null)
            {
                var clip = next == DoorState.Open ? openClip : wasOpen ? closeClip : null;
                if (clip != null) audioSource.PlayOneShot(clip);
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.MoveTowards(_current, _target, swingSpeed * 60f * Time.deltaTime * (0.35f + 0.65f * Mathf.Clamp01(Mathf.Abs(_target - _current) / 30f)));
            Apply();
        }

        private void Apply()
        {
            if (leaf != null) leaf.localRotation = Quaternion.Euler(0f, _current, 0f);
        }
    }
}
