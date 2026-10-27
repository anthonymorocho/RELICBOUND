using UnityEngine;

namespace Relicbound.Game.Audio
{
    /// <summary>Clips del castillo (docs/14 §Lista P1). Orígenes y licencias en docs/19.</summary>
    [CreateAssetMenu(menuName = "Relicbound/Biblioteca de audio", fileName = "AL_Castle")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Header("Interfaz")]
        public AudioClip click;
        public AudioClip panelOpen;
        public AudioClip panelClose;
        public AudioClip confirm;
        public AudioClip error;
        public AudioClip tick;
        public AudioClip banner;
        public AudioClip coins;
        public AudioClip select;

        [Header("Ambiente (bucles)")]
        public AudioClip[] ambience;

        [Header("Música del castillo")]
        public AudioClip[] castleMusic;
    }
}
