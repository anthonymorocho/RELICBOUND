using System;
using System.Collections;
using NUnit.Framework;
using Relicbound.Core.Boot;
using Relicbound.Core.Save;
using Relicbound.Game.Boot;
using Relicbound.Game.Castle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Relicbound.Game.Tests
{
    /// <summary>
    /// Recorrido real de la primera apertura (docs/01 §Flujo, pasos 1-7) desde la escena Boot, con capturas
    /// de cada pantalla para la revisión de calidad (docs/18).
    /// </summary>
    public class PlayerJourneyTests
    {
        private MemorySaveStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new MemorySaveStorage();
            GameRoot.StorageOverride = _storage;
            if (GameRoot.Instance != null) UnityEngine.Object.DestroyImmediate(GameRoot.Instance.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            GameRoot.StorageOverride = null;
            if (GameRoot.Instance != null) UnityEngine.Object.DestroyImmediate(GameRoot.Instance.gameObject);
        }

        [UnityTest]
        public IEnumerator FirstOpen_GoesFromSplashToCastleWithoutMainMenu()
        {
            SceneManager.LoadScene("Boot");
            yield return new WaitForSeconds(1.0f);
            yield return JourneyCapture.Capture("01_splash");

            yield return WaitUntil(() => CastleDirector.Instance != null && CastleDirector.Instance.Stage == BootStage.ProfileCreation, 20f, "pantalla de perfil");
            yield return new WaitForSeconds(1.2f);
            yield return JourneyCapture.Capture("02_perfil_vacio");

            var director = CastleDirector.Instance;
            director.Profile.SetNames("A", "Bastión del Alba");
            Assert.That(director.Profile.Submit(), Is.False, "Un nombre de una letra no debe aceptarse.");
            yield return JourneyCapture.Capture("03_perfil_error");

            director.Profile.SelectBanner("banner_crimson");
            director.Profile.SetNames("Patricia", "Bastión del Alba");
            yield return JourneyCapture.Capture("04_perfil_relleno");
            Assert.That(director.Profile.Submit(), Is.True);

            yield return WaitUntil(() => director.Stage == BootStage.Intro, 5f, "introducción");
            yield return new WaitForSeconds(2.0f);
            yield return JourneyCapture.Capture("05_intro");
            Assert.That(_storage.Read(SaveService.MainKey), Is.Not.Null, "El perfil se guarda al fundar la fortaleza.");

            yield return new WaitForSeconds(5.0f);
            yield return JourneyCapture.Capture("06_intro_plano2");
            yield return WaitUntil(() => director.Stage == BootStage.Castle, 20f, "castillo");
            yield return new WaitForSeconds(1.5f);
            yield return JourneyCapture.Capture("07_castillo_hud");

            var keep = FindBuilding("keep");
            Assert.That(keep, Is.Not.Null, "El torreón debe ser seleccionable.");
            director.Selection.Select(keep);
            yield return new WaitForSeconds(1.2f);
            yield return JourneyCapture.Capture("08_seleccion_torreon");

            director.Selection.Select(null);
            director.OpenSettings();
            yield return new WaitForSeconds(0.6f);
            yield return JourneyCapture.Capture("09_ajustes");

            var saved = new SaveService(_storage).Load();
            Assert.That(saved.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(saved.Save.Profile.PlayerName, Is.EqualTo("Patricia"));
            Assert.That(saved.Save.Profile.BannerId, Is.EqualTo("banner_crimson"));
            Assert.That(saved.Save.Tutorial.IntroSeen, Is.True);
        }

        [UnityTest]
        public IEnumerator ReturningPlayer_LandsDirectlyInCastle()
        {
            var session = GameSession.StartNew(
                Core.Profile.PlayerProfile.CreateNew("Patricia", "Bastión del Alba", "banner_azure", DateTime.UtcNow),
                StartingRules.CreateDefault(), Core.Progression.LevelCurve.CreateDefault());
            session.Tutorial.IntroSeen = true;
            session.Progress.AddXp(160);
            new SaveService(_storage).Save(session.ToSave(), DateTime.UtcNow);

            SceneManager.LoadScene("Boot");
            yield return WaitUntil(() => CastleDirector.Instance != null && CastleDirector.Instance.Stage == BootStage.Castle, 20f, "castillo");
            yield return new WaitForSeconds(1.5f);
            yield return JourneyCapture.Capture("10_jugador_que_vuelve");
            yield return JourneyCapture.Capture("11_android_2400x1080", 2400, 1080);
        }

        private static BuildingView FindBuilding(string id)
        {
            foreach (var view in UnityEngine.Object.FindObjectsByType<BuildingView>(FindObjectsSortMode.None))
            {
                if (view.Definition != null && view.Definition.id == id) return view;
            }
            return null;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeout, string what)
        {
            var start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout) Assert.Fail($"No se llegó a: {what} en {timeout} s.");
                yield return null;
            }
        }
    }
}
