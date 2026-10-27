using System;

namespace Relicbound.Core.CameraControl
{
    public enum GestureKind
    {
        /// <summary>Todavía no se sabe (puntero pulsado sin superar umbrales).</summary>
        Pending,
        Tap,
        Drag,
        /// <summary>Pulsación larga sin mover: info rápida del edificio.</summary>
        Hold,
        /// <summary>Gesto anulado (p. ej. entró un segundo dedo para hacer zoom).</summary>
        Cancelled,
    }

    /// <summary>
    /// Distingue toque, arrastre y pulsación larga para que mover la cámara nunca seleccione
    /// un edificio por accidente (docs/04 §Cámara, docs/16). El umbral va en milímetros físicos
    /// para que sea igual en un móvil de 400 ppp y en un monitor de 96 ppp.
    /// </summary>
    public sealed class PointerGesture
    {
        public const float DefaultDragThresholdMm = 2.5f;
        public const float DefaultHoldSeconds = 0.45f;

        private readonly float _thresholdPixels;
        private readonly float _holdSeconds;
        private float _startX;
        private float _startY;
        private float _startTime;

        public PointerGesture(float dpi, float dragThresholdMm = DefaultDragThresholdMm, float holdSeconds = DefaultHoldSeconds)
        {
            if (dpi <= 0 || float.IsNaN(dpi)) dpi = 160f; // Screen.dpi devuelve 0 en algunos dispositivos.
            _thresholdPixels = dragThresholdMm / 25.4f * dpi;
            _holdSeconds = holdSeconds;
            Kind = GestureKind.Cancelled;
        }

        public GestureKind Kind { get; private set; }

        public float ThresholdPixels => _thresholdPixels;

        public void Begin(float x, float y, float time)
        {
            _startX = x;
            _startY = y;
            _startTime = time;
            Kind = GestureKind.Pending;
        }

        /// <summary>Llamar cada frame mientras el puntero está pulsado.</summary>
        public GestureKind Update(float x, float y, float time)
        {
            if (Kind != GestureKind.Pending) return Kind;
            var dx = x - _startX;
            var dy = y - _startY;
            if (dx * dx + dy * dy > _thresholdPixels * _thresholdPixels) Kind = GestureKind.Drag;
            else if (time - _startTime >= _holdSeconds) Kind = GestureKind.Hold;
            return Kind;
        }

        /// <summary>Al soltar: si no llegó a ser arrastre ni pulsación larga, es un toque.</summary>
        public GestureKind End(float x, float y, float time)
        {
            Update(x, y, time);
            if (Kind == GestureKind.Pending) Kind = GestureKind.Tap;
            return Kind;
        }

        public void Cancel() => Kind = GestureKind.Cancelled;
    }
}
