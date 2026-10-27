using System.Collections;
using System.Collections.Generic;
using Relicbound.Core.Boot;
using Relicbound.Core.Economy;
using Relicbound.Core.Save;
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

            _profile.Submitted += OnProfileSubmitted;
            _profile.BannerChanged += BannerCloth.DressAll;
            _intro.SkipRequested += () => _skipIntro = true;
            _hud.SettingsRequested += OpenSettings;
            _settings.Changed += QualityApplier.Apply;
            _settings.Closed += CloseSettings;
            _buildingPanel.CloseRequested += () => selection.Select(null);
            selection.SelectionChanged += OnSelectionChanged;
            cameraRig.Tapped += OnTapped;
            cameraRig.IsPointerOverUi = screen => UiUtil.IsOverInteractive(ui, screen);

            if (_root.Session != null) QualityApplier.Apply(_root.Session.Settings);
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

        private void OnTapped(Vector2 screen)
        {
            if (_root.Flow.Stage != BootStage.Castle || _settings.IsOpen) return;
            selection.PickAt(cameraRig.Camera, screen);
        }

        private void OnSelectionChanged(BuildingView view)
        {
            _buildingPanel.Show(view);
            if (view == null) return;
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
        }

        private void CloseSettings()
        {
            _settings.Close();
            _root.Save();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (_settings.IsOpen) CloseSettings();
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
