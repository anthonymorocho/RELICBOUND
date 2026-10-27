using System;
using System.Collections.Generic;
using Relicbound.Core.Boot;
using Relicbound.Core.Profile;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Panel «Tu fortaleza»: estandarte, renombrar la fortaleza y progreso (docs/04 §Perfil, P2).</summary>
    public sealed class ProfilePanelView
    {
        private readonly VisualElement _screen;
        private readonly ShieldElement _shield;
        private readonly Label _player;
        private readonly Label _level;
        private readonly Label _since;
        private readonly TextField _castleField;
        private readonly Label _castleHint;
        private readonly Button _save;
        private readonly List<Button> _banners = new List<Button>();
        private string _banner;

        public ProfilePanelView(VisualElement root)
        {
            _screen = root.Q("profile-panel");
            _player = root.Q<Label>("profile-player");
            _level = root.Q<Label>("profile-level");
            _since = root.Q<Label>("profile-since");
            _castleField = root.Q<TextField>("profile-castle-field");
            _castleHint = root.Q<Label>("profile-castle-hint");
            _save = root.Q<Button>("profile-save");
            _shield = new ShieldElement(Banners.Starting[0]);
            root.Q("profile-shield").Add(_shield);
            var close = root.Q<Button>("profile-close");
            close.Add(new GlyphElement(GlyphKind.Close));
            close.clicked += () => Closed?.Invoke();

            var choices = root.Q("profile-banners");
            foreach (var banner in Banners.Starting)
            {
                var button = new Button { tooltip = banner.Name };
                button.AddToClassList("banner-choice");
                button.AddToClassList("banner-choice--small");
                var shield = new ShieldElement(banner);
                shield.style.left = shield.style.right = 10;
                shield.style.top = 8;
                shield.style.bottom = 10;
                button.Add(shield);
                var id = banner.Id;
                button.clicked += () => SelectBanner(id);
                choices.Add(button);
                _banners.Add(button);
            }
            _castleField.RegisterValueChangedCallback(_ => Validate());
            _save.clicked += () =>
            {
                if (Validate()) Saved?.Invoke(_castleField.value, _banner);
            };
        }

        public event Action<string, string> Saved;

        public event Action<string> BannerPreviewed;

        public event Action Closed;

        public bool IsOpen => !_screen.ClassListContains("hidden");

        public void Open(GameSession session)
        {
            var profile = session.Profile;
            _player.text = profile.PlayerName;
            _level.text = $"Nivel {session.Progress.Level}";
            _since.text = "Señor desde el " + profile.CreatedUtc.ToLocalTime().ToString("d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
            _castleField.SetValueWithoutNotify(profile.CastleName);
            SelectBanner(profile.BannerId, false);
            Validate();
            UiUtil.Show(_screen, true);
        }

        public void Close() => UiUtil.Show(_screen, false);

        private void SelectBanner(string id, bool notify = true)
        {
            _banner = id;
            _shield.SetStyle(Banners.Get(id));
            for (var i = 0; i < _banners.Count; i++) _banners[i].EnableInClassList("banner-choice--selected", Banners.Starting[i].Id == id);
            if (notify) BannerPreviewed?.Invoke(id);
        }

        private bool Validate()
        {
            var error = NameValidator.ValidateCastleName(_castleField.value);
            _castleHint.text = error switch
            {
                NameError.None => "Aparecerá en tus estandartes.",
                NameError.TooShort => $"Mínimo {NameValidator.CastleMin} caracteres.",
                NameError.TooLong => $"Máximo {NameValidator.CastleMax} caracteres.",
                NameError.Empty => "Escribe un nombre.",
                _ => "Solo letras, números, espacios, guiones y apóstrofos.",
            };
            _castleHint.EnableInClassList("hint--error", error != NameError.None);
            _castleField.EnableInClassList("field--error", error != NameError.None);
            _save.SetEnabled(error == NameError.None);
            return error == NameError.None;
        }
    }
}
