using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 扎营（W3.2i）。**本轮只做两个动作**：
/// * **完全治疗**：玩家 hp 回满，消耗 **1 瓶药**；
/// * **补充减员**：需 `MetaProgressService.CampRefillUnlocked`（家族树 303），消耗 **2 瓶药**（占位实现 + 日志）；
/// * **修甲本轮不做**（无护甲系统）。
///
/// 药品 = `Item.Category == 1`，从 **RunInventory** 扣（局内库存）。
/// 条件：**当前房间无存活敌人** + **每局 1 次**（`CampUsed`，随 `EnterScene`/`Clear` 重置）；
/// **三动作都不可用时禁止扎营且不耗次数**。
/// </summary>
public static class CampService
{
    /// <summary>本局是否已扎营过（每局 1 次）。</summary>
    public static bool CampUsed { get; private set; }

    /// <summary>治疗消耗药数。</summary>
    public const int HealCost = 1;
    /// <summary>补充减员消耗药数。</summary>
    public const int RefillCost = 2;

    private static List<ItemConfig> _itemDefs;
    private static bool _loaded;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        string path = GlobalPath.data_JsonPathToRead + "ItemConfig";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null) { ChaosLog.Warn(LogChannel.Config, "[扎营] 找不到 " + path); _itemDefs = new List<ItemConfig>(); return; }
        DataList<ItemConfig> list = JsonUtility.FromJson<DataList<ItemConfig>>(asset.text);
        _itemDefs = (list == null || list.datas == null) ? new List<ItemConfig>() : list.datas;
    }

    /// <summary>该物品是否"药"（`Item.Category == 1`）。</summary>
    public static bool IsMedicine(uint itemId)
    {
        EnsureLoaded();
        for (int i = 0; i < _itemDefs.Count; i++)
            if (_itemDefs[i] != null && _itemDefs[i].Index == itemId) return _itemDefs[i].Category == 1u;
        return false;
    }

    /// <summary>局内库存里的药总数。</summary>
    public static int MedicineCount
    {
        get
        {
            int total = 0;
            List<RunInventory.Stack> stacks = RunInventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (IsMedicine(stacks[i].itemId)) total += (int)stacks[i].count;
            return total;
        }
    }

    /// <summary>当前房间是否安全（无存活敌人）。近似：用玩家所在房间的矩形判断。</summary>
    public static bool IsInSafeRoom
    {
        get
        {
            GameObject player = PlayerSpawner.CurrentPlayer;
            if (player == null) return false;
            Rect room;
            if (!RunEncounter.TryGetRoomRect(player.transform.position, out room)) return false;

            foreach (EnemyPlaceholder enemy in Object.FindObjectsOfType<EnemyPlaceholder>())
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 p = enemy.transform.position;
                if (p.x >= room.xMin && p.x <= room.xMax && p.y >= room.yMin && p.y <= room.yMax) return false;
            }
            return true;
        }
    }

    /// <summary>是否允许"治疗"。</summary>
    public static bool CanHeal(out string reason)
    {
        reason = null;
        if (CampUsed) { reason = "本局已扎营过（每局 1 次）"; return false; }
        if (!IsInSafeRoom) { reason = "战斗中不能扎营（当前房间还有敌人）"; return false; }
        if (MedicineCount < HealCost) { reason = "药品不足：需要 " + HealCost + "，当前 " + MedicineCount; return false; }
        return true;
    }

    /// <summary>是否允许"补充减员"。</summary>
    public static bool CanRefill(out string reason)
    {
        reason = null;
        if (!MetaProgressService.Instance.CampRefillUnlocked) { reason = "未解锁：需点亮家族树 303（扎营补充减员）"; return false; }
        if (CampUsed) { reason = "本局已扎营过（每局 1 次）"; return false; }
        if (!IsInSafeRoom) { reason = "战斗中不能扎营（当前房间还有敌人）"; return false; }
        if (MedicineCount < RefillCost) { reason = "药品不足：需要 " + RefillCost + "，当前 " + MedicineCount; return false; }
        return true;
    }

    /// <summary>三动作都不可用 → 禁止扎营（**且不耗次数**）。</summary>
    public static bool AnyActionAvailable
    {
        get
        {
            string r;
            if (CanHeal(out r)) return true;
            if (CanRefill(out r)) return true;
            return false;
        }
    }

    /// <summary>完全治疗：hp 回满 + 扣 1 药 + 标记已扎营。</summary>
    public static bool TryHeal(out string reason)
    {
        if (!CanHeal(out reason)) return false;

        if (!SpendMedicine(HealCost, out reason)) return false;

        GameObject player = PlayerSpawner.CurrentPlayer;
        PlayerHealth health = player == null ? null : player.GetComponent<PlayerHealth>();
        if (health != null) health.RefillFullHp();

        CampUsed = true;
        ChaosLog.Success(LogChannel.Config, "[扎营] 完全治疗完成：hp 回满，消耗 " + HealCost + " 药；剩余药 " + MedicineCount);
        ChaosLog.Info(LogChannel.Config, "[扎营] 门客已满状态（门客暂无 HP，占位）");
        return true;
    }

    /// <summary>补充减员（占位实现）：扣 2 药 + 标记已扎营。真正的减员补充留 W3.3。</summary>
    public static bool TryRefill(out string reason)
    {
        if (!CanRefill(out reason)) return false;

        if (!SpendMedicine(RefillCost, out reason)) return false;

        CampUsed = true;
        ChaosLog.Success(LogChannel.Config, "[扎营] 补充减员（占位实现）：消耗 " + RefillCost + " 药；剩余药 " + MedicineCount);
        return true;
    }

    private static bool SpendMedicine(int cost, out string reason)
    {
        reason = null;
        int left = cost;
        List<RunInventory.Stack> stacks = RunInventory.Stacks;

        //逐个物品扣（同种可能分散在多个堆里）
        for (int i = 0; i < stacks.Count && left > 0; i++)
        {
            if (!IsMedicine(stacks[i].itemId)) continue;
            uint take = (uint)Mathf.Min(left, (int)stacks[i].count);
            string removeReason;
            if (RunInventory.RemoveItem(stacks[i].itemId, take, out removeReason)) left -= (int)take;
        }

        if (left > 0) { reason = "药品扣除失败：还差 " + left; return false; }
        return true;
    }

    /// <summary>进入新场景 / 撤离时重置（每局 1 次）。</summary>
    public static void ResetForNewRun()
    {
        if (CampUsed) ChaosLog.Info(LogChannel.Config, "[扎营] 新一局：扎营次数已重置");
        CampUsed = false;
    }

    public static string DumpState()
    {
        string healReason, refillReason;
        bool canHeal = CanHeal(out healReason);
        bool canRefill = CanRefill(out refillReason);
        var sb = new System.Text.StringBuilder();
        sb.Append("[扎营] CampUsed=").Append(CampUsed)
          .Append("  安全房间=").Append(IsInSafeRoom)
          .Append("  药=").Append(MedicineCount)
          .Append("  可治疗=").Append(canHeal).Append(canHeal ? "" : ("（" + healReason + "）"))
          .Append("  可补员=").Append(canRefill).Append(canRefill ? "" : ("（" + refillReason + "）"));
        return sb.ToString();
    }
}
