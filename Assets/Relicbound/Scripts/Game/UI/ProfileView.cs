using System;
using System.Collections.Generic;
using Relicbound.Core.Profile;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>
    /// Primera apertura: estandarte, nombre del señor y de la fortaleza (docs/04 §Perfil).
    /// Validación en vivo con las mismas reglas que el guardado (<see cref="NameValidator"/>).
    /// </summary>
    public sealed class ProfileView
    {
        private readonly VisualElement _screen;
        private readonly TextField _playerField;
        private readonly TextField _castleField;
        private readonly Label _playerHint;
        private readonly Label _castleHint;
        private readonly Button _foundButton;
        private readonly List<Button> _bannerButtons = new List<Button>();
        private bool _playerTouched;
        private bool _castleTouched;

        public ProfileView(VisualElement root)
        {
            _screen = root.Q("profile-screen");
            _playerField = root.Q<TextField>("player-name-field");
            _castleField = root.Q<TextField>("castle-name-field");
            _playerHint = root.Q<Label>("player-name-hint");
            _castleHint = root.Q<Label>("castle-name-hint");
            _foundButton = root.Q<Button>("found-button");

            var choices = root.Q("banner-choices");
            foreach (var banner in Banners.Starting)
            {
                var button = new Button { tooltip = banner.Name };
                button.AddToClassList("banner-choice");
                var shield = new ShieldElement(banner);
                shield.style.left = shield.style.right = 14;
                shield.style.top = 12;
                shield.style.bottom = 14;
                button.Add(shield);
                var id = banner.Id;
                button.clicked += () => SelectBanner(id);
                choices.Add(button);
                _bannerButtons.Add(button);
            }
            SelectBanner(Banners.Starting[0].Id);

            _playerField.RegisterValueChangedCallback(_ => { _playerTouched = true; Validate(); });
            _castleField.RegisterValueChangedCallback(_ => { _castleTouched = true; Validate(); });
            _foundButton.clicked += () => Submit();
            Validate();
        }

        public string SelectedBanner { get; private set; }

        public event Action<string, string, string> Submitted;

        /// <summary>Al elegir estandarte, el castillo del fondo se viste con él al instante.</summary>
        public event Action<string> BannerChanged;

        public void Show(bool visible)
        {
            UiUtil.Show(_screen, visible);
            if (visible)
            {
                _screen.AddToClassList("transparent");
                UiUtil.NextFrame(_screen, () => _screen.RemoveFromClassList("transparent"));
            }
        }

        /// <summary>Rellena los campos como lo haría el jugador (usado también por los tests del recorrido).</summary>
        public void SetNames(string playerName, string castleName)
        {
            _playerField.value = playerName;
            _castleField.value = castleName;
        }

        public void SelectBanner(string id)
        {
            SelectedBanner = id;
            BannerChanged?.Invoke(id);
            for (var i = 0; i < _bannerButtons.Count; i++)
            {
                _bannerButtons[i].EnableInClassList("banner-choice--selected", Banners.Starting[i].Id == id);
            }
        }

        public bool Submit()
        {
            _playerTouched = _castleTouched = true;
            if (!Validate()) return false;
            Submitted?.Invoke(_playerField.value, _castleField.value, SelectedBanner);
            return true;
        }

        private bool Validate()
        {
            var playerError = NameValidator.ValidatePlayerName(_playerField.value);
            var castleError = NameValidator.ValidateCastleName(_castleField.value);
            ShowHint(_playerField, _playerHint, _playerTouched ? playerError : NameError.None,
                NameValidator.PlayerMin, NameValidator.PlayerMax, "Así te conocerán tus aliados y rivales.");
            ShowHint(_castleField, _castleHint, _castleTouched ? castleError : NameError.None,
                NameValidator.CastleMin, NameValidator.CastleMax, "Aparecerá en tus estandartes.");
            var valid = playerError == NameError.None && castleError == NameError.None;
            _foundButton.SetEnabled(valid);
            return valid;
        }

        private static void ShowHint(TextField field, Label hint, NameError error, int min, int max, string help)
        {
            hint.text = error switch
            {
                NameError.Empty => "Escribe un nombre.",
                NameError.TooShort => $"Mínimo {min} caracteres.",
                NameError.TooLong => $"Máximo {max} caracteres.",
                NameError.InvalidCharacter => "Solo letras, números, espacios, guiones y apóstrofos.",
                _ => help,
            };
            var hasError = error != NameError.None;
            hint.EnableInClassList("hint--error", hasError);
            field.EnableInClassList("field--error", hasError);
        }
    }
}
