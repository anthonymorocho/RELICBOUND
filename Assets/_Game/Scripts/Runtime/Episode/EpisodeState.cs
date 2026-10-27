using System;
using System.Collections.Generic;
using BeforeItWasYou.Core.Save;
using UnityEngine;

namespace BeforeItWasYou.Episode
{
    /// <summary>Objetos recogidos y hechos narrativos del episodio; se guardan y restauran con el snapshot.</summary>
    public sealed class EpisodeState : MonoBehaviour
    {
        [SerializeField] private EpisodeDirector director;

        private readonly HashSet<string> _items = new HashSet<string>();
        private readonly HashSet<string> _flags = new HashSet<string>();

        public event Action<string> ItemAdded;
        public event Action<string> FlagSet;

        public static EpisodeState Current { get; private set; }

        public void Configure(EpisodeDirector episode) => director = episode;

        private void Awake()
        {
            Current = this;
            director.Started += Restore;
            director.SnapshotRequested += Store;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public bool Has(string item) => _items.Contains(item);

        public bool Flag(string flag) => _flags.Contains(flag);

        public void AddItem(string item)
        {
            if (_items.Add(item)) ItemAdded?.Invoke(item);
        }

        public void SetFlag(string flag)
        {
            if (_flags.Add(flag)) FlagSet?.Invoke(flag);
        }

        private void Restore(Snapshot snapshot)
        {
            _items.Clear();
            _flags.Clear();
            if (snapshot == null) return;
            foreach (var i in snapshot.Items) _items.Add(i);
            foreach (var f in snapshot.Flags)
            {
                if (f.Value) _flags.Add(f.Key);
            }
            foreach (var pickup in FindObjectsByType<Interaction.Pickup>(FindObjectsSortMode.None)) pickup.SyncWithState(this);
            // Estado del mundo: puertas e interruptores tal como estaban al guardar.
            foreach (var door in FindObjectsByType<Interaction.Door>(FindObjectsSortMode.None))
            {
                if (snapshot.Doors.TryGetValue(door.Id, out var saved) && Enum.TryParse(saved, out Interaction.DoorState state)) door.Restore(state);
            }
            foreach (var sw in FindObjectsByType<Interaction.LightSwitch>(FindObjectsSortMode.None))
            {
                if (snapshot.Switches.TryGetValue(sw.Id, out var on)) sw.Set(on, true);
            }
        }

        private void Store(Snapshot snapshot)
        {
            snapshot.Items.AddRange(_items);
            foreach (var f in _flags) snapshot.Flags[f] = true;
            foreach (var door in FindObjectsByType<Interaction.Door>(FindObjectsSortMode.None)) snapshot.Doors[door.Id] = door.State.ToString();
            foreach (var sw in FindObjectsByType<Interaction.LightSwitch>(FindObjectsSortMode.None)) snapshot.Switches[sw.Id] = sw.On;
        }
    }
}
