using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>仓库物品与村庄资源账本。普通物品不限格位，资源逐种受仓库等级限制。</summary>
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
        Money = 3,
        Wood = 4,
        IronOre = 5,
        Stone = 6,
        RawSalt = 7,
    }

    private List<ItemTable_ItemConfig> _defs;
    private bool _loaded;
    private int _slotBonus;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        string path = GlobalPath.data_JsonPathToRead + "ItemTable_ItemConfig";
        TextAsset asset = ConfigTableAssets.Load(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[库存] 找不到 " + path + "（展示会退化，不抛异常）");
            _defs = new List<ItemTable_ItemConfig>();
            return;
        }

        DataList<ItemTable_ItemConfig> list = JsonUtility.FromJson<DataList<ItemTable_ItemConfig>>(asset.text);
        _defs = (list == null || list.datas == null) ? new List<ItemTable_ItemConfig>() : list.datas;
        var itemUses = Chaos.Gameplay.Items.ItemUseCatalog.Load();
        if (itemUses != null) foreach (var definition in itemUses.additionalDefinitions)
            if (definition != null && !_defs.Exists(d => d.Index == definition.Index)) _defs.Add(definition);
    }

    public void Reload()
    {
        _loaded = false;
        _defs = null;
        EnsureLoaded();
    }

    public ItemTable_ItemConfig GetDefinition(uint itemId)
    {
        EnsureLoaded();
        if (_defs == null) return null;
        for (int i = 0; i < _defs.Count; i++)
            if (_defs[i] != null && _defs[i].Index == itemId) return _defs[i];
        return null;
    }

    /// <summary>
    /// 全部道具定义（**表顺序，只读用途，别改里面的对象**）。
    /// 给“按表遍历”的场景用（W5.3 市场要列出所有可交易物品，逐个 itemId 试探既慢又不可靠）。
    /// </summary>
    public List<ItemTable_ItemConfig> GetAllDefinitions()
    {
        EnsureLoaded();
        if (_defs == null) _defs = new List<ItemTable_ItemConfig>();
        return _defs;
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
            TextAsset asset = ConfigTableAssets.Load(GlobalPath.data_JsonPathToRead + "EffectTable_EffectConfig");
            if (asset != null)
            {
                DataList<EffectTable_EffectConfig> list = JsonUtility.FromJson<DataList<EffectTable_EffectConfig>>(asset.text);
                if (list != null && list.datas != null)
                {
                    for (int i = 0; i < effectIds.Count; i++)
                    {
                        for (int k = 0; k < list.datas.Count; k++)
                        {
                            EffectTable_EffectConfig e = list.datas[k];
                            if (e == null || !e.Enable || e.Index != effectIds[i]) continue;
                            //EffectType=2 = 格位（增量型，相加）
                            if (e.EffectType == 2u) _slotBonus += (int)e.Value;
                        }
                    }
                }
            }
            else
            {
                ChaosLog.Warn(LogChannel.Config, "[库存] 找不到 EffectTable_EffectConfig，格位加成按 0 处理");
            }
        }

        ChaosLog.Info(LogChannel.Config,
            "[库存] 已挂效果 [" + string.Join(",", effectIds == null ? new string[0] : System.Array.ConvertAll(effectIds.ToArray(), x => x.ToString()))
            + "] → 格位加成 " + _slotBonus + "，总格位 " + TotalSlots);
        return _slotBonus;
    }

    public bool AddItem(uint itemId, uint count, out string reason)
    {
        if (!CanReceive(itemId, count, out reason)) return false;
        if (TryGetResourceCommodity(itemId, out var resource)) return TryCreditResource(resource, count, out reason);
        var definition = GetDefinition(itemId);
        if (definition.RunCategory == 1)
        {
            if (MetaCommandRunner.IsMutating)
            {
                RunBagProgression.RegisterImportantInCommand(itemId);
                return true;
            }
            return RunBagProgression.AcquireImportant(itemId, out reason);
        }
        var stacks = GetStacks();
        var stack = stacks.Find(entry => entry != null && entry.itemId == itemId);
        if (stack == null) stacks.Add(new ItemStackSave { itemId = itemId, count = count });
        else stack.count += count;
        return true;
    }

    public bool CanReceive(uint id, uint count, out string reason)
    {
        reason = null;
        if (count == 0) { reason = "数量为零"; return false; }
        var definition = GetDefinition(id);
        if (definition == null || !definition.Enable) { reason = "物品或商品未配置或已禁用"; return false; }
        if (TryGetResourceCommodity(id, out var resource))
        {
            if (ResourceRoom(resource) >= count) return true;
            reason = ResourceDisplayName(resource) + "已达到仓库容量";
            return false;
        }
        if (definition.RunCategory == 1) return true;
        uint owned = GetItemCount(id);
        if (definition.IsUnique && (owned != 0 || count != 1))
        {
            reason = "该唯一物品只能持有一件";
            return false;
        }
        if (count > uint.MaxValue - owned)
        {
            reason = "物品计数已达到存档数值上限";
            return false;
        }
        return true;
    }

    public uint GetItemCount(uint id)
    {
        ulong total = 0;
        foreach (var stack in GetStacks())
            if (stack != null && stack.itemId == id) total += stack.count;
        return (uint)System.Math.Min(total, uint.MaxValue);
    }

    public bool RemoveItem(uint itemId, uint count, out string reason)
    {
        reason = null;
        if (TryGetResourceCommodity(itemId, out var resource)) return SpendResource(resource, count, out reason);
        if (RunInventory.IsImportant(itemId)) { reason = "重要道具永久保留，不能消耗或出售"; return false; }
        if (GetItemCount(itemId) < count) { reason = "物品数量不足"; return false; }
        var stacks = GetStacks();
        for (int i = 0; i < stacks.Count && count > 0;)
        {
            var stack = stacks[i];
            if (stack == null || stack.itemId != itemId) { i++; continue; }
            uint removed = System.Math.Min(stack.count, count);
            stack.count -= removed;
            count -= removed;
            if (stack.count == 0) stacks.RemoveAt(i);
            else i++;
        }
        return true;
    }

    /// <summary>清空库存（不动资源）。</summary>
    public void ClearItems()
    {
        GetStacks().Clear();
        ChaosLog.Info(LogChannel.Config, "[库存] 已清空库存（资源不变）");
    }

    // ══════════════ 4 资源（全局数字，不占格位）══════════════
    public uint GetResource(ResourceKind kind) => ReadResource(MetaProgressService.Instance.Payload, kind);

    private static uint ReadResource(SavePayload payload, ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Goods: return payload.resGoods;
            case ResourceKind.Material: return payload.resMaterial;
            case ResourceKind.Blueprint: return payload.resBlueprint;
            case ResourceKind.Money: return payload.resMoney;
            case ResourceKind.Wood: return payload.resWood;
            case ResourceKind.IronOre: return payload.resIronOre;
            case ResourceKind.Stone: return payload.resStone;
            case ResourceKind.RawSalt: return payload.resRawSalt;
            default: throw new System.ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static void WriteResource(SavePayload payload, ResourceKind kind, uint value)
    {
        switch (kind)
        {
            case ResourceKind.Goods: payload.resGoods = value; break;
            case ResourceKind.Material: payload.resMaterial = value; break;
            case ResourceKind.Blueprint: payload.resBlueprint = value; break;
            case ResourceKind.Money: payload.resMoney = value; break;
            case ResourceKind.Wood: payload.resWood = value; break;
            case ResourceKind.IronOre: payload.resIronOre = value; break;
            case ResourceKind.Stone: payload.resStone = value; break;
            case ResourceKind.RawSalt: payload.resRawSalt = value; break;
            default: throw new System.ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public uint ResourceRoom(ResourceKind kind)
    {
        uint have = GetResource(kind);
        uint capacity = VillageService.Instance.ResourceCapacity;
        return have >= capacity ? 0 : capacity - have;
    }

    public uint AddResource(ResourceKind kind, uint amount)
    {
        uint result = GetResource(kind) + System.Math.Min(amount, ResourceRoom(kind));
        WriteResource(MetaProgressService.Instance.Payload, kind, result);
        return result;
    }

    public bool TryCreditResource(ResourceKind kind, uint amount, out string reason)
    {
        reason = null;
        if (amount > ResourceRoom(kind))
        {
            reason = ResourceDisplayName(kind) + "的仓库容量不足";
            return false;
        }
        AddResource(kind, amount);
        return true;
    }

    public bool SpendResource(ResourceKind kind, uint amount, out string reason)
    {
        uint have = GetResource(kind);
        reason = null;
        if (have < amount)
        {
            reason = $"资源不足（有 {have}，需要 {amount}）";
            return false;
        }
        WriteResource(MetaProgressService.Instance.Payload, kind, have - amount);
        return true;
    }

    // 旧商品编号继续作为交易、产出和奖励的稳定键，入库统一走资源账本。
    public static bool TryGetResourceCommodity(uint id, out ResourceKind resource)
    {
        if (id >= 9101 && id <= 9104)
        {
            resource = (ResourceKind)(id - 9101 + (uint)ResourceKind.Wood);
            return true;
        }
        resource = default;
        return false;
    }

    public static void MigrateLegacyResourceItems(SavePayload payload)
    {
        var items = payload.items;
        if (items == null) return;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item == null || !TryGetResourceCommodity(item.itemId, out var kind)) continue;
            uint balance = ReadResource(payload, kind);
            uint migrated = System.Math.Min(item.count, uint.MaxValue - balance);
            WriteResource(payload, kind, balance + migrated);
            item.count -= migrated;
            if (item.count == 0) items.RemoveAt(i);
        }
    }

    public static string ResourceDisplayName(ResourceKind kind)
    {
        switch (kind)
        {
            case ResourceKind.Goods: return "物资";
            case ResourceKind.Material: return "材料";
            case ResourceKind.Blueprint: return "图纸";
            case ResourceKind.Money: return "钱财";
            case ResourceKind.Wood: return "木材";
            case ResourceKind.IronOre: return "铁矿石";
            case ResourceKind.Stone: return "石料";
            case ResourceKind.RawSalt: return "原盐";
            default: throw new System.ArgumentOutOfRangeException(nameof(kind));
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
            ItemTable_ItemConfig def = GetDefinition(stacks[i].itemId);
            sb.Append("\n  itemId=").Append(stacks[i].itemId)
              .Append(" ").Append(def == null ? "(表缺失)" : def.NameKey)
              .Append(" ×").Append(stacks[i].count).Append("/").Append(def == null ? "?" : def.StackLimit.ToString());
        }
        return sb.ToString();
    }
}
