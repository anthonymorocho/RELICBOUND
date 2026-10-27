using System;
using Relicbound.Core.Save;

namespace Relicbound.Core.Boot
{
    /// <summary>Etapas del arranque (docs/01 §Flujo, pasos 1-6). No existe menú principal.</summary>
    public enum BootStage
    {
        Splash,
        LoadingSave,
        /// <summary>Primera apertura o partida irrecuperable: crear perfil.</summary>
        ProfileCreation,
        /// <summary>Introducción contextual breve, solo la primera vez.</summary>
        Intro,
        /// <summary>El castillo 3D del jugador: pantalla principal.</summary>
        Castle,
        /// <summary>Guardado de una versión más nueva: no se puede continuar sin actualizar.</summary>
        UpdateRequired,
    }

    /// <summary>
    /// Máquina de estados del arranque, sin dependencias de Unity. El BootDirector de escena
    /// escucha <see cref="StageChanged"/> y carga lo necesario para cada etapa.
    /// </summary>
    public sealed class BootFlow
    {
        public BootFlow()
        {
            Stage = BootStage.Splash;
        }

        public BootStage Stage { get; private set; }

        /// <summary>Último resultado de carga; la UI avisa si hubo recuperación o corrupción.</summary>
        public LoadResult LastLoad { get; private set; }

        public event Action<BootStage> StageChanged;

        public void SplashFinished()
        {
            Expect(BootStage.Splash);
            MoveTo(BootStage.LoadingSave);
        }

        public void SaveLoaded(LoadResult result)
        {
            Expect(BootStage.LoadingSave);
            LastLoad = result ?? throw new ArgumentNullException(nameof(result));
            switch (result.Status)
            {
                case LoadStatus.Loaded:
                case LoadStatus.RecoveredFromBackup:
                    if (result.Save.Profile == null) MoveTo(BootStage.ProfileCreation);
                    else MoveTo(result.Save.Tutorial.IntroSeen ? BootStage.Castle : BootStage.Intro);
                    break;
                case LoadStatus.NewerVersion:
                    MoveTo(BootStage.UpdateRequired);
                    break;
                default:
                    MoveTo(BootStage.ProfileCreation);
                    break;
            }
        }

        public void ProfileCreated()
        {
            Expect(BootStage.ProfileCreation);
            MoveTo(BootStage.Intro);
        }

        public void IntroFinished()
        {
            Expect(BootStage.Intro);
            MoveTo(BootStage.Castle);
        }

        private void Expect(BootStage expected)
        {
            if (Stage != expected)
            {
                throw new InvalidOperationException("Transición no válida: se esperaba " + expected + " y la etapa es " + Stage + ".");
            }
        }

        private void MoveTo(BootStage stage)
        {
            Stage = stage;
            StageChanged?.Invoke(stage);
        }
    }
}
