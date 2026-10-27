using System.Collections;
using Relicbound.Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Relicbound.Game.Boot
{
    /// <summary>
    /// Escena Boot: muestra la marca (máx. ~3 s, docs/01 paso 2), carga el guardado y pasa al castillo.
    /// No existe menú principal: lo siguiente siempre es la fortaleza del jugador.
    /// </summary>
    public sealed class BootDirector : MonoBehaviour
    {
        public const string CastleScene = "Castle";

        [SerializeField] private UIDocument document;
        [SerializeField] private float minimumSplashSeconds = 2.2f;

        private SplashView _splash;

        private IEnumerator Start()
        {
            Application.targetFrameRate = Application.isMobilePlatform ? 60 : -1;
            var root = GameRoot.Ensure();
            _splash = new SplashView(document.rootVisualElement);
            _splash.FadeIn();

            var started = Time.realtimeSinceStartup;
            root.LoadAndAdvance();
            var loading = SceneManager.LoadSceneAsync(CastleScene);
            loading.allowSceneActivation = false;

            while (loading.progress < 0.9f || Time.realtimeSinceStartup - started < minimumSplashSeconds)
            {
                _splash.SetProgress(Mathf.Min(loading.progress / 0.9f, (Time.realtimeSinceStartup - started) / minimumSplashSeconds));
                yield return null;
            }
            _splash.SetProgress(1f);
            _splash.FadeOut();
            yield return new WaitForSecondsRealtime(SplashView.FadeSeconds);
            loading.allowSceneActivation = true;
        }
    }
}
