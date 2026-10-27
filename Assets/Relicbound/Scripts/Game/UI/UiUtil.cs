using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relicbound.Game.UI
{
    public static class UiUtil
    {
        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

        public static void Show(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);

        /// <summary>Recursos legibles: 1.250 / 48,5 k / 1,2 M.</summary>
        public static string FormatAmount(long value)
        {
            if (value >= 1_000_000) return (value / 1_000_000d).ToString("0.#", Spanish) + " M";
            if (value >= 100_000) return (value / 1_000d).ToString("0", Spanish) + " k";
            return value.ToString("N0", Spanish);
        }

        /// <summary>
        /// ¿Hay un elemento interactivo de la interfaz bajo el punto de pantalla? Los contenedores marcados
        /// con picking-mode Ignore dejan pasar el clic al mundo 3D.
        /// </summary>
        public static bool IsOverInteractive(VisualElement root, Vector2 screenPosition)
        {
            var panel = root?.panel;
            if (panel == null) return false;
            var flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            var point = RuntimePanelUtils.ScreenToPanel(panel, flipped);
            var picked = panel.Pick(point);
            return picked != null && picked != root;
        }

        /// <summary>Activa/desactiva una clase en el siguiente frame (necesario para que arranquen las transiciones USS).</summary>
        public static void NextFrame(VisualElement element, System.Action action) => element.schedule.Execute(action).StartingIn(16);
    }
}
