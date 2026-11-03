using System;
using System.Collections.Generic;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class InventoryRow
    {
        public string Name, Count;
        public uint Category;
        public uint ItemId;
        public bool Permanent;
    }
    public sealed class InventoryState
    {
        public string Title, Resources, Slots, Hint, PermanentLabel;
        public bool HasSession;
        public bool CanManageWarehouse;
        public IReadOnlyList<InventoryRow> Rows;
    }
    public sealed class InventoryPresenter : GameplayPresenter<InventoryState>
    {
        protected override void OnAttach() { base.OnAttach(); MetaRuntimeDriver.Changed += Refresh; }
        protected override void OnDetach() { MetaRuntimeDriver.Changed -= Refresh; base.OnDetach(); }
        private static VillageFacilityText SettingsText(Func<VillageFacilityText> pick)
        {
            try { return pick(); } catch { return null; }
        }

        protected override InventoryState CreateState()
        {
            string title = SettingsText(() => VillageFacilitySettings.Load().warehouseTitle)?.Resolve();
            string permanentLabel = SettingsText(() => VillageFacilitySettings.Load().permanentLabel)?.Resolve();
            string missingSession = SettingsText(() => VillageFacilitySettings.Load().missingSession)?.Resolve();
            if (string.IsNullOrEmpty(title)) title = GameplayText.Text("ui_inventory_panel_title", "库存");
            if (string.IsNullOrEmpty(permanentLabel)) permanentLabel = GameplayText.Text("ui_inventory_permanent", "永久收藏");
            if (string.IsNullOrEmpty(missingSession)) missingSession = GameplayText.Text("ui_inventory_missing_session", "没有有效存档会话，暂不显示库存");

            var rows = new List<InventoryRow>();
            if (!GameSession.Instance.HasSession)
            {
                return new InventoryState
                {
                    Title = title,
                    Resources = string.Empty,
                    Slots = string.Empty,
                    Hint = missingSession,
                    PermanentLabel = permanentLabel,
                    HasSession = false,
                    Rows = rows.ToArray()
                };
            }

            var svc = InventoryService.Instance;
            uint capacity = VillageService.Instance.ResourceCapacity;
            uint level = VillageService.Instance.EffectiveLevel(VillageService.WarehouseId);
            var owned = new HashSet<uint>(RunBagProgression.OwnedItems());
            var seen = new HashSet<uint>();
            // 普通库存：全部条目按存档顺序排列（不截断），供滚动查看。
            foreach (var stack in svc.GetStacks())
            {
                if (stack == null || stack.count == 0 || InventoryService.TryGetResourceCommodity(stack.itemId, out _)
                    || !seen.Add(stack.itemId)) continue;
                var def = svc.GetDefinition(stack.itemId);
                rows.Add(new InventoryRow { ItemId = stack.itemId, Permanent = owned.Contains(stack.itemId),
                    Name = GameplayText.ItemName(def, stack.itemId),
                    Count = owned.Contains(stack.itemId) ? permanentLabel : "×" + svc.GetItemCount(stack.itemId), Category = def == null ? 0u : def.Category });
            }
            // 永久重要道具：与普通库存按物品ID去重合并，不重复、不占普通格位提示。
            var permanent = new List<uint>(RunBagProgression.OwnedItems() ?? new List<uint>());
            permanent.Sort();
            foreach (uint itemId in permanent)
            {
                if (!seen.Add(itemId)) continue;
                var def = svc.GetDefinition(itemId);
                rows.Add(new InventoryRow { ItemId = itemId, Permanent = true,
                    Name = GameplayText.ItemName(def, itemId), Count = permanentLabel,
                    Category = def == null ? 0u : def.Category });
            }
            return new InventoryState
            {
                Title = $"{title}  Lv{level}",
                Resources = ResourceLine(svc, capacity, 0),
                Slots = ResourceLine(svc, capacity, 4),
                PermanentLabel = permanentLabel,
                HasSession = true,
                CanManageWarehouse = VillageSceneFlow.IsVillageScene,
                Hint = rows.Count == 0 ? "暂无物品" : $"{rows.Count} 种物品",
                Rows = rows.ToArray()
            };
        }
        private static string ResourceLine(InventoryService inventory, uint capacity, int start)
        {
            var entries = new string[4];
            for (int i = 0; i < entries.Length; i++)
            {
                var kind = (InventoryService.ResourceKind)(start + i);
                uint value = inventory.GetResource(kind);
                entries[i] = $"{InventoryService.ResourceDisplayName(kind)} {value}/{capacity}" + (value > capacity ? " 超限" : "");
            }
            return string.Join("　　", entries);
        }

        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action == PanelAction.Upgrade && GameSession.Instance.HasSession && VillageSceneFlow.IsVillageScene)
                VillagePanel.OpenForBuilding(VillageService.WarehouseId);
        }
    }
}
