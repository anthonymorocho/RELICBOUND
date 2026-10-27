using UnityEngine;

namespace BeforeItWasYou.Audio
{
    /// <summary>
    /// Ambiente nocturno exterior (grillos, viento): dentro de la casa se oye más bajo y apagado, como a través
    /// de las paredes. La casa se define con una caja; el oyente es la cámara activa.
    /// </summary>
    public sealed class AmbienceZone : MonoBehaviour
    {
        [SerializeField] private AudioSource[] sources = new AudioSource[0];
        [SerializeField] private Bounds indoor = new Bounds(new Vector3(9.5f, 1.6f, 12f), new Vector3(10f, 3.2f, 12f));
        [SerializeField] private float indoorVolume = 0.32f;
        [SerializeField] private float indoorCutoff = 900f;

        private float _blend;

        public void Configure(AudioSource[] ambience, Bounds house)
        {
            sources = ambience;
            indoor = house;
        }

        private void Update()
        {
            var listener = Camera.main != null ? Camera.main.transform.position : transform.position;
            foreach (var cam in Camera.allCameras)
            {
                if (cam.isActiveAndEnabled) listener = cam.transform.position;
            }
            var target = indoor.Contains(listener) ? 1f : 0f;
            _blend = Mathf.MoveTowards(_blend, target, Time.unscaledDeltaTime * 1.6f);
            foreach (var s in sources)
            {
                if (s == null) continue;
                var bus = s.GetComponent<BusVolume>();
                if (bus != null) s.volume = bus.BaseVolume * AudioBuses.Get(bus.Bus) * Mathf.Lerp(1f, indoorVolume, _blend);
                var lp = s.GetComponent<AudioLowPassFilter>();
                if (lp != null) lp.cutoffFrequency = Mathf.Lerp(22000f, indoorCutoff, _blend);
            }
        }
    }
}
