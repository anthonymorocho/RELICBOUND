using System;
using System.Collections.Generic;
using Relicbound.Core.Boot;
using Relicbound.Core.Economy;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>
    /// HUD del castillo (docs/04 §HUD): insignia del jugador con nivel y experiencia, recursos con su capacidad
    /// y accesos. Solo muestra sistemas ya disponibles: nada de botones que no hacen nada.
    /// </summary>
    public sealed class HudView
    {
        private readonly VisualElement _hud;
        private readonly Label _playerName;
        private readonly Label _castleName;
        private readonly Label _levelLabel;
        private readonly VisualElement _xpFill;
        private readonly Label _xpLabel;
        private readonly ShieldElement _shield;
        private readonly Dictionary<ResourceType, (VisualElement pill, Label value)> _pills =
            new Dictionary<ResourceType, (VisualElement, Label)>();
        private GameSession _session;

        public HudView(VisualElement root, IReadOnlyDictionary<ResourceType, Texture2D> icons)
        {
            _hud = root.Q("hud");
            _playerName = root.Q<Label>("player-name");
            _castleName = root.Q<Label>("castle-name");
            _levelLabel = root.Q<Label>("level-label");
            _xpFill = root.Q("xp-fill");
            _xpLabel = root.Q<Label>("xp-label");
            _shield = new ShieldElement(Banners.Starting[0]);
            var shieldSlot = root.Q("badge-shield");
            shieldSlot.Insert(0, _shield);

            var settings = root.Q<Button>("settings-button");
            settings.Add(new GlyphElement(GlyphKind.Gear));
            settings.clicked += () => SettingsRequested?.Invoke();

            var resources = root.Q("resources");
            foreach (var type in ResourceTypes.All)
            {
                var pill = new VisualElement { tooltip = ResourceName(type) };
                pill.AddToClassList("resource-pill");
                var icon = new VisualElement();
                icon.AddToClassList("resource-icon");
                if (icons != null && icons.TryGetValue(type, out var texture)) icon.style.backgroundImage = new StyleBackground(texture);
                var value = new Label();
                value.AddToClassList("resource-value");
                value.AddToClassList("number");
                pill.Add(icon);
                pill.Add(value);
                resources.Add(pill);
                _pills[type] = (pill, value);
            }
        }

        public event Action SettingsRequested;

        public void Show(bool visible)
        {
            UiUtil.Show(_hud, visible);
            if (visible)
            {
                _hud.AddToClassList("transparent");
                UiUtil.NextFrame(_hud, () => _hud.RemoveFromClassList("transparent"));
            }
        }

        public void Bind(GameSession session)
        {
            if (_session != null)
            {
                _session.Wallet.Changed -= OnResourceChanged;
                _session.Progress.LevelUp -= OnLevelUp;
            }
            _session = session;
            _session.Wallet.Changed += OnResourceChanged;
            _session.Progress.LevelUp += OnLevelUp;
            _playerName.text = session.Profile.PlayerName;
            _castleName.text = session.Profile.CastleName;
            _shield.SetStyle(Banners.Get(session.Profile.BannerId));
            foreach (var type in ResourceTypes.All) OnResourceChanged(type, session.Wallet.Get(type));
            RefreshProgress();
        }

        private void OnLevelUp(int level) => RefreshProgress();

        private void RefreshProgress()
        {
            var progress = _session.Progress;
            _levelLabel.text = progress.Level.ToString();
            _xpFill.style.width = Length.Percent(Mathf.Clamp01(progress.LevelFraction) * 100f);
            _xpLabel.text = progress.IsMaxLevel ? "Nivel máximo" : $"{UiUtil.FormatAmount(progress.XpIntoLevel)} / {UiUtil.FormatAmount(progress.XpToNext)} XP";
        }

        private void OnResourceChanged(ResourceType type, long amount)
        {
            if (!_pills.TryGetValue(type, out var entry)) return;
            entry.value.text = UiUtil.FormatAmount(amount);
            var capacity = _session.Wallet.GetCapacity(type);
            var full = ResourceTypes.IsStorageCapped(type) && amount >= capacity;
            entry.pill.EnableInClassList("resource-pill--full", full);
            entry.pill.tooltip = ResourceTypes.IsStorageCapped(type)
                ? $"{ResourceName(type)}: {UiUtil.FormatAmount(amount)} / {UiUtil.FormatAmount(capacity)}"
                : ResourceName(type);
        }

        public static string ResourceName(ResourceType type) => type switch
        {
            ResourceType.Gold => "Oro",
            ResourceType.Wood => "Madera",
            ResourceType.Stone => "Piedra",
            ResourceType.Iron => "Hierro",
            ResourceType.Gems => "Gemas",
            _ => type.ToString(),
        };
    }
}
