using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 资源 + 库存（M2/M9）的服务层 —— **唯一读写入口**。
///
/// ══════════════ 两个概念别混（06 §3.3）══════════════
/// * **格位 Slot** = 能放多少**种**物品（基础 8，可由 `EffectType=2` 提升，原型硬上限 24）。
/// * **叠加上限 StackLimit** = 同一种能堆多少**个**（`Item.StackLimit`，默认 99）。
///
/// 判定优先级：**格位判定 &gt; 叠加判定**（06 §3.5）——新种类先要有格位，才谈叠加。
/// 资源是 4 个**全局数字**，不占格位、不参与叠加。
/// </summary>
public class InventoryService : SingletonBase<InventoryService>
{
    /// <summary>基础格位（U3 裁决：8）。</summary>
    public const int BaseSlots = 8;

    /// <summary>原型硬上限（Lead 裁决 24；Effect 聚合后 clamp）。</summary>
    public const int HardMaxSlots = 24;

    /// <summary>资源种类（与 SavePayload 字段一一对应）。</summary>
    public enum ResourceKind
    {
        Goods = 0,      // 物资  → resGoods
        Material = 1,   // 材料  → resMaterial
        Blueprint = 2,  // 图纸  → resBlueprint
        Money = 3,      // 钱财  → resMoney
    }

