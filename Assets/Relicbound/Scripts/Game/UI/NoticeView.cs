using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Aviso breve (p. ej. partida recuperada de la copia de seguridad).</summary>
    public sealed class NoticeView
    {
        private readonly Label _label;

        public NoticeView(VisualElement root)
        {
            _label = root.Q<Label>("notice");
        }

        public void Show(string message, long milliseconds = 4000)
        {
            _label.text = message;
            UiUtil.Show(_label, true);
            _label.AddToClassList("transparent");
            UiUtil.NextFrame(_label, () => _label.RemoveFromClassList("transparent"));
            _label.schedule.Execute(() => _label.AddToClassList("transparent")).StartingIn(milliseconds);
            _label.schedule.Execute(() => UiUtil.Show(_label, false)).StartingIn(milliseconds + 500);
        }
    }
}
