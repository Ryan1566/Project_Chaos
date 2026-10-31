using System.Collections.Generic;
using Chaos.UI.RadialMenu;

namespace Chaos.Gameplay.Items
{
    public sealed class InventoryRadialProvider : IRadialMenuProvider
    {
        readonly Dictionary<string, (RunBagSection section, int index, uint id)> _addresses = new Dictionary<string, (RunBagSection, int, uint)>();
        readonly int _context = GameSession.Instance.ContextId;
        public IReadOnlyList<RadialMenuOption> BuildOptions()
        {
            _addresses.Clear();
            var roots = new List<RadialMenuOption>();
            string[] names = { "重要道具", "工程道具", "强化治疗" };
            for (int row = 0; row < 3; row++)
            {
                var section = (RunBagSection)row;
                int capacity = RunInventory.GetCapacity(section);
                var children = new List<RadialMenuOption>();
                for (int i = 0; i < capacity; i++)
                {
                    var stack = RunInventory.GetSlot(section, i);
                    var item = stack == null ? null : ItemBase.Create(stack.itemId);
                    string reason = "空格子";
                    bool usable = item != null && RunItemUseService.CanUse(item, out reason);
                    string id = row + ":" + i;
                    if (item != null) _addresses[id] = (section, i, item.Id);
                    children.Add(new RadialMenuOption { Id = id, Name = item?.Name ?? "空格子", IconKey = item?.IconKey,
                        CountText = stack == null ? "" : "×" + stack.count, ShortcutText = (i + 1).ToString(), ShortcutNumber = i + 1,
                        Enabled = usable, DisabledReason = reason });
                }
                roots.Add(new RadialMenuOption { Id = "category:" + row, Name = names[row], Children = children,
                    Enabled = capacity > 0, DisabledReason = "该行尚未开放格位" });
            }
            return roots;
        }
        public static UnityEngine.Sprite ResolveIcon(RadialMenuOption option)
        {
            var sprite = string.IsNullOrEmpty(option.IconKey) ? null : UnityEngine.Resources.Load<UnityEngine.Sprite>(option.IconKey);
            if (sprite != null) return sprite;
            string rowText = option.Id.StartsWith("category:") ? option.Id.Substring(9) : option.Id.Split(':')[0];
            int row; if (!int.TryParse(rowText, out row)) return null;
            return ItemIconFallback.Resolve(row == 0 ? ItemParentCategory.Important : row == 1 ? ItemParentCategory.Engineering : ItemParentCategory.Recovery);
        }
        public bool TryExecute(string id, out string reason)
        {
            if (_context != GameSession.Instance.ContextId || !_addresses.TryGetValue(id, out var address))
            { reason = "背包会话已改变，请重新打开轮盘"; return false; }
            return RunItemUseService.TryUse(address.section, address.index, address.id, out reason);
        }
    }
}
