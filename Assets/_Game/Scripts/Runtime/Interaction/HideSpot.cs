using BeforeItWasYou.Input;
using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    /// <summary>
    /// Escondite donde cabe una persona (el armario de Elena). Daniel entra, mira por la rendija y sale con E.
    /// Si Víctor le ve entrar, el escondite no sirve: nunca hay invisibilidad mágica.
    /// </summary>
    public sealed class HideSpot : Interactable
    {
        [SerializeField] private Transform inside;
        [SerializeField] private Transform exit;

        private Interactor _hider;
        private float _since;
        private float _near = 0.05f;

        public static HideSpot Occupied { get; private set; }
        public float HiddenSince => _since;

        public override string Prompt => _hider != null ? "Salir del armario" : "Esconderse en el armario";

        public void Configure(Transform insidePoint, Transform exitPoint)
        {
            inside = insidePoint;
            exit = exitPoint;
        }

        public override void Interact(Interactor interactor)
        {
            if (_hider == null)
            {
                _hider = interactor;
                _since = Time.time;
                Occupied = this;
                interactor.Player.Teleport(inside.position, inside.eulerAngles.y, 4f);
                interactor.Player.SetCrouched(false);
                interactor.Player.Lock("escondido");
                interactor.Player.Hidden = true;
                // Se mira por la rendija entre las puertas: la cámara salta la hoja y la interfaz enmarca la ranura.
                _near = interactor.Player.Camera.nearClipPlane;
                interactor.Player.Camera.nearClipPlane = 0.16f;
                Episode.EpisodeDirector.Current?.Ui.SetHiding(true);
            }
            else
            {
                Leave();
            }
        }

        private void Update()
        {
            // Dentro del armario el jugador no tiene control de movimiento; E le saca.
            if (_hider != null && GameInput.Instance.Interact.WasPressedThisFrame() && Time.time - _since > 0.3f) Leave();
        }

        private void Leave()
        {
            if (_hider == null) return;
            var player = _hider.Player;
            player.Unlock("escondido");
            player.Hidden = false;
            player.Camera.nearClipPlane = _near;
            Episode.EpisodeDirector.Current?.Ui.SetHiding(false);
            player.Teleport(exit.position, exit.eulerAngles.y);
            _hider = null;
            Occupied = null;
        }
    }
}
