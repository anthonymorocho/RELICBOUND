using System;
using UnityEngine.UIElements;

namespace BeforeItWasYou.UI
{
    /// <summary>Créditos y licencias de terceros (se amplían con cada asset registrado en Docs/Proyecto/REGISTRO_ASSETS.csv).</summary>
    public sealed class CreditsScreen
    {
        private static readonly string[][] Blocks =
        {
            new[] { "EPISODIO 01", "Alguien estuvo aquí", "Una antología de terror cotidiano." },
            new[] { "MOTOR", "Hecho con Unity 6 · Universal Render Pipeline" },
            new[] { "TIPOGRAFÍAS", "Bebas Neue — Dharma Type · SIL Open Font License 1.1", "Oswald — Vernon Adams, Kalapi Gajjar, Cyreal · SIL Open Font License 1.1", "Inter — Rasmus Andersson · SIL Open Font License 1.1" },
        };

        private static string[][] _extra = new string[0][];

        private readonly VisualElement _panel;
        private readonly ScrollView _scroll;
        private readonly MenuList _list = new MenuList();

        public CreditsScreen(VisualElement panel)
        {
            _panel = panel;
            _scroll = panel.Q<ScrollView>("credits-scroll");
            _list.Cancel = Close;
        }

        private Action _onClose;

        public MenuList List => _list;

        public bool IsOpen { get; private set; }

        /// <summary>Bloques adicionales (assets CC0 y otros) generados desde el registro de assets.</summary>
        public static void SetExtraBlocks(string[][] blocks) => _extra = blocks ?? new string[0][];

        public void Open(Action onClose)
        {
            _onClose = onClose;
            _scroll.Clear();
            _list.Clear();
            Add(Blocks);
            Add(_extra);
            var back = MenuList.CreateItem("VOLVER");
            back.style.marginTop = 30;
            _scroll.Add(back);
            _list.Add(back, Close);
            _list.SelectFirst();
            IsOpen = true;
            _panel.RemoveFromClassList("hidden");
            _panel.AddToClassList("panel--away");
            _panel.schedule.Execute(() => _panel.RemoveFromClassList("panel--away")).StartingIn(16);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _panel.AddToClassList("hidden");
            _onClose?.Invoke();
        }

        private void Add(string[][] blocks)
        {
            foreach (var block in blocks)
            {
                var heading = new Label(block[0]);
                heading.AddToClassList("credits-heading");
                _scroll.Add(heading);
                for (var i = 1; i < block.Length; i++)
                {
                    var line = new Label(block[i]);
                    line.AddToClassList(block.Length > 4 ? "credits-small" : "credits-line");
                    _scroll.Add(line);
                }
            }
        }
    }
}
