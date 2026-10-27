using System.Collections;
using System.IO;
using BeforeItWasYou.AI;
using BeforeItWasYou.Core.Flow;
using BeforeItWasYou.Episode;
using BeforeItWasYou.Interaction;
using BeforeItWasYou.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeforeItWasYou.Tests
{
    /// <summary>Partes de la historia que dependen de la escena real: checkpoints, reintento, Víctor y escondite.</summary>
    public sealed class StoryTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var folder = Path.Combine(Application.temporaryCachePath, "StorySaves");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            GameServices.Initialize(folder);
            SceneManager.LoadScene("L_E01_Arrival");
            yield return WaitFor(() => EpisodeDirector.Current != null, 30f, "No cargó el episodio.");
            var director = EpisodeDirector.Current;
            director.StartCoroutine(director.NewGameRoutine(true));
            yield return WaitFor(() => GameServices.Flow.State == GameState.Playing, 20f, "No empezó la partida.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator EntrarEnCasa_Guarda_YReintentoVuelveAlCheckpoint()
        {
            var player = EpisodeDirector.Current.Player;
            player.Teleport(new Vector3(7.0f, 0.5f, 8.0f), 0f);
            yield return WaitFor(() => GameServices.Saves.HasContinuableSave(), 5f, "Entrar en la casa debe guardar CP00.");
            Assert.AreEqual("CP00", GameServices.Saves.Load().Snapshot.CheckpointId);
            yield return JourneyCapture.Capture("10_sala_a_oscuras");

            EpisodeDirector.Current.Fail("NO HUBO TIEMPO", "Prueba");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(GameState.Failed, GameServices.Flow.State);
            yield return JourneyCapture.Capture("11_pantalla_fallo");
            EpisodeDirector.Current.Retry();
            yield return WaitFor(() => EpisodeDirector.Current != null && GameServices.Flow.State == GameState.Playing, 30f, "El reintento no volvió a la partida.");
            var back = EpisodeDirector.Current.Player.transform.position;
            Assert.Less(Vector3.Distance(back, new Vector3(7.0f, 0.5f, 8.0f)), 1.5f, "El reintento debe empezar donde se guardó.");
        }

        [UnityTest]
        public IEnumerator Victor_VisitaElLavadero_PorLaNavMesh_YVuelveAlPatio()
        {
            var victor = Object.FindFirstObjectByType<IntruderAI>();
            Assert.IsNotNull(victor, "Víctor debe existir en la escena desde el principio.");
            Assert.IsTrue(victor.GetComponent<NavMeshAgent>().isOnNavMesh, "Víctor debe estar sobre la NavMesh.");
            EpisodeDirector.Current.Player.Teleport(new Vector3(6.0f, 0.5f, 7.5f), 0f);
            victor.BeginLaundryVisit();
            yield return WaitFor(() => victor.State == IntruderState.Laundry, 3f, "No empezó la visita.");
            yield return WaitFor(() => victor.State == IntruderState.Outside, 90f, "Víctor no volvió al patio.");
            var box = GameObject.Find("Story/Clues/Prop_LaundryBox");
            Assert.IsFalse(box.GetComponent<Rigidbody>().isKinematic, "La caja del lavadero debe haber caído.");
        }

        [UnityTest]
        public IEnumerator Encuentro_EnElCobertizo_YRetiradaSinConfrontacion()
        {
            var state = EpisodeState.Current;
            state.SetFlag(StoryDirector.FlagNoise);
            var player = EpisodeDirector.Current.Player;
            var shed = GameObject.Find("Environment/Doors/Door_Shed").GetComponent<Door>();
            shed.SetState(DoorState.Open, true);
            player.Teleport(new Vector3(16.6f, 0.2f, 23.6f), 230f);
            var victor = Object.FindFirstObjectByType<IntruderAI>();
            yield return WaitFor(() => victor.State == IntruderState.Warning, 30f, "Víctor no llegó a la puerta del cobertizo.");
            yield return new WaitForSeconds(1.5f);
            player.Teleport(new Vector3(16.6f, 0.2f, 23.6f), 260f);
            yield return JourneyCapture.Capture("12_encuentro_victor");
            // Daniel se aparta al fondo, lejos de la mochila: Víctor la coge y se va.
            player.Teleport(new Vector3(16.75f, 0.2f, 24.2f), 250f);
            yield return WaitFor(() => victor.State == IntruderState.RetrievingBag || victor.State == IntruderState.Fleeing, 15f, "Víctor debía recoger la mochila.");
            yield return WaitFor(() => victor.State == IntruderState.Gone, 60f, "Víctor debía irse por el portón.");
            Assert.IsTrue(state.Flag(StoryDirector.FlagVictorGone));
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
