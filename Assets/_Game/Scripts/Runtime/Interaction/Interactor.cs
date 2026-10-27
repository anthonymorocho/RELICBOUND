using System;
using BeforeItWasYou.Input;
using BeforeItWasYou.Player;
using BeforeItWasYou.Services;
using UnityEngine;

namespace BeforeItWasYou.Interaction
{
    /// <summary>Busca el interactuable al que apunta la cámara y lo activa con E.</summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class Interactor : MonoBehaviour
    {
        private const float MaxRay = 2.6f;
        private PlayerController _player;

        public Interactable Focus { get; private set; }

        public PlayerController Player => _player;

        public event Action<Interactable> FocusChanged;

        public event Action<Interactable> Interacted;

        private void Awake() => _player = GetComponent<PlayerController>();

        private void Update()
        {
            var active = _player.HasControl && GameServices.Flow != null && GameServices.Flow.IsSimulating;
            var found = active ? Probe() : null;
            if (found != Focus)
            {
                Focus = found;
                FocusChanged?.Invoke(Focus);
            }
            if (Focus != null && GameInput.Instance.Interact.WasPressedThisFrame())
            {
                Focus.Interact(this);
                Interacted?.Invoke(Focus);
            }
        }

        private Interactable Probe()
        {
            var camera = _player.Camera;
            if (camera == null) return null;
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            // Primero lo que bloquea la vista (paredes incluidas); luego un margen fino para objetos pequeños.
            if (!Physics.Raycast(ray, out var hit, MaxRay, Layers.AllButPlayer, QueryTriggerInteraction.Collide))
            {
                if (!Physics.SphereCast(ray, 0.06f, out hit, MaxRay, Layers.AllButPlayer, QueryTriggerInteraction.Collide)) return null;
            }
            var target = hit.collider.GetComponentInParent<Interactable>();
            if (target == null || !target.CanInteract || hit.distance > target.Reach) return null;
            return target;
        }
    }
}
