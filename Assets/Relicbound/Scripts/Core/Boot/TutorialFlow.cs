using System;
using Relicbound.Core.Save;

namespace Relicbound.Core.Boot
{
    public enum TutorialStep
    {
        Pan,
        Zoom,
        SelectKeep,
        Done,
    }

    public enum TutorialSignal
    {
        CameraPanned,
        CameraZoomed,
        KeepSelected,
    }

    /// <summary>
    /// Tutorial contextual de la pantalla principal (docs/01 paso 7): mover cámara, zoom y seleccionar el torreón.
    /// El progreso vive en <see cref="TutorialData.CompletedSteps"/> para no repetirlo tras reiniciar.
    /// </summary>
    public sealed class TutorialFlow
    {
        private static readonly TutorialStep[] Order = { TutorialStep.Pan, TutorialStep.Zoom, TutorialStep.SelectKeep };
        private readonly TutorialData _data;

        public TutorialFlow(TutorialData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public event Action<TutorialStep> StepChanged;

        public TutorialStep Current
        {
            get
            {
                foreach (var step in Order)
                {
                    if (!_data.CompletedSteps.Contains(step.ToString())) return step;
                }
                return TutorialStep.Done;
            }
        }

        public bool IsDone => Current == TutorialStep.Done;

        public int CompletedCount => Array.FindAll(Order, s => _data.CompletedSteps.Contains(s.ToString())).Length;

        public static int TotalSteps => Order.Length;

        /// <summary>Avanza si la señal corresponde al paso actual. Devuelve true si el tutorial acaba de terminar.</summary>
        public bool Notify(TutorialSignal signal)
        {
            var current = Current;
            var matches = (current == TutorialStep.Pan && signal == TutorialSignal.CameraPanned)
                          || (current == TutorialStep.Zoom && signal == TutorialSignal.CameraZoomed)
                          || (current == TutorialStep.SelectKeep && signal == TutorialSignal.KeepSelected);
            if (!matches) return false;
            _data.CompletedSteps.Add(current.ToString());
            StepChanged?.Invoke(Current);
            return IsDone;
        }
    }
}
