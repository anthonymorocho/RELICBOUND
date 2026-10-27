using System;
using System.Collections;
using System.Collections.Generic;
using BeforeItWasYou.Input;
using BeforeItWasYou.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeforeItWasYou.UI
{
    public readonly struct DialogueLine
    {
        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }

        public string Speaker { get; }
        public string Text { get; }

        /// <summary>Tiempo de lectura cómodo: base más ritmo por carácter.</summary>
        public float Duration => 1.4f + Text.Length * 0.058f;
    }

    /// <summary>
    /// Toda la interfaz del episodio en un único documento: menú principal, opciones, créditos, confirmaciones,
    /// pausa, prólogo, fundidos y HUD. Solo la lista de la capa superior recibe la entrada.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameUi : MonoBehaviour
    {
        private const string Hidden = "hidden";

        [SerializeField] private TextAsset credits;

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _frontEnd;
        private VisualElement _main;
        private VisualElement _footer;
        private VisualElement _pause;
        private VisualElement _prologue;
        private VisualElement _modal;
        private VisualElement _fade;
        private VisualElement _hud;
        private VisualElement _crosshair;
        private VisualElement _prompt;
        private Label _promptText;
        private VisualElement _objective;
        private Label _objectiveText;
        private VisualElement _toast;
        private Label _toastText;
        private Label _hudSubtitle;

        private readonly MenuList _mainList = new MenuList();
        private readonly MenuList _pauseList = new MenuList(false);
        private readonly MenuList _modalList = new MenuList(true);
        private OptionsScreen _options;
        private CreditsScreen _credits;
        private bool _skipRequested;
        private Coroutine _objectiveRoutine;
        private Coroutine _toastRoutine;
        private Coroutine _subtitleRoutine;

        public bool IsModalOpen => _ready && !_modal.ClassListContains(Hidden);
        public bool IsPauseOpen => _ready && !_pause.ClassListContains(Hidden);
        public bool IsBusyWithMenus => _ready && (IsModalOpen || _options.IsOpen || _credits.IsOpen);
        public VisualElement Root => _root;

        private PhoneView _phoneView;

        public PhoneView Phone
        {
            get
            {
                EnsureReady();
                return _phoneView;
            }
        }

        private bool _ready;

        private void OnEnable() => EnsureReady();

        /// <summary>El árbol del UIDocument existe tras su OnEnable; se inicializa en el primer uso.</summary>
        private void EnsureReady()
        {
            if (_ready) return;
            _document = GetComponent<UIDocument>();
            if (_document.rootVisualElement == null || _document.rootVisualElement.Q("root") == null) return;
            _ready = true;
            _root = _document.rootVisualElement.Q("root");
            _frontEnd = _root.Q("frontend");
            _main = _root.Q("main");
            _footer = _root.Q("footer");
            _pause = _root.Q("pause");
            _prologue = _root.Q("prologue");
            _modal = _root.Q("modal");
            _fade = _root.Q("fade");
            _hud = _root.Q("hud");
            _crosshair = _root.Q("crosshair");
            _prompt = _root.Q("prompt");
            _promptText = _root.Q<Label>("prompt-text");
            _objective = _root.Q("objective");
            _objectiveText = _root.Q<Label>("objective-text");
            _toast = _root.Q("toast");
            _toastText = _root.Q<Label>("toast-text");
            _hudSubtitle = _root.Q<Label>("hud-subtitle");
            _phoneView = new PhoneView(_root);
            _options = new OptionsScreen(_root.Q("options"));
            _credits = new CreditsScreen(_root.Q("credits"));
            if (credits != null) CreditsScreen.SetExtraBlocks(CreditsScreen.Parse(credits.text));
            _options.List.SelectionChanged += entry => SetAdjustHint(entry.Adjust != null);
            _root.Q<Label>("version").text = "v" + Application.version + "  ·  Episodio 01";
            SettingsApplier.Applied += ApplySubtitleScale;
            if (GameServices.IsReady) ApplySubtitleScale(GameServices.Settings);
        }

        private void OnDestroy() => SettingsApplier.Applied -= ApplySubtitleScale;

        private void Update()
        {
            EnsureReady();
            if (!_ready) return;
            var input = GameInput.Instance;
            var list = ActiveList();
            list?.Tick(input, Time.unscaledTime);
            if (!_prologue.ClassListContains(Hidden) && (input.Submit.WasPressedThisFrame() || input.Cancel.WasPressedThisFrame()))
            {
                _skipRequested = true;
            }
        }

        private MenuList ActiveList()
        {
            if (IsModalOpen) return _modalList;
            if (_options.IsOpen) return _options.List;
            if (_credits.IsOpen) return _credits.List;
            if (IsPauseOpen) return _pauseList;
            if (!_frontEnd.ClassListContains(Hidden) && !_main.ClassListContains(Hidden)) return _mainList;
            return null;
        }

        // ---------- Menú principal ----------

        public void ShowMainMenu(bool canContinue, Action onContinue, Action onNewGame, Action onQuit)
        {
            EnsureReady();
            _hud.AddToClassList(Hidden);
            _frontEnd.RemoveFromClassList(Hidden);
            _main.RemoveFromClassList(Hidden);
            _footer.RemoveFromClassList(Hidden);
            SetAdjustHint(false);
            var container = _root.Q("main-list");
            container.Clear();
            _mainList.Clear();
            if (canContinue) AddMain(container, "CONTINUAR", onContinue);
            AddMain(container, "NUEVA PARTIDA", onNewGame);
            AddMain(container, "OPCIONES", () => OpenOptions(() => ShowMain(true)));
            AddMain(container, "CRÉDITOS", () =>
            {
                ShowMain(false);
                _credits.Open(() => ShowMain(true));
                SetAdjustHint(false);
            });
            AddMain(container, "SALIR", onQuit);
            _mainList.SelectFirst();
            _main.AddToClassList("menu--away");
            _main.schedule.Execute(() => _main.RemoveFromClassList("menu--away")).StartingIn(30);
        }

        public void HideFrontEnd()
        {
            EnsureReady();
            _frontEnd.AddToClassList(Hidden);
        }

        private void AddMain(VisualElement container, string text, Action submit)
        {
            var item = MenuList.CreateItem(text);
            container.Add(item);
            _mainList.Add(item, submit);
        }

        private void ShowMain(bool visible)
        {
            _main.EnableInClassList(Hidden, !visible);
            if (visible) SetAdjustHint(false);
        }

        private void OpenOptions(Action onClose)
        {
            ShowMain(false);
            _frontEnd.RemoveFromClassList(Hidden);
            _options.Open(() =>
            {
                SetAdjustHint(false);
                onClose();
            });
        }

        private void SetAdjustHint(bool visible)
        {
            _root.Q("footer-adjust-key").EnableInClassList(Hidden, !visible);
            _root.Q("footer-adjust").EnableInClassList(Hidden, !visible);
            // Esc solo tiene sentido dentro de un submenú (Opciones, Créditos).
            var inSubmenu = _options.IsOpen || _credits.IsOpen;
            _root.Q("footer-back-key").EnableInClassList(Hidden, !inSubmenu);
            _root.Q("footer-back").EnableInClassList(Hidden, !inSubmenu);
        }

        // ---------- Confirmaciones ----------

        public void Confirm(string title, string text, string accept, string cancel, Action onAccept, Action onCancel = null)
        {
            EnsureReady();
            _modal.RemoveFromClassList(Hidden);
            _root.Q<Label>("modal-title").text = title;
            _root.Q<Label>("modal-text").text = text;
            var container = _root.Q("modal-list");
            container.Clear();
            _modalList.Clear();
            void Close() => _modal.AddToClassList(Hidden);
            var no = MenuList.CreateItem(cancel);
            container.Add(no);
            _modalList.Add(no, () =>
            {
                Close();
                onCancel?.Invoke();
            });
            var yes = MenuList.CreateItem(accept);
            container.Add(yes);
            _modalList.Add(yes, () =>
            {
                Close();
                onAccept();
            });
            _modalList.Cancel = () =>
            {
                Close();
                onCancel?.Invoke();
            };
            _modalList.SelectFirst();
        }

        // ---------- Pausa ----------

        public void ShowPause(Action onResume, Action onMainMenu)
        {
            EnsureReady();
            _pause.RemoveFromClassList(Hidden);
            _hud.AddToClassList(Hidden);
            var container = _root.Q("pause-list");
            container.Clear();
            _pauseList.Clear();
            AddPause(container, "CONTINUAR", onResume);
            AddPause(container, "OPCIONES", () =>
            {
                _pause.AddToClassList(Hidden);
                _main.AddToClassList(Hidden);
                _footer.RemoveFromClassList(Hidden);
                _options.Open(() =>
                {
                    _frontEnd.AddToClassList(Hidden);
                    _pause.RemoveFromClassList(Hidden);
                });
            });
            AddPause(container, "MENÚ PRINCIPAL", () => Confirm(
                "¿VOLVER AL MENÚ?",
                "Perderás lo que hayas hecho desde el último punto de guardado.",
                "VOLVER AL MENÚ", "CANCELAR", onMainMenu));
            _pauseList.Cancel = onResume;
            _pauseList.SelectFirst();
        }

        public void HidePause()
        {
            EnsureReady();
            _pause.AddToClassList(Hidden);
            _modal.AddToClassList(Hidden);
            if (_options.IsOpen) _options.Close();
            _frontEnd.AddToClassList(Hidden);
            _hud.RemoveFromClassList(Hidden);
        }

        private void AddPause(VisualElement container, string text, Action submit)
        {
            var item = MenuList.CreateItem(text, "pause-item");
            container.Add(item);
            _pauseList.Add(item, submit);
        }

        // ---------- Fundidos y prólogo ----------

        public IEnumerator Fade(bool toBlack, float seconds)
        {
            EnsureReady();
            _fade.style.transitionDuration = new List<TimeValue> { new TimeValue(seconds, TimeUnit.Second) };
            _fade.EnableInClassList("fade--on", toBlack);
            var end = Time.unscaledTime + seconds + 0.05f;
            while (Time.unscaledTime < end) yield return null;
        }

        public void SetBlack(bool black)
        {
            EnsureReady();
            _fade.style.transitionDuration = new List<TimeValue> { new TimeValue(0f, TimeUnit.Second) };
            _fade.EnableInClassList("fade--on", black);
        }

        public IEnumerator PlayPrologue(string card, IReadOnlyList<DialogueLine> lines, Action<int> onLine = null)
        {
            EnsureReady();
            _skipRequested = false;
            _frontEnd.AddToClassList(Hidden);
            _prologue.RemoveFromClassList(Hidden);
            var cardLabel = _root.Q<Label>("prologue-card");
            var subtitle = _root.Q<Label>("prologue-subtitle");
            cardLabel.text = card;
            subtitle.RemoveFromClassList("subtitle--on");
            yield return Fade(false, 0.4f);
            cardLabel.AddToClassList("card--on");
            if (!_skipRequested) yield return Wait(2.6f);
            cardLabel.RemoveFromClassList("card--on");
            if (!_skipRequested) yield return Wait(1.4f);
            for (var i = 0; i < lines.Count && !_skipRequested; i++)
            {
                onLine?.Invoke(i);
                subtitle.text = Format(lines[i]);
                subtitle.AddToClassList("subtitle--on");
                yield return Wait(lines[i].Duration);
                subtitle.RemoveFromClassList("subtitle--on");
                yield return Wait(0.45f);
            }
            if (!_skipRequested) yield return Wait(1.2f);
            subtitle.RemoveFromClassList("subtitle--on");
            yield return Fade(true, 0.6f);
            _prologue.AddToClassList(Hidden);
        }

        private IEnumerator Wait(float seconds)
        {
            var end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end && !_skipRequested) yield return null;
        }

        public static string Format(DialogueLine line) =>
            string.IsNullOrEmpty(line.Speaker) ? line.Text : "<color=#B9B2A6>" + line.Speaker + ":</color>  " + line.Text;

        // ---------- HUD ----------

        public void ShowHud(bool visible)
        {
            EnsureReady();
            _hud.EnableInClassList(Hidden, !visible);
        }

        public void SetPrompt(string text)
        {
            EnsureReady();
            var on = !string.IsNullOrEmpty(text);
            if (on) _promptText.text = text;
            _prompt.EnableInClassList("prompt--on", on);
            _crosshair.EnableInClassList("crosshair--focus", on);
        }

        public void ShowObjective(string text, float seconds = 6f)
        {
            EnsureReady();
            if (_objectiveRoutine != null) StopCoroutine(_objectiveRoutine);
            _objectiveRoutine = StartCoroutine(TimedClass(_objective, "objective--on", seconds, () => _objectiveText.text = text));
        }

        public void ShowToast(string text, float seconds = 5f)
        {
            EnsureReady();
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(TimedClass(_toast, "toast--on", seconds, () => _toastText.text = text));
        }

        public void ShowSubtitle(DialogueLine line)
        {
            EnsureReady();
            if (_subtitleRoutine != null) StopCoroutine(_subtitleRoutine);
            _subtitleRoutine = StartCoroutine(TimedClass(_hudSubtitle, "subtitle--on", line.Duration, () => _hudSubtitle.text = Format(line)));
        }

        private static IEnumerator TimedClass(VisualElement element, string cssClass, float seconds, Action setup)
        {
            setup();
            element.AddToClassList(cssClass);
            yield return new WaitForSeconds(seconds);
            element.RemoveFromClassList(cssClass);
        }

        private void ApplySubtitleScale(Core.Settings.GameSettings settings)
        {
            var size = 30f * settings.SubtitleScale;
            if (_hudSubtitle != null) _hudSubtitle.style.fontSize = size;
            var prologueSubtitle = _root?.Q<Label>("prologue-subtitle");
            if (prologueSubtitle != null) prologueSubtitle.style.fontSize = size;
        }
    }
}
