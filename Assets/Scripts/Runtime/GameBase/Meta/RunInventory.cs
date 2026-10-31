using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunBagSection { Important, Engineering, Recovery, Misc }

/// <summary>Context-owned physical bag. Permanent ownership and unlocks live in SavePayload.</summary>
public static class RunInventory
{
    public sealed class Stack : InventoryRules.IStack
    {
        public uint itemId { get; set; }
        public uint count { get; set; }
        internal Stack Copy() => new Stack { itemId = itemId, count = count };
    }
    private static readonly Stack[][] Slots = { new Stack[5], new Stack[7], new Stack[7], new Stack[30] };
    private static int _context = -1;
    private static bool _started;
    public static event Action Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { ClearSlots(); _context = -1; Changed = null; _started = false; }
    private static void EnsureContext()
    {
        if (_context == GameSession.Instance.ContextId) return;
        ClearSlots(); _started = false; _context = GameSession.Instance.ContextId;
    }
    private static void ClearSlots()
    { foreach (var row in Slots) Array.Clear(row, 0, row.Length); }
    private static void Publish()
    {
        if (Changed == null) return;
        foreach (Action listener in Changed.GetInvocationList())
            try { listener(); } catch (Exception e) { Debug.LogException(e); }
    }
    public static int GetMaximum(RunBagSection section)
    { return Valid(section) ? Slots[(int)section].Length : 0; }
    public static int GetCapacity(RunBagSection section)
    {
        EnsureContext();
        return RunBagProgression.GetCapacity(section);
    }
    private static bool Valid(RunBagSection section) => (uint)section <= 3;
    private static bool Address(RunBagSection section, int index)
    { return Valid(section) && index >= 0 && index < GetCapacity(section); }
    public static Stack GetSlot(RunBagSection section, int index)
    {
        EnsureContext();
        return Address(section, index) ? Slots[(int)section][index]?.Copy() : null;
    }
    public static List<Stack> Stacks
    {
        get
        {
            EnsureContext();
            var result = new List<Stack>();
            foreach (var row in Slots) foreach (var item in row) if (item != null) result.Add(item.Copy());
            return result;
        }
    }
    public static int UsedSlots => Stacks.Count;
    public static int TotalSlots
    {
        get { int sum = 0; for (int i = 0; i < 4; i++) sum += GetCapacity((RunBagSection)i); return sum; }
    }
    public static uint TotalCount
    {
        get { ulong total = 0; foreach (var item in Stacks) total += item.count; return (uint)Math.Min(uint.MaxValue, total); }
    }
    public static bool IsImportant(uint itemId)
    { return InventoryService.Instance.GetDefinition(itemId)?.RunCategory == 1; }
    // No event between quantity commit and effect commit; observers see the complete transaction.
    internal static bool TryConsumeExact(RunBagSection section, int index, uint expectedId, out string reason)
    {
        if (!Ready(out reason)) return false;
        if (!Address(section, index) || IsImportant(expectedId)) { reason = "道具不能消耗"; return false; }
        var stack = Slots[(int)section][index];
        if (stack == null || stack.itemId != expectedId || stack.count == 0) { reason = "格子内容已经改变"; return false; }
        if (--stack.count == 0) Slots[(int)section][index] = null;
        return true;
    }
    internal static void NotifyUseCommitted() { Publish(); }
    private static bool Accepts(RunBagSection section, uint id)
    {
        if (section == RunBagSection.Misc) return true;
        var def = InventoryService.Instance.GetDefinition(id);
        return def != null && def.RunCategory == (uint)section + 1;
    }
    public static uint GetCount(uint id)
    { foreach (var stack in Stacks) if (stack.itemId == id) return stack.count; return 0; }
    public static void BeginRun()
    {
        Chaos.Gameplay.Items.RunItemUseService.Clear();
        EnsureContext(); ClearSlots(); _started = true;
        foreach (var id in RunBagProgression.OwnedItems())
            PlaceImportant(id);
        Publish();
    }
    public static void Clear()
    {
        Chaos.Gameplay.Items.RunItemUseService.Clear();
        EnsureContext(); bool changed = UsedSlots != 0;
        ClearSlots(); _started = false;
        if (changed) Publish();
    }
    private static bool Ready(out string reason)
    {
        EnsureContext(); reason = null;
        if (!GameSession.Instance.HasSession) { reason = "没有有效存档会话"; return false; }
        if (!_started) { reason = "局内背包尚未初始化"; return false; }
        return true;
    }
    private static bool EmptySlot(RunBagSection section, out int index)
    {
        for (int i = 0; i < GetCapacity(section); i++)
            if (Slots[(int)section][i] == null) { index = i; return true; }
        index = -1; return false;
    }
    private static bool FindDestination(ItemConfig def, bool important, out RunBagSection section, out int index)
    {
        if (important && EmptySlot(RunBagSection.Important, out index)) { section = RunBagSection.Important; return true; }
        if (EmptySlot(RunBagSection.Misc, out index)) { section = RunBagSection.Misc; return true; }
        if (!important && def.RunCategory >= 1 && def.RunCategory <= 3)
        {
            section = (RunBagSection)(def.RunCategory - 1);
            if (EmptySlot(section, out index)) return true;
        }
        section = RunBagSection.Misc; index = -1; return false;
    }
    private static bool PlaceImportant(uint id)
    {
        var def = InventoryService.Instance.GetDefinition(id);
        if (def == null || !def.Enable || def.RunCategory != 1 || GetCount(id) != 0) return false;
        if (!FindDestination(def, true, out RunBagSection section, out int index)) return false;
        Slots[(int)section][index] = new Stack { itemId = id, count = 1 };
        return true;
    }
    public static bool AddItem(uint itemId, uint count, out string reason)
    {
        if (!Ready(out reason)) return false;
        var def = InventoryService.Instance.GetDefinition(itemId);
        if (def == null || !def.Enable || def.RunCategory > 3) { reason = "道具定义无效或未启用"; return false; }
        if (count == 0) { reason = "数量必须大于零"; return false; }
        if (IsImportant(itemId))
        {
            // Domain acquisition commits first; ground drops can disappear only after this returns success.
            if (!RunBagProgression.AcquireImportant(itemId, out reason)) return false;
            bool carried = PlaceImportant(itemId);
            if (carried) Publish();
            if (GetCount(itemId) == 0) reason = "已永久收藏，背包满时可稍后带入";
            return true;
        }
        uint limit = def.IsUnique ? 1u : Math.Max(1, def.StackLimit);
        uint have = GetCount(itemId);
        if (have > limit || count > limit - have) { reason = "超过该物品的叠加上限 " + limit; return false; }
        foreach (var row in Slots)
            foreach (var item in row)
                if (item != null && item.itemId == itemId) { item.count += count; Publish(); return true; }
        if (!FindDestination(def, false, out RunBagSection target, out int free))
        { reason = "杂物区及对应专用行没有空格"; return false; }
        Slots[(int)target][free] = new Stack { itemId = itemId, count = count };
        Publish(); return true;
    }
    public static bool RemoveItem(uint itemId, uint count, out string reason)
    {
        if (!Ready(out reason)) return false;
        if (IsImportant(itemId)) { reason = "重要道具永久保留，不能消耗或丢弃"; return false; }
        if (count == 0) { reason = "数量必须大于零"; return false; }
        foreach (var row in Slots)
            for (int i = 0; i < row.Length; i++)
            {
                var item = row[i];
                if (item == null || item.itemId != itemId) continue;
                if (item.count < count) { reason = "数量不足"; return false; }
                item.count -= count;
                if (item.count == 0) row[i] = null;
                Publish(); return true;
            }
        reason = "没有该物品"; return false;
    }
    public static bool TryMove(RunBagSection from, int fromIndex, RunBagSection to, int toIndex, out string reason)
    {
        if (!Ready(out reason)) return false;
        if (!Address(from, fromIndex) || !Address(to, toIndex)) { reason = "目标格尚未开放或不存在"; return false; }
        if (from == to && fromIndex == toIndex) return true;
        var source = Slots[(int)from][fromIndex]; var target = Slots[(int)to][toIndex];
        if (source == null) { reason = "源格没有物品"; return false; }
        if (!Accepts(to, source.itemId) || (target != null && !Accepts(from, target.itemId)))
        { reason = "物品类型与目标专用行不匹配，无法交换"; return false; }
        Slots[(int)to][toIndex] = source; Slots[(int)from][fromIndex] = target;
        Publish(); return true;
    }
    public static bool TryCarryImportant(uint itemId, out string reason)
    {
        if (!Ready(out reason)) return false;
        if (!RunBagProgression.Owns(itemId)) { reason = "尚未获得该重要道具"; return false; }
        if (GetCount(itemId) != 0) { reason = "该道具已在背包中"; return false; }
        if (!PlaceImportant(itemId)) { reason = "没有可用格位或道具尚未启用"; return false; }
        Publish(); return true;
    }
    public static IReadOnlyList<uint> GetUncarriedImportant()
    {
        EnsureContext();
        var ids = new List<uint>();
        foreach (var id in RunBagProgression.OwnedItems()) if (GetCount(id) == 0) ids.Add(id);
        return ids;
    }
    public static bool GrantSlots(RunBagSection section, int amount, string sourceId, out string reason)
    {
        bool result = RunBagProgression.GrantSlots(section, amount, sourceId, out reason);
        if (result && !MetaCommandRunner.IsExecuting) Publish();
        return result;
    }
    public static string DumpState()
    {
        var text = new System.Text.StringBuilder("局内背包 " + UsedSlots + "/" + TotalSlots);
        foreach (var stack in Stacks) text.Append("\n").Append(stack.itemId).Append(" ×").Append(stack.count);
        return text.ToString();
    }
}
