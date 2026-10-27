using System.Collections;
using System.Collections.Generic;
using BeforeItWasYou.AI;
using BeforeItWasYou.Core.Save;
using BeforeItWasYou.Input;
using BeforeItWasYou.Interaction;
using BeforeItWasYou.Services;
using BeforeItWasYou.UI;
using UnityEngine;

namespace BeforeItWasYou.Episode
{
    /// <summary>
    /// La noche del episodio (GUION A1–A6): entrada con llave (CP00), objetos (CP01), primeras pruebas y ruido del
    /// lavadero, refugio del cobertizo (CP02), encuentro con Víctor (CP03), huida (CP04), llamada a emergencias y
    /// los finales. Todo depende de hechos (objetos, puertas, zonas, lo que Víctor ve), no de temporizadores ciegos.
    /// </summary>
    public sealed class StoryDirector : MonoBehaviour
    {
        [SerializeField] private EpisodeDirector director;
        [SerializeField] private IntruderAI victor;
        [SerializeField] private ZoneTrigger house;
        [SerializeField] private ZoneTrigger shed;
        [SerializeField] private ZoneTrigger street;
        [SerializeField] private ZoneTrigger backyard;
        [SerializeField] private Door frontDoor;
        [SerializeField] private Door serviceDoor;
        [SerializeField] private ActionPoint car;
        [SerializeField] private ActionPoint backpack;

        private EpisodeState _state;
        private bool _callActive;
        private bool _ending;

        public const string FlagNoise = "ruido_lavadero";
        public const string FlagLatch = "puerta_no_engancha";
        public const string FlagRefuge = "refugio_visto";
        public const string FlagEncounter = "encuentro";
        public const string FlagShove = "empujon";
        public const string FlagCalled = "emergencias";
        public const string FlagVictorGone = "victor_se_fue";
        public const string Backpack = "mochila_victor";

        public void Configure(EpisodeDirector episode, IntruderAI intruder, ZoneTrigger houseZone, ZoneTrigger shedZone, ZoneTrigger streetZone,
            ZoneTrigger yardZone, Door front, Door service, ActionPoint carDoor, ActionPoint bag)
        {
            director = episode;
            victor = intruder;
            house = houseZone;
            shed = shedZone;
            street = streetZone;
            backyard = yardZone;
            frontDoor = front;
            serviceDoor = service;
            car = carDoor;
            backpack = bag;
        }

        private void Start()
        {
            _state = EpisodeState.Current;
            house.Entered += _ => Checkpoint("CP00", "Recoge las cosas de mamá.", once: true);
            _state.ItemAdded += OnItem;
            serviceDoor.FailedToLatch += _ =>
            {
                _state.SetFlag(FlagLatch);
                Think("No engancha.");
            };
            shed.Entered += _ => StartCoroutine(InShed());
            street.Entered += _ => TryEnd(false);
            car.Available = () => _state.Has(ArrivalNarrative.Folder) || Danger;
            car.DynamicPrompt = () => HasEverything || Danger ? "Irse en el coche" : "Subir al coche";
            car.Used += _ => OnCar();
            backpack.Available = () => !_state.Has(Backpack);
            backpack.DynamicPrompt = () => _state.Flag("carnet_visto") ? "Coger la mochila" : "Mirar la mochila";
            backpack.Used += OnBackpack;
            victor.Spoke += (speaker, line) => director.Ui.ShowSubtitle(new DialogueLine(speaker, line));
            victor.EncounterLine += OnDanielAnswers;
            victor.Attacked += () => director.Fail("NO HUBO TIEMPO", "Víctor solo quería su mochila y una salida. Apártate o suéltala.");
            victor.StateChanged += OnVictorState;
        }

        private bool HasEverything => _state.Has(ArrivalNarrative.Folder) && _state.Has(ArrivalNarrative.Charger) && _state.Has(ArrivalNarrative.Clothes);

        /// <summary>Daniel tiene motivos para pensar que alguien entra en la casa (pistas acumuladas o encuentro).</summary>
        private bool Danger
        {
            get
            {
                if (_state.Flag(FlagRefuge) || _state.Flag(FlagEncounter)) return true;
                var clues = 0;
                foreach (var flag in new[] { FlagNoise, FlagLatch, "lata_abierta", "taza_ajena", "manta_cobertizo" })
                {
                    if (_state.Flag(flag)) clues++;
                }
                return clues >= 2;
            }
        }

        // ---------- A2 / A3 ----------

        private void OnItem(string item)
        {
            var count = (_state.Has(ArrivalNarrative.Folder) ? 1 : 0) + (_state.Has(ArrivalNarrative.Charger) ? 1 : 0) + (_state.Has(ArrivalNarrative.Clothes) ? 1 : 0);
            if (count == 2 && !_state.Flag(FlagNoise)) StartCoroutine(LaundryNoiseWhenAway());
            if (HasEverything) Checkpoint("CP01", "Llévale las cosas a mamá.", once: true);
            if (item == Backpack) _state.SetFlag(FlagShove);
        }

