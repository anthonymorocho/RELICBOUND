using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    /// <summary>Marca del juego y barra de carga fina (sin menú: lo siguiente es el castillo).</summary>
    public sealed class SplashView
    {
        public const float FadeSeconds = 0.45f;

        private readonly VisualElement _logo;
        private readonly VisualElement _fill;

        public SplashView(VisualElement root)
        {
            _logo = root.Q("logo");
            _fill = root.Q("loading-fill");
        }

        public void FadeIn() => UiUtil.NextFrame(_logo, () => _logo.RemoveFromClassList("transparent"));

        public void FadeOut() => _logo.AddToClassList("transparent");

        public void SetProgress(float progress) => _fill.style.width = Length.Percent(progress * 100f);
    }
}
