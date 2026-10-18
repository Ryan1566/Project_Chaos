using System.Collections.Generic;

/// <summary>
/// **库存判定公共逻辑**（W3.2b）：局外 `InventoryService` 与局内 `RunInventory` 共用同一套
/// 格位/叠加规则，**不复制两份**。
///
/// 规则：基础 8 格 + `EffectType=2` 加成（由调用方算好传入 `totalSlots`），硬上限 24，
/// 同名叠加上限 99；先叠到未满的同名堆，再占新格。
/// </summary>
public static class InventoryRules
{
    public const int BaseSlots = 8;
    public const int HardMaxSlots = 24;
    public const uint MaxStackPerItem = 99u;

    /// <summary>库存里的一个堆叠（局外/局内各自实现，只要提供可读写属性）。</summary>
    public interface IStack
    {
        uint itemId { get; set; }
        uint count { get; set; }
    }

    /// <summary>能否加入：叠放 + 新格两种可能都算一遍，给出失败原因。</summary>
    /// <summary>3 参重载：叠加上限用默认 <see cref="MaxStackPerItem"/>（局内 RunInventory 用它）。</summary>
    public static bool CanAdd<T>(List<T> stacks, int totalSlots, uint itemId, uint count, out string reason) where T : class, IStack
    {
        return CanAdd(stacks, totalSlots, itemId, count, MaxStackPerItem, out reason);
    }

    /// <summary>4 参：**叠加上限由调用方给**（局外传 `Item.StackLimit`，表是权威；99 只是兜底）。</summary>
    public static bool CanAdd<T>(List<T> stacks, int totalSlots, uint itemId, uint count, uint stackLimit, out string reason) where T : class, IStack
    {
        reason = null;
        if (count == 0u) { reason = "数量为 0"; return false; }

        //★W3.2b：99 是**每种物品的总量上限**（不是每堆），超过直接拒绝（对齐验收"叠到 99 再掉→失败"）
        uint have = 0u;
        for (int i = 0; i < stacks.Count; i++)
            if (stacks[i] != null && stacks[i].itemId == itemId) have += stacks[i].count;
        if (have + count > stackLimit)
        {
            reason = "已达叠加上限（已有 " + have + "，上限 " + stackLimit + "）";
            return false;
        }

        uint remain = count;

        //① 先看能叠多少
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] == null || stacks[i].itemId != itemId) continue;
            if (stacks[i].count >= stackLimit) continue;
            uint canStack = stackLimit - stacks[i].count;
            remain -= remain < canStack ? remain : canStack;
            if (remain == 0u) return true;
        }

        //② 剩下的需要新格
        int freeSlots = totalSlots - stacks.Count;
        if (freeSlots <= 0)
        {
            reason = "格位已满（" + stacks.Count + "/" + totalSlots + "），且同名堆已到 " + stackLimit;
            return false;
        }
        if (remain > (uint)freeSlots * stackLimit)
        {
            reason = "格位不足：剩余 " + remain + " 需要 " + ((remain + stackLimit - 1) / stackLimit) + " 格，实际空 " + freeSlots;
            return false;
        }
        return true;
    }

    /// <summary>
    /// **可入数量**（W3.2c 裁决 C）：同名未满堆的剩余容量 + 空格的容量，取 min(count, 容量)。
    /// 局外用它做"部分成功"；局内不用（局内是全有或全无）。
    /// </summary>
    public static uint MaxAddable<T>(List<T> stacks, int totalSlots, uint itemId, uint count, uint stackLimit) where T : class, IStack
    {
        //① 该物品**总量**上限（与 CanAdd 一致：stackLimit 是每种物品的总量上限）
        uint have = 0u;
        for (int i = 0; i < stacks.Count; i++)
            if (stacks[i] != null && stacks[i].itemId == itemId) have += stacks[i].count;
        uint totalCap = stackLimit > have ? stackLimit - have : 0u;

        //② 物理容量：同名未满堆剩余 + 空格容量
        uint capacity = 0u;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] == null || stacks[i].itemId != itemId) continue;
            if (stacks[i].count < stackLimit) capacity += stackLimit - stacks[i].count;
        }
        int freeSlots = totalSlots - stacks.Count;
        if (freeSlots > 0) capacity += (uint)freeSlots * stackLimit;

        uint addable = count < capacity ? count : capacity;
        return addable < totalCap ? addable : totalCap;
    }

    /// <summary>存档桶版 MaxAddable（局外 InventoryService 用）。</summary>
    public static uint MaxAddableStacks(List<ItemStackSave> stacks, int totalSlots, uint itemId, uint count, uint stackLimit)
    {
        return MaxAddable(ToAdapters(stacks), totalSlots, itemId, count, stackLimit);
    }

    // ── 存档桶适配（`ItemStackSave` 是**字段**型，JsonUtility 要求字段，不能改属性）──
    private class FieldStack : IStack
    {
        public uint itemId { get; set; }
        public uint count { get; set; }
    }

    private static List<FieldStack> ToAdapters(List<ItemStackSave> stacks)
    {
        var list = new List<FieldStack>();
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] == null) continue;
            list.Add(new FieldStack { itemId = stacks[i].itemId, count = stacks[i].count });
        }
        return list;
    }

    /// <summary>存档桶版的 CanAdd（局外 InventoryService 用）。</summary>
    public static bool CanAddStacks(List<ItemStackSave> stacks, int totalSlots, uint itemId, uint count, uint stackLimit, out string reason)
    {
        return CanAdd(ToAdapters(stacks), totalSlots, itemId, count, stackLimit, out reason);
    }

    /// <summary>存档桶版的 ApplyAdd（局外 InventoryService 用）：算完写回原列表。</summary>
    public static void ApplyAddStacks(List<ItemStackSave> stacks, int totalSlots, uint itemId, uint count, uint stackLimit)
    {
        var adapters = ToAdapters(stacks);
        ApplyAdd(adapters, totalSlots, itemId, count, stackLimit);
        stacks.Clear();
        for (int i = 0; i < adapters.Count; i++)
            stacks.Add(new ItemStackSave { itemId = adapters[i].itemId, count = adapters[i].count });
    }

    /// <summary>执行加入（调用前应先 <see cref="CanAdd{T}"/>）。</summary>
    /// <summary>3 参重载：叠加上限用默认值。</summary>
    public static void ApplyAdd<T>(List<T> stacks, int totalSlots, uint itemId, uint count) where T : class, IStack, new()
    {
        ApplyAdd(stacks, totalSlots, itemId, count, MaxStackPerItem);
    }

    /// <summary>4 参：叠加上限由调用方给。</summary>
    public static void ApplyAdd<T>(List<T> stacks, int totalSlots, uint itemId, uint count, uint stackLimit) where T : class, IStack, new()
    {
        uint remain = count;

        //① 叠到已有未满堆
        for (int i = 0; i < stacks.Count && remain > 0u; i++)
        {
            if (stacks[i] == null || stacks[i].itemId != itemId) continue;
            if (stacks[i].count >= stackLimit) continue;
            uint canStack = stackLimit - stacks[i].count;
            uint add = remain < canStack ? remain : canStack;
            stacks[i].count += add;
            remain -= add;
        }

        //② 新格
        while (remain > 0u)
        {
            uint add = remain < stackLimit ? remain : stackLimit;
            T stack = new T();
            stack.itemId = itemId;
            stack.count = add;
            stacks.Add(stack);
            remain -= add;
        }
    }
}
