using System.Collections;
using System.IO;
using BeforeItWasYou.Core.Flow;
using BeforeItWasYou.Episode;
using BeforeItWasYou.Services;
using BeforeItWasYou.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace BeforeItWasYou.Tests
{
    /// <summary>
    /// Recorrido del jugador como lo vive: arranque, menú, opciones, prólogo, llegada, movimiento y pausa.
    /// Deja capturas en Evidence/journey para la revisión visual.
    /// </summary>
    public sealed class PlayerJourneyTests
    {
        private Keyboard _keyboard;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Partidas y opciones de prueba en una carpeta aparte: nunca se tocan las del jugador.
            var folder = Path.Combine(Application.temporaryCachePath, "TestSaves");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            GameServices.Initialize(folder);
            // En el editor sin ventana de juego enfocada, el teclado iría al editor: se envía todo al juego.
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Arranque_MenuPrincipal_Opciones_Prologo_Llegada_Pausa()
        {
            SceneManager.LoadScene("L_Startup");
            yield return WaitFor(() => EpisodeDirector.Current != null, 40f, "El episodio no se cargó desde L_Startup.");
            Assert.AreEqual(GameState.Menu, GameServices.Flow.State);
            var root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Assert.IsFalse(root.Q("main").ClassListContains("hidden"), "El menú principal debe estar visible.");
            var items = root.Q("main-list").Query<Label>(className: "menu-item__label").ToList();
            CollectionAssert.AreEqual(new[] { "NUEVA PARTIDA", "OPCIONES", "CRÉDITOS", "SALIR" }, items.ConvertAll(l => l.text),
                "Sin partida guardada no debe aparecer Continuar.");
            yield return new WaitForSecondsRealtime(2.2f);
            yield return JourneyCapture.Capture("01_menu_principal");

            // Opciones: bajar dos filas y subir el volumen con la flecha derecha.
            yield return Tap(Key.DownArrow);
            yield return Tap(Key.Enter);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsFalse(root.Q("options").ClassListContains("hidden"), "Opciones debe abrirse desde el menú.");
            yield return JourneyCapture.Capture("02_opciones");
            var before = GameServices.Settings.MouseSensitivity;
            for (var i = 0; i < 10; i++) yield return Tap(Key.DownArrow);
            yield return Tap(Key.RightArrow);
            yield return Tap(Key.Escape);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(root.Q("options").ClassListContains("hidden"));
            Assert.Greater(GameServices.Settings.MouseSensitivity, before, "La sensibilidad debe cambiar y guardarse al volver.");
            Assert.AreEqual(GameServices.Settings.MouseSensitivity, GameServices.SettingsStore.Load().MouseSensitivity);

            // Nueva partida con prólogo.
            var director = EpisodeDirector.Current;
            director.StartCoroutine(director.NewGameRoutine(false));
            yield return WaitFor(() => !root.Q("prologue").ClassListContains("hidden"), 5f, "El prólogo no empezó.");
            yield return new WaitForSecondsRealtime(6.8f);
            yield return JourneyCapture.Capture("03_prologo");
            yield return WaitFor(() => GameServices.Flow.State == GameState.Playing, 60f, "La llegada no empezó tras el prólogo.");
            yield return new WaitForSecondsRealtime(2.2f);
            yield return JourneyCapture.Capture("04_llegada");

            // Caminar hacia delante un segundo.
            var player = director.Player;
            var start = player.transform.position;
            Press(Key.W, true);
            yield return new WaitForSeconds(1.2f);
            Press(Key.W, false);
            yield return null;
            var moved = Vector3.Distance(start, player.transform.position);
            Assert.Greater(moved, 1f, "Daniel debe caminar con W.");
            Assert.Less(moved, 3.5f, "Caminar no debe ser a velocidad de carrera.");

            // Pausa con Esc y vuelta al juego.
            yield return Tap(Key.Escape);
            Assert.AreEqual(GameState.Paused, GameServices.Flow.State);
            Assert.AreEqual(0f, Time.timeScale);
            yield return JourneyCapture.Capture("05_pausa");
            yield return Tap(Key.Escape);
            Assert.AreEqual(GameState.Playing, GameServices.Flow.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        private void Press(Key key, bool down)
        {
            // Sin InputSystem.Update() manual: el evento se procesa en la actualización normal del siguiente frame,
            // así WasPressedThisFrame lo ve igual que con un teclado real.
            InputSystem.QueueStateEvent(_keyboard, down ? new KeyboardState(key) : new KeyboardState());
        }

        private IEnumerator Tap(Key key)
        {
            Press(key, true);
            yield return null;
            Press(key, false);
            yield return null;
            yield return new WaitForSecondsRealtime(0.12f);
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string message)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail(message);
                yield return null;
            }
        }
    }
}