        /// <summary>
        /// Víctor entra al lavadero solo cuando Daniel está lejos (sala o dormitorios): si Daniel vigila la cocina o el
        /// patio, Víctor espera (CRONOLOGIA §Invariante: nada se mueve sin un agente y un momento plausibles).
        /// </summary>
        private IEnumerator LaundryNoiseWhenAway()
        {
            var player = director.Player.transform;
            while (true)
            {
                var p = player.position;
                var far = p.x < 9.6f || p.z < 10.4f;
                if (far && house.PlayerInside && !victor.CanSeePlayer()) break;
                yield return new WaitForSeconds(1f);
            }
            _state.SetFlag(FlagNoise);
            victor.BeginLaundryVisit();
        }

        private IEnumerator InShed()
        {
            if (!_state.Flag(FlagNoise) || _state.Flag(FlagEncounter) || victor.State != IntruderState.Outside) yield break;
            Checkpoint("CP03", "", once: true);
            // Unos segundos dentro bastan para que Víctor, que vigilaba desde detrás del cobertizo, se decida.
            var since = Time.time;
            while (shed.PlayerInside && Time.time - since < 5f && !_state.Has(Backpack)) yield return null;
            if (!shed.PlayerInside && !_state.Has(Backpack)) yield break;
            _state.SetFlag(FlagEncounter);
            victor.BeginEncounter();
        }

        private void OnBackpack(ActionPoint point)
        {
            if (!_state.Flag("carnet_visto"))
            {
                _state.SetFlag("carnet_visto");
                _state.SetFlag(FlagRefuge);
                Think("Ropa, un cepillo de dientes. Un carnet: Víctor Salas.");
                Checkpoint("CP02", "", once: true);
                return;
            }
            _state.AddItem(Backpack);
            point.gameObject.SetActive(false);
        }

        private void OnDanielAnswers(int index)
        {
            var line = index == 0 ? "¿Quién eres?" : "¿Estabas viviendo aquí?";
            director.Ui.ShowSubtitle(new DialogueLine("Daniel", line));
        }

        private void OnVictorState(IntruderState previous, IntruderState next, string cause)
        {
            if (next == IntruderState.Gone) _state.SetFlag(FlagVictorGone);
            if (next == IntruderState.AggressiveIfCornered) _state.SetFlag(FlagShove);
            // CP04: tras sobrevivir al primer cruce, solo si Víctor ya no le ve.
            if (previous == IntruderState.AggressiveIfCornered && next != IntruderState.AggressiveIfCornered) StartCoroutine(SaveWhenSafe("CP04"));
        }

