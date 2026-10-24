using System;
using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    public sealed class RunInteractionService
    {
        private static int executedFrame = -1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetExecution() { executedFrame = -1; }
        private readonly float range;
        public RunInteractionService(float range)
        { if (range <= 0 || float.IsNaN(range) || float.IsInfinity(range)) throw new ArgumentOutOfRangeException(nameof(range)); this.range = range; }
        public static bool CanControl(GameObject actor)
        {
            if (actor == null || PlayerSpawner.CurrentPlayer != actor || !actor.activeInHierarchy
                || !NaturalMapRuntime.IsReady || RunSceneFlow.IsTransitioning || RunEncounter.RunCleared
                || RunEncounter.Defeated || Time.timeScale <= 0 || !InputManager.Instance.CanReadGameplayInput) return false;
            var health = actor.GetComponent<PlayerHealth>();
            return health == null || !health.IsDowned;
        }
        public RunInteractable Select(GameObject actor)
        {
            if (!CanControl(actor)) return null;
            RunInteractable best = null; float distance = range * range;
            Vector2 origin = CombatQuery.Center(actor.transform);
            foreach (var target in RunInteractable.Active)
            {
                if (target == null || !target.isActiveAndEnabled) continue;
                float d = (target.InteractionPoint - origin).sqrMagnitude;
                if (d > range * range || !RunMapPhysics.HasLineOfSight(origin, target.InteractionPoint)) continue;
                if (best == null || d < distance - .0001f || (Mathf.Abs(d - distance) < .0001f && string.CompareOrdinal(target.StableId, best.StableId) < 0))
                { best = target; distance = d; }
            }
            return best;
        }
        public bool TryExecute(GameObject actor, RunInteractable target, out string result)
        {
            result = string.Empty;
            if (!CanControl(actor) || target == null || !target.isActiveAndEnabled || !target.IsRegistered)
            { result = RunInteractionText.Get("Inactive"); return false; }
            if (executedFrame == Time.frameCount) return false;
            var origin = CombatQuery.Center(actor.transform);
            if (Vector2.Distance(origin, target.InteractionPoint) > range)
            { result = RunInteractionText.Get("OutOfRange"); return false; }
            if (!RunMapPhysics.HasLineOfSight(origin, target.InteractionPoint))
            { result = RunInteractionText.Get("Blocked"); return false; }
            executedFrame = Time.frameCount;
            return target.Execute(actor, out result);
        }
    }
}
