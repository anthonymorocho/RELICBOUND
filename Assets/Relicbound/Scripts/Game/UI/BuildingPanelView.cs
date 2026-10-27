using System;
using Relicbound.Game.Castle;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Panel inferior compacto del edificio seleccionado (docs/04 §Selección). Mejorar llega en P3.</summary>
    public sealed class BuildingPanelView
    {
        private readonly VisualElement _panel;
        private readonly Label _name;
        private readonly Label _level;
        private readonly Label _role;
        private readonly Label _description;
        private readonly VisualElement _stats;

        public BuildingPanelView(VisualElement root)
        {
            _panel = root.Q("building-panel");
            _name = root.Q<Label>("building-name");
            _level = root.Q<Label>("building-level");
            _role = root.Q<Label>("building-role");
            _description = root.Q<Label>("building-description");
            _stats = root.Q("building-stats");
            var close = root.Q<Button>("building-close");
            close.Add(new GlyphElement(GlyphKind.Close));
            close.clicked += () => CloseRequested?.Invoke();
        }

        public event Action CloseRequested;

        public void Show(BuildingView view)
        {
            if (view == null || view.Definition == null)
            {
                UiUtil.Show(_panel, false);
                return;
            }
            var definition = view.Definition;
            _name.text = definition.displayName;
            _level.text = $"Nv {view.Level}";
            _role.text = definition.role.ToUpperInvariant();
            _description.text = definition.description;
            _stats.Clear();
            foreach (var stat in definition.stats)
            {
                var row = new VisualElement();
                row.AddToClassList("stat");
                row.AddToClassList("card");
                var label = new Label(stat.label);
                label.AddToClassList("stat-label");
                var value = new Label(stat.value);
                value.AddToClassList("stat-value");
                value.AddToClassList("number");
                row.Add(label);
                row.Add(value);
                _stats.Add(row);
            }
            UiUtil.Show(_panel, true);
        }

        public void Hide() => UiUtil.Show(_panel, false);
    }
}