        private IEnumerator SaveWhenSafe(string id)
        {
            var end = Time.time + 20f;
            while (Time.time < end)
            {
                if (!victor.isActiveAndEnabled || !victor.CanSeePlayer(18f, 90f))
                {
                    Checkpoint(id, "", once: true);
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ---------- Llamada a emergencias (móvil, tecla 3) ----------

        private void Update()
        {
            if (_callActive || _ending || GameServices.Flow == null || !GameServices.Flow.IsSimulating) return;
            if (!Danger || _state.Flag(FlagCalled)) return;
            var phone = director.Ui.Phone;
            phone.SetCallHint(true);
            if (phone.IsOpen && GameInput.Instance.Choices[2].WasPressedThisFrame()) StartCoroutine(EmergencyCall());
        }

        private IEnumerator EmergencyCall()
        {
            _callActive = true;
            _state.SetFlag(FlagCalled);
            var ui = director.Ui;
            ui.Phone.SetCallHint(false);
            ui.Phone.SetOpen(false);
            var start = Time.time;
            var outside = street.PlayerInside || !house.PlayerInside && !backyard.PlayerInside && !shed.PlayerInside;
            var lines = new List<DialogueLine>
            {
                new DialogueLine("Emergencias", "Emergencias, ¿qué le ocurre?"),
                new DialogueLine("Daniel", "Hay alguien en la casa de mi madre. Estaba viviendo en el cobertizo."),
                new DialogueLine("Emergencias", "¿Está usted a salvo ahora mismo?"),
                outside ? new DialogueLine("Daniel", "Estoy en la calle, delante de la casa.") : new DialogueLine("Daniel", "Sigo dentro. Creo que está en el patio."),
                outside
                    ? new DialogueLine("Emergencias", "Quédese ahí, en un sitio visible. Le mando una patrulla, puede tardar unos minutos.")
                    : new DialogueLine("Emergencias", "Si puede salir sin cruzarse con él, salga a la calle y espere. Le mando una patrulla."),
                new DialogueLine("Emergencias", "¿Cuál es la dirección?"),
                new DialogueLine("Daniel", "Calle Los Álamos, 27. La casa del porche."),
                new DialogueLine("Emergencias", "Bien. No vuelva a entrar. Si cambia algo, llame otra vez."),
            };
            foreach (var line in lines)
            {
                ui.SetCall(true, "Emergencias", $"{(int)(Time.time - start) / 60:00}:{(int)(Time.time - start) % 60:00}");
                ui.ShowSubtitle(line);
                var end = Time.time + line.Duration + 0.4f;
                while (Time.time < end)
                {
                    ui.SetCall(true, "Emergencias", $"{(int)(Time.time - start) / 60:00}:{(int)(Time.time - start) % 60:00}");
                    yield return null;
                }
            }
            ui.SetCall(false, "Emergencias", string.Empty);
            _callActive = false;
            if (street.PlayerInside) TryEnd(true);
        }

        // ---------- Finales ----------

        private void OnCar()
        {
            if (!HasEverything && !Danger)
            {
                director.Ui.ShowSubtitle(new DialogueLine(string.Empty, "Todavía falta la carpeta. Y el cargador."));
                return;
            }
            TryEnd(true);
        }

        private void TryEnd(bool byCar)
        {
            if (_ending) return;
            // Salir a la calle solo termina el episodio si hay motivo: tener las cosas o haber visto peligro.
            if (!byCar && !Danger) return;
            if (!byCar && victor.State == IntruderState.Warning) return;
            _ending = true;
            director.Ending(Epilogue());
        }

        private string Branch()
        {
            if (_state.Flag(FlagShove)) return "supervivencia";
            if (_state.Flag(FlagEncounter)) return "retirada";
            return Danger ? "prudente" : "temprana";
        }

        private IEnumerator Epilogue()
        {
            var branch = Branch();
            var lines = new List<DialogueLine>
            {
                new DialogueLine("Elena", "¿Sacaste la ropa?"),
                new DialogueLine("Daniel", _state.Has(ArrivalNarrative.Folder) ? "Sí. Y los papeles." : "La ropa sí. Los papeles los traigo mañana."),
                new DialogueLine("Elena", "¿Revisaron la casa?"),
            };
            if (branch == "temprana")
            {
                lines.Add(new DialogueLine("Daniel", "Todavía no. La puerta de atrás sigue sin enganchar."));
                lines.Add(new DialogueLine("Elena", "Yo te decía que había alguien."));
                lines.Add(new DialogueLine("Daniel", "Mañana voy con Lucía y la arreglamos."));
            }
            else
            {
                lines.Add(new DialogueLine("Daniel", "Sí, ma. La policía ya fue."));
                lines.Add(new DialogueLine("Elena", "Yo te decía que había alguien."));
                lines.Add(new DialogueLine("Daniel", "Sí. Tenías razón."));
                lines.Add(new DialogueLine("Elena", "Pensé que no me creían."));
                lines.Add(new DialogueLine("Daniel", "No te escuchamos. Perdón."));
            }
            var closing = branch switch
            {
                "supervivencia" => "Daniel salió por la casa mientras Víctor escapaba por el portón. La policía no lo encontró esa noche. En el cobertizo había ropa, envases y señales de uso prolongado. Elena se recuperó de la caída. La familia cambió las cerraduras y reforzó la puerta de servicio.",
                "retirada" => "Víctor se fue con su mochila por el portón. Daniel avisó a la policía desde la calle. En el cobertizo encontraron ropa, envases y señales de uso prolongado. Elena se recuperó de la caída. La familia cambió las cerraduras y reforzó el acceso.",
                "prudente" => "Daniel se fue sin entrar en el cobertizo y avisó desde el coche. Cuando llegó la patrulla, en el cobertizo había ropa, envases y señales de uso prolongado. Elena se recuperó de la caída. La familia cambió las cerraduras.",
                _ => "Esa noche nadie revisó el cobertizo. Elena se recuperó de la caída. Lo que había detrás de la casa siguió allí unos días más.",
            };
            var ui = director.Ui;
            yield return ui.PlayEpilogue("CUATRO DÍAS DESPUÉS", lines, closing);
        }

        // ---------- Utilidades ----------

        private readonly HashSet<string> _saved = new HashSet<string>();

        private void Checkpoint(string id, string objective, bool once)
        {
            if (once && _saved.Contains(id)) return;
            // Nunca en pleno peligro ni a la vista de Víctor (SISTEMAS §Checkpoints).
            var s = victor.State;
            if (s == IntruderState.Warning || s == IntruderState.Searching || s == IntruderState.AggressiveIfCornered || s == IntruderState.Aware) return;
            if (victor.isActiveAndEnabled && victor.CanSeePlayer()) return;
            _saved.Add(id);
            director.SaveCheckpoint(id, objective);
        }

        private void Think(string line) => director.Ui.ShowSubtitle(new DialogueLine(string.Empty, line));

        public void RestoreSaved(Snapshot snapshot)
        {
            if (snapshot == null) return;
            _saved.Clear();
            foreach (var id in new[] { "CP00", "CP01", "CP02", "CP03", "CP04" })
            {
                _saved.Add(id);
                if (id == snapshot.CheckpointId) break;
            }
        }

        private void Awake()
        {
            director.Started += RestoreSaved;
        }
    }
}
