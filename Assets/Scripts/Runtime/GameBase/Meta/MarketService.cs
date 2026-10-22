using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 市场（M5 / W5.3）—— **资源 ↔ 物品兑换最小版**的服务层。
///
/// ══════════════ 范围（策划 `18_市场.md`）══════════════
/// * 只做「买 / 卖」，**每次 1 个**；货币**只认钱财**（`ResourceKind.Money` → `resMoney`）；
/// * 价格**全部来自 `Item` 表的 `BuyPrice` / `SellPrice`**（`0` = 不可交易），本类**不写死任何价格**；
/// * **不新建表**、不做限购/刷新/时代调价/运送护送/批量（N1~N6）。
///
/// ══════════════ 唯一的原子性要求 ══════════════
/// **买入失败必须退还钱财**（B3 / E2 / E3）：先 `SpendResource` 再 `AddItem`，
/// `AddItem` 返回 false（格位满 / 叠加满 / 禁用 / 表缺失）时**立刻** `AddResource` 退回去，
/// 并断言"持有数确实没涨"，避免出现"扣了钱没拿到货"。
///
/// 库存与资源本身仍由 `InventoryService`（唯一读写入口）负责，本类只做**市场规则**。
/// </summary>
public class MarketService : SingletonBase<MarketService>
{
    /// <summary>日志前缀（GM 打印 / 验收日志都靠它定位）。</summary>
    public const string Tag = "[市场]";

    // ══════════════════════ 价格 / 名称 / 持有 ══════════════════════

    /// <summary>买价；物品不存在或表里为 0 → `0`（= 不可买）。</summary>
    public uint GetBuyPrice(uint itemId)
    {
        ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        return def == null ? 0u : def.BuyPrice;
    }

    /// <summary>卖价；物品不存在或表里为 0 → `0`（= 不可卖）。</summary>
    public uint GetSellPrice(uint itemId)
    {
        ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        return def == null ? 0u : def.SellPrice;
    }

    /// <summary>可交易 = 启用 且（买价 &gt; 0 或 卖价 &gt; 0）；两列都 0 不进市场列表（E5）。</summary>
    public bool IsTradable(uint itemId)
    {
        ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        if (def == null || !def.Enable) return false;
        return def.BuyPrice > 0u || def.SellPrice > 0u;
    }

