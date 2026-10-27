using System.Collections;
using System.Collections.Generic;
using BeforeItWasYou.Audio;
using BeforeItWasYou.Core.Narrative;
using BeforeItWasYou.Core.Save;
using BeforeItWasYou.Input;
using BeforeItWasYou.Services;
using UnityEngine;

namespace BeforeItWasYou.Episode
{
    /// <summary>
    /// A1 — Llegada (GUION §A1): mensajes de Lucía al aparcar, respuestas de Daniel y el objetivo breve.
    /// El móvil se saca con Tab; las respuestas se eligen con 1, 2 o 3. El reloj del móvil avanza con la partida.
    /// El progreso de la conversación se guarda en el snapshot y al cargar se reconstruye sin repetir avisos.
    /// </summary>
    public sealed class ArrivalNarrative : MonoBehaviour
    {
        private const string StepKey = "phone_arrival_step";
        private const int StartMinutes = 21 * 60 + 12;

        private enum Kind
        {
            Incoming,
            Replies,
            Objective,
        }

        private sealed class Step
        {
            public Kind Kind;
            public string Id;
            public string Text;
            public PhoneReply[] Replies;
            public float Delay;
        }

        // Texto de producción del guion, sin comillas angulares ni frases teatrales.
        private static readonly Step[] Script =
        {
            new Step { Kind = Kind.Incoming, Id = "lucia_01", Text = "¿Ya llegaste?", Delay = 5f },
            new Step { Kind = Kind.Replies, Replies = new[] { new PhoneReply("daniel_01", "Sí. Voy entrando."), new PhoneReply("daniel_01b", "Recién. Estoy afuera.") } },
            new Step { Kind = Kind.Incoming, Id = "lucia_02", Text = "La carpeta azul está en la cómoda de mamá. Creo.", Delay = 2.6f },
            new Step { Kind = Kind.Replies, Replies = new[] { new PhoneReply("daniel_02", "Voy a ver.") } },
            new Step { Kind = Kind.Incoming, Id = "lucia_03", Text = "Y el cargador, porfa. Aquí solo hay uno y lo estamos turnando.", Delay = 3.2f },
            new Step { Kind = Kind.Objective, Text = "Recoge las cosas de mamá." },
        };

        [SerializeField] private EpisodeDirector director;
        [SerializeField] private AudioSource vibrate;
        [SerializeField] private AudioClip vibrateClip;

        private readonly PhoneLog _log = new PhoneLog("Lucía");
        private int _step;
        private bool _running;
        private float _playSeconds;

        public PhoneLog Log => _log;
        public int StepIndex => _step;

        public void Configure(EpisodeDirector episode, AudioSource source, AudioClip clip)
        {
            director = episode;
            vibrate = source;
            vibrateClip = clip;
        }

        private void Awake()
        {
            director.Started += OnStarted;
            director.SnapshotRequested += OnSnapshot;
            _log.Received += OnReceived;
            _log.ReplySent += _ => Advance();
        }

        private void OnDestroy()
        {
            if (director == null) return;
            director.Started -= OnStarted;
            director.SnapshotRequested -= OnSnapshot;
        }

        private void OnStarted(Snapshot snapshot)
        {
            var ui = director.Ui;
            ui.Phone.Bind(_log);
            ui.Phone.SetOpen(false);
            _step = 0;
            if (snapshot != null)
            {
                _playSeconds = snapshot.EpisodeSeconds;
                var target = snapshot.Numbers.TryGetValue(StepKey, out var saved) ? (int)saved : 0;
                // Reconstruye en silencio la conversación hasta el punto guardado.
                while (_step < target && _step < Script.Length)
                {
                    var s = Script[_step];
                    if (s.Kind == Kind.Incoming) _log.Add(new PhoneMessage(s.Id, Sender.Contact, s.Text, Clock()), true);
                    else if (s.Kind == Kind.Replies)
                    {
                        var sentId = snapshot.MessagesSent.Find(id => System.Array.Exists(s.Replies, r => r.Id == id));
                        var reply = System.Array.Find(s.Replies, r => r.Id == sentId) ?? s.Replies[0];
                        _log.Add(new PhoneMessage(reply.Id, Sender.Player, reply.Text, Clock()), true);
                    }
                    _step++;
                }
                _log.RestoreRead(snapshot.MessagesRead);
            }
            _running = true;
            StartCoroutine(RunFrom());
        }

        private void OnSnapshot(Snapshot snapshot)
        {
            snapshot.Numbers[StepKey] = _step;
            snapshot.EpisodeSeconds = _playSeconds;
            foreach (var m in _log.Messages)
            {
                if (m.Sender == Sender.Player) snapshot.MessagesSent.Add(m.Id);
            }
            snapshot.MessagesRead.AddRange(_log.ReadIds);
        }

        private IEnumerator RunFrom()
        {
            while (_running && _step < Script.Length)
            {
                var s = Script[_step];
                switch (s.Kind)
                {
                    case Kind.Incoming:
                        if (s.Delay > 2f)
                        {
                            yield return WaitPlaying(s.Delay - 1.8f);
                            director.Ui.Phone.SetTyping(true);
                            yield return WaitPlaying(1.8f);
                            director.Ui.Phone.SetTyping(false);
                        }
                        else
                        {
                            yield return WaitPlaying(s.Delay);
                        }
                        _log.Add(new PhoneMessage(s.Id, Sender.Contact, s.Text, Clock()));
                        _step++;
                        break;
                    case Kind.Replies:
                        _log.OfferReplies(s.Replies);
                        yield break; // Continúa cuando Daniel responde (Advance).
                    case Kind.Objective:
                        yield return WaitPlaying(1.2f);
                        director.Ui.ShowObjective(s.Text, 7f);
                        _step++;
                        break;
                }
            }
        }

        private void Advance()
        {
            _step++;
            StartCoroutine(RunFrom());
        }

        private IEnumerator WaitPlaying(float seconds)
        {
            var t = 0f;
            while (t < seconds)
            {
                if (GameServices.Flow.IsSimulating) t += Time.deltaTime;
                yield return null;
            }
        }

        private void OnReceived(PhoneMessage message)
        {
            if (vibrate != null && vibrateClip != null) vibrate.PlayOneShot(vibrateClip, AudioBuses.Get(AudioBus.Effects));
            if (!director.Ui.Phone.IsOpen) director.Ui.ShowToast(_log.Contact + ": " + message.Text, 6f);
            else _log.MarkAllRead();
        }

        private string Clock()
        {
            var minutes = StartMinutes + (int)(_playSeconds / 60f);
            return $"{minutes / 60 % 24:00}:{minutes % 60:00}";
        }

        private void Update()
        {
            if (!_running || GameServices.Flow == null || !GameServices.Flow.IsSimulating) return;
            _playSeconds += Time.deltaTime;
            var phone = director.Ui.Phone;
            phone.SetClock(Clock());
            var input = GameInput.Instance;
            if (input.Phone.WasPressedThisFrame()) phone.SetOpen(!phone.IsOpen);
            if (!phone.IsOpen) return;
            for (var i = 0; i < input.Choices.Length; i++)
            {
                if (input.Choices[i].WasPressedThisFrame() && _log.Choose(i, Clock())) break;
            }
        }
    }
}
