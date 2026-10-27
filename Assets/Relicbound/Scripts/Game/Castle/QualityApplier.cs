using Relicbound.Core.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Relicbound.Game.Castle
{
    /// <summary>
    /// Aplica calidad gráfica (docs/16) y volumen general. Trabaja sobre una copia en memoria del asset URP
    /// para no modificar el asset del proyecto.
    /// </summary>
    public static class QualityApplier
    {
        private static UniversalRenderPipelineAsset _runtimeAsset;

        public static void Apply(GameSettings settings)
        {
            AudioListener.volume = settings.MasterVolume;

            if (_runtimeAsset == null)
            {
                var source = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                if (source == null) return;
                _runtimeAsset = Object.Instantiate(source);
                _runtimeAsset.name = source.name + " (runtime)";
                QualitySettings.renderPipeline = _runtimeAsset;
            }

            switch (settings.Quality)
            {
                case QualityPreset.Low:
                    GrassField.DensityScale = 0.3f;
                    _runtimeAsset.renderScale = 0.75f;
                    _runtimeAsset.msaaSampleCount = 1;
                    _runtimeAsset.shadowCascadeCount = 1;
                    _runtimeAsset.shadowDistance = 70f;
                    break;
                case QualityPreset.Medium:
                    GrassField.DensityScale = Application.isMobilePlatform ? 0.45f : 0.75f;
                    _runtimeAsset.renderScale = Application.isMobilePlatform ? 0.85f : 1f;
                    _runtimeAsset.msaaSampleCount = 2;
                    _runtimeAsset.shadowCascadeCount = 2;
                    _runtimeAsset.shadowDistance = 100f;
                    break;
                default:
                    GrassField.DensityScale = 1f;
                    _runtimeAsset.renderScale = 1f;
                    _runtimeAsset.msaaSampleCount = 4;
                    _runtimeAsset.shadowCascadeCount = 4;
                    _runtimeAsset.shadowDistance = 120f;
                    break;
            }
        }
    }
}
