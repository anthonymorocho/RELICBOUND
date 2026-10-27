using System.Collections;
using BeforeItWasYou.Core.Flow;
using BeforeItWasYou.Input;
using BeforeItWasYou.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BeforeItWasYou.Boot
{
    /// <summary>
    /// L_Startup: aviso de contenido mientras se carga el episodio en segundo plano; después pasa al menú,
    /// que vive dentro de L_E01_Arrival sobre la casa real.
    /// </summary>
    public sealed class StartupDirector : MonoBehaviour
    {
        public const string EpisodeScene = "L_E01_Arrival";

        [SerializeField] private UIDocument document;
        [SerializeField] private float noticeSeconds = 4.5f;

        private IEnumerator Start()
        {
            GameServices.Initialize();
            Cursor.visible = false;
            GameInput.Instance.UsePlayer(false);
            var notice = document != null ? document.rootVisualElement.Q("notice") : null;
            yield return null;
            notice?.AddToClassList("notice--on");

            var loading = SceneManager.LoadSceneAsync(EpisodeScene, LoadSceneMode.Single);
            loading.allowSceneActivation = false;
            var end = Time.unscaledTime + noticeSeconds;
            while (Time.unscaledTime < end || loading.progress < 0.9f)
            {
                var input = GameInput.Instance;
                if (Time.unscaledTime > 1.2f && (input.Submit.WasPressedThisFrame() || input.Cancel.WasPressedThisFrame())) end = 0f;
                yield return null;
            }
            notice?.RemoveFromClassList("notice--on");
            var fadeEnd = Time.unscaledTime + 1.1f;
            while (Time.unscaledTime < fadeEnd) yield return null;
            GameServices.Flow.TryEnter(GameState.Menu);
            loading.allowSceneActivation = true;
        }
    }
}
