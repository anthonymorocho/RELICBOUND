using System;
using Relicbound.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Ajustes (docs/04, docs/14): volumen por grupo, calidad gráfica y vibración. Se aplican en vivo.</summary>
    public sealed class SettingsView
    {
        private readonly VisualElement _screen;
        private readonly VisualElement _body;
        private GameSettings _settings;

        public SettingsView(VisualElement root)
        {
            _screen = root.Q("settings-screen");
            _body = root.Q("settings-body");
            var close = root.Q<Button>("settings-close");
            close.Add(new GlyphElement(GlyphKind.Close));
            close.clicked += () => Closed?.Invoke();
        }

        /// <summary>Cada cambio se notifica para aplicarlo al instante.</summary>
        public event Action<GameSettings> Changed;

        public event Action Closed;

        public bool IsOpen => !_screen.ClassListContains("hidden");

        public void Open(GameSettings settings)
        {
            _settings = settings;
            _body.Clear();
            AddVolume("General", () => _settings.MasterVolume, v => _settings.MasterVolume = v);
            AddVolume("Música", () => _settings.MusicVolume, v => _settings.MusicVolume = v);
            AddVolume("Ambiente", () => _settings.AmbienceVolume, v => _settings.AmbienceVolume = v);
            AddVolume("Efectos", () => _settings.EffectsVolume, v => _settings.EffectsVolume = v);
            AddVolume("Interfaz", () => _settings.InterfaceVolume, v => _settings.InterfaceVolume = v);
            AddQuality();
            if (Application.isMobilePlatform) AddHaptics();
            UiUtil.Show(_screen, true);
        }

        public void Close() => UiUtil.Show(_screen, false);

        private VisualElement Row(string name)
        {
            var row = new VisualElement();
            row.AddToClassList("setting-row");
            var label = new Label(name);
            label.AddToClassList("setting-name");
            row.Add(label);
            _body.Add(row);
            return row;
        }

        private void AddVolume(string name, Func<float> get, Action<float> set)
        {
            var row = Row(name);
            var slider = new Slider(0f, 1f) { value = get() };
            slider.AddToClassList("setting-control");
            var value = new Label(Percent(get()));
            value.AddToClassList("setting-value");
            value.AddToClassList("number");
            slider.RegisterValueChangedCallback(evt =>
            {
                set(evt.newValue);
                value.text = Percent(evt.newValue);
                Changed?.Invoke(_settings);
            });
            row.Add(slider);
            row.Add(value);
        }

        private void AddQuality()
        {
            var row = Row("Calidad gráfica");
            var choices = new VisualElement();
            choices.AddToClassList("choice-row");
            var buttons = new Button[3];
            var names = new[] { "Baja", "Media", "Alta" };
            for (var i = 0; i < 3; i++)
            {
                var preset = (QualityPreset)i;
                var button = new Button { text = names[i] };
                button.AddToClassList("choice");
                button.clicked += () =>
                {
                    _settings.Quality = preset;
                    for (var j = 0; j < buttons.Length; j++) buttons[j].EnableInClassList("choice--selected", j == (int)preset);
                    Changed?.Invoke(_settings);
                };
                button.EnableInClassList("choice--selected", _settings.Quality == preset);
                buttons[i] = button;
                choices.Add(button);
            }
            row.Add(choices);
        }

        private void AddHaptics()
        {
            var row = Row("Vibración");
            var choices = new VisualElement();
            choices.AddToClassList("choice-row");
            var on = new Button { text = "Sí" };
            var off = new Button { text = "No" };
            foreach (var button in new[] { on, off }) button.AddToClassList("choice");
            void Refresh()
            {
                on.EnableInClassList("choice--selected", _settings.Haptics);
                off.EnableInClassList("choice--selected", !_settings.Haptics);
            }
            on.clicked += () => { _settings.Haptics = true; Refresh(); Changed?.Invoke(_settings); };
            off.clicked += () => { _settings.Haptics = false; Refresh(); Changed?.Invoke(_settings); };
            Refresh();
            choices.Add(on);
            choices.Add(off);
            row.Add(choices);
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + " %";
    }
}
