using System;
using System.Collections;
using BeforeItWasYou.Core.Flow;
using BeforeItWasYou.Core.Save;
using BeforeItWasYou.Input;
using BeforeItWasYou.Interaction;
using BeforeItWasYou.Player;
using BeforeItWasYou.Services;
using BeforeItWasYou.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeforeItWasYou.Episode
{
    /// <summary>
    /// Orquesta el episodio en L_E01_Arrival: menú sobre la casa, prólogo, llegada, pausa, continuar desde
    /// checkpoint y vuelta al menú. La narrativa concreta se engancha a los eventos Started y CheckpointRequested.
    /// </summary>
    public sealed class EpisodeDirector : MonoBehaviour
    {
        private static readonly DialogueLine[] Prologue =
        {
            new DialogueLine("Elena", "La puerta del lavadero volvió a quedar abierta."),
            new DialogueLine("Daniel", "¿La de atrás? A veces no engancha bien."),
            new DialogueLine("Elena", "Ya sé. Pero ayer la cerré."),
            new DialogueLine("Daniel", "El sábado paso y la reviso, ¿sí?"),
            new DialogueLine("Elena", "Bueno, hijo."),
        };

        [SerializeField] private GameUi ui;
        [SerializeField] private PlayerController player;
        [SerializeField] private Interactor interactor;
        [SerializeField] private MenuCameraRig menuCamera;
        [SerializeField] private Transform arrivalSpawn;
        [SerializeField] private AudioSource prologueAudio;

        private bool _transitioning;

        /// <summary>El jugador toma el control (partida nueva o continuada). El argumento es el snapshot cargado o null.</summary>
        public event Action<Snapshot> Started;

        public static EpisodeDirector Current { get; private set; }

        public PlayerController Player => player;
        public GameUi Ui => ui;
        public bool IsTransitioning => _transitioning;

        public void Configure(GameUi gameUi, PlayerController daniel, Interactor danielInteractor, MenuCameraRig rig, Transform spawn, AudioSource prologueSource)
        {
            ui = gameUi;
            player = daniel;
            interactor = danielInteractor;
            menuCamera = rig;
            arrivalSpawn = spawn;
            prologueAudio = prologueSource;
        }

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void Start()
        {
            GameServices.Initialize();
            if (interactor != null) interactor.FocusChanged += focus => ui.SetPrompt(focus != null ? focus.Prompt : null);
            var flow = GameServices.Flow;
            if (flow.State == GameState.Boot) flow.Enter(GameState.Menu);
            if (flow.State != GameState.Menu)
            {
                // Recarga desde pausa o fallo: siempre se vuelve al menú con un estado limpio.
                flow.TryEnter(GameState.Menu);
            }
            if (ContinueOnLoad && GameServices.Saves.HasContinuableSave())
            {
                ContinueOnLoad = false;
                SetPlayerActive(false);
                ui.HideFrontEnd();
                ui.SetBlack(true);
                StartCoroutine(ContinueRoutine(GameServices.Saves.Load().Snapshot));
                return;
            }
            ContinueOnLoad = false;
            EnterMenu(true);
        }

        private void Update()
        {
            var flow = GameServices.Flow;
            if (flow == null || _transitioning) return;
            if (flow.State == GameState.Playing && GameInput.Instance.Pause.WasPressedThisFrame()) Pause();
        }

        // ---------- Menú ----------

        private void EnterMenu(bool fadeIn)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SetPlayerActive(false);
            SetCursor(true);
            GameInput.Instance.UsePlayer(false);
            ui.ShowMainMenu(GameServices.Saves.HasContinuableSave(), ContinueGame, RequestNewGame, RequestQuit);
            if (fadeIn)
            {
                ui.SetBlack(true);
                StartCoroutine(ui.Fade(false, 1.6f));
            }
        }

        private void RequestNewGame()
        {
            if (GameServices.Saves.HasContinuableSave())
            {
                ui.Confirm("¿EMPEZAR DE NUEVO?", "Se borrará la partida guardada y empezarás el episodio desde el principio.",
                    "EMPEZAR", "CANCELAR", () => StartCoroutine(NewGameRoutine(false)));
                return;
            }
            StartCoroutine(NewGameRoutine(false));
        }

        private void RequestQuit()
        {
            ui.Confirm("¿SALIR DEL JUEGO?", "Tu progreso se guarda en los puntos de control.", "SALIR", "CANCELAR", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        /// <summary>Nueva partida: prólogo (opcional en tests) y llegada en coche.</summary>
        public IEnumerator NewGameRoutine(bool skipPrologue)
        {
            if (_transitioning) yield break;
            _transitioning = true;
            GameServices.Saves.DeleteAll();
            yield return ui.Fade(true, 1.1f);
            ui.HideFrontEnd();
            if (!skipPrologue)
            {
                if (prologueAudio != null) prologueAudio.Play();
                yield return ui.PlayPrologue("DÍAS ANTES", Prologue);
                if (prologueAudio != null) prologueAudio.Stop();
            }
            yield return BeginPlay(arrivalSpawn.position, arrivalSpawn.eulerAngles.y, 0f, null);
        }

        private void ContinueGame()
        {
            var result = GameServices.Saves.Load();
            if (!result.HasSnapshot)
            {
                ui.Confirm("NO SE PUDO CARGAR", "La partida guardada está dañada y no hay copia de seguridad válida.", "ACEPTAR", "VOLVER",
                    () => EnterMenu(false), () => EnterMenu(false));
                return;
            }
            StartCoroutine(ContinueRoutine(result.Snapshot));
        }

        private IEnumerator ContinueRoutine(Snapshot snapshot)
        {
            if (_transitioning) yield break;
            _transitioning = true;
            yield return ui.Fade(true, 1.1f);
            ui.HideFrontEnd();
            var p = snapshot.PlayerPosition;
            yield return BeginPlay(new Vector3(p.X, p.Y, p.Z), snapshot.PlayerYaw, snapshot.PlayerPitch, snapshot);
        }

        private IEnumerator BeginPlay(Vector3 position, float yaw, float pitch, Snapshot snapshot)
        {
            SetPlayerActive(true);
            player.Teleport(position, yaw, pitch);
            if (snapshot != null) player.SetCrouched(snapshot.PlayerCrouched);
            GameServices.Flow.Enter(GameState.Playing);
            GameInput.Instance.UsePlayer(true);
            SetCursor(false);
            ui.ShowHud(true);
            Started?.Invoke(snapshot);
            yield return ui.Fade(false, 1.8f);
            _transitioning = false;
        }

        // ---------- Pausa ----------

        public void Pause()
        {
            if (!GameServices.Flow.TryEnter(GameState.Paused)) return;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            GameInput.Instance.UsePlayer(false);
            SetCursor(true);
            ui.ShowPause(Resume, ReturnToMenu);
        }

        public void Resume()
        {
            if (!GameServices.Flow.TryEnter(GameState.Playing)) return;
            ui.HidePause();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameInput.Instance.UsePlayer(true);
            SetCursor(false);
        }

        private void ReturnToMenu() => StartCoroutine(ReturnToMenuRoutine());

        private IEnumerator ReturnToMenuRoutine()
        {
            _transitioning = true;
            ui.HidePause();
            ui.ShowHud(false);
            yield return ui.Fade(true, 0.8f);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameServices.Flow.TryEnter(GameState.Menu);
            SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
        }

        // ---------- Fallo, reintento y final ----------

        /// <summary>Muerte por contacto confirmado: breve corte, pantalla sobria y reintento rápido.</summary>
        public void Fail(string title, string text)
        {
            if (_transitioning || !GameServices.Flow.TryEnter(GameState.Failed)) return;
            StartCoroutine(FailRoutine(title, text));
        }

        private IEnumerator FailRoutine(string title, string text)
        {
            _transitioning = true;
            player.Lock("fallo");
            GameInput.Instance.UsePlayer(false);
            yield return ui.Fade(true, 0.35f);
            Time.timeScale = 0f;
            SetCursor(true);
            ui.SetBlack(false);
            ui.ShowFail(title, text, Retry, () => StartCoroutine(ReturnToMenuRoutine()));
            _transitioning = false;
        }

        /// <summary>Recarga la escena y continúa desde el último checkpoint sin pasar por el menú.</summary>
        public void Retry()
        {
            ui.HideFail();
            Time.timeScale = 1f;
            ContinueOnLoad = true;
            GameServices.Flow.TryEnter(GameState.Menu);
            SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
        }

        /// <summary>Al recargar tras un fallo se entra directo a la partida guardada.</summary>
        public static bool ContinueOnLoad;

        public void Ending(IEnumerator epilogue)
        {
            if (_transitioning || !GameServices.Flow.TryEnter(GameState.Epilogue)) return;
            StartCoroutine(EndingRoutine(epilogue));
        }

        private IEnumerator EndingRoutine(IEnumerator epilogue)
        {
            _transitioning = true;
            player.Lock("final");
            GameInput.Instance.UsePlayer(false);
            yield return ui.Fade(true, 1.6f);
            ui.ShowHud(false);
            yield return epilogue;
            GameServices.Flow.TryEnter(GameState.Credits);
            yield return ui.PlayCreditsRoll(GameUi.CreditBlocks());
            // El episodio terminado ya no se puede continuar.
            GameServices.Saves.DeleteAll();
            GameServices.Flow.TryEnter(GameState.Menu);
            SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
        }

        // ---------- Checkpoints ----------

        /// <summary>Guarda el estado del jugador. Los sistemas del mundo completan el snapshot vía SnapshotRequested.</summary>
        public void SaveCheckpoint(string checkpointId, string objective)
        {
            var snapshot = new Snapshot
            {
                CheckpointId = checkpointId,
                Scene = gameObject.scene.name,
                PlayerPosition = new Vector3Data(player.transform.position.x, player.transform.position.y, player.transform.position.z),
                PlayerYaw = player.Yaw,
                PlayerPitch = player.Pitch,
                PlayerCrouched = player.IsCrouched,
                Objective = objective ?? string.Empty,
                EpisodeSeconds = Time.timeSinceLevelLoad,
            };
            SnapshotRequested?.Invoke(snapshot);
            GameServices.Saves.Save(snapshot, DateTime.UtcNow);
        }

        public event Action<Snapshot> SnapshotRequested;

        private void SetPlayerActive(bool active)
        {
            if (player != null) player.gameObject.SetActive(active);
            if (menuCamera != null) menuCamera.gameObject.SetActive(!active);
            if (!active) ui.SetPrompt(null);
        }

        private static void SetCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
