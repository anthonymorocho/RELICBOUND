using System.Collections;
using Relicbound.Core.Settings;
using UnityEngine;

namespace Relicbound.Game.Audio
{
    public enum UiSound
    {
        Click,
        PanelOpen,
        PanelClose,
        Confirm,
        Error,
        Tick,
        Banner,
        Coins,
        Select,
    }

    /// <summary>
    /// Audio de la pantalla principal: ambiente en bucle, música discreta con fundidos y efectos de interfaz.
    /// Volumen por categoría según los ajustes (docs/14); el general lo aplica el AudioListener.
    /// </summary>
    public sealed class CastleAudio : MonoBehaviour
    {
        [SerializeField] private AudioLibrary library;
        [SerializeField] private float musicFadeSeconds = 3f;
        [SerializeField] private float pauseBetweenTracks = 12f;

        private AudioSource _ui;
        private AudioSource[] _ambience;
        private AudioSource[] _music;
        private int _activeMusic;
        private int _track;
        private GameSettings _settings = new GameSettings();
        private float _lastTick;

        private void Awake()
        {
            _ui = CreateSource("UI", false);
            _ambience = new AudioSource[library != null ? library.ambience.Length : 0];
            for (var i = 0; i < _ambience.Length; i++)
            {
                _ambience[i] = CreateSource("Ambience" + i, true);
                _ambience[i].clip = library.ambience[i];
            }
            _music = new[] { CreateSource("MusicA", false), CreateSource("MusicB", false) };
        }

        private void Start()
        {
            foreach (var source in _ambience)
            {
                // Desfase aleatorio: los bucles no coinciden y la repetición no se percibe.
                source.time = Random.Range(0f, Mathf.Max(0f, source.clip.length - 1f));
                source.Play();
            }
            if (library != null && library.castleMusic.Length > 0) StartCoroutine(MusicLoop());
            ApplyVolumes(_settings);
        }

        public void ApplyVolumes(GameSettings settings)
        {
            _settings = settings ?? new GameSettings();
            _ui.volume = _settings.InterfaceVolume;
            foreach (var source in _ambience) source.volume = _settings.AmbienceVolume;
        }

        public void Play(UiSound sound)
        {
            if (library == null) return;
            var clip = sound switch
            {
                UiSound.Click => library.click,
                UiSound.PanelOpen => library.panelOpen,
                UiSound.PanelClose => library.panelClose,
                UiSound.Confirm => library.confirm,
                UiSound.Error => library.error,
                UiSound.Tick => library.tick,
                UiSound.Banner => library.banner,
                UiSound.Coins => library.coins,
                UiSound.Select => library.select,
                _ => null,
            };
            if (clip == null) return;
            if (sound == UiSound.Tick)
            {
                // Los deslizadores disparan muchos eventos: como mucho un tic cada 70 ms.
                if (Time.unscaledTime - _lastTick < 0.07f) return;
                _lastTick = Time.unscaledTime;
            }
            _ui.pitch = sound == UiSound.Click || sound == UiSound.Tick ? Random.Range(0.96f, 1.04f) : 1f;
            _ui.PlayOneShot(clip, sound == UiSound.Tick ? 0.5f : 1f);
        }

        private IEnumerator MusicLoop()
        {
            _track = Random.Range(0, library.castleMusic.Length);
            yield return new WaitForSecondsRealtime(2f);
            while (true)
            {
                var source = _music[_activeMusic];
                source.clip = library.castleMusic[_track];
                source.volume = 0f;
                source.Play();
                yield return Fade(source, _settings.MusicVolume);
                // Seguir el volumen de ajustes mientras suena; preparar el fundido antes del final.
                while (source.isPlaying && source.time < source.clip.length - musicFadeSeconds)
                {
                    source.volume = _settings.MusicVolume;
                    yield return null;
                }
                yield return Fade(source, 0f);
                source.Stop();
                yield return new WaitForSecondsRealtime(pauseBetweenTracks);
                _track = (_track + 1) % library.castleMusic.Length;
                _activeMusic = 1 - _activeMusic;
            }
        }

        private IEnumerator Fade(AudioSource source, float target)
        {
            var start = source.volume;
            for (var t = 0f; t < musicFadeSeconds; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, target, t / musicFadeSeconds);
                yield return null;
            }
            source.volume = target;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
