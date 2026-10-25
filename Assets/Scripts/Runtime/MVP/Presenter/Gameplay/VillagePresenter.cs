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
        private uint focus;
        public void Focus(uint id) { focus = id; Refresh(); }
        protected override void OnAttach() { base.OnAttach(); MetaRuntimeDriver.Changed += Refresh; }
        protected override void OnDetach() { MetaRuntimeDriver.Changed -= Refresh; base.OnDetach(); }
        protected override VillageState CreateState()
        {
            var svc = VillageService.Instance;
            var inv = InventoryService.Instance;
            svc.EnsureLoaded();
            var rows = new List<TextRow>();
            foreach (uint id in new[] { 2001u, 2002u, 2003u, 2004u })
            {
                if (focus != 0 && id != focus) continue;
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
                bool enabled = command == PanelAction.Build || command == PanelAction.Upgrade ? !svc.IsQueueBusy
                    : command != PanelAction.Harvest || (save != null && save.pendingOutput > 0);
                string production = save != null && save.completed ? "　" + VillageText.Get("Production", save.pendingOutput)
                    + "　" + VillageText.Get("Producing", save.productionSeconds.ToString("F1"), def.HarvestSeconds) : string.Empty;
                rows.Add(new TextRow { Id = id, Text = VillageText.Get("Building" + id) + "　" + status + production,
                    Tone = state == VillageService.BuildingState.Building ? TextTone.Warning : TextTone.Normal,
                    ActionLabel = action, Action = command, Enabled = enabled,
                    SecondaryEnabled = save != null && save.pendingOutput > 0,
                    SecondaryActionLabel = state == VillageService.BuildingState.Upgradable ? "收取" : null,
                    SecondaryAction = PanelAction.Harvest });
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
                case PanelAction.Build: ok = svc.Execute(VillageService.Command.Build, intent.Id, out _, out reason); break;
                case PanelAction.CancelBuild: ok = svc.Execute(VillageService.Command.Cancel, intent.Id, out _, out reason); break;
                case PanelAction.Upgrade: ok = svc.Execute(VillageService.Command.Upgrade, intent.Id, out _, out reason); break;
                case PanelAction.Harvest: ok = svc.Execute(VillageService.Command.Harvest, intent.Id, out _, out reason); break;
                default: return;
            }
            Refresh(); Message(ok ? "操作成功" : reason, ok);
        }
    }
}
