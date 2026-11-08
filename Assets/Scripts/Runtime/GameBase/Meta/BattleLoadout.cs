using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>战备草稿只预留仓库物品，格位和叠加规则由局内背包共用。</summary>
public sealed class BattleLoadout
{
    public const int MinPresets = 3, MaxPresets = 10;
    private readonly int context = GameSession.Instance.ContextId;
    private List<BattleSlotSave> slots = new List<BattleSlotSave>();

    public bool IsCurrent => GameSession.Instance.HasSession && context == GameSession.Instance.ContextId;
    public List<BattleSlotSave> Snapshot() => slots.Select(slot => slot.Copy()).ToList();
    public uint Count(uint id) => slots.Find(slot => slot.itemId == id)?.count ?? 0;
    public uint Maximum(uint id)
    {
        var definition = Definition(id);
        return definition == null ? 0 : Math.Min(RunInventory.StackLimit(definition), Available(id));
    }

    public BattleLoadout()
    {
        foreach (uint id in RunBagProgression.OwnedItems()) SetQuantity(id, 1, out _);
    }

    public static ItemTable_ItemConfig Definition(uint id)
    {
        if (id == 0 || InventoryService.TryGetResourceCommodity(id, out _)) return null;
        var definition = InventoryService.Instance.GetDefinition(id);
        return definition != null && definition.Enable && definition.RunCategory <= 3 ? definition : null;
    }

    public static uint Available(uint id) => RunInventory.IsImportant(id)
        ? (RunBagProgression.Owns(id) ? 1u : 0u)
        : InventoryService.Instance.GetItemCount(id);

    public static bool Validate(IReadOnlyList<BattleSlotSave> list, bool checkWarehouse, out string reason)
    {
        reason = null;
        if (list == null) { reason = "缺少背包记录"; return false; }
        var items = new HashSet<uint>();
        var addresses = new HashSet<(RunBagSection, int)>();
        foreach (var slot in list)
        {
            if (slot == null) { reason = "背包记录无效"; return false; }
            var definition = Definition(slot.itemId);
            if (definition == null || !RunInventory.Address(slot.section, slot.index)
                || !RunInventory.Accepts(slot.section, slot.itemId) || slot.count == 0
                || slot.count > RunInventory.StackLimit(definition) || !items.Add(slot.itemId)
                || !addresses.Add((slot.section, slot.index)))
            { reason = "道具 #" + slot.itemId + " 的格位、类型或数量无效"; return false; }
            if ((checkWarehouse || definition.RunCategory == 1) && slot.count > Available(slot.itemId))
            { reason = "道具 #" + slot.itemId + " 的可用库存不足"; return false; }
        }
        return true;
    }

    public bool SetQuantity(uint id, uint count, out string reason, RunBagSection? target = null, int index = -1)
    {
        reason = null;
        if (!IsCurrent) { reason = "战备会话已切换"; return false; }
        var existing = slots.Find(slot => slot.itemId == id);
        if (count == 0) { if (existing != null) slots.Remove(existing); return true; }
        var definition = Definition(id);
        if (definition == null || count > Maximum(id)) { reason = "库存不足或已达到叠加上限"; return false; }
        if (target.HasValue)
        {
            if (!RunInventory.Address(target.Value, index) || !RunInventory.Accepts(target.Value, id))
            { reason = "格位未开放或道具类型不符"; return false; }
            var occupant = Find(target.Value, index);
            if (occupant != null && occupant != existing) { reason = "目标格已有其他物品"; return false; }
            if (existing != null && existing != occupant) { reason = "该物品已有堆叠，请在原格追加或移动整叠"; return false; }
        }
        if (existing != null) { existing.count = count; return true; }
        RunBagSection section;
        if (target.HasValue) section = target.Value;
        else
        {
            section = definition.RunCategory == 0 ? RunBagSection.Misc : (RunBagSection)(definition.RunCategory - 1);
            index = FirstFree(section);
            if (index < 0 && section != RunBagSection.Misc) { section = RunBagSection.Misc; index = FirstFree(section); }
            if (index < 0) { reason = "没有可用格位"; return false; }
        }
        slots.Add(new BattleSlotSave { section = section, index = index, itemId = id, count = count });
        return true;
    }

