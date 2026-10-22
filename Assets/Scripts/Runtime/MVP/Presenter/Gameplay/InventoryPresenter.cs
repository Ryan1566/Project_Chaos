using System.Collections.Generic;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class InventoryRow
    {
        public string Name, Count;
        public uint Category;
    }
    public sealed class InventoryState
    {
        public string Resources, Slots, Hint;
        public bool Full;
        public IReadOnlyList<InventoryRow> Rows;
    }
    public sealed class InventoryPresenter : GameplayPresenter<InventoryState>
    {
        protected override InventoryState CreateState()
        {
            var svc = InventoryService.Instance;
            int used = svc.UsedSlots, total = svc.TotalSlots;
            var rows = new List<InventoryRow>();
            foreach (var stack in svc.GetStacks())
            {
                if (stack == null || stack.count == 0) continue;
                var def = svc.GetDefinition(stack.itemId);
                rows.Add(new InventoryRow { Name = GameplayText.ItemName(def, stack.itemId),
                    Count = stack.count + "/" + (def == null ? 0u : def.StackLimit), Category = def == null ? 0u : def.Category });
            }
            return new InventoryState
            {
                Resources = GameplayText.Text("ui_inventory_resources", "物资 {0}　　材料 {1}　　图纸 {2}　　钱财 {3}",
                    svc.GetResource(InventoryService.ResourceKind.Goods), svc.GetResource(InventoryService.ResourceKind.Material),
                    svc.GetResource(InventoryService.ResourceKind.Blueprint), svc.GetResource(InventoryService.ResourceKind.Money)),
                Slots = GameplayText.Text("ui_inventory_slots", "格位 {0}/{1}　（基础 {2} + 效果 {3}）", used, total, InventoryService.BaseSlots, svc.SlotBonus),
                Full = used >= total,
                Hint = used >= total ? GameplayText.Text("ui_inventory_full", "格位已满（{0}/{1}），新种类无法入库，已有种类仍可叠到上限", used, total)
                    : used == 0 ? GameplayText.Text("ui_inventory_empty", "暂无库存") : "",
                Rows = rows.ToArray()
            };
        }
        protected override void Execute(PanelIntent intent) { }
    }
}
