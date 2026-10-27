using System;
using System.Collections;
using BeforeItWasYou.Core.Save;
using BeforeItWasYou.Episode;
using BeforeItWasYou.Interaction;
using BeforeItWasYou.Player;
using BeforeItWasYou.Services;
using UnityEngine;
using UnityEngine.AI;

namespace BeforeItWasYou.AI
{
    public enum IntruderState
    {
        Outside,
        Shed,
        Laundry,
        RetrievingBag,
        Aware,
        Warning,
        Searching,
        Fleeing,
        AggressiveIfCornered,
        Gone,
    }

    /// <summary>
    /// Víctor (CRONOLOGIA_VICTOR): una persona que quiere recuperar su mochila e irse sin que lo vean. Existe
    /// físicamente en todo momento y solo se mueve por la NavMesh; nunca aparece de la nada. Ve con un cono y
    /// oclusión, oye los ruidos con atenuación por paredes y recuerda la última posición conocida de Daniel.
    /// Solo ataca si se siente acorralado y siempre con aviso previo.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class IntruderAI : MonoBehaviour
    {
        [Serializable]
        public sealed class Nodes
        {
            public Transform behindShed;
            public Transform shedInside;
            public Transform shedDoorway;
            public Transform serviceDoorOutside;
            public Transform laundryInside;
            public Transform gateInside;
            public Transform street;
        }

        [SerializeField] private Nodes nodes = new Nodes();
        [SerializeField] private Animator animator;
        [SerializeField] private Transform eyes;
        [SerializeField] private Door serviceDoor;
        [SerializeField] private Door shedDoor;
        [SerializeField] private Door gate;
        [SerializeField] private GameObject backpack;
        [SerializeField] private Rigidbody laundryBox;
        [SerializeField] private AudioSource voice;
        [SerializeField] private float walkSpeed = 1.35f;
        [SerializeField] private float runSpeed = 3.6f;

        private NavMeshAgent _agent;
        private PlayerController _player;
        private Vector3 _lastKnown;
        private float _lastSeenTime = -100f;
        private Coroutine _routine;
        private bool _hasBag;

        public IntruderState State { get; private set; } = IntruderState.Outside;
        public bool HasBag => _hasBag;
        public bool KnowsAboutPlayer { get; private set; }
        public Vector3 LastKnownPlayerPosition => _lastKnown;

        /// <summary>Cambio de estado con su causa (telemetría DEV y lógica narrativa).</summary>
        public event Action<IntruderState, IntruderState, string> StateChanged;

        public event Action<string, string> Spoke;

        public event Action Attacked;

        public void Configure(Nodes points, Animator anim, Transform head, Door service, Door shed, Door sideGate, GameObject bag, Rigidbody box, AudioSource audioSource)
        {
            nodes = points;
            animator = anim;
            eyes = head;
            serviceDoor = service;
            shedDoor = shed;
            gate = sideGate;
            backpack = bag;
            laundryBox = box;
            voice = audioSource;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = walkSpeed;
            _agent.angularSpeed = 240f;
            _agent.acceleration = 6f;
            _agent.stoppingDistance = 0.25f;
            NoiseEvents.Emitted += OnNoise;
        }

        private void OnDestroy() => NoiseEvents.Emitted -= OnNoise;

        private void Start()
        {
            if (EpisodeDirector.Current != null)
            {
                _player = EpisodeDirector.Current.Player;
                EpisodeDirector.Current.SnapshotRequested += Store;
                EpisodeDirector.Current.Started += Restore;
            }
        }

        // ---------- Percepción ----------

        public bool CanSeePlayer(float range = 13f, float halfAngle = 62f)
        {
            if (_player == null || !_player.isActiveAndEnabled || eyes == null) return false;
            // Escondido: solo cuenta si Víctor le vio meterse (hace menos de dos segundos desde que le vio).
            if (_player.Hidden && HideSpot.Occupied != null && _lastSeenTime < HideSpot.Occupied.HiddenSince - 0.5f) return false;
            var target = _player.Camera.transform.position - Vector3.up * (_player.IsCrouched ? 0.15f : 0.35f);
            var to = target - eyes.position;
            if (to.magnitude > range) return false;
            if (Vector3.Angle(eyes.forward, to) > halfAngle && to.magnitude > 1.6f) return false;
            var mask = ~((1 << Layers.Player) | (1 << Layers.Intruder) | (1 << Layers.Glass));
            if (Physics.Raycast(eyes.position, to.normalized, to.magnitude - 0.15f, mask, QueryTriggerInteraction.Ignore)) return false;
            _lastKnown = _player.transform.position;
            _lastSeenTime = Time.time;
            KnowsAboutPlayer = true;
            return true;
        }

        private void OnNoise(Vector3 position, float radius, GameObject source)
        {
            if (State == IntruderState.Gone || source == gameObject) return;
            var to = position - transform.position;
            var blocked = Physics.Raycast(transform.position + Vector3.up * 1.5f, to.normalized, to.magnitude,
                ~((1 << Layers.Player) | (1 << Layers.Intruder)), QueryTriggerInteraction.Ignore);
            if (to.magnitude > radius * (blocked ? 0.45f : 1f)) return;
            _lastKnown = position;
            KnowsAboutPlayer |= source != null && source.layer == Layers.Player;
            Heard?.Invoke(position);
        }

        public event Action<Vector3> Heard;

        // ---------- Rutinas de la cronología ----------

        /// <summary>T+8: entra por la puerta de servicio a por la bolsa de comida del lavadero y tira la caja.</summary>
        public void BeginLaundryVisit()
        {
            if (State != IntruderState.Outside) return;
            Run(LaundryVisit());
        }

        private IEnumerator LaundryVisit()
        {
            SetState(IntruderState.Laundry, "visita al lavadero");
            yield return MoveTo(nodes.serviceDoorOutside.position, walkSpeed);
            if (CanSeePlayer()) { yield return Retreat("le ven entrar"); yield break; }
            serviceDoor.SetState(DoorState.Open, false);
            yield return MoveTo(nodes.laundryInside.position, walkSpeed);
            Animate("Interact");
            yield return Wait(1.1f);
            if (laundryBox != null)
            {
                laundryBox.isKinematic = false;
                laundryBox.AddForce((nodes.laundryInside.forward + Vector3.up * 0.2f) * 2.2f, ForceMode.Impulse);
                NoiseEvents.Emit(laundryBox.position, 14f, gameObject);
                var box = laundryBox.GetComponent<AudioSource>();
                if (box != null) box.Play();
            }
            yield return Wait(0.8f);
            yield return Retreat("cogió lo que buscaba en el lavadero");
        }

        private IEnumerator Retreat(string cause)
        {
            yield return MoveTo(nodes.serviceDoorOutside.position, walkSpeed * 1.4f);
            serviceDoor.SetState(DoorState.Ajar, false);
            yield return MoveTo(nodes.behindShed.position, walkSpeed);
            Face(nodes.shedDoorway.position);
            SetState(IntruderState.Outside, cause);
        }

        /// <summary>A4: Daniel está en el cobertizo. Víctor llega desde el patio y se para en la puerta.</summary>
        public void BeginEncounter()
        {
            if (State != IntruderState.Outside && State != IntruderState.Shed) return;
            Run(Encounter());
        }

        private IEnumerator Encounter()
        {
            SetState(IntruderState.Aware, "Daniel dentro del cobertizo");
            yield return MoveTo(nodes.shedDoorway.position, walkSpeed);
            Face(_player.transform.position);
            SetState(IntruderState.Warning, "encuentro en la puerta");
            Animate("Idle");
            // Pausa de figura inmóvil (SISTEMAS §Terror: 0.7–1.5 s) antes de hablar.
            yield return Wait(1.1f);
            Say("Víctor", "No toques eso.", 2.2f);
            yield return Wait(2.4f);
            EncounterLine?.Invoke(0);
            yield return Wait(2.2f);
            Say("Víctor", "Es mío. Lo agarro y me voy.", 2.6f);
            yield return Wait(2.8f);
            EncounterLine?.Invoke(1);
            yield return Wait(2.2f);
            Say("Víctor", "No quiero problemas.", 2.2f);
            // Ventana de decisión: Daniel se aparta del paso o se queda entre Víctor y la mochila.
            var deadline = Time.time + 5.5f;
            while (Time.time < deadline)
            {
                if (PlayerHoldsBag()) break;
                if (!PlayerBlocks()) { yield return TakeBagAndLeave(false); yield break; }
                yield return null;
            }
            yield return Shove();
        }

        /// <summary>Daniel responde en voz alta (lo gestiona el director narrativo con subtítulos).</summary>
        public event Action<int> EncounterLine;

        private bool PlayerHoldsBag() => EpisodeState.Current != null && EpisodeState.Current.Has("mochila_victor");

        private bool PlayerBlocks()
        {
            if (_player == null || backpack == null || !backpack.activeInHierarchy) return false;
            var door = nodes.shedDoorway.position;
            var bag = backpack.transform.position;
            var p = _player.transform.position;
            var distToBag = Vector3.Distance(Flat(p), Flat(bag));
            var t = Mathf.Clamp01(Vector3.Dot(Flat(p - door), Flat(bag - door).normalized) / Mathf.Max(0.1f, Vector3.Distance(Flat(door), Flat(bag))));
            var onLine = Vector3.Distance(Flat(p), Flat(Vector3.Lerp(door, bag, t))) < 0.55f;
            return distToBag < 1.0f || onLine;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private IEnumerator TakeBagAndLeave(bool hurried)
        {
            SetState(IntruderState.RetrievingBag, hurried ? "recupera la mochila tras el empujón" : "Daniel le deja pasar");
            if (backpack != null && backpack.activeInHierarchy)
            {
                yield return MoveTo(backpack.transform.position, walkSpeed);
                Animate("PickUp");
                yield return Wait(1.2f);
                backpack.SetActive(false);
                _hasBag = true;
            }
            yield return Flee(hurried ? "huye con la mochila" : "se va sin mirar atrás");
        }

        private IEnumerator Shove()
        {
            SetState(IntruderState.AggressiveIfCornered, PlayerHoldsBag() ? "Daniel tiene su mochila" : "Daniel le bloquea el paso");
            Say("Víctor", "¡Apártate!", 1.4f);
            yield return MoveTo(_player.transform.position, runSpeed, 1.1f);
            Animate("Push");
            yield return Wait(0.45f);
            if (_player != null && Vector3.Distance(transform.position, _player.transform.position) < 1.6f)
            {
                var away = (_player.transform.position - transform.position).normalized;
                _player.Knockback(away * 2.2f);
            }
            yield return Wait(0.6f);
            if (PlayerHoldsBag()) { yield return Pursue(); yield break; }
            yield return TakeBagAndLeave(true);
        }

        /// <summary>A5: Daniel se lleva la mochila. Víctor la quiere de vuelta: busca por última posición y ruidos.</summary>
        private IEnumerator Pursue()
        {
            SetState(IntruderState.Searching, "persigue a quien tiene su mochila");
            while (PlayerHoldsBag())
            {
                if (CanSeePlayer())
                {
                    _agent.speed = runSpeed;
                    _agent.SetDestination(_lastKnown);
                    if (Vector3.Distance(transform.position, _player.transform.position) < 1.35f)
                    {
                        yield return TelegraphedAttack();
                        if (State == IntruderState.Gone) yield break;
                    }
                }
                else if (Time.time - _lastSeenTime < 14f)
                {
                    _agent.speed = walkSpeed * 1.3f;
                    _agent.SetDestination(_lastKnown);
                    if (!_agent.pathPending && _agent.remainingDistance < 0.6f) _lastKnown = RandomNear(_lastKnown, 3f);
                }
                else
                {
                    // Abandona: la calle está a un paso y teme que lleguen.
                    Say("Víctor", "Quédatela. Me voy.", 2f);
                    yield return Flee("abandona la búsqueda");
                    yield break;
                }
                yield return new WaitForSeconds(0.15f);
            }
            yield return Flee("recupera la mochila");
        }

        private IEnumerator TelegraphedAttack()
        {
            _agent.isStopped = true;
            Face(_player.transform.position);
            Animate("Attack");
            Say("Víctor", "¡Dámela!", 1f);
            // Ventana de reacción: 0.9 s para apartarse o soltarla.
            yield return Wait(0.9f);
            _agent.isStopped = false;
            if (_player != null && Vector3.Distance(transform.position, _player.transform.position) < 1.55f && CanSeePlayer(3f, 80f))
            {
                Attacked?.Invoke();
            }
        }

        private IEnumerator Flee(string cause)
        {
            SetState(IntruderState.Fleeing, cause);
            yield return MoveTo(nodes.gateInside.position, runSpeed * 0.8f);
            gate.SetState(DoorState.Open, false);
            yield return Wait(0.6f);
            yield return MoveTo(nodes.street.position, runSpeed * 0.8f);
            SetState(IntruderState.Gone, "se fue");
            gameObject.SetActive(false);
        }

        // ---------- Utilidades ----------

        private void Run(IEnumerator routine)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(routine);
        }

        private IEnumerator MoveTo(Vector3 target, float speed, float giveUpAfter = 30f)
        {
            if (!_agent.isOnNavMesh) yield break;
            _agent.speed = speed;
            _agent.isStopped = false;
            _agent.SetDestination(target);
            var end = Time.time + giveUpAfter;
            while (Time.time < end && (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance + 0.05f)) yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            var end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        private static Vector3 RandomNear(Vector3 center, float radius)
        {
            var p = center + new Vector3(UnityEngine.Random.Range(-radius, radius), 0f, UnityEngine.Random.Range(-radius, radius));
            return NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas) ? hit.position : center;
        }

        private void Face(Vector3 point)
        {
            var dir = Flat(point - transform.position);
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        }

        private void Animate(string trigger)
        {
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(trigger);
        }

        private void Say(string speaker, string line, float seconds) => Spoke?.Invoke(speaker, line);

        private void SetState(IntruderState next, string cause)
        {
            var prev = State;
            State = next;
            if (Debug.isDebugBuild) Debug.Log($"[Víctor] {prev} → {next}: {cause}");
            StateChanged?.Invoke(prev, next, cause);
        }

        private void Update()
        {
            if (animator != null) animator.SetFloat("Speed", _agent.velocity.magnitude);
            if (State == IntruderState.Outside || State == IntruderState.Laundry) CanSeePlayer();
        }

        // ---------- Guardado ----------

        private void Store(Snapshot snapshot)
        {
            var p = transform.position;
            snapshot.Intruder.State = State.ToString();
            snapshot.Intruder.Position = new Vector3Data(p.x, p.y, p.z);
            snapshot.Intruder.Yaw = transform.eulerAngles.y;
            snapshot.Intruder.HasBag = _hasBag;
            snapshot.Intruder.KnowsAboutPlayer = KnowsAboutPlayer;
            snapshot.Intruder.LastKnownPlayerPosition = new Vector3Data(_lastKnown.x, _lastKnown.y, _lastKnown.z);
        }

        private void Restore(Snapshot snapshot)
        {
            if (snapshot == null) return;
            if (!Enum.TryParse(snapshot.Intruder.State, out IntruderState saved)) return;
            var p = snapshot.Intruder.Position;
            if (_agent.isOnNavMesh) _agent.Warp(new Vector3(p.X, p.Y, p.Z));
            transform.rotation = Quaternion.Euler(0f, snapshot.Intruder.Yaw, 0f);
            _hasBag = snapshot.Intruder.HasBag;
            KnowsAboutPlayer = snapshot.Intruder.KnowsAboutPlayer;
            if (backpack != null) backpack.SetActive(!_hasBag);
            // Los checkpoints nunca se guardan a mitad de una persecución: solo estados de espera.
            State = saved == IntruderState.Gone ? IntruderState.Gone : saved == IntruderState.Shed ? IntruderState.Shed : IntruderState.Outside;
            if (State == IntruderState.Gone) gameObject.SetActive(false);
        }
    }
}
