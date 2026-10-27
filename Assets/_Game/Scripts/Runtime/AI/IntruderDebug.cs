#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace BeforeItWasYou.AI
{
    /// <summary>Telemetría DEV (F1): estado de Víctor, destino y causa del último cambio. Excluida de la build final.</summary>
    public sealed class IntruderDebug : MonoBehaviour
    {
        private IntruderAI _ai;
        private string _last = "—";
        private bool _visible;

        private void Awake()
        {
            _ai = GetComponent<IntruderAI>();
            _ai.StateChanged += (p, n, cause) => _last = $"{p} → {n}: {cause}";
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            GUI.Label(new Rect(16, 16, 900, 22), $"Víctor: {_ai.State}  ·  sabe de Daniel: {_ai.KnowsAboutPlayer}  ·  mochila: {_ai.HasBag}");
            GUI.Label(new Rect(16, 38, 900, 22), $"Último cambio: {_last}");
            GUI.Label(new Rect(16, 60, 900, 22), $"Última posición conocida: {_ai.LastKnownPlayerPosition}");
        }
    }
}
#endif
