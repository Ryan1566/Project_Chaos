using System.Collections.Generic;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class VillageState
    {
        public string Resources, Hint;
        public IReadOnlyList<TextRow> Rows;
    }
    public sealed class VillagePresenter : GameplayPresenter<VillageState>
    {
        protected override VillageState CreateState()
        {
            var svc = VillageService.Instance;
            var inv = InventoryService.Instance;
            svc.EnsureLoaded();
            var rows = new List<TextRow>();
            foreach (uint id in new[] { 2001u, 2002u, 2003u, 2004u })
            {
                var save = svc.Find(id);
                var state = svc.GetState(id);
                var def = svc.GetDef(id, save == null ? 1u : save.level);
                string status, action;
                PanelAction command;
                switch (state)
                {
                    case VillageService.BuildingState.NotBuilt: status = "未建造"; action = "建造"; command = PanelAction.Build; break;
                    case VillageService.BuildingState.Building: status = "建造中 剩 " + save.remainSeconds + "s"; action = "取消（返 50%）"; command = PanelAction.CancelBuild; break;
                    case VillageService.BuildingState.Upgradable: status = "已建成 Lv" + save.level + "（可升级）"; action = "升级"; command = PanelAction.Upgrade; break;
                    case VillageService.BuildingState.MaxLevel: status = "已满级 Lv" + save.level; action = "收取"; command = PanelAction.Harvest; break;
                    default: status = "已建成 Lv" + save.level; action = "收取"; command = PanelAction.Harvest; break;
                }
                rows.Add(new TextRow { Id = id, Text = (def == null ? "建筑" + id : def.NameKey) + "　" + status,
                    Tone = state == VillageService.BuildingState.Building ? TextTone.Warning : TextTone.Normal,
                    ActionLabel = action, Action = command, Enabled = true });
            }
            return new VillageState { Rows = rows.ToArray(), Hint = svc.IsQueueBusy ? "建造队列忙（同时只能建/升 1 个）——取消建造返还 50%" : "",
                Resources = "物资 " + inv.GetResource(InventoryService.ResourceKind.Goods) + "　　材料 " + inv.GetResource(InventoryService.ResourceKind.Material)
                    + "　　图纸 " + inv.GetResource(InventoryService.ResourceKind.Blueprint) + "　　钱财 " + inv.GetResource(InventoryService.ResourceKind.Money) };
        }
        protected override void Execute(PanelIntent intent)
        {
            var svc = VillageService.Instance;
            bool ok; string reason;
            switch (intent.Action)
            {
                case PanelAction.Build: ok = svc.StartBuild(intent.Id, out reason); break;
                case PanelAction.CancelBuild: ok = svc.CancelBuild(intent.Id, out uint refund, out reason); break;
                case PanelAction.Upgrade: ok = svc.StartUpgrade(intent.Id, out reason); break;
                case PanelAction.Harvest: ok = svc.Harvest(intent.Id, out uint amount, out reason); break;
                default: return;
            }
            Refresh(); Message(ok ? "操作成功" : reason, ok);
        }
    }
}
