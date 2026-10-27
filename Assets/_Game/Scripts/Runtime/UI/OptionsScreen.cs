using System;
using System.Collections.Generic;
using System.Globalization;
using BeforeItWasYou.Core.Settings;
using BeforeItWasYou.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeforeItWasYou.UI
{
    /// <summary>
    /// Opciones completas (SISTEMAS §Pausa y opciones): pantalla, gráficos, audio, controles y accesibilidad.
    /// Trabaja sobre una copia: cada cambio se aplica al momento y se guarda al volver.
    /// </summary>
    public sealed class OptionsScreen
    {
        private readonly VisualElement _panel;
        private readonly ScrollView _scroll;
        private readonly Label _description;
        private readonly MenuList _list = new MenuList(false, "option-row--selected");
        private readonly List<Action> _refreshers = new List<Action>();
        private GameSettings _working;
        private Action _onClose;

        public OptionsScreen(VisualElement panel)
        {
            _panel = panel;
            _scroll = panel.Q<ScrollView>("options-scroll");
            _description = panel.Q<Label>("options-description");
            _list.SelectionChanged += entry =>
            {
                _description.text = entry.Description ?? string.Empty;
                _scroll.ScrollTo(entry.Element);
            };
            _list.Cancel = Close;
        }

        public MenuList List => _list;

        public bool IsOpen { get; private set; }

        public void Open(Action onClose)
        {
            _onClose = onClose;
            _working = GameServices.Settings.Clone();
            Build();
            IsOpen = true;
            _panel.RemoveFromClassList("hidden");
            _panel.AddToClassList("panel--away");
            _panel.schedule.Execute(() => _panel.RemoveFromClassList("panel--away")).StartingIn(16);
            _list.SelectFirst();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameServices.SaveSettings(_working);
            _panel.AddToClassList("hidden");
            _onClose?.Invoke();
        }

        private void Build()
        {
            _scroll.Clear();
            _list.Clear();
            _refreshers.Clear();

            AddSection("PANTALLA");
            AddChoice("Modo de pantalla", new[] { "Pantalla completa", "Ventana sin bordes", "Ventana" },
                () => (int)_working.Display, v => _working.Display = (DisplayMode)v,
                "Ventana sin bordes facilita cambiar de aplicación.");
            var resolutions = AvailableResolutions();
            AddChoice("Resolución", resolutions.ConvertAll(r => r.x == 0 ? "Nativa" : r.x + " × " + r.y).ToArray(),
                () => Math.Max(0, resolutions.FindIndex(r => r.x == _working.ResolutionWidth && r.y == _working.ResolutionHeight)),
                v =>
                {
                    _working.ResolutionWidth = resolutions[v].x;
                    _working.ResolutionHeight = resolutions[v].y;
                },
                "Nativa usa la resolución del monitor.");
            AddToggle("Sincronización vertical", () => _working.VSync, v => _working.VSync = v, "Evita el desgarro de imagen. Si la desactivas, el juego se limita a 144 FPS.");

            AddSection("GRÁFICOS");
            AddChoice("Calidad", new[] { "Baja", "Media", "Alta", "Ultra" }, () => (int)_working.Quality, v => _working.Quality = (QualityLevel)v,
                "Afecta a sombras, oclusión ambiental, suavizado y distancia de detalle.");
            AddSlider("Campo de visión", GameSettings.MinFov, GameSettings.MaxFov, 1f, () => _working.FieldOfView, v => _working.FieldOfView = v,
                v => v.ToString("0", CultureInfo.InvariantCulture) + "°", "Ángulo vertical de la cámara.");
            AddToggle("Desenfoque de movimiento", () => _working.MotionBlur, v => _working.MotionBlur = v, "Desenfoque al girar rápido la cámara.");

            AddSection("AUDIO");
            AddPercent("Volumen general", () => _working.MasterVolume, v => _working.MasterVolume = v, null);
            AddPercent("Música", () => _working.MusicVolume, v => _working.MusicVolume = v, null);
            AddPercent("Efectos", () => _working.EffectsVolume, v => _working.EffectsVolume = v, "Pasos, puertas, objetos y voces.");
            AddPercent("Ambiente", () => _working.AmbienceVolume, v => _working.AmbienceVolume = v, "Viento, tráfico lejano, sonidos de la casa.");

            AddSection("CONTROLES");
            AddSlider("Sensibilidad del ratón", 0.2f, 3f, 0.1f, () => _working.MouseSensitivity, v => _working.MouseSensitivity = v,
                v => v.ToString("0.0", CultureInfo.InvariantCulture), null);
            AddToggle("Invertir eje vertical", () => _working.InvertY, v => _working.InvertY = v, null);
            AddStatic("Moverse", "W A S D");
            AddStatic("Correr", "Shift");
            AddStatic("Agacharse", "Ctrl");
            AddStatic("Interactuar", "E");
            AddStatic("Teléfono", "Tab");
            AddStatic("Pausa", "Esc");

            AddSection("ACCESIBILIDAD");
            AddSlider("Tamaño de subtítulos", 0.8f, 1.6f, 0.1f, () => _working.SubtitleScale, v => _working.SubtitleScale = v,
                v => Mathf.RoundToInt(v * 100f) + " %", null);
            AddPercent("Movimiento de cámara", () => _working.CameraShake, v => _working.CameraShake = v,
                "Balanceo al caminar y sacudidas. A 0 % la cámara queda estable.");
            AddToggle("Reducir destellos", () => _working.ReduceFlashes, v => _working.ReduceFlashes = v,
                "Suaviza cambios bruscos de luz y parpadeos.");

            var spacer = new VisualElement();
            spacer.style.height = 22;
            _scroll.Add(spacer);
            AddButton("RESTABLECER VALORES", () =>
            {
                _working = new GameSettings();
                Commit();
            }, "Vuelve a la configuración inicial.");
            AddButton("VOLVER", Close, null);
        }

        private void Commit()
        {
            _working.Sanitize();
            SettingsApplier.Apply(_working);
            foreach (var refresh in _refreshers) refresh();
        }

        private void AddSection(string title)
        {
            var label = new Label(title);
            label.AddToClassList("section");
            _scroll.Add(label);
        }

        private VisualElement Row(string label, out VisualElement value)
        {
            var row = new VisualElement();
            row.AddToClassList("option-row");
            var text = new Label(label);
            text.AddToClassList("option-row__label");
            row.Add(text);
            value = new VisualElement();
            value.AddToClassList("option-value");
            row.Add(value);
            _scroll.Add(row);
            return row;
        }

        private void AddChoice(string label, string[] options, Func<int> get, Action<int> set, string description)
        {
            var row = Row(label, out var value);
            var left = Arrow("left", "‹");
            var text = new Label();
            text.AddToClassList("option-text");
            var right = Arrow("right", "›");
            value.Add(left);
            value.Add(text);
            value.Add(right);
            void Refresh() => text.text = options[Mathf.Clamp(get(), 0, options.Length - 1)];
            _refreshers.Add(Refresh);
            Refresh();
            _list.Add(row, null, step =>
            {
                set((get() + step + options.Length) % options.Length);
                Commit();
            }, description);
        }

        private void AddToggle(string label, Func<bool> get, Action<bool> set, string description) =>
            AddChoice(label, new[] { "No", "Sí" }, () => get() ? 1 : 0, v => set(v == 1), description);

        private void AddPercent(string label, Func<float> get, Action<float> set, string description) =>
            AddSlider(label, 0f, 1f, 0.05f, get, set, v => Mathf.RoundToInt(v * 100f) + " %", description);

        private void AddSlider(string label, float min, float max, float step, Func<float> get, Action<float> set, Func<float, string> format, string description)
        {
            var row = Row(label, out var value);
            var bar = new VisualElement();
            bar.AddToClassList("slider");
            var fill = new VisualElement();
            fill.AddToClassList("slider__fill");
            bar.Add(fill);
            var text = new Label();
            text.AddToClassList("slider-text");
            value.Add(bar);
            value.Add(text);
            void Refresh()
            {
                var v = get();
                fill.style.width = Length.Percent(Mathf.InverseLerp(min, max, v) * 100f);
                text.text = format(v);
            }
            _refreshers.Add(Refresh);
            Refresh();
            bar.RegisterCallback<PointerDownEvent>(evt =>
            {
                var t = Mathf.Clamp01(evt.localPosition.x / bar.resolvedStyle.width);
                set(Quantize(Mathf.Lerp(min, max, t), min, max, step));
                Commit();
                evt.StopPropagation();
            });
            _list.Add(row, null, direction =>
            {
                set(Quantize(get() + direction * step, min, max, step));
                Commit();
            }, description);
        }

        private void AddStatic(string label, string text)
        {
            Row(label, out var value);
            var key = new Label(text);
            key.AddToClassList("option-static");
            value.Add(key);
        }

        private void AddButton(string label, Action submit, string description)
        {
            var row = new VisualElement();
            row.AddToClassList("option-row");
            row.AddToClassList("option-row--action");
            var text = new Label(label);
            text.AddToClassList("option-row__label");
            row.Add(text);
            _scroll.Add(row);
            _list.Add(row, submit, null, description);
        }

        private static Label Arrow(string name, string glyph)
        {
            var arrow = new Label(glyph) { name = name };
            arrow.AddToClassList("option-arrow");
            return arrow;
        }

        private static float Quantize(float value, float min, float max, float step) =>
            Mathf.Clamp(Mathf.Round((value - min) / step) * step + min, min, max);

        private static List<Vector2Int> AvailableResolutions()
        {
            var list = new List<Vector2Int> { Vector2Int.zero };
            foreach (var resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (size.x >= 1280 && !list.Contains(size)) list.Add(size);
            }
            return list;
        }
    }
}
