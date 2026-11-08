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
            Vector2 origin = CombatQuery.Center(actor.transform);

            // 攀爬中当前梯子的“退出”优先于附近的 NPC/出口；其余目标仍按原距离排序。
            // 自动模式过滤梯子，普通内容仍显示原有的完成和不可用提示。
            var climber = actor.GetComponent<LadderClimber>();
            if (climber != null && climber.IsClimbing && climber.Current != null)
            {
                foreach (var target in RunInteractable.Active)
                {
                    var ladderTarget = target as RunLadderInteractable;
                    if (ladderTarget == null || !ladderTarget.isActiveAndEnabled || !ladderTarget.Matches(climber.Current)) continue;
                    string reason;
                    if (!ladderTarget.CanInteract(actor, out reason)) continue;
                    float currentDistance = (ladderTarget.InteractionPoint - origin).sqrMagnitude;
                    if (currentDistance > range * range || !RunMapPhysics.HasLineOfSight(origin, ladderTarget.InteractionPoint)) continue;
                    return ladderTarget;
                }
            }

            RunInteractable best = null; float distance = range * range;
            foreach (var target in RunInteractable.Active)
            {
                if (target == null || !target.isActiveAndEnabled) continue;
                if (target is RunObjectiveMarker) continue;
                if (target is RunLadderInteractable && !target.CanInteract(actor, out _)) continue;
                Vector2 point = target.InteractionPoint;
                float d = (point - origin).sqrMagnitude;
                if (d > range * range || !RunMapPhysics.HasLineOfSight(origin, point)) continue;
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
            // 先刷新 actor 上下文，动态 InteractionPoint（如梯子入口）才能按当前脚底取值。
            string unavailable;
            if (!target.CanInteract(actor, out unavailable))
            { result = string.IsNullOrEmpty(unavailable) ? RunInteractionText.Get("Inactive") : unavailable; return false; }
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
