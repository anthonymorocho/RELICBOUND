using System;
using System.Collections.Generic;

namespace BeforeItWasYou.Core.Flow
{
    public enum GameState
    {
        Boot,
        Menu,
        Playing,
        Paused,
        Failed,
        Epilogue,
        Credits,
    }

    /// <summary>
    /// Estados globales del juego (ARQUITECTURA_UNITY §Servicios). Solo se permiten transiciones con sentido:
    /// por ejemplo, no se puede pausar en el menú ni morir durante el epílogo.
    /// </summary>
    public sealed class GameFlow
    {
        private static readonly Dictionary<GameState, GameState[]> Allowed = new Dictionary<GameState, GameState[]>
        {
            { GameState.Boot, new[] { GameState.Menu } },
            { GameState.Menu, new[] { GameState.Playing, GameState.Credits } },
            { GameState.Playing, new[] { GameState.Paused, GameState.Failed, GameState.Epilogue, GameState.Menu } },
            { GameState.Paused, new[] { GameState.Playing, GameState.Menu } },
            { GameState.Failed, new[] { GameState.Playing, GameState.Menu } },
            { GameState.Epilogue, new[] { GameState.Credits, GameState.Menu } },
            { GameState.Credits, new[] { GameState.Menu } },
        };

        public GameState State { get; private set; } = GameState.Boot;

        public event Action<GameState, GameState> Changed;

        public bool CanEnter(GameState next) => Allowed[State].Contains(next);

        public void Enter(GameState next)
        {
            if (next == State) return;
            if (!CanEnter(next)) throw new InvalidOperationException($"Transición no permitida: {State} → {next}.");
            var previous = State;
            State = next;
            Changed?.Invoke(previous, next);
        }

        public bool TryEnter(GameState next)
        {
            if (next == State) return true;
            if (!CanEnter(next)) return false;
            Enter(next);
            return true;
        }

        /// <summary>La simulación del episodio (jugador, IA, narrativa) solo avanza en este estado.</summary>
        public bool IsSimulating => State == GameState.Playing;
    }

    internal static class ArrayExtensions
    {
        public static bool Contains(this GameState[] states, GameState value) => Array.IndexOf(states, value) >= 0;
    }
}
