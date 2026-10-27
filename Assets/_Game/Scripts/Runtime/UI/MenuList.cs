using System;
using System.Collections.Generic;
using BeforeItWasYou.Input;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeforeItWasYou.UI
{
    /// <summary>
    /// Lista navegable con teclado, mando y ratón: flechas/WASD mueven la selección (con repetición),
    /// izquierda/derecha ajustan valores, Enter acepta. El ratón selecciona al pasar y acepta al hacer clic.
    /// </summary>
    public sealed class MenuList
    {
        public sealed class Entry
        {
            public VisualElement Element;
            public Action Submit;
            public Action<int> Adjust;
            public string Description;
            public bool Enabled = true;
        }

        private const float RepeatDelay = 0.38f;
        private const float RepeatRate = 0.085f;

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly bool _horizontal;
        private readonly string _selectedClass;
        private int _index = -1;
        private int _heldDirection;
        private float _nextRepeat;

        public MenuList(bool horizontal = false, string selectedClass = "menu-item--selected")
        {
            _horizontal = horizontal;
            _selectedClass = selectedClass;
        }

        public event Action<Entry> SelectionChanged;

        public Action Cancel { get; set; }

        public IReadOnlyList<Entry> Entries => _entries;

        public Entry Selected => _index >= 0 && _index < _entries.Count ? _entries[_index] : null;

        public int SelectedIndex => _index;

        public static VisualElement CreateItem(string text, string extraClass = null)
        {
            var item = new VisualElement();
            item.AddToClassList("menu-item");
            if (!string.IsNullOrEmpty(extraClass)) item.AddToClassList(extraClass);
            var label = new Label(text) { name = "label" };
            label.AddToClassList("menu-item__label");
            item.Add(label);
            return item;
        }

        public Entry Add(VisualElement element, Action submit, Action<int> adjust = null, string description = null)
        {
            var entry = new Entry { Element = element, Submit = submit, Adjust = adjust, Description = description };
            _entries.Add(entry);
            element.RegisterCallback<PointerEnterEvent>(_ => Select(_entries.IndexOf(entry), true));
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || !entry.Enabled) return;
                Select(_entries.IndexOf(entry), false);
                // Clic en las flechas de un valor: ajusta en lugar de aceptar.
                if (entry.Adjust != null && evt.target is VisualElement target && target.ClassListContains("option-arrow"))
                {
                    entry.Adjust(target.name == "left" ? -1 : 1);
                    UiSounds.Play(UiSound.Adjust);
                    return;
                }
                if (entry.Submit != null)
                {
                    UiSounds.Play(UiSound.Confirm);
                    entry.Submit();
                }
                else if (entry.Adjust != null)
                {
                    entry.Adjust(1);
                    UiSounds.Play(UiSound.Adjust);
                }
            });
            return entry;
        }

        public void SetEnabled(Entry entry, bool enabled)
        {
            entry.Enabled = enabled;
            entry.Element.EnableInClassList("menu-item--disabled", !enabled);
        }

        public void Clear()
        {
            _entries.Clear();
            _index = -1;
        }

        public void Select(int index, bool sound)
        {
            if (index < 0 || index >= _entries.Count || index == _index) return;
            if (Selected != null) Selected.Element.RemoveFromClassList(_selectedClass);
            _index = index;
            Selected.Element.AddToClassList(_selectedClass);
            if (sound) UiSounds.Play(UiSound.Move);
            SelectionChanged?.Invoke(Selected);
        }

        public void SelectFirst()
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (!_entries[i].Enabled) continue;
                if (Selected != null) Selected.Element.RemoveFromClassList(_selectedClass);
                _index = -1;
                Select(i, false);
                return;
            }
        }

        /// <summary>Procesa la entrada de un frame. Solo la lista activa debe recibir Tick.</summary>
        public void Tick(GameInput input, float unscaledTime)
        {
            if (input.Cancel.WasPressedThisFrame() && Cancel != null)
            {
                UiSounds.Play(UiSound.Back);
                Cancel();
                return;
            }
            if (_entries.Count == 0) return;
            if (input.Submit.WasPressedThisFrame() && Selected != null && Selected.Enabled && Selected.Submit != null)
            {
                UiSounds.Play(UiSound.Confirm);
                Selected.Submit();
                return;
            }

            var nav = input.Navigate.ReadValue<Vector2>();
            var primary = _horizontal ? nav.x : -nav.y;
            var secondary = _horizontal ? 0f : nav.x;
            var direction = Mathf.Abs(primary) > 0.5f ? (int)Mathf.Sign(primary) * 1 : Mathf.Abs(secondary) > 0.5f ? (int)Mathf.Sign(secondary) * 10 : 0;
            if (direction == 0)
            {
                _heldDirection = 0;
                return;
            }
            if (direction == _heldDirection && unscaledTime < _nextRepeat) return;
            _nextRepeat = unscaledTime + (direction == _heldDirection ? RepeatRate : RepeatDelay);
            _heldDirection = direction;

            if (Mathf.Abs(direction) == 10)
            {
                if (Selected?.Adjust == null) return;
                Selected.Adjust(direction / 10);
                UiSounds.Play(UiSound.Adjust);
                return;
            }
            Step(direction);
        }

        private void Step(int direction)
        {
            var count = _entries.Count;
            var start = _index < 0 ? 0 : _index;
            for (var i = 1; i <= count; i++)
            {
                var candidate = (start + direction * i + count * 4) % count;
                if (!_entries[candidate].Enabled) continue;
                Select(candidate, true);
                return;
            }
        }
    }
}
