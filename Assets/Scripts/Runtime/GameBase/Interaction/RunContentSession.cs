using System;
using System.Collections.Generic;
using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    public sealed class RunContentSession
    {
        readonly RunContentInteractable target;
        readonly GameObject actor;
        readonly NaturalMapPlan map;
        readonly string targetStableId;
        readonly int contextId;
        readonly RunContentRewardState[] rewards;
        readonly IReadOnlyList<RunContentRewardState> readOnlyRewards;
        bool closed;

        public RunContentInteractable Target { get { return target; } }
        public GameObject Actor { get { return actor; } }
        public bool IsValid { get { string reason; return !closed && Owns(out reason); } }
        public bool Completed { get { return target != null && target.Completed; } }
        public IReadOnlyList<RunContentRewardState> Rewards { get { return readOnlyRewards; } }
        public RunNpcQuestSnapshot Quest => RunNpcQuestService.Read(target == null ? null : target.Definition);
        public event Action Changed;

        internal RunContentSession(RunContentInteractable target, GameObject actor)
        {
            this.target = target;
            this.actor = actor;
            map = target.Map;
            targetStableId = target.StableId;
            contextId = GameSession.Instance.ContextId;
            rewards = target.SnapshotRewards();
            readOnlyRewards = Array.AsReadOnly(rewards);
        }

        public bool TryClaim(int index, out string reason)
        {
            if (!Owns(out reason)) return false;
            if (!WithinReach(out reason)) return false;
            if (!target.TryClaimReward(index, out reason)) return false;
            RaiseChanged();
            return true;
        }

        public bool TryAdvanceQuest(out string reason)
        {
            if (!Owns(out reason) || !WithinReach(out reason)) return false;
            if (!RunNpcQuestService.TryAdvance(target.Definition, out reason)) return false;
            RaiseChanged(); return true;
        }

        public void Close()
        {
            if (closed) return;
            closed = true;
            var owner = target;
            if (owner != null) owner.UnbindSession(this);
            RunContentService.NotifySessionClosed(this);
            Changed = null;
        }

        bool Owns(out string reason)
        {
            if (closed) { reason = "会话已关闭"; return false; }
            if (target == null) { reason = "交互目标已销毁"; return false; }
            if (actor == null) { reason = "交互角色已销毁"; return false; }
            var game = GameSession.Instance;
            if (game == null || !game.HasSession) { reason = "没有有效存档会话"; return false; }
            if (game.ContextId != contextId) { reason = "存档会话已经切换"; return false; }
            if (RunContentService.Current != this) { reason = "该交互会话已被替换"; return false; }
            if (PlayerSpawner.CurrentPlayer != actor) { reason = "当前受控角色已经切换"; return false; }
            if (!actor.activeInHierarchy) { reason = "交互角色当前不可用"; return false; }
            if (!target.isActiveAndEnabled || !target.IsRegistered) { reason = "交互目标当前不可用"; return false; }
            if (target.StableId != targetStableId) { reason = "交互目标已经替换"; return false; }
            if (map == null || !NaturalMapRuntime.IsReady || NaturalMapRuntime.Current != map)
            { reason = "当前地图与交互目标不一致"; return false; }
            if (RunSceneFlow.IsTransitioning) { reason = "场景切换中"; return false; }
            if (RunEncounter.Defeated || RunEncounter.RunCleared) { reason = "本局已经结算"; return false; }
            var health = actor.GetComponent<PlayerHealth>();
            if (health != null && health.IsDowned) { reason = "角色已倒地"; return false; }
            reason = string.Empty;
            return true;
        }

        bool WithinReach(out string reason)
        {
            var settings = RunBehaviorSettings.Load("Ally");
            Vector2 origin = CombatQuery.Center(actor.transform);
            if (Vector2.Distance(origin, target.InteractionPoint) > settings.interactionRange)
            { reason = RunInteractionText.Get("OutOfRange"); return false; }
            if (!RunMapPhysics.HasLineOfSight(origin, target.InteractionPoint))
            { reason = RunInteractionText.Get("Blocked"); return false; }
            reason = string.Empty;
            return true;
        }

        void RaiseChanged()
        {
            var listeners = Changed;
            if (listeners == null) return;
            foreach (Action listener in listeners.GetInvocationList())
                try { listener(); } catch (Exception e) { Debug.LogException(e); }
        }
    }
}
