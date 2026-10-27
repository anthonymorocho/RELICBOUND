using System;
using System.Collections.Generic;
using BeforeItWasYou.Input;
using BeforeItWasYou.Services;
using UnityEngine;

namespace BeforeItWasYou.Player
{
    /// <summary>
    /// Daniel en primera persona: caminar, correr (más ruido), agacharse, gravedad, balanceo de cámara suave y
    /// bloqueo de control por motivos (menús, secuencias, teléfono). Los valores de velocidad son de paso humano
    /// en interior, no de shooter.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public const float StandingHeight = 1.8f;
        public const float CrouchHeight = 1.15f;

        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float walkSpeed = 1.9f;
        [SerializeField] private float runSpeed = 3.7f;
        [SerializeField] private float crouchSpeed = 1.05f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float gravity = -14f;
        [SerializeField] private float standingEyeHeight = 1.64f;
        [SerializeField] private float crouchEyeHeight = 1.02f;
        [SerializeField] private float lookScale = 0.075f;

        private readonly HashSet<string> _locks = new HashSet<string>();
        private CharacterController _body;
        private Vector3 _planarVelocity;
        private float _verticalVelocity;
        private float _yaw;
        private float _pitch;
        private float _eyeHeight;
        private float _bobPhase;
        private float _stepDistance;
        private bool _crouched;
        private Vector3 _impulse;
        private float _shake;

        public event Action<Surface, float> Footstep;

        public Camera Camera => playerCamera;
        public Transform CameraPivot => cameraPivot;
        public bool IsCrouched => _crouched;
        public bool IsRunning { get; private set; }
        public float Speed => _planarVelocity.magnitude;
        public bool HasControl => _locks.Count == 0;

        /// <summary>Dentro de un escondite (armario): Víctor no le ve salvo que le viera entrar.</summary>
        public bool Hidden { get; set; }
        public float Yaw => _yaw;
        public float Pitch => _pitch;

        private void Awake()
        {
            _body = GetComponent<CharacterController>();
            if (cameraPivot == null && playerCamera != null) cameraPivot = playerCamera.transform;
            _yaw = transform.eulerAngles.y;
            _eyeHeight = standingEyeHeight;
            SettingsApplier.Applied += OnSettings;
            if (GameServices.IsReady) OnSettings(GameServices.Settings);
        }

        private void OnDestroy() => SettingsApplier.Applied -= OnSettings;

        private void OnSettings(Core.Settings.GameSettings settings)
        {
            // El FOV de Opciones es vertical, como en el inspector de Unity.
            if (playerCamera != null) playerCamera.fieldOfView = settings.FieldOfView;
        }

        /// <summary>Bloquea el control por un motivo con nombre; varios sistemas pueden bloquear a la vez.</summary>
        public void Lock(string reason) => _locks.Add(reason);

        public void Unlock(string reason) => _locks.Remove(reason);

        private float _hideYaw;

        public void Teleport(Vector3 position, float yaw, float pitch = 0f)
        {
            _hideYaw = yaw;
            _body.enabled = false;
            transform.position = position;
            _body.enabled = true;
            _yaw = yaw;
            _pitch = pitch;
            _planarVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            ApplyRotation();
        }

        /// <summary>Empujón: desplaza a Daniel y sacude la cámara (escalado por la opción Movimiento de cámara).</summary>
        public void Knockback(Vector3 impulse)
        {
            _impulse = impulse;
            _shake = 1f;
            Lock("empujon");
            CancelInvoke(nameof(EndKnockback));
            Invoke(nameof(EndKnockback), 0.7f);
        }

        private void EndKnockback() => Unlock("empujon");

        public void SetCrouched(bool crouched)
        {
            if (!crouched && !CanStand()) return;
            _crouched = crouched;
            _body.height = crouched ? CrouchHeight : StandingHeight;
            _body.center = new Vector3(0f, _body.height * 0.5f, 0f);
        }

