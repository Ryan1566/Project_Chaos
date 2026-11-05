using System;
using LocalizationSystem;
using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    public sealed class RunContentInteractable : RunInteractable
    {
        RunContentDefinition definition;
        NaturalMapPlan map;
        RunContentRewardState[] rewards;
        RunContentSession activeSession;
        bool npcCompleted;
        bool claiming;

        public RunContentDefinition Definition { get { return definition == null ? null : Copy(definition); } }
        static RunContentDefinition Copy(RunContentDefinition source) => JsonUtility.FromJson<RunContentDefinition>(JsonUtility.ToJson(source));
        internal NaturalMapPlan Map { get { return map; } }

        public override string DisplayName
        {
            get
            {
                if (definition == null) return string.Empty;
                string text = definition.name == null ? null : definition.name.Resolve();
                return string.IsNullOrWhiteSpace(text) ? definition.id : text;
            }
        }

        public override string ActionLabel
        {
            get
            {
                if (definition == null) return RunInteractionText.Get("Inspect");
                var text = RunContentVisualSettings.Load();
                return definition.kind == RunContentKind.Npc ? (text.talkText?.Resolve() ?? text.chooseText.Resolve())
                    : definition.kind == RunContentKind.Chest ? text.claimText.Resolve() : text.harvestText.Resolve();
            }
        }

        public override bool Completed
        {
            get
            {
                if (definition == null || rewards == null || rewards.Length == 0) return false;
                if (definition.kind == RunContentKind.Npc) return npcCompleted;
                if (definition.kind == RunContentKind.Gatherable) return rewards[0].Claimed;
                for (int i = 0; i < rewards.Length; i++) if (!rewards[i].Claimed) return false;
                return true;
            }
        }

        public void ConfigureContent(string stableId, RunContentDefinition definition, NaturalMapPlan map)
        {
            // A repeated setup call must not reset claims or reroll this map object's contents.
            if (this.map == map && StableId == stableId && rewards != null) return;
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (map == null) throw new ArgumentNullException(nameof(map));
            RunContentCatalog.ValidateDefinition(definition);
            if (activeSession != null) activeSession.Close();
            Configure(stableId);
            this.definition = Copy(definition);
            this.map = map;
            npcCompleted = false;
            BuildSnapshot(stableId);
        }

        void BuildSnapshot(string stableId)
        {
            var configured = definition.rewards;
            var states = new RunContentRewardState[configured.Length];
            for (int i = 0; i < configured.Length; i++)
            {
                var reward = configured[i];
                var rng = new RunMapRng(MapRandom.Seed(map.RunSeed, map.SceneIndex,
                    "run-content/" + stableId + "/" + i));
                int count = rng.Range((int)reward.minCount, (int)reward.maxCount);
                string label = reward.label == null ? null : reward.label.Resolve();
                if (string.IsNullOrWhiteSpace(label)) label = GameplayText.ItemName(reward.itemId);
                states[i] = new RunContentRewardState(i, reward.itemId, (uint)count, label);
            }
            rewards = states;
        }

        public override bool CanInteract(GameObject actor, out string reason)
        {
            if (actor == null || definition == null || map == null || rewards == null)
            { reason = "交互内容尚未配置"; return false; }
            if (definition.kind != RunContentKind.Npc && Completed)
            { reason = RunInteractionText.Get("Completed"); return false; }
            reason = string.Empty;
            return true;
        }

        protected override bool Perform(GameObject actor, out string result)
        {
            return RunContentService.RequestOpen(this, actor, out result);
        }

        internal RunContentRewardState[] SnapshotRewards()
        {
            return rewards ?? Array.Empty<RunContentRewardState>();
        }

        internal bool TryClaimReward(int index, out string reason)
        {
            if (claiming) { reason = "正在领取奖励"; return false; }
            if (definition == null || rewards == null || index < 0 || index >= rewards.Length)
            { reason = "领取项不存在"; return false; }
            if (Completed) { reason = "内容已完成"; return false; }
            var state = rewards[index];
            if (state.Claimed) { reason = "该奖励已领取"; return false; }
            claiming = true;
            try
            {
                if (!RunInventory.AddItem(state.ItemId, state.Count, out reason)) return false;
                state.MarkClaimed();
                if (definition.kind == RunContentKind.Npc) npcCompleted = true;
                if (definition.kind == RunContentKind.Gatherable)
                    RunNpcQuestService.RecordGathering(map, StableId, definition);
                if (reason == null) reason = string.Empty;
                return true;
            }
            finally { claiming = false; }
        }

        internal void BindSession(RunContentSession session)
        {
            if (activeSession != null && activeSession != session) activeSession.Close();
            activeSession = session;
        }

        internal void UnbindSession(RunContentSession session)
        {
            if (activeSession == session) activeSession = null;
        }

        protected override void OnDisable()
        {
            RunContentService.CloseForTarget(this);
            base.OnDisable();
        }

        void OnDestroy()
        {
            RunContentService.CloseForTarget(this);
        }
    }
}
