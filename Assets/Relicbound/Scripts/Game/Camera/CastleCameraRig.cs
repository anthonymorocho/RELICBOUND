using System;
using Relicbound.Core.CameraControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Relicbound.Game.CameraControl
{
    public enum CameraMode
    {
        /// <summary>Control del jugador (pantalla principal).</summary>
        Interactive,
        /// <summary>Órbita lenta de fondo (creación de perfil).</summary>
        Orbit,
        /// <summary>Planos guiados (introducción, encuadre de edificios).</summary>
        Cinematic,
    }

    /// <summary>
    /// Cámara del castillo (docs/04 §Cámara, docs/16). Pivote sobre el suelo + yaw + distancia; la inclinación
    /// depende del zoom (más cenital al alejar). Arrastrar "agarra" el suelo bajo el dedo/ratón.
    /// Un toque nunca se confunde con un arrastre gracias a <see cref="PointerGesture"/>.
    /// </summary>
    public sealed class CastleCameraRig : MonoBehaviour
    {
        [SerializeField] private float minDistance = 24f;
        [SerializeField] private float maxDistance = 95f;
        [SerializeField] private float minPitch = 36f;
        [SerializeField] private float maxPitch = 56f;
        [SerializeField] private float damping = 12f;
        [SerializeField] private float keyboardPanSpeed = 28f;
        [SerializeField] private float orbitDegreesPerSecond = 4f;

        private Camera _camera;
        private CameraLimits _limits;
        private PointerGesture _gesture;

        private Vector3 _pivot;
        private float _yaw = -35f;
        private float _distance = 70f;
        private Vector3 _targetPivot;
        private float _targetYaw = -35f;
        private float _targetDistance = 70f;

        private bool _pointerDown;
        private Vector3 _grabWorld;
        private float _lastPinchDistance;
        private float _lastTwistAngle;

        public CameraMode Mode { get; set; } = CameraMode.Interactive;

        /// <summary>Se dispara con la posición de pantalla cuando el gesto termina siendo un toque/clic.</summary>
        public event Action<Vector2> Tapped;

        /// <summary>El director lo asigna para ignorar punteros sobre la interfaz.</summary>
        public Func<Vector2, bool> IsPointerOverUi { get; set; } = _ => false;

        public Camera Camera => _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _limits = CameraLimits.ForGrid(40, 1f, 8f, minDistance, maxDistance);
            _gesture = new PointerGesture(Screen.dpi);
            _targetPivot = _pivot = Vector3.zero;
            Apply(true);
        }

        private void OnEnable() => EnhancedTouchSupport.Enable();

        private void OnDisable() => EnhancedTouchSupport.Disable();

        /// <summary>Coloca la cámara al instante (sin suavizado).</summary>
        public void SnapTo(Vector3 pivot, float yaw, float distance)
        {
            _targetPivot = _pivot = pivot;
            _targetYaw = _yaw = yaw;
            _targetDistance = _distance = distance;
            Apply(true);
        }

        /// <summary>Mueve la cámara suavemente hacia un encuadre (edificio seleccionado, planos de la intro).</summary>
        public void FocusOn(Vector3 pivot, float distance, float? yaw = null)
        {
            _targetPivot = pivot;
            _targetDistance = distance;
            if (yaw.HasValue) _targetYaw = yaw.Value;
        }

        public void FrameCastle() => FocusOn(new Vector3(0f, 0f, 0f), 70f, -35f);

        private void Update()
        {
            switch (Mode)
            {
                case CameraMode.Interactive:
                    if (!HandleTouch()) HandleMouse();
                    HandleKeyboard();
                    break;
                case CameraMode.Orbit:
                    _targetYaw += orbitDegreesPerSecond * Time.unscaledDeltaTime;
                    break;
            }
            Apply(false);
        }

        // ------------------------------------------------------------ entrada

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var position = mouse.position.ReadValue();
            var now = Time.unscaledTime;

            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi(position))
            {
                _pointerDown = true;
                _gesture.Begin(position.x, position.y, now);
                GroundPoint(position, out _grabWorld);
            }
            if (_pointerDown && mouse.leftButton.isPressed)
            {
                if (_gesture.Update(position.x, position.y, now) == GestureKind.Drag) DragTo(position);
            }
            if (_pointerDown && mouse.leftButton.wasReleasedThisFrame)
            {
                _pointerDown = false;
                if (_gesture.End(position.x, position.y, now) == GestureKind.Tap) Tapped?.Invoke(position);
            }

            if (mouse.rightButton.isPressed)
            {
                _targetYaw += mouse.delta.ReadValue().x * 0.25f;
            }

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && !IsPointerOverUi(position))
            {
                _targetDistance = _limits.ClampDistance(_targetDistance * (1f - Mathf.Sign(scroll) * 0.12f));
            }
        }

        private bool HandleTouch()
        {
            var touches = ETouch.activeTouches;
            if (touches.Count == 0)
            {
                if (_pointerDown && Touchscreen.current != null)
                {
                    _pointerDown = false;
                }
                return Touchscreen.current != null && Mouse.current == null;
            }

            var now = Time.unscaledTime;
            if (touches.Count == 1)
            {
                var touch = touches[0];
                var position = touch.screenPosition;
                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        if (IsPointerOverUi(position)) return true;
                        _pointerDown = true;
                        _gesture.Begin(position.x, position.y, now);
                        GroundPoint(position, out _grabWorld);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary:
                        if (_pointerDown && _gesture.Update(position.x, position.y, now) == GestureKind.Drag) DragTo(position);
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        if (_pointerDown && _gesture.End(position.x, position.y, now) == GestureKind.Tap) Tapped?.Invoke(position);
                        _pointerDown = false;
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        _gesture.Cancel();
                        _pointerDown = false;
                        break;
                }
                return true;
            }

            // Dos dedos: pellizco (zoom) y giro (rotación). Anula cualquier toque en curso.
            _gesture.Cancel();
            _pointerDown = false;
            var a = touches[0].screenPosition;
            var b = touches[1].screenPosition;
            var pinch = Vector2.Distance(a, b);
            var twist = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            if (touches[0].phase == UnityEngine.InputSystem.TouchPhase.Began || touches[1].phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _lastPinchDistance = pinch;
                _lastTwistAngle = twist;
                return true;
            }
            if (_lastPinchDistance > 1f)
            {
                _targetDistance = _limits.ClampDistance(_targetDistance * (_lastPinchDistance / Mathf.Max(1f, pinch)));
            }
            _targetYaw -= Mathf.DeltaAngle(_lastTwistAngle, twist);
            _lastPinchDistance = pinch;
            _lastTwistAngle = twist;
            return true;
        }

        private void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            if (input.sqrMagnitude > 0f)
            {
                var forward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
                var right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
                var speed = keyboardPanSpeed * (_distance / 70f) * Time.unscaledDeltaTime;
                _targetPivot += (forward * input.y + right * input.x) * speed;
            }
            if (keyboard.qKey.wasPressedThisFrame) _targetYaw -= 45f;
            if (keyboard.eKey.wasPressedThisFrame) _targetYaw += 45f;
            if (keyboard.fKey.wasPressedThisFrame) FrameCastle();
        }

        /// <summary>Mantiene el punto del suelo agarrado bajo el puntero.</summary>
        private void DragTo(Vector2 screen)
        {
            if (!GroundPoint(screen, out var current)) return;
            var delta = _grabWorld - current;
            _targetPivot += new Vector3(delta.x, 0f, delta.z);
            _pivot += new Vector3(delta.x, 0f, delta.z);
            ClampPivot(ref _pivot);
            Apply(true);
        }

        private bool GroundPoint(Vector2 screen, out Vector3 point)
        {
            var ray = _camera.ScreenPointToRay(screen);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out var enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = default;
            return false;
        }

        // ------------------------------------------------------------ aplicación

        private void ClampPivot(ref Vector3 pivot)
        {
            float x = pivot.x, z = pivot.z;
            _limits.ClampPivot(ref x, ref z, _targetDistance);
            pivot = new Vector3(x, 0f, z);
        }

        private void Apply(bool instant)
        {
            if (Mode == CameraMode.Interactive)
            {
                _targetDistance = _limits.ClampDistance(_targetDistance);
                ClampPivot(ref _targetPivot);
            }
            var t = instant ? 1f : 1f - Mathf.Exp(-damping * Time.unscaledDeltaTime);
            _pivot = Vector3.Lerp(_pivot, _targetPivot, t);
            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, t);
            _distance = Mathf.Lerp(_distance, _targetDistance, t);

            var pitch = Mathf.Lerp(minPitch, maxPitch, _limits.ZoomFraction(_distance));
            var rotation = Quaternion.Euler(pitch, _yaw, 0f);
            var lookAt = _pivot + Vector3.up * 3f;
            transform.SetPositionAndRotation(lookAt - rotation * Vector3.forward * _distance, rotation);
        }
    }
}
