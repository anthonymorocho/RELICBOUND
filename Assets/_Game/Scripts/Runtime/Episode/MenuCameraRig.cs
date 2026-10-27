using BeforeItWasYou.Services;
using UnityEngine;

namespace BeforeItWasYou.Episode
{
    /// <summary>
    /// Cámara del menú: un travelling muy lento entre dos encuadres de la fachada con leve movimiento de mano,
    /// para que el fondo sea la escena real y no una imagen fija.
    /// </summary>
    public sealed class MenuCameraRig : MonoBehaviour
    {
        [SerializeField] private Vector3 fromPosition;
        [SerializeField] private Vector3 fromEuler;
        [SerializeField] private Vector3 toPosition;
        [SerializeField] private Vector3 toEuler;
        [SerializeField] private float period = 48f;
        [SerializeField] private float handheld = 0.35f;

        private float _time;

        public void Configure(Vector3 startPosition, Vector3 startEuler, Vector3 endPosition, Vector3 endEuler, float seconds)
        {
            fromPosition = startPosition;
            fromEuler = startEuler;
            toPosition = endPosition;
            toEuler = endEuler;
            period = seconds;
            Apply(0f);
        }

        /// <summary>Encuadre fijo en un punto del recorrido (para capturas reproducibles).</summary>
        public void Hold(float t)
        {
            _time = t * period * 0.5f;
            Apply(t);
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            var phase = Mathf.PingPong(_time / (period * 0.5f), 1f);
            Apply(phase * phase * (3f - 2f * phase));
        }

        private void Apply(float t)
        {
            var shake = GameServices.IsReady ? GameServices.Settings.CameraShake : 1f;
            var noise = new Vector3(
                Mathf.PerlinNoise(_time * 0.11f, 0.3f) - 0.5f,
                Mathf.PerlinNoise(0.7f, _time * 0.09f) - 0.5f,
                0f) * (handheld * shake);
            transform.SetPositionAndRotation(
                Vector3.Lerp(fromPosition, toPosition, t),
                Quaternion.Euler(Vector3.Lerp(fromEuler, toEuler, t) + noise));
        }
    }
}
