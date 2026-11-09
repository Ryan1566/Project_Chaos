using System;
using System.Collections.Generic;
using System.Linq;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class VillageState
    {
        public uint FocusId;
        public bool FacilityOnly;
        public string[] Resources;
        public IReadOnlyList<VillageBuildingCard> Buildings;
        public VillageBuildingCard Selected;
        public VillageBuildingCard Queue;
    }
    public sealed class VillageBuildingCard
    {
        public uint Id, Level, TargetLevel, RemainingSeconds;
        public string Name, Status, FeatureTitle, FeatureValue, FeatureDetail, ActionLabel, Requirement, ActionHint, CancelPrompt;
        public bool Fixed, CanAct, ShowCollect, CanCollect;
        public float ConstructionProgress, ProductionProgress;
        public PanelAction Action;
        public VillageService.BuildingState State;
    }
    public sealed class VillagePresenter : GameplayPresenter<VillageState>
    {
        private uint focus, selected;
        public void Focus(uint id) { selected = focus = id; Refresh(); }
        protected override void OnAttach() { base.OnAttach(); MetaRuntimeDriver.Changed += Refresh; }
        protected override void OnDetach() { MetaRuntimeDriver.Changed -= Refresh; base.OnDetach(); }
        protected override VillageState CreateState()
        {
            var svc = VillageService.Instance;
            var inv = InventoryService.Instance;
            svc.EnsureLoaded();
            var active = svc.GetAll().Find(building => building != null && !building.completed);
            bool facilityOnly = VillageService.IsFixedFacility(focus);
            var ids = facilityOnly ? new[] { focus } : svc.GetAllDefs()
                .Where(def => def != null && def.Level == 1 && !VillageService.IsFixedFacility(def.BuildingId)
                    && (def.Enable || def.BuildingId == focus || svc.Find(def.BuildingId) != null))
                .Select(def => def.BuildingId).Distinct().OrderBy(id => id).ToArray();
            var cards = ids.Select(id => ReadCard(svc, inv, id, active?.buildingType ?? 0)).Where(card => card != null).ToArray();
            var current = Array.Find(cards, card => card.Id == selected) ?? cards.FirstOrDefault();
            selected = current?.Id ?? 0;
            if (current != null && focus != 0 && current.Id != focus)
            {
                current.CanAct = current.CanCollect = false;
                if (current.ActionLabel != null || current.ShowCollect)
                    current.ActionHint = (current.ActionHint == null ? "" : current.ActionHint + " · ") + "请前往该地块操作";
            }
            var kinds = new[] { InventoryService.ResourceKind.Goods, InventoryService.ResourceKind.Material,
                InventoryService.ResourceKind.Blueprint, InventoryService.ResourceKind.Money };
            return new VillageState
            {
                FocusId = focus, FacilityOnly = facilityOnly, Buildings = cards, Selected = current,
                Resources = kinds.Select(kind => InventoryService.ResourceDisplayName(kind) + "   " + inv.GetResource(kind).ToString("N0")).ToArray(),
                Queue = active == null ? null : Array.Find(cards, card => card.Id == active.buildingType)
                    ?? ReadCard(svc, inv, active.buildingType, active.buildingType)
                    ?? new VillageBuildingCard { Name = $"建筑 {active.buildingType}", TargetLevel = active.level, RemainingSeconds = active.remainSeconds }
            };
        }

        private static VillageBuildingCard ReadCard(VillageService svc, InventoryService inv, uint id, uint queueId)
        {
            var saved = svc.Find(id);
            uint level = svc.EffectiveLevel(id);
            var state = svc.GetState(id);
            var current = svc.GetDef(id, Math.Max(1u, level));
            if (current == null) return null;
            bool fixedFacility = VillageService.IsFixedFacility(id);
            var card = new VillageBuildingCard
            {
                Id = id, Level = level, Fixed = fixedFacility, State = state,
                Name = id >= 2001 && id <= 2004 ? VillageText.Get("Building" + id)
                    : GameplayText.Text(current.NameKey, fixedFacility ? current.NameKey : $"建筑 {id}"),
                Status = level == 0 ? "未建造" : state == VillageService.BuildingState.MaxLevel ? $"Lv{level} · 已满级" : $"Lv{level} · 已建成"
            };
            bool inProgress = state == VillageService.BuildingState.Building;
            var target = inProgress ? svc.GetDef(id, saved.level)
                : level == 0 ? current : svc.GetDef(id, level + 1);
            if (target != null && (target.Enable || inProgress))
            {
                uint duration = VillageService.BuildDuration(target);
                var costKind = VillageService.ResourceKindOf(target.CostResourceType);
                string cost = InventoryService.ResourceDisplayName(costKind);
                uint balance = inv.GetResource(costKind);
                card.TargetLevel = target.Level;
                if (inProgress)
                {
                    uint refund = target.CostAmount * VillageService.CancelRefundPercent / 100;
                    card.RemainingSeconds = saved.remainSeconds;
                    card.ConstructionProgress = 1f - (float)saved.remainSeconds / Math.Max(duration, saved.remainSeconds);
                    card.Status = level == 0 ? "建造中" : $"Lv{level} · 升级中";
                    card.Action = PanelAction.CancelBuild;
                    card.ActionLabel = "取消施工";
                    card.CanAct = refund <= inv.ResourceRoom(costKind);
                    card.Requirement = $"目标 Lv{target.Level}    剩余 {saved.remainSeconds} 秒";
                    card.ActionHint = card.CanAct ? $"取消可返还 {cost} {refund}" : $"仓库的{cost}空间不足，暂不能返还";
                    card.CancelPrompt = $"取消「{card.Name}」的{(level == 0 ? "建造" : "升级")}？\n将返还 {cost} {refund}（原费用的 {VillageService.CancelRefundPercent}% 向下取整）。";
                }
                else
                {
                    card.Action = level == 0 ? PanelAction.Build : PanelAction.Upgrade;
                    card.ActionLabel = level == 0 ? "开始建造" : $"升至 Lv{target.Level}";
                    card.CanAct = queueId == 0 && balance >= target.CostAmount;
                    card.Requirement = $"{cost} {target.CostAmount:N0} / 持有 {balance:N0}    ·    {duration} 秒";
                    card.ActionHint = queueId != 0 ? "已有建筑施工，需等待队列空闲"
                        : balance < target.CostAmount ? $"还缺 {cost} {target.CostAmount - balance:N0}" : "施工完成后自动生效";
                }
            }
            else
            {
                card.Requirement = level == 0 ? "当前建筑暂不可建造" : "已达到最高等级";
            }
            if (fixedFacility)
            {
                card.FeatureTitle = "设施用途";
                card.FeatureValue = id == VillageService.WarehouseId ? $"每种资源上限  {svc.ResourceCapacity:N0}"
                    : id == VillageService.OfficeId ? "办理事务、交易与税务" : "查阅并发展家族技能树";
                card.FeatureDetail = id == VillageService.WarehouseId && target?.ResourceCapacity > 0 && target.Enable
                    ? $"Lv{target.Level} 完工后，上限提高至 {target.ResourceCapacity:N0}"
                    : "可在村庄内与设施交互";
                return card;
            }
            ReadProduction(card, inv, saved, current, target?.Enable == true ? target : null);
            return card;
        }

        private static void ReadProduction(VillageBuildingCard card, InventoryService inv, BuildingSave saved,
            BuildingTable_BuildingConfig current, BuildingTable_BuildingConfig target)
        {
            card.FeatureTitle = "资源生产";
            uint pending = saved?.pendingOutput ?? 0;
            var outputKind = VillageService.ResourceKindOf(current.OutputResourceType);
            var collectKind = VillageService.ResourceKindOf(saved != null && saved.pendingResourceType != 0 ? saved.pendingResourceType : current.OutputResourceType);
            string resource = InventoryService.ResourceDisplayName(outputKind);
            card.FeatureValue = card.Level == 0 ? $"产出{resource}"
                : $"待收取  {InventoryService.ResourceDisplayName(collectKind)} {pending:N0}";
            int bonus = EffectAggregator.Instance.Snapshot.GetOutputDelta(card.Id);
            string cycle = current.HarvestSeconds == 0 ? "未配置生产"
                : $"{resource} +{Math.Max(0L, (long)current.OutputAmount + bonus)} / {current.HarvestSeconds} 秒";
            bool completed = saved?.completed == true;
            bool working = completed && VillageWorkforce.ActiveWorkers(card.Id) > 0;
            card.FeatureDetail = (card.Level == 0 ? "建成后  " : working ? "生产中  " : completed ? "等待工人  " : "升级前  ") + cycle;
            if (card.Level != 0 && target != null && target.HarvestSeconds > 0)
                card.FeatureDetail += $"    →    Lv{target.Level}  +{Math.Max(0L, (long)target.OutputAmount + bonus)} / {target.HarvestSeconds} 秒";
            if (completed && current.HarvestSeconds > 0)
                card.ProductionProgress = saved.productionSeconds / current.HarvestSeconds;
            card.ShowCollect = completed;
            uint availableRoom = inv.ResourceRoom(collectKind);
            card.CanCollect = completed && pending > 0 && availableRoom > 0;
            if (completed && pending > 0 && !card.CanCollect)
                card.FeatureDetail += "\n对应资源已达仓库上限";
            else if (completed && availableRoom < pending)
                card.FeatureDetail += $"\n本次可收取 {availableRoom:N0}，其余保留";
        }

        public void SelectBuilding(uint id)
        {
            if (VillageService.IsFixedFacility(focus) && id != focus) return;
            selected = id;
            Refresh();
        }

        protected override void Execute(PanelIntent intent)
        {
            if (intent.Id != selected || intent.Id == 0 || (focus != 0 && intent.Id != focus)) return;
            var svc = VillageService.Instance;
            bool ok; string reason, feedback;
            uint amount;
            switch (intent.Action)
            {
                case PanelAction.Build: ok = svc.StartBuild(intent.Id, out reason); feedback = "已加入施工队列"; break;
                case PanelAction.CancelBuild: ok = svc.CancelBuild(intent.Id, out amount, out reason); feedback = $"已取消施工，返还 {amount:N0}"; break;
                case PanelAction.Upgrade: ok = svc.StartUpgrade(intent.Id, out reason); feedback = "已加入升级队列"; break;
                case PanelAction.Harvest: ok = svc.Harvest(intent.Id, out amount, out reason); feedback = $"已收取 {amount:N0}"; break;
                default: return;
            }
            Refresh(); Message(ok ? feedback : reason, ok);
        }
    }
}
