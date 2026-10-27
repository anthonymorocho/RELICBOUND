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
        [SerializeField] private AudioClip unlockClip;
        [SerializeField] private bool unlockWithKey;
        [SerializeField] private bool latchFails;

        /// <summary>Se dispara la primera vez que la puerta no engancha y vuelve a abrirse sola.</summary>
        public event System.Action<Door> FailedToLatch;
        public event System.Action<Door> Opened;
        private bool _latchReported;

        private float _current;
        private float _target;
        private float _direction = 1f;

        public DoorState State => state;

        public override string Prompt => state switch
        {
            DoorState.Locked => unlockWithKey ? "Abrir con la llave" : lockedReason,
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

        /// <summary>Puerta cerrada con llave que Daniel abre con la suya (la puerta principal).</summary>
        public void ConfigureKey(AudioClip unlock)
        {
            unlockWithKey = true;
            unlockClip = unlock;
        }

        /// <summary>El cierre no engancha: al cerrarla vuelve a quedar entornada (puerta de servicio).</summary>
        public void ConfigureFaultyLatch() => latchFails = true;

        private void Awake() => SetState(state, true);

        /// <summary>Restaura el estado guardado (una puerta abierta con llave ya no vuelve a cerrarse con llave).</summary>
        public void Restore(DoorState saved)
        {
            if (saved != DoorState.Locked) unlockWithKey = false;
            SetState(saved, true);
        }

        public override void Interact(Interactor interactor)
        {
            if (state == DoorState.Locked)
            {
                if (!unlockWithKey) return;
                unlockWithKey = false;
                if (audioSource != null && unlockClip != null) audioSource.PlayOneShot(unlockClip);
                state = DoorState.Closed;
                Invoke(nameof(OpenAfterUnlock), 0.55f);
                _pendingOpener = interactor;
                return;
            }
            if (state == DoorState.Open)
            {
                SetState(DoorState.Closed, false);
                if (latchFails) Invoke(nameof(SwingBack), 1.6f);
                return;
            }
            if (opensBothWays && interactor != null)
            {
                // Abre hacia el lado contrario al jugador, como al empujar.
                var local = transform.InverseTransformPoint(interactor.transform.position);
                _direction = local.z > 0f ? -1f : 1f;
            }
            SetState(DoorState.Open, false);
            Opened?.Invoke(this);
            if (interactor != null) NoiseEvents.Emit(transform.position, 5f, interactor.gameObject);
        }

        private Interactor _pendingOpener;

        private void OpenAfterUnlock() => Interact(_pendingOpener);

        private void SwingBack()
        {
            if (state != DoorState.Closed) return;
            SetState(DoorState.Ajar, false);
            if (!_latchReported)
            {
                _latchReported = true;
                FailedToLatch?.Invoke(this);
            }
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
