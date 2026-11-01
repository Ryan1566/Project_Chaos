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
        public bool Full;
        public bool HasSession;
        public IReadOnlyList<InventoryRow> Rows;
    }
    public sealed class InventoryPresenter : GameplayPresenter<InventoryState>
    {
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
                    Full = false,
                    HasSession = false,
                    Rows = rows.ToArray()
                };
            }

            var svc = InventoryService.Instance;
            int used = svc.UsedSlots, total = svc.TotalSlots;
            var owned = new HashSet<uint>(RunBagProgression.OwnedItems());
            var seen = new HashSet<uint>();
            // 普通库存：全部条目按存档顺序排列（不截断），供滚动查看。
            foreach (var stack in svc.GetStacks())
            {
                if (stack == null || stack.count == 0) continue;
                var def = svc.GetDefinition(stack.itemId);
                rows.Add(new InventoryRow { ItemId = stack.itemId, Permanent = owned.Contains(stack.itemId),
                    Name = GameplayText.ItemName(def, stack.itemId),
                    Count = owned.Contains(stack.itemId) ? permanentLabel : stack.count + "/" + (def == null ? 0u : def.StackLimit), Category = def == null ? 0u : def.Category });
                seen.Add(stack.itemId);
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
                Title = title,
                Resources = GameplayText.Text("ui_inventory_resources", "物资 {0}　　材料 {1}　　图纸 {2}　　钱财 {3}",
                    svc.GetResource(InventoryService.ResourceKind.Goods), svc.GetResource(InventoryService.ResourceKind.Material),
                    svc.GetResource(InventoryService.ResourceKind.Blueprint), svc.GetResource(InventoryService.ResourceKind.Money)),
                Slots = GameplayText.Text("ui_inventory_slots", "格位 {0}/{1}　（基础 {2} + 效果 {3}）", used, total, InventoryService.BaseSlots, svc.SlotBonus),
                PermanentLabel = permanentLabel,
                Full = used >= total,
                HasSession = true,
                Hint = used >= total ? GameplayText.Text("ui_inventory_full", "格位已满（{0}/{1}），新种类无法入库，已有种类仍可叠到上限", used, total)
                    : rows.Count == 0 ? GameplayText.Text("ui_inventory_empty", "暂无库存") : "",
                Rows = rows.ToArray()
            };
        }
        protected override void Execute(PanelIntent intent) { }
    }
}
