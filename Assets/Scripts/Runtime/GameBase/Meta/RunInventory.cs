using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// **局内库存**（W3.2b）：独立内存，**不落盘**、不写 `SavePayload`。
///
/// 规则与局外一致（8 格 + `EffectType=2` 加成、硬上限 24、同名叠加 99）——
/// 判定逻辑**复用 `InventoryRules`**（与 `InventoryService` 同一套），不复制第二份。
/// `EnterScene` / `ReturnToMeta` 都 `Clear()`。
/// </summary>
public static class RunInventory
{
    /// <summary>一个堆叠（实现 `InventoryRules.IStack`）。</summary>
    public class Stack : InventoryRules.IStack
    {
        public uint itemId { get; set; }
        public uint count { get; set; }
    }

    private static readonly List<Stack> _stacks = new List<Stack>();

    public static List<Stack> Stacks { get { return _stacks; } }
    public static int UsedSlots { get { return _stacks.Count; } }

    /// <summary>总格位 = 8 + EffectType=2 加成，clamp 到 24（与局外同一套）。</summary>
    public static int TotalSlots
    {
        get
        {
            int bonus = EffectAggregator.Instance.Snapshot.TotalSlotsBonus;
            return Mathf.Clamp(InventoryRules.BaseSlots + bonus, 0, InventoryRules.HardMaxSlots);
        }
    }

    public static uint TotalCount
    {
        get
        {
            uint total = 0u;
            for (int i = 0; i < _stacks.Count; i++) total += _stacks[i].count;
            return total;
        }
    }

    /// <summary>入包：先叠同名未满堆，再占新格；失败给原因。</summary>
    public static bool AddItem(uint itemId, uint count, out string reason)
    {
        reason = null;
        if (!InventoryRules.CanAdd(_stacks, TotalSlots, itemId, count, out reason))
        {
            ChaosLog.Warn(LogChannel.Config, "[局内库存] 拾取失败：物品 " + itemId + " ×" + count + " —— " + reason);
            return false;
        }

        InventoryRules.ApplyAdd(_stacks, TotalSlots, itemId, count);
        ChaosLog.Info(LogChannel.Config, "[局内库存] +" + count + " 物品 " + itemId
            + "（已用格 " + UsedSlots + "/" + TotalSlots + "，共 " + TotalCount + " 个）");
        return true;
    }

    public static bool RemoveItem(uint itemId, uint count, out string reason)
    {
        reason = null;
        for (int i = 0; i < _stacks.Count; i++)
        {
            if (_stacks[i].itemId != itemId) continue;
            if (_stacks[i].count < count) { reason = "数量不足"; return false; }
            _stacks[i].count -= count;
            if (_stacks[i].count == 0u) _stacks.RemoveAt(i);
            return true;
        }
        reason = "没有该物品";
        return false;
    }

    public static uint GetCount(uint itemId)
    {
        uint total = 0u;
        for (int i = 0; i < _stacks.Count; i++) if (_stacks[i].itemId == itemId) total += _stacks[i].count;
        return total;
    }

    public static void Clear()
    {
        if (_stacks.Count > 0) ChaosLog.Info(LogChannel.Config, "[局内库存] 清空（原有 " + _stacks.Count + " 种物品）");
        _stacks.Clear();
    }

    public static string DumpState()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[局内库存] 已用 ").Append(UsedSlots).Append("/").Append(TotalSlots)
          .Append("，共 ").Append(TotalCount).Append(" 个：");
        if (_stacks.Count == 0) sb.Append("（空）");
        for (int i = 0; i < _stacks.Count; i++)
            sb.Append("\n    物品 ").Append(_stacks[i].itemId).Append(" ×").Append(_stacks[i].count);
        return sb.ToString();
    }
}
