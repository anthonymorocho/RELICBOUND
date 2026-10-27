using System;
using BeforeItWasYou.Player;
using UnityEngine;

namespace BeforeItWasYou.Episode
{
    /// <summary>Volumen que avisa cuando Daniel entra o sale (dentro de casa, cobertizo, calle...).</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ZoneTrigger : MonoBehaviour
    {
        [SerializeField] private string zone;

        public string Zone => zone;
        public bool PlayerInside { get; private set; }
        public float InsideSince { get; private set; }

        public event Action<ZoneTrigger> Entered;
        public event Action<ZoneTrigger> Exited;

        public void Configure(string zoneName, Vector3 size)
        {
            zone = zoneName;
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            PlayerInside = true;
            InsideSince = Time.time;
            Entered?.Invoke(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            PlayerInside = false;
            Exited?.Invoke(this);
        }
    }
}
