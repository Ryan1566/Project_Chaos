using System.Collections.Generic;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class FamilyTreeState
    {
        public string Summary;
        public IReadOnlyList<TextRow> Rows;
    }
    public sealed class FamilyTreePresenter : GameplayPresenter<FamilyTreeState>
    {
        protected override FamilyTreeState CreateState()
        {
            var svc = FamilyTreeService.Instance;
            var rows = new List<TextRow>();
            foreach (var def in svc.GetAll())
            {
                if (def == null) continue;
                bool unlocked = svc.IsUnlocked(def.Index);
                string reason = null;
                bool ready = !unlocked && svc.CanUnlock(def.Index, out reason);
                rows.Add(new TextRow { Id = def.Index, Group = def.Board, Enabled = ready,
                    Tone = unlocked ? TextTone.Success : ready ? TextTone.Warning : TextTone.Dim,
                    Text = "  " + def.Index + " " + def.NameKey + "　消耗 " + InventoryService.ResourceDisplayName(VillageService.ResourceKindOf(def.CostType))
                        + " " + def.CostValue + "　" + (unlocked ? "已点亮" : ready ? "可点亮" : "锁定：" + reason) });
            }
            return new FamilyTreeState { Rows = rows.ToArray(), Summary = "已点亮 " + svc.UnlockedCount + "/15　　带出上限 "
                + MetaProgressService.Instance.DeployCap + "　　格位 " + InventoryService.Instance.TotalSlots
                + "　　扎营补员 " + (EffectAggregator.Instance.Snapshot.CampRefillUnlocked ? "已解锁" : "未解锁") };
        }
        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action != PanelAction.Unlock) return;
            bool ok = FamilyTreeService.Instance.Unlock(intent.Id, out string reason);
            Refresh(); Message(ok ? "节点已点亮" : reason, ok);
        }
    }
}