    private List<ItemConfig> _defs;
    private bool _loaded;
    private int _slotBonus;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        string path = GlobalPath.data_JsonPathToRead + "ItemConfig";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[库存] 找不到 " + path + "（展示会退化，不抛异常）");
            _defs = new List<ItemConfig>();
            return;
        }

        DataList<ItemConfig> list = JsonUtility.FromJson<DataList<ItemConfig>>(asset.text);
        _defs = (list == null || list.datas == null) ? new List<ItemConfig>() : list.datas;
    }

    public void Reload()
    {
        _loaded = false;
        _defs = null;
        EnsureLoaded();
    }

    public ItemConfig GetDefinition(uint itemId)
    {
        EnsureLoaded();
        if (_defs == null) return null;
        for (int i = 0; i < _defs.Count; i++)
            if (_defs[i] != null && _defs[i].Index == itemId) return _defs[i];
        return null;
    }

    /// <summary>库存堆叠列表（直接返回存档引用）。</summary>
    public List<ItemStackSave> GetStacks()
    {
        SavePayload p = MetaProgressService.Instance.Payload;
        if (p.items == null) p.items = new List<ItemStackSave>();
        return p.items;
    }

    /// <summary>已用格位 = 库存里不同 itemId 的个数。</summary>
    public int UsedSlots
    {
        get
        {
            List<ItemStackSave> stacks = GetStacks();
            var ids = new List<uint>();
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] == null || stacks[i].count == 0) continue;
                if (!ids.Contains(stacks[i].itemId)) ids.Add(stacks[i].itemId);
            }
            return ids.Count;
        }
    }

    /// <summary>总格位 = clamp(8 + ΣEffectType=2 的数值, 0, 24)。</summary>
    public int TotalSlots
    {
        get { return Mathf.Clamp(BaseSlots + SlotBonus, 0, HardMaxSlots); }
    }

    /// <summary>格位加成 —— **统一从 EffectAggregator 读**（08 §3.3；不再自己算）。</summary>
    public int SlotBonus { get { return EffectAggregator.Instance.Snapshot.TotalSlotsBonus; } }

    /// <summary>
    /// 挂一组效果，按 `EffectType=2`（格位）累计加成。
    /// （W1a 原型：由 GM 直接传入 effectId 列表；W1b 家族树点亮后接同一入口。）
    /// </summary>
    public int ApplySlotEffects(List<uint> effectIds)
    {
        //W1b：格位加成改由 EffectAggregator 统一聚合（家族树点亮即生效），这里只负责触发重算。
        EffectAggregator.Instance.MarkDirty();
        _slotBonus = EffectAggregator.Instance.Snapshot.TotalSlotsBonus;
        ChaosLog.Info(LogChannel.Config,
            "[库存] 已按效果快照重算格位：加成 " + _slotBonus + "，总格位 " + TotalSlots);
        return _slotBonus;
    }

    private int ApplySlotEffectsLegacy(List<uint> effectIds)
    {
        _slotBonus = 0;
        if (effectIds != null)
        {
            TextAsset asset = Resources.Load<TextAsset>(GlobalPath.data_JsonPathToRead + "EffectConfig");
            if (asset != null)
            {
                DataList<EffectConfig> list = JsonUtility.FromJson<DataList<EffectConfig>>(asset.text);
                if (list != null && list.datas != null)
                {
                    for (int i = 0; i < effectIds.Count; i++)
                    {
                        for (int k = 0; k < list.datas.Count; k++)
                        {
                            EffectConfig e = list.datas[k];
                            if (e == null || !e.Enable || e.Index != effectIds[i]) continue;
                            //EffectType=2 = 格位（增量型，相加）
                            if (e.EffectType == 2u) _slotBonus += (int)e.Value;
                        }
                    }
                }
            }
            else
            {
                ChaosLog.Warn(LogChannel.Config, "[库存] 找不到 EffectConfig，格位加成按 0 处理");
            }
        }

        ChaosLog.Info(LogChannel.Config,
            "[库存] 已挂效果 [" + string.Join(",", effectIds == null ? new string[0] : System.Array.ConvertAll(effectIds.ToArray(), x => x.ToString()))
            + "] → 格位加成 " + _slotBonus + "，总格位 " + TotalSlots);
        return _slotBonus;
    }

    /// <summary>
    /// 入库。**先判格位（新种类），再判叠加**；返回 `true` 表示至少入了一部分，
    /// `reason` 给出失败/部分成功原因（**不静默丢弃**）。
    /// </summary>
    public bool AddItem(uint itemId, uint count, out string reason)
            {
        reason = null;
        if (count == 0) { reason = "数量为 0"; return false; }

        ItemConfig def = GetDefinition(itemId);
        if (def == null) { reason = "道具表里没有 itemId=" + itemId; ChaosLog.Warn(LogChannel.Config, "[库存] " + reason); return false; }
        if (!def.Enable) { reason = "道具 itemId=" + itemId + " 已禁用（Enable=false）"; ChaosLog.Warn(LogChannel.Config, "[库存] " + reason); return false; }

        uint stackLimit = def.StackLimit == 0 ? 1u : def.StackLimit;
        List<ItemStackSave> stacks = GetStacks();

        //★W3.2c 裁决 C：局外保留"部分成功" —— 只加能加的部分，>0 就返回 true
        uint addable = InventoryRules.MaxAddableStacks(stacks, TotalSlots, itemId, count, stackLimit);
        if (addable == 0u)
        {
            reason = "格位/叠加已满，无法入库（已有该物品上限 " + stackLimit + "，格位 " + UsedSlots + "/" + TotalSlots + "）";
            ChaosLog.Warn(LogChannel.Config, "[库存] " + reason);
            return false;
        }

        InventoryRules.ApplyAddStacks(stacks, TotalSlots, itemId, addable, stackLimit);

        if (addable < count)
        {
            uint lost = count - addable;
            reason = "实际入库 " + addable + "，剩余 " + lost + " 未入库（格位/叠加已满）";
            ChaosLog.Warn(LogChannel.Config, "[库存] itemId=" + itemId + " " + reason);
        }
        else
        {
            ChaosLog.Info(LogChannel.Config, "[库存] +" + addable + " itemId=" + itemId
                + "（已用格 " + UsedSlots + "/" + TotalSlots + "，叠加上限 " + stackLimit + "）");
        }
        return true;
    }

    /// <summary>出库；减到 0 释放格位（R4）。</summary>
    public bool RemoveItem(uint itemId, uint count, out string reason)
    {
        reason = null;
        List<ItemStackSave> stacks = GetStacks();
        for (int i = 0; i < stacks.Count; i++)
        {
            ItemStackSave stack = stacks[i];
            if (stack == null || stack.itemId != itemId) continue;

            if (stack.count < count)
            {
                reason = "库存不足（有 " + stack.count + "，要扣 " + count + "）";
                ChaosLog.Warn(LogChannel.Config, "[库存] " + reason + " itemId=" + itemId);
                return false;
            }

            stack.count -= count;
            if (stack.count == 0) stacks.RemoveAt(i);
            ChaosLog.Info(LogChannel.Config, "[库存] itemId=" + itemId + " -" + count + "，已用 " + UsedSlots + "/" + TotalSlots);
            return true;
        }

        reason = "库存里没有 itemId=" + itemId;
        ChaosLog.Warn(LogChannel.Config, "[库存] " + reason);
        return false;
    }

    /// <summary>清空库存（不动资源）。</summary>
    public void ClearItems()
    {
        GetStacks().Clear();
        ChaosLog.Info(LogChannel.Config, "[库存] 已清空库存（资源不变）");
    }

    // ══════════════ 4 资源（全局数字，不占格位）══════════════

    public uint GetResource(ResourceKind kind)
    {
        SavePayload p = MetaProgressService.Instance.Payload;
        switch (kind)
        {
            case ResourceKind.Goods: return p.resGoods;
            case ResourceKind.Material: return p.resMaterial;
            case ResourceKind.Blueprint: return p.resBlueprint;
            default: return p.resMoney;
        }
    }

    public uint AddResource(ResourceKind kind, uint amount)
    {
        SavePayload p = MetaProgressService.Instance.Payload;
        switch (kind)
        {
            case ResourceKind.Goods: p.resGoods = SaturatingAdd(p.resGoods, amount); return p.resGoods;
            case ResourceKind.Material: p.resMaterial = SaturatingAdd(p.resMaterial, amount); return p.resMaterial;
            case ResourceKind.Blueprint: p.resBlueprint = SaturatingAdd(p.resBlueprint, amount); return p.resBlueprint;
            default: p.resMoney = SaturatingAdd(p.resMoney, amount); return p.resMoney;
        }
    }

    /// <summary>消耗资源；不足则拒绝并返回 false（**不减到负**）。</summary>
    public bool SpendResource(ResourceKind kind, uint amount, out string reason)
    {
        reason = null;
        uint have = GetResource(kind);
        if (have < amount)
        {
            reason = "资源不足（有 " + have + "，要花 " + amount + "）";
            ChaosLog.Warn(LogChannel.Config, "[库存] " + reason);
            return false;
        }

        SavePayload p = MetaProgressService.Instance.Payload;
        switch (kind)
        {
            case ResourceKind.Goods: p.resGoods -= amount; break;
            case ResourceKind.Material: p.resMaterial -= amount; break;
            case ResourceKind.Blueprint: p.resBlueprint -= amount; break;
            default: p.resMoney -= amount; break;
        }
        return true;
    }

    private static uint SaturatingAdd(uint a, uint b)
    {
        uint sum = a + b;
        if (sum < a) return uint.MaxValue;   //溢出 → 夹到 uint 上限
        return sum;
    }

    public static string ResourceDisplayName(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Goods: return "物资";
            case ResourceKind.Material: return "材料";
            case ResourceKind.Blueprint: return "图纸";
            default: return "钱财";
        }
    }

    /// <summary>给 GM/面板打印的库存摘要。</summary>
    public string DumpState()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[库存] 已用格位 ").Append(UsedSlots).Append("/").Append(TotalSlots)
          .Append("（基础 ").Append(BaseSlots).Append(" + 效果 ").Append(_slotBonus).Append("）")
          .Append(" | 资源 物资=").Append(GetResource(ResourceKind.Goods))
          .Append(" 材料=").Append(GetResource(ResourceKind.Material))
          .Append(" 图纸=").Append(GetResource(ResourceKind.Blueprint))
          .Append(" 钱财=").Append(GetResource(ResourceKind.Money));
        List<ItemStackSave> stacks = GetStacks();
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] == null) continue;
            ItemConfig def = GetDefinition(stacks[i].itemId);
            sb.Append("\n  itemId=").Append(stacks[i].itemId)
              .Append(" ").Append(def == null ? "(表缺失)" : def.NameKey)
              .Append(" ×").Append(stacks[i].count).Append("/").Append(def == null ? "?" : def.StackLimit.ToString());
        }
        return sb.ToString();
    }
}
