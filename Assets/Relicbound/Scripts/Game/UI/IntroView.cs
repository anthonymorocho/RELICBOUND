using System;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Introducción contextual breve sobre el propio castillo, con bandas de cine y opción de saltar.</summary>
    public sealed class IntroView
    {
        private readonly VisualElement _screen;
        private readonly VisualElement _card;
        private readonly Label _title;
        private readonly Label _text;

        public IntroView(VisualElement root)
        {
            _screen = root.Q("intro-screen");
            _card = root.Q("intro-card");
            _title = root.Q<Label>("intro-title");
            _text = root.Q<Label>("intro-text");
            root.Q<Button>("skip-button").clicked += () => SkipRequested?.Invoke();
        }

        public event Action SkipRequested;

        public void Show(bool visible) => UiUtil.Show(_screen, visible);

        public void ShowCard(string title, string text)
        {
            _title.text = title;
            _text.text = text;
            UiUtil.NextFrame(_card, () => _card.RemoveFromClassList("transparent"));
        }

        public void HideCard() => _card.AddToClassList("transparent");
    }
}
