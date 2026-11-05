using System;
using Chaos.Gameplay.Interaction;
using UnityEngine;
using LocalizationSystem;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Owns this modal's pause and converts an interaction ticket into a passive snapshot.</summary>
    public sealed class RunContentPanelModel : IDisposable
    {
        // 独立任务动作保留负索引，普通奖励入口仍拒绝所有非法索引。
        public const int QuestChoiceIndex = -1;
        readonly RunContentSession session;
        readonly RunContentVisualSettings text;
        readonly RunMenuModel pause;
        string feedback;
        bool disposed;
        public bool IsValid => !disposed && session.IsValid;
        public event Action Changed;
        public RunContentPanelModel(RunContentSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            if (!session.IsValid) throw new InvalidOperationException("Interaction session is no longer current");
            text = RunContentVisualSettings.Load();
            pause = new RunMenuModel(); pause.Pause(); session.Changed += Notify;
            RunNpcQuestService.Changed += Notify;
        }
        void Notify() { Changed?.Invoke(); }
        public void Claim(int index)
        {
            if (!IsValid) return;
            bool result = index == QuestChoiceIndex ? session.TryAdvanceQuest(out feedback) : session.TryClaim(index, out feedback);
            if (result && string.IsNullOrEmpty(feedback)) feedback = text.receivedText.Resolve();
            Notify();
        }
        public RunContentViewState Read()
        {
            var definition = session.Target.Definition; var rewards = session.Rewards;
            var quest = session.Quest;
            var choices = new RunContentChoiceState[rewards.Count + (quest == null ? 0 : 1)];
            string action = definition.kind == RunContentKind.Npc ? text.chooseText.Resolve()
                : definition.kind == RunContentKind.Chest ? text.claimText.Resolve() : text.harvestText.Resolve();
            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i]; var item = InventoryService.Instance.GetDefinition(reward.ItemId);
                string name = definition.rewards[reward.Index].label?.Resolve();
                if (string.IsNullOrWhiteSpace(name)) name = GameplayText.ItemName(reward.ItemId);
                choices[i] = new RunContentChoiceState { Index = reward.Index, Name = name,
                    Detail = "×" + reward.Count, Icon = item == null ? null : Resources.Load<Sprite>(item.IconKey),
                    Enabled = IsValid && !session.Completed && !reward.Claimed, Claimed = reward.Claimed,
                    ButtonText = reward.Claimed ? text.claimedText.Resolve() : session.Completed ? text.completedText.Resolve() : action };
            }
            if (quest != null)
            {
                bool available = quest.Stage == RunNpcQuestStage.Available, ready = quest.Stage == RunNpcQuestStage.Ready;
                bool done = quest.Stage == RunNpcQuestStage.Completed;
                var item = InventoryService.Instance.GetDefinition(quest.RewardItemId);
                string actionText = available ? Q("接取委托", "Accept", "接取委託", "受注", "수락")
                    : ready ? Q("交付委托", "Turn in", "交付委託", "報告", "완료 보고")
                    : done ? Q("委托已完成", "Completed", "委託已完成", "完了", "완료")
                    : Q("采集中", "Gathering", "採集中", "採集中", "채집 중");
                choices[rewards.Count] = new RunContentChoiceState { Index = QuestChoiceIndex,
                    Name = quest.Title, Icon = item == null ? null : Resources.Load<Sprite>(item.IconKey),
                    Detail = string.Format(Q("采集 {0}/{1} · 奖励：{2}×{3}", "Gather {0}/{1} · Reward: {2} ×{3}", "採集 {0}/{1} · 獎勵：{2}×{3}", "採集 {0}/{1} · 報酬：{2}×{3}", "채집 {0}/{1} · 보상: {2} ×{3}"),
                        quest.Progress, quest.Goal, GameplayText.ItemName(quest.RewardItemId), quest.RewardCount),
                    Enabled = IsValid && (available || ready), Claimed = done, ButtonText = actionText };
            }
            return new RunContentViewState { Title = definition.name.Resolve(), Body = definition.body.Resolve() + (quest == null ? string.Empty : "\n" + quest.Description),
                Hint = string.IsNullOrEmpty(feedback) ? (definition.kind == RunContentKind.Npc ? text.npcHint.Resolve() : text.lootHint.Resolve()) : feedback,
                CloseText = text.closeText.Resolve(), Portrait = definition.kind == RunContentKind.Npc ? text.npcPortrait : null,
                Completed = session.Completed, Choices = choices };
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            session.Changed -= Notify; RunNpcQuestService.Changed -= Notify; session.Close(); pause.Dispose(); Changed = null;
        }
        static string Q(string zh, string en, string tw, string ja, string ko) => new RunContentText
        { zhCN = zh, enUS = en, zhTW = tw, jaJP = ja, koKR = ko }.Resolve();
    }
}
