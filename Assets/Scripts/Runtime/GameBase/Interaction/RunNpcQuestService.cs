using System;
using System.Collections.Generic;
using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    public enum RunNpcQuestStage { Available, Gathering, Ready, Completed }

    /// <summary>只读任务快照，不向界面暴露可变配置或领取记录。</summary>
    public sealed class RunNpcQuestSnapshot
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public int Progress { get; }
        public int Goal { get; }
        public uint RewardItemId { get; }
        public uint RewardCount { get; }
        public RunNpcQuestStage Stage { get; }
        internal RunNpcQuestSnapshot(RunNpcQuestDefinition definition, int progress, bool completed)
        {
            Id = definition.id; Title = definition.title.Resolve(); Description = definition.description.Resolve();
            Progress = progress; Goal = definition.requiredCount;
            RewardItemId = definition.rewardItemId; RewardCount = definition.rewardCount;
            Stage = completed ? RunNpcQuestStage.Completed : progress >= Goal ? RunNpcQuestStage.Ready : RunNpcQuestStage.Gathering;
        }
        internal RunNpcQuestSnapshot(RunNpcQuestDefinition definition) : this(definition, 0, false)
        { Stage = RunNpcQuestStage.Available; }
    }

    /// <summary>整局持有任务进度，地图卸载不清理；普通奖励只在既有结算事务中保存。</summary>
    public static class RunNpcQuestService
    {
        sealed class Entry
        {
            public RunNpcQuestDefinition Definition;
            public int Progress;
            public bool Completed;
            public readonly HashSet<string> Gathered = new HashSet<string>(StringComparer.Ordinal);
        }
        static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        static int context = -1, generation;
        static bool active, mutating;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { entries.Clear(); context = -1; generation = 0; active = mutating = false; Changed = null; }
        public static void BeginRun() { Clear(); context = GameSession.Instance.ContextId; active = GameSession.Instance.HasSession; }
        public static void Clear() { entries.Clear(); active = false; generation++; Publish(); }
        static bool OwnsRun()
        {
            var session = GameSession.Instance;
            if (context != session.ContextId || !session.HasSession)
            { entries.Clear(); active = false; context = session.ContextId; generation++; }
            return active;
        }
        static bool Ready(out string reason)
        {
            if (!OwnsRun()) { reason = "当前没有有效的局内委托会话"; return false; }
            if (mutating) { reason = "委托正在处理，请稍候"; return false; }
            if (!RunEncounter.IsSceneReady || !NaturalMapRuntime.IsReady || RunSceneFlow.IsTransitioning || RunEncounter.Defeated || RunEncounter.RunCleared)
            { reason = "地图未就绪或本局已经结束"; return false; }
            reason = string.Empty; return true;
        }
        public static RunNpcQuestSnapshot Read(RunContentDefinition npc)
        {
            if (!OwnsRun() || npc?.kind != RunContentKind.Npc || npc.quest == null || !npc.quest.enabled) return null;
            if (entries.TryGetValue(npc.quest.id, out var entry))
                return new RunNpcQuestSnapshot(entry.Definition, entry.Progress, entry.Completed);
            return new RunNpcQuestSnapshot(npc.quest);
        }
        internal static bool TryAdvance(RunContentDefinition npc, out string reason)
        {
            if (!Ready(out reason)) return false;
            if (npc?.kind != RunContentKind.Npc || npc.quest == null || !npc.quest.enabled)
            { reason = "这位人物没有可接取的委托"; return false; }
            var definition = npc.quest;
            if (!entries.TryGetValue(definition.id, out var entry))
            {
                // 接取时拍下规则，后续地图重建和配置引用修改不能改变本轮目标或奖励。
                entries.Add(definition.id, new Entry { Definition = JsonUtility.FromJson<RunNpcQuestDefinition>(JsonUtility.ToJson(definition)) });
                reason = "已接取委托；完成指定采集后回访委托人。"; Publish(); return true;
            }
            if (entry.Completed) { reason = "本局已经完成该委托"; return false; }
            if (entry.Progress < entry.Definition.requiredCount) { reason = "采集尚未完成，请按委托要求寻找采集点"; return false; }
            mutating = true;
            int ownerGeneration = generation;
            try
            {
                if (!RunInventory.AddItem(entry.Definition.rewardItemId, entry.Definition.rewardCount, out reason)) return false;
                // 背包事件可能同步触发界面或切换会话；不能把旧任务的完成状态写进新局。
                if (ownerGeneration != generation || !OwnsRun()) { reason = "奖励发放时会话已结束"; return false; }
                entry.Completed = true;
                reason = "委托完成，奖励已加入局内背包。";
            }
            finally { mutating = false; }
            Publish(); return true;
        }
        internal static void RecordGathering(NaturalMapPlan map, string stableId, RunContentDefinition source)
        {
            if (!Ready(out _) || map == null || !ReferenceEquals(map, NaturalMapRuntime.Current)
                || source?.kind != RunContentKind.Gatherable || string.IsNullOrEmpty(stableId)) return;
            bool changed = false;
            string key = map.SceneIndex + "/" + stableId;
            foreach (var entry in entries.Values)
            {
                if (entry.Completed || entry.Progress >= entry.Definition.requiredCount || entry.Definition.targetContentId != source.id
                    || !entry.Gathered.Add(key)) continue;
                entry.Progress++; changed = true;
            }
            if (changed) Publish();
        }
        static void Publish()
        {
            var listeners = Changed;
            if (listeners == null) return;
            foreach (Action listener in listeners.GetInvocationList())
                try { listener(); } catch (Exception e) { Debug.LogException(e); }
        }
    }
}
