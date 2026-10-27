using System;
using System.Collections.Generic;

namespace BeforeItWasYou.Core.Save
{
    /// <summary>
    /// Estado completo del episodio en un checkpoint (SISTEMAS_Y_CHECKPOINTS §Datos persistentes).
    /// Un checkpoint restaura el mundo entero, no solo la posición del jugador.
    /// Solo datos planos: nunca referencias a objetos de escena.
    /// </summary>
    public sealed class Snapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string EpisodeId = "EP01";
        public string CheckpointId = "CP00";
        public string Scene = "L_E01_Arrival";
        public DateTime SavedUtc;

        public Vector3Data PlayerPosition;
        public float PlayerYaw;
        public float PlayerPitch;
        public bool PlayerCrouched;

        public string Objective = string.Empty;
        public string Branch = string.Empty;
        public List<string> Items = new List<string>();
        public Dictionary<string, string> Doors = new Dictionary<string, string>();
        public Dictionary<string, bool> Switches = new Dictionary<string, bool>();
        public Dictionary<string, bool> Drawers = new Dictionary<string, bool>();
        public List<string> MessagesRead = new List<string>();
        public List<string> MessagesSent = new List<string>();
        public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
        public Dictionary<string, float> Numbers = new Dictionary<string, float>();
        public IntruderData Intruder = new IntruderData();
        public float EpisodeSeconds;
        public int RandomSeed;
    }

    public struct Vector3Data
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3Data(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>Estado de Víctor guardado en el checkpoint (CRONOLOGIA_VICTOR §Sistema requerido).</summary>
    public sealed class IntruderData
    {
        public string State = "Outside";
        public string Node = "Outside";
        public Vector3Data Position;
        public float Yaw;
        public string Goal = string.Empty;
        public bool HasBag;
        public bool KnowsAboutPlayer;
        public Vector3Data LastKnownPlayerPosition;
    }
}
