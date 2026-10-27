using BeforeItWasYou.Core.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeforeItWasYou.Services
{
    /// <summary>Aplica al volumen de postproceso las opciones de accesibilidad (desenfoque de movimiento, grano).</summary>
    [RequireComponent(typeof(Volume))]
    public sealed class PostProcessSettings : MonoBehaviour
    {
        private Volume _volume;

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            // Copia en tiempo de ejecución: no se modifica el asset del proyecto.
            _volume.profile = Instantiate(_volume.sharedProfile);
            SettingsApplier.Applied += Apply;
            if (GameServices.IsReady) Apply(GameServices.Settings);
        }

        private void OnDestroy() => SettingsApplier.Applied -= Apply;

        private void Apply(GameSettings settings)
        {
            if (_volume == null || _volume.profile == null) return;
            if (_volume.profile.TryGet<MotionBlur>(out var blur)) blur.active = settings.MotionBlur;
            if (_volume.profile.TryGet<FilmGrain>(out var grain)) grain.intensity.value = settings.ReduceFlashes ? 0.08f : 0.16f;
            if (_volume.profile.TryGet<Bloom>(out var bloom)) bloom.intensity.value = settings.ReduceFlashes ? 0.35f : 0.55f;
        }
    }
}