    /// <summary>当前持有数（同一 itemId 的堆叠相加）。</summary>
    public uint GetCount(uint itemId)
    {
        List<ItemStackSave> stacks = InventoryService.Instance.GetStacks();
        uint total = 0u;
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i] == null || stacks[i].itemId != itemId) continue;
            total += stacks[i].count;
        }
        return total;
    }

    /// <summary>
    /// 面板、交易日志与 GM 价格表共享 Item.NameKey 对应的本地化名称。
    /// </summary>
    public string GetDisplayName(uint itemId)
    {
        return LocalizationSystem.GameplayText.ItemName(itemId);
    }

    /// <summary>类别 → 中文名（与 `InventoryPanel` 的占位色分组一致：1 药品 / 2 材料 / 3 装备 / 4 图纸 / 5 消耗）。</summary>
    public static string CategoryName(uint category)
    {
        switch (category)
        {
            case 1u: return "药品";
            case 2u: return "材料";
            case 3u: return "装备";
            case 4u: return "图纸";
            case 5u: return "消耗品";
            default: return "物品";
        }
    }

    // ══════════════════════ 买 ══════════════════════

    /// <summary>
    /// 买 1 个。失败时 `reason` 给出可直接展示的中文原因，且**钱财与库存都不变**。
    /// 顺序（§3.4 规则优先级）：价格可买 → 钱财足够 → 扣钱 → 入库 → 入库失败**退钱**。
    /// </summary>
    public bool TryBuy(uint itemId, out string reason)
    {
        reason = null;
        InventoryService inv = InventoryService.Instance;

        ItemConfig def = inv.GetDefinition(itemId);
        if (def == null)
        {
            reason = "道具表里没有 itemId=" + itemId;
            ChaosLog.Warn(LogChannel.Config, Tag + " 买入失败：" + reason);
            return false;
        }
        if (!def.Enable)
        {
            reason = "该物品已禁用（Enable=false）";
            ChaosLog.Warn(LogChannel.Config, Tag + " 买入失败：" + GetDisplayName(itemId) + " " + reason);
            return false;
        }

        uint price = def.BuyPrice;
        if (price == 0u)
        {
            reason = "该物品不可购买（BuyPrice=0）";
            ChaosLog.Warn(LogChannel.Config, Tag + " 买入失败：" + GetDisplayName(itemId) + " " + reason);
            return false;
        }

        string name = GetDisplayName(itemId);
        uint moneyBefore = inv.GetResource(InventoryService.ResourceKind.Money);
        if (moneyBefore < price)
        {
            reason = "钱财不足（有 " + moneyBefore + "，要 " + price + "）";
            ChaosLog.Warn(LogChannel.Config, Tag + " 买入失败：" + name + " ×1 " + reason + "，钱财不变（" + moneyBefore + "）");
            return false;
        }

        string beforeTrade = JsonUtility.ToJson(MetaProgressService.Instance.Payload);
        string spendReason;
        if (!inv.SpendResource(InventoryService.ResourceKind.Money, price, out spendReason))
        {
            reason = spendReason;
            ChaosLog.Warn(LogChannel.Config, Tag + " 买入失败：" + name + " 扣钱被拒：" + spendReason);
            return false;
        }
        uint moneyAfterSpend = inv.GetResource(InventoryService.ResourceKind.Money);
        uint countBefore = GetCount(itemId);

        string addReason;
        bool added = inv.AddItem(itemId, 1u, out addReason);
        uint countAfter = GetCount(itemId);

        if (!added || countAfter <= countBefore)
        {
            //★ B3 / E2 / E3：入库失败 → 立刻退款，绝不出现"扣了钱没拿到货"
            inv.AddResource(InventoryService.ResourceKind.Money, price);
            reason = DescribeAddFailure(itemId, inv);
            ChaosLog.Warn(LogChannel.Config, Tag + " 买入失败：" + name + " ×1 " + reason
                + "（入库原因：" + addReason + "）；已退还 " + price + " 钱财（钱财 " + moneyAfterSpend
                + " → " + inv.GetResource(InventoryService.ResourceKind.Money) + "），持有仍 " + countAfter);
            return false;
        }

        if (!SaveTrade(beforeTrade, out reason)) return false;
        ChaosLog.Info(LogChannel.Config, Tag + " 买入 " + name + "（itemId=" + itemId + "）×1：钱财 "
            + moneyBefore + " → " + inv.GetResource(InventoryService.ResourceKind.Money) + "（-" + price + "），持有 "
            + countBefore + " → " + countAfter + "，格位 " + inv.UsedSlots + "/" + inv.TotalSlots);
        return true;
    }

    /// <summary>把 `AddItem` 的原始原因（很长）翻译成面板/验收要的两句话（§9 E2 / E3）。</summary>
    private string DescribeAddFailure(uint itemId, InventoryService inv)
    {
        ItemConfig def = inv.GetDefinition(itemId);
        uint stackLimit = def == null ? 1u : (def.StackLimit == 0u ? 1u : def.StackLimit);
        if (GetCount(itemId) == 0u && inv.UsedSlots >= inv.TotalSlots)
            return "格位已满（" + inv.UsedSlots + "/" + inv.TotalSlots + "）";
        return "已达叠加上限（" + stackLimit + "）";
    }

    // ══════════════════════ 卖 ══════════════════════

    /// <summary>卖 1 个：价格可卖 → 持有 ≥1 → 出库成功 → 加钱财（顺序不可换，避免"钱到手货还在"）。</summary>
    public bool TrySell(uint itemId, out string reason)
    {
        reason = null;
        InventoryService inv = InventoryService.Instance;

        ItemConfig def = inv.GetDefinition(itemId);
        if (def == null)
        {
            reason = "道具表里没有 itemId=" + itemId;
            ChaosLog.Warn(LogChannel.Config, Tag + " 卖出失败：" + reason);
            return false;
        }
        if (!def.Enable)
        {
            reason = "该物品已禁用（Enable=false）";
            ChaosLog.Warn(LogChannel.Config, Tag + " 卖出失败：" + GetDisplayName(itemId) + " " + reason);
            return false;
        }

        uint price = def.SellPrice;
        if (price == 0u)
        {
            reason = "该物品不可出售（SellPrice=0）";
            ChaosLog.Warn(LogChannel.Config, Tag + " 卖出失败：" + GetDisplayName(itemId) + " " + reason);
            return false;
        }

        string name = GetDisplayName(itemId);
        uint have = GetCount(itemId);
        if (have == 0u)
        {
            reason = "没有可卖（持有 0）";
            ChaosLog.Warn(LogChannel.Config, Tag + " 卖出失败：" + name + " " + reason);
            return false;
        }

        uint moneyBefore = inv.GetResource(InventoryService.ResourceKind.Money);
        string beforeTrade = JsonUtility.ToJson(MetaProgressService.Instance.Payload);
        string removeReason;
        if (!inv.RemoveItem(itemId, 1u, out removeReason))
        {
            reason = removeReason;
            ChaosLog.Warn(LogChannel.Config, Tag + " 卖出失败：" + name + " 出库被拒：" + removeReason);
            return false;
        }

        uint moneyNow = inv.AddResource(InventoryService.ResourceKind.Money, price);
        if (!SaveTrade(beforeTrade, out reason)) return false;
        ChaosLog.Info(LogChannel.Config, Tag + " 卖出 " + name + "（itemId=" + itemId + "）×1：持有 "
            + have + " → " + GetCount(itemId) + "，钱财 " + moneyBefore + " → " + moneyNow + "（+" + price + "）");
        return true;
    }

    private static bool SaveTrade(string beforeTrade, out string reason)
    {
        if (MetaProgressService.Instance.SaveProgress(out reason)) return true;
        JsonUtility.FromJsonOverwrite(beforeTrade, MetaProgressService.Instance.Payload);
        return false;
    }

    // ══════════════════════ 打印（GM / 验收）══════════════════════

    /// <summary>
    /// 价格表：逐行「名称 买价 卖价 持有」，并顺带核对「卖价 = floor(买价 × 50%)」。
    /// 两列都为 0 的物品单独标注（不可交易、不进列表）。
    /// </summary>
    public string DumpPrices()
    {
        InventoryService inv = InventoryService.Instance;
        var sb = new System.Text.StringBuilder();

        sb.Append(Tag).Append(" 价格表（来源 Item.BuyPrice / Item.SellPrice；0=不可交易）｜钱财 ")
          .Append(inv.GetResource(InventoryService.ResourceKind.Money))
          .Append("｜格位 ").Append(inv.UsedSlots).Append("/").Append(inv.TotalSlots);

        List<ItemConfig> defs = inv.GetAllDefinitions();
        int tradable = 0;
        int halfPriceOk = 0;
        for (int i = 0; i < defs.Count; i++)
        {
            ItemConfig def = defs[i];
            if (def == null) continue;

            if (!def.Enable)
            {
                sb.Append("\n  itemId=").Append(def.Index).Append(" ").Append(GetDisplayName(def.Index))
                  .Append(" 已禁用（Enable=false）→ 不可交易");
                continue;
            }
            if (def.BuyPrice == 0u && def.SellPrice == 0u)
            {
                sb.Append("\n  itemId=").Append(def.Index).Append(" ").Append(GetDisplayName(def.Index))
                  .Append(" 买=0 卖=0 → 不可交易（不显示）");
                continue;
            }

            uint expected = def.BuyPrice / 2u;
            bool half = def.BuyPrice > 0u && def.SellPrice == expected;
            if (half) halfPriceOk++;
            tradable++;

            sb.Append("\n  itemId=").Append(def.Index).Append(" ").Append(GetDisplayName(def.Index))
              .Append(" 买价=").Append(def.BuyPrice)
              .Append(" 卖价=").Append(def.SellPrice)
              .Append(" 持有=").Append(GetCount(def.Index))
              .Append(" 半价=").Append(half ? "是" : "否");
        }

        sb.Append("\n  可交易 ").Append(tradable).Append(" 条；卖价=买价50% ").Append(halfPriceOk).Append("/").Append(tradable);
        return sb.ToString();
    }
}
