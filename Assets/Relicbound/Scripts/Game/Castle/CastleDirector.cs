using System.Collections;
using System.Collections.Generic;
using Relicbound.Core.Boot;
using Relicbound.Core.Economy;
using Relicbound.Core.Save;
using Relicbound.Game.Audio;
using Relicbound.Game.Boot;
using Relicbound.Game.CameraControl;
using Relicbound.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Relicbound.Game.Castle
{
    /// <summary>
    /// Pantalla principal = castillo del jugador (docs/01, docs/04). Orquesta perfil → introducción → castillo
    /// con HUD, cámara, selección de edificios y ajustes. No hay menú principal.
    /// </summary>
    public sealed class CastleDirector : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private CastleCameraRig cameraRig;
        [SerializeField] private SelectionController selection;
        [SerializeField] private CastleAudio audioPlayer;
        [SerializeField] private Texture2D goldIcon;
        [SerializeField] private Texture2D woodIcon;
        [SerializeField] private Texture2D stoneIcon;
        [SerializeField] private Texture2D ironIcon;
        [SerializeField] private Texture2D gemsIcon;
        [SerializeField] private float introShotSeconds = 4.5f;

        private GameRoot _root;
        private ProfileView _profile;
        private IntroView _intro;
        private HudView _hud;
        private BuildingPanelView _buildingPanel;
        private SettingsView _settings;
        private NoticeView _notice;
        private TutorialView _tutorialView;
        private ProfilePanelView _profilePanel;
        private TutorialFlow _tutorial;
        private BuildingView _keep;
        private bool _skipIntro;

        public static CastleDirector Instance { get; private set; }

        public ProfileView Profile => _profile;

        public IntroView Intro => _intro;

        public SettingsView Settings => _settings;

        public SelectionController Selection => selection;

        public BootStage Stage => _root != null ? _root.Flow.Stage : BootStage.Splash;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            _root = GameRoot.Ensure();
            // Abrir Castle directamente (editor) recorre el arranque sin splash.
            _root.LoadAndAdvance();

            var ui = document.rootVisualElement;
            _profile = new ProfileView(ui);
            _intro = new IntroView(ui);
            _hud = new HudView(ui, new Dictionary<ResourceType, Texture2D>
            {
                { ResourceType.Gold, goldIcon },
                { ResourceType.Wood, woodIcon },
                { ResourceType.Stone, stoneIcon },
                { ResourceType.Iron, ironIcon },
                { ResourceType.Gems, gemsIcon },
            });
            _buildingPanel = new BuildingPanelView(ui);
            _settings = new SettingsView(ui);
            _notice = new NoticeView(ui);
            _tutorialView = new TutorialView(ui);
            _profilePanel = new ProfilePanelView(ui);
            foreach (var view in FindObjectsByType<BuildingView>(FindObjectsSortMode.None))
            {
                if (view.Definition != null && view.Definition.id == "keep") _keep = view;
            }
            ui.Q("profile-badge").RegisterCallback<ClickEvent>(_ => OpenProfilePanel());
            _profilePanel.Closed += CloseProfilePanel;
            _profilePanel.BannerPreviewed += id =>
            {
                BannerCloth.DressAll(id);
                audioPlayer.Play(UiSound.Banner);
            };
            _profilePanel.Saved += OnProfileEdited;
            cameraRig.Panned += () => AdvanceTutorial(TutorialSignal.CameraPanned);
            cameraRig.Zoomed += () => AdvanceTutorial(TutorialSignal.CameraZoomed);

            _profile.Submitted += OnProfileSubmitted;
            _profile.BannerChanged += BannerCloth.DressAll;
            _profile.BannerChanged += _ => audioPlayer.Play(UiSound.Banner);
            _profile.Rejected += () => audioPlayer.Play(UiSound.Error);
            _settings.Changed += audioPlayer.ApplyVolumes;
            _settings.Changed += _ => audioPlayer.Play(UiSound.Tick);
            // Clic genérico para cualquier botón de la interfaz.
            ui.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is Button) audioPlayer.Play(UiSound.Click);
            }, TrickleDown.TrickleDown);
            _intro.SkipRequested += () => _skipIntro = true;
            _hud.SettingsRequested += OpenSettings;
            _settings.Changed += QualityApplier.Apply;
            _settings.Closed += CloseSettings;
            _buildingPanel.CloseRequested += () => selection.Select(null);
            selection.SelectionChanged += OnSelectionChanged;
            cameraRig.Tapped += OnTapped;
            cameraRig.IsPointerOverUi = screen => UiUtil.IsOverInteractive(ui, screen);

            if (_root.Session != null)
            {
                QualityApplier.Apply(_root.Session.Settings);
                audioPlayer.ApplyVolumes(_root.Session.Settings);
            }
            ReportLoadIssues(_root.Flow.LastLoad);
            EnterStage(_root.Flow.Stage);
        }

        private void EnterStage(BootStage stage)
        {
            switch (stage)
            {
                case BootStage.ProfileCreation:
                    cameraRig.Mode = CameraMode.Orbit;
                    cameraRig.SnapTo(Vector3.zero, -35f, 62f);
                    _profile.Show(true);
                    break;
                case BootStage.Intro:
                    _profile.Show(false);
                    StartCoroutine(PlayIntro());
                    break;
                case BootStage.Castle:
                    BannerCloth.DressAll(_root.Session.Profile.BannerId);
                    _profile.Show(false);
                    _intro.Show(false);
                    cameraRig.Mode = CameraMode.Interactive;
                    cameraRig.FrameCastle();
                    _hud.Bind(_root.Session);
                    _hud.Show(true);
                    StartTutorial();
                    break;
                case BootStage.UpdateRequired:
                    _notice.Show("Esta partida es de una versión más nueva del juego. Actualiza RELICBOUND para continuar.", 600000);
                    break;
            }
        }

        private void OnProfileSubmitted(string playerName, string castleName, string bannerId)
        {
            _root.CreateProfile(playerName, castleName, bannerId);
            BannerCloth.DressAll(bannerId);
            QualityApplier.Apply(_root.Session.Settings);
            audioPlayer.ApplyVolumes(_root.Session.Settings);
            audioPlayer.Play(UiSound.Confirm);
            audioPlayer.Play(UiSound.Coins);
            EnterStage(_root.Flow.Stage);
        }

        private IEnumerator PlayIntro()
        {
            _skipIntro = false;
            cameraRig.Mode = CameraMode.Cinematic;
            _intro.Show(true);
            var castle = _root.Session != null ? _root.Session.Profile.CastleName : "tu fortaleza";
            var shots = new[]
            {
                (new Vector3(0f, 0f, 2.5f), 40f, -20f, "La Frontera Verde",
                    "Bajo estas tierras duermen las reliquias de los reinos antiguos. Quien las custodia, gobierna."),
                (new Vector3(-8f, 0f, 4f), 34f, 55f, castle,
                    "Tus murallas son la primera defensa. Cada mejora las hará más altas, más gruesas y más temidas."),
                (new Vector3(0f, 0f, -11f), 30f, -10f, "Se acercan los asedios",
                    "Prepara tus máquinas en el taller y conquista las fortalezas rivales piedra a piedra."),
            };
            foreach (var (pivot, distance, yaw, title, text) in shots)
            {
                if (_skipIntro) break;
                cameraRig.FocusOn(pivot, distance, yaw);
                _intro.ShowCard(title, text);
                var elapsed = 0f;
                while (elapsed < introShotSeconds && !_skipIntro)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                _intro.HideCard();
                if (!_skipIntro) yield return new WaitForSecondsRealtime(0.45f);
            }
            _intro.Show(false);
            _root.FinishIntro();
            EnterStage(_root.Flow.Stage);
        }

        private void StartTutorial()
        {
            _tutorial = new TutorialFlow(_root.Session.Tutorial);
            if (_tutorial.IsDone) return;
            ShowTutorialStep();
        }

        private void ShowTutorialStep()
        {
            _tutorialView.Show(_tutorial.Current, _tutorial.CompletedCount);
            selection.Hint(_tutorial.Current == TutorialStep.SelectKeep ? _keep : null);
        }

        /// <summary>Notifica una acción del jugador al tutorial (cámara, selección).</summary>
        public void AdvanceTutorial(TutorialSignal signal)
        {
            if (_tutorial == null || _tutorial.IsDone) return;
            var finished = _tutorial.Notify(signal);
            if (!finished)
            {
                ShowTutorialStep();
                _root.Save();
                return;
            }
            _tutorialView.Hide();
            foreach (var reward in _root.Rules.TutorialReward) _root.Session.Wallet.Add(reward.Key, reward.Value);
            _notice.Show($"¡Bien hecho! Recompensa: +{UiUtil.FormatAmount(_root.Rules.TutorialReward[ResourceType.Gold])} oro y +{UiUtil.FormatAmount(_root.Rules.TutorialReward[ResourceType.Wood])} madera.");
            audioPlayer.Play(UiSound.Coins);
            _root.Save();
        }

        public void OpenProfilePanel()
        {
            if (_root.Session == null || _root.Flow.Stage != BootStage.Castle) return;
            selection.Select(null);
            _profilePanel.Open(_root.Session);
            audioPlayer.Play(UiSound.PanelOpen);
        }

        private void CloseProfilePanel()
        {
            _profilePanel.Close();
            BannerCloth.DressAll(_root.Session.Profile.BannerId);
            audioPlayer.Play(UiSound.PanelClose);
        }

        private void OnProfileEdited(string castleName, string bannerId)
        {
            _root.Session.UpdateProfile(_root.Session.Profile.WithCastleName(castleName).WithBanner(bannerId));
            _root.Save();
            _hud.Bind(_root.Session);
            _profilePanel.Close();
            BannerCloth.DressAll(bannerId);
            audioPlayer.Play(UiSound.Confirm);
            _notice.Show("Cambios guardados.", 2500);
        }

        private void OnTapped(Vector2 screen)
        {
            if (_root.Flow.Stage != BootStage.Castle || _settings.IsOpen || _profilePanel.IsOpen) return;
            selection.PickAt(cameraRig.Camera, screen);
        }

        private void OnSelectionChanged(BuildingView view)
        {
            _buildingPanel.Show(view);
            if (view == null) return;
            audioPlayer.Play(UiSound.Select);
            if (view == _keep) AdvanceTutorial(TutorialSignal.KeepSelected);
            var bounds = view.WorldBounds;
            var distance = view.Definition != null ? view.Definition.focusDistance : 40f;
            // El edificio queda en el tercio superior: el panel inferior no lo tapa.
            cameraRig.FocusOn(new Vector3(bounds.center.x, 0f, bounds.center.z - distance * 0.12f), distance);
        }

        public void OpenSettings()
        {
            if (_root.Session == null) return;
            selection.Select(null);
            _settings.Open(_root.Session.Settings);
            audioPlayer.Play(UiSound.PanelOpen);
        }

        private void CloseSettings()
        {
            _settings.Close();
            audioPlayer.Play(UiSound.PanelClose);
            _root.Save();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (_settings.IsOpen) CloseSettings();
            else if (_profilePanel.IsOpen) CloseProfilePanel();
            else if (selection.Selected != null) selection.Select(null);
        }

        private void ReportLoadIssues(LoadResult load)
        {
            if (load == null) return;
            if (load.Status == LoadStatus.RecoveredFromBackup)
            {
                _notice.Show("Tu última partida estaba dañada. Se ha recuperado la copia de seguridad más reciente.");
            }
            else if (load.Status == LoadStatus.Corrupted)
            {
                _notice.Show("No se pudo leer la partida guardada. Se ha conservado una copia para soporte.");
            }
        }
    }
}