    private BattleSlotSave Find(RunBagSection section, int index) => slots.Find(slot => slot.section == section && slot.index == index);

    private int FirstFree(RunBagSection section)
    {
        for (int index = 0; index < RunInventory.GetCapacity(section); index++)
            if (Find(section, index) == null) return index;
        return -1;
    }

    public bool Move(RunBagSection from, int fromIndex, RunBagSection to, int toIndex, out string reason)
    {
        reason = null;
        var source = Find(from, fromIndex);
        var target = Find(to, toIndex);
        if (!IsCurrent || source == null || !RunInventory.Address(from, fromIndex) || !RunInventory.Address(to, toIndex)
            || !RunInventory.Accepts(to, source.itemId) || (target != null && !RunInventory.Accepts(from, target.itemId)))
        { reason = "格位未开放或道具类型不符"; return false; }
        if (target != null) { target.section = from; target.index = fromIndex; }
        source.section = to; source.index = toIndex;
        return true;
    }

    public IReadOnlyList<BattlePresetSave> Presets()
    {
        var result = new List<BattlePresetSave>();
        var saved = GameSession.Instance.LoadedPayload?.battlePresets;
        if (saved != null)
            foreach (var preset in saved.Take(MaxPresets))
                result.Add(new BattlePresetSave { saved = preset?.saved == true,
                    slots = preset?.slots?.Where(slot => slot != null).Select(slot => slot.Copy()).ToList() ?? new List<BattleSlotSave>() });
        while (result.Count < MinPresets) result.Add(new BattlePresetSave());
        return result;
    }

    public bool ChangePreset(int index, bool add, bool remove, out string reason)
    {
        reason = null;
        if (!IsCurrent || (add && remove)) { reason = "当前无法修改组合"; return false; }
        var next = Presets().ToList();
        if (add)
        {
            if (next.Count >= MaxPresets) { reason = "最多 10 个档位"; return false; }
            next.Add(new BattlePresetSave());
        }
        else
        {
            if (index < 0 || index >= next.Count) { reason = "请选中一个档位"; return false; }
            if (remove)
            {
                if (next.Count <= MinPresets) { reason = "至少保留 3 个档位"; return false; }
                next.RemoveAt(index);
            }
            else next[index] = new BattlePresetSave { saved = true, slots = Snapshot() };
        }
        return MetaCommandRunner.Execute("Battle.Presets", (out string why) =>
        {
            GameSession.Instance.LoadedPayload.battlePresets = next;
            why = null;
            return true;
        }, out reason);
    }

    public bool LoadPreset(int index, out string message)
    {
        var presets = Presets();
        if (!IsCurrent || index < 0 || index >= presets.Count || !presets[index].saved)
        { message = "该档位还没有保存组合"; return false; }
        slots = new List<BattleSlotSave>();
        var skipped = new List<string>();
        foreach (var item in presets[index].slots)
        {
            uint count = Math.Min(item.count, Maximum(item.itemId));
            string label = Chaos.Gameplay.Items.ItemBase.Create(item.itemId)?.Name ?? ("道具 #" + item.itemId);
            if (count == 0) skipped.Add(label + " 缺货或不可携带");
            else if (!SetQuantity(item.itemId, count, out string why, item.section, item.index)) skipped.Add(label + " " + why);
            else if (count < item.count) skipped.Add(label + " 缺少 " + (item.count - count) + " 件");
        }
        message = skipped.Count == 0 ? "已加载组合" : "已加载组合\n" + string.Join("\n", skipped);
        return true;
    }
}