        private void Update()
        {
            var input = GameInput.Instance;
            var control = HasControl && GameServices.Flow != null && GameServices.Flow.IsSimulating;
            var lookOnly = Hidden && _locks.Count == 1 && GameServices.Flow != null && GameServices.Flow.IsSimulating;
            var settings = GameServices.Settings;

            if (control || lookOnly)
            {
                var look = input.Look.ReadValue<Vector2>() * (lookScale * (settings?.MouseSensitivity ?? 1f));
                _yaw += look.x;
                _pitch += (settings != null && settings.InvertY ? 1f : -1f) * look.y;
                _pitch = Mathf.Clamp(_pitch, -84f, 84f);
                if (lookOnly) _yaw = Mathf.Clamp(Mathf.DeltaAngle(0f, _yaw - _hideYaw), -35f, 35f) + _hideYaw;
                if (control && input.Crouch.WasPressedThisFrame()) SetCrouched(!_crouched);
            }
            ApplyRotation();
            if (Hidden)
            {
                UpdateCamera(0f, 0f);
                return;
            }

            var move = control ? Vector2.ClampMagnitude(input.Move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            IsRunning = control && input.Run.IsPressed() && !_crouched && move.y > 0.1f;
            if (IsRunning && _crouched) SetCrouched(false);
            var speed = _crouched ? crouchSpeed : IsRunning ? runSpeed : walkSpeed;
            var heading = Quaternion.Euler(0f, _yaw, 0f);
            var target = heading * new Vector3(move.x, 0f, move.y) * speed;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, target, acceleration * speed * Time.deltaTime);

            if (_body.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;
            var before = transform.position;
            _body.Move((_planarVelocity + _impulse + Vector3.up * _verticalVelocity) * Time.deltaTime);
            _impulse = Vector3.MoveTowards(_impulse, Vector3.zero, 6f * Time.deltaTime);
            var travelled = Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude;

            UpdateCamera(travelled, settings?.CameraShake ?? 1f);
            UpdateFootsteps(travelled);
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdateCamera(float travelled, float shake)
        {
            var targetEye = _crouched ? crouchEyeHeight : standingEyeHeight;
            _eyeHeight = Mathf.Lerp(_eyeHeight, targetEye, 1f - Mathf.Exp(-10f * Time.deltaTime));
            _bobPhase += travelled * (IsRunning ? 2.1f : 2.6f);
            var amount = Mathf.Clamp01(Speed / walkSpeed) * shake * (IsRunning ? 0.032f : 0.018f);
            var bob = new Vector3(Mathf.Cos(_bobPhase * 0.5f) * amount * 0.6f, Mathf.Abs(Mathf.Sin(_bobPhase * 0.5f)) * amount, 0f);
            _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 1.8f);
            var jolt = _shake > 0f ? UnityEngine.Random.insideUnitSphere * (0.05f * _shake * shake) : Vector3.zero;
            if (cameraPivot != null) cameraPivot.localPosition = new Vector3(0f, _eyeHeight, 0f) + bob + jolt;
        }

        private void UpdateFootsteps(float travelled)
        {
            if (!_body.isGrounded || travelled <= 0f) return;
            _stepDistance += travelled;
            var stride = _crouched ? 0.55f : IsRunning ? 1.05f : 0.72f;
            if (_stepDistance < stride) return;
            _stepDistance = 0f;
            var surface = Surface.Concrete;
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 1f, Layers.AllButPlayer, QueryTriggerInteraction.Ignore))
            {
                surface = SurfaceType.Resolve(hit);
            }
            var loudness = SurfaceType.Loudness(surface) * (_crouched ? 0.35f : IsRunning ? 1.9f : 1f);
            Footstep?.Invoke(surface, loudness);
            NoiseEvents.Emit(transform.position, 6f * loudness, gameObject);
        }

        private bool CanStand()
        {
            var radius = _body.radius * 0.95f;
            var bottom = transform.position + Vector3.up * (radius + 0.05f);
            var top = transform.position + Vector3.up * (StandingHeight - radius);
            return !Physics.CheckCapsule(bottom, top, radius, Layers.AllButPlayer, QueryTriggerInteraction.Ignore);
        }
    }
}
