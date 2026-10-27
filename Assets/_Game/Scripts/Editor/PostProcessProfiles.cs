using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeforeItWasYou.EditorTools
{
    /// <summary>
    /// Perfil de postproceso nocturno: tonemapping ACES, bloom contenido para lámparas, viñeta suave y grano fino.
    /// Sin filtro VHS ni saturación exagerada (DIRECCION_ARTISTICA §Norte visual).
    /// </summary>
    public static class PostProcessProfiles
    {
        public const string NightPath = ProjectBootstrap.SettingsFolder + "/PP_E01_Night.asset";

        public static VolumeProfile Night()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(NightPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, NightPath);
            }

            var tonemapping = Get<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var exposure = Get<ColorAdjustments>(profile);
            exposure.postExposure.Override(0.35f);
            exposure.contrast.Override(8f);
            exposure.saturation.Override(-6f);
            exposure.colorFilter.Override(new Color(1f, 0.985f, 0.96f));

            var white = Get<WhiteBalance>(profile);
            white.temperature.Override(-4f);
            white.tint.Override(2f);

            var smh = Get<ShadowsMidtonesHighlights>(profile);
            smh.shadows.Override(new Vector4(0.93f, 0.98f, 1.08f, -0.02f));
            smh.highlights.Override(new Vector4(1.05f, 1.0f, 0.93f, 0f));

            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.62f);
            bloom.highQualityFiltering.Override(true);

            var vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.27f);
            vignette.smoothness.Override(0.42f);

            var grain = Get<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.16f);
            grain.response.Override(0.85f);

            var blur = Get<MotionBlur>(profile);
            blur.active = false;
            blur.intensity.Override(0.18f);
            blur.quality.Override(MotionBlurQuality.Medium);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>(NightPath);
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var component)) return component;
            component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
