using System;
using System.IO;
using Relicbound.Core.Boot;
using Relicbound.Core.Profile;
using Relicbound.Core.Progression;
using Relicbound.Core.Save;
using UnityEngine;

namespace Relicbound.Game.Boot
{
    /// <summary>
    /// Estado persistente de la aplicación entre escenas: flujo de arranque, guardado y sesión de juego.
    /// Se crea en la escena Boot; si se abre Castle directamente (editor), se crea bajo demanda.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        /// <summary>Permite a los tests usar almacenamiento en memoria en lugar del disco.</summary>
        public static ISaveStorage StorageOverride;

        public static GameRoot Instance { get; private set; }

        public BootFlow Flow { get; private set; }

        public SaveService Saves { get; private set; }

        public GameSession Session { get; private set; }

        public LevelCurve Curve { get; private set; }

        public StartingRules Rules { get; private set; }

        public static GameRoot Ensure()
        {
            if (Instance == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var storage = StorageOverride ?? new FileSaveStorage(Path.Combine(Application.persistentDataPath, "saves"));
            Saves = new SaveService(storage);
            Flow = new BootFlow();
            Curve = LevelCurve.CreateDefault();
            Rules = StartingRules.CreateDefault();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Carga la partida y avanza el flujo de arranque.</summary>
        public void LoadAndAdvance()
        {
            if (Flow.Stage == BootStage.Splash) Flow.SplashFinished();
            if (Flow.Stage != BootStage.LoadingSave) return;
            var result = Saves.Load();
            if (result.Save != null && result.Save.Profile != null)
            {
                Session = GameSession.FromSave(result.Save, Rules, Curve);
            }
            Flow.SaveLoaded(result);
        }

        public void CreateProfile(string playerName, string castleName, string bannerId)
        {
            var profile = PlayerProfile.CreateNew(playerName, castleName, bannerId, DateTime.UtcNow);
            Session = GameSession.StartNew(profile, Rules, Curve);
            Save();
            Flow.ProfileCreated();
        }

        public void FinishIntro()
        {
            if (Session != null)
            {
                Session.Tutorial.IntroSeen = true;
                Save();
            }
            Flow.IntroFinished();
        }

        public void Save()
        {
            if (Session == null) return;
            try
            {
                Saves.Save(Session.ToSave(), DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                // Un fallo de disco no debe cerrar el juego; se reintentará en el siguiente guardado.
                Debug.LogException(exception);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();
    }
}
