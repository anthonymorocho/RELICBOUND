using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relicbound.Game.Castle
{
    [Serializable]
    public struct BuildingStat
    {
        public string label;
        public string value;
    }

    /// <summary>
    /// Definición de datos de un tipo de edificio (docs/06). En P1 solo información; mejoras y costes llegan en P3.
    /// </summary>
    [CreateAssetMenu(menuName = "Relicbound/Edificio", fileName = "BD_")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string role;
        [TextArea(2, 4)] public string description;
        public List<BuildingStat> stats = new List<BuildingStat>();
        [Tooltip("Distancia de cámara al encuadrar el edificio seleccionado.")]
        public float focusDistance = 40f;
    }
}
