using Relicbound.Core.Boot;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Consejo contextual sobre el castillo con indicador de progreso (docs/01 paso 7).</summary>
    public sealed class TutorialView
    {
        private readonly VisualElement _card;
        private readonly Label _title;
        private readonly Label _text;
        private readonly VisualElement _dots;

        public TutorialView(VisualElement root)
        {
            _card = root.Q("tutorial-card");
            _title = root.Q<Label>("tutorial-title");
            _text = root.Q<Label>("tutorial-text");
            _dots = root.Q("tutorial-dots");
        }

        public void Show(TutorialStep step, int completed)
        {
            if (step == TutorialStep.Done)
            {
                Hide();
                return;
            }
            var touch = Application.isMobilePlatform;
            (_title.text, _text.text) = step switch
            {
                TutorialStep.Pan => ("Explora tu fortaleza", touch ? "Desliza un dedo por la pantalla para mover la cámara." : "Arrastra con el botón izquierdo (o usa WASD) para mover la cámara."),
                TutorialStep.Zoom => ("Acércate", touch ? "Pellizca con dos dedos para acercar o alejar la vista." : "Usa la rueda del ratón para acercar o alejar la vista."),
                _ => ("Tu torreón", touch ? "Toca el torreón para ver su información." : "Haz clic en el torreón para ver su información."),
            };
            _dots.Clear();
            for (var i = 0; i < TutorialFlow.TotalSteps; i++)
            {
                var dot = new VisualElement();
                dot.AddToClassList("tutorial-dot");
                dot.EnableInClassList("tutorial-dot--done", i < completed);
                dot.EnableInClassList("tutorial-dot--current", i == completed);
                _dots.Add(dot);
            }
            UiUtil.Show(_card, true);
            UiUtil.NextFrame(_card, () => _card.RemoveFromClassList("transparent"));
        }

        public void Hide()
        {
            _card.AddToClassList("transparent");
            _card.schedule.Execute(() => UiUtil.Show(_card, false)).StartingIn(450);
        }
    }
}
