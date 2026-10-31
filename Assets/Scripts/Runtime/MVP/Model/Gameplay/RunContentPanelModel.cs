using System;
using Chaos.Gameplay.Interaction;
using UnityEngine;
using LocalizationSystem;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Owns this modal's pause and converts an interaction ticket into a passive snapshot.</summary>
    public sealed class RunContentPanelModel : IDisposable
    {
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
        }
        void Notify() { Changed?.Invoke(); }
        public void Claim(int index)
        {
            if (!IsValid) return;
            bool result = session.TryClaim(index, out feedback);
            if (result && string.IsNullOrEmpty(feedback)) feedback = text.receivedText.Resolve();
            Notify();
        }
        public RunContentViewState Read()
        {
            var definition = session.Target.Definition; var rewards = session.Rewards;
            var choices = new RunContentChoiceState[rewards.Count];
            string action = definition.kind == RunContentKind.Npc ? text.chooseText.Resolve()
                : definition.kind == RunContentKind.Chest ? text.claimText.Resolve() : text.harvestText.Resolve();
            for (int i = 0; i < choices.Length; i++)
            {
                var reward = rewards[i]; var item = InventoryService.Instance.GetDefinition(reward.ItemId);
                string name = definition.rewards[reward.Index].label?.Resolve();
                if (string.IsNullOrWhiteSpace(name)) name = GameplayText.ItemName(reward.ItemId);
                choices[i] = new RunContentChoiceState { Index = reward.Index, Name = name,
                    Detail = "×" + reward.Count, Icon = item == null ? null : Resources.Load<Sprite>(item.IconKey),
                    Enabled = IsValid && !session.Completed && !reward.Claimed, Claimed = reward.Claimed,
                    ButtonText = reward.Claimed ? text.claimedText.Resolve() : session.Completed ? text.completedText.Resolve() : action };
            }
            return new RunContentViewState { Title = definition.name.Resolve(), Body = definition.body.Resolve(),
                Hint = string.IsNullOrEmpty(feedback) ? (definition.kind == RunContentKind.Npc ? text.npcHint.Resolve() : text.lootHint.Resolve()) : feedback,
                CloseText = text.closeText.Resolve(), Portrait = definition.kind == RunContentKind.Npc ? text.npcPortrait : null,
                Completed = session.Completed, Choices = choices };
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            session.Changed -= Notify; session.Close(); pause.Dispose(); Changed = null;
        }
    }
}
