using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    public abstract class RunInteractable : MonoBehaviour
    {
        private static readonly Dictionary<string, RunInteractable> Registry = new Dictionary<string, RunInteractable>(StringComparer.Ordinal);
        public static IEnumerable<RunInteractable> Active => Registry.Values;
        public string StableId { get; private set; }
        public abstract string DisplayName { get; }
        public virtual Vector2 InteractionPoint => transform.position;
        public int ExecutionCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() { Registry.Clear(); }
        public void Configure(string stableId)
        {
            if (string.IsNullOrEmpty(stableId)) throw new ArgumentException("Interaction requires a stable map ID");
            Unregister(); StableId = stableId; if (isActiveAndEnabled) Register();
        }
        protected virtual void OnEnable() { if (!string.IsNullOrEmpty(StableId)) Register(); }
        protected virtual void OnDisable() { Unregister(); }
        private void Register()
        {
            RunInteractable previous;
            if (Registry.TryGetValue(StableId, out previous) && previous != this)
                throw new InvalidOperationException("Duplicate interaction ID: " + StableId);
            Registry[StableId] = this;
        }
        private void Unregister()
        {
            RunInteractable current;
            if (!string.IsNullOrEmpty(StableId) && Registry.TryGetValue(StableId, out current) && current == this) Registry.Remove(StableId);
        }
        public bool IsRegistered => !string.IsNullOrEmpty(StableId) && Registry.TryGetValue(StableId, out var current) && current == this;
        public abstract bool CanInteract(GameObject actor, out string reason);
        protected abstract bool Perform(GameObject actor, out string result);
        internal bool Execute(GameObject actor, out string result)
        {
            if (!CanInteract(actor, out result)) return false;
            if (!Perform(actor, out result)) return false;
            ExecutionCount++; return true;
        }
    }
}
