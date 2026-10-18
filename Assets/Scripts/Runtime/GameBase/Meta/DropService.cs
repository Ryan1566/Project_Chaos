using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 掉落服务（W3.2a）。
///
/// 口径（Lead 已裁决）：
/// * `Drop` 表按 `DropGroup` 分组，**组内按 `Weight` 加权抽 1 个**，抽出后按 `MinCount~MaxCount` 给数量；
/// * **不做保底**（每组必中 1 件）；**不做**按时代/深度调掉落；
/// * 随机**可复现**：由 `runSeed` + `killSequence` 派生，同一局同一击杀序列结果一致；
/// * 本轮只有普通敌人 → 绑 `DropGroup = 普通敌人(1)`；精英/boss（组 2）留常量备用。
/// </summary>
public class DropService : SingletonBase<DropService>
{
    /// <summary>普通敌人掉落组。</summary>
    public const uint GroupNormal = 1u;
    /// <summary>精英/boss 掉落组（本轮未用）。</summary>
    public const uint GroupElite = 2u;

    private List<DropConfig> _defs;
    private bool _loaded;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        string path = GlobalPath.data_JsonPathToRead + "DropConfig";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[掉落] 找不到 " + path);
            _defs = new List<DropConfig>();
            return;
        }
        DataList<DropConfig> list = JsonUtility.FromJson<DataList<DropConfig>>(asset.text);
        _defs = (list == null || list.datas == null) ? new List<DropConfig>() : list.datas;
    }

    public void Reload() { _loaded = false; _defs = null; EnsureLoaded(); }

    public List<DropConfig> GetAll() { EnsureLoaded(); return _defs; }

    /// <summary>
    /// **组内加权抽 1 件**。返回 false 表示该组没有可用行。
    /// 随机源：`new System.Random((int)(runSeed * 31 + killSeq))` → 同 seed + 同击杀序可复现。
    /// </summary>
    public bool RollForEnemy(uint dropGroup, uint runSeed, uint killSeq, out uint itemId, out uint count)
    {
        itemId = 0u;
        count = 0u;
        EnsureLoaded();

        //收集该组的可用行
        var candidates = new List<DropConfig>();
        uint totalWeight = 0u;
        for (int i = 0; i < _defs.Count; i++)
        {
            DropConfig def = _defs[i];
            if (def == null || !def.Enable) continue;
            if (def.DropGroup != dropGroup) continue;
            if (def.Weight == 0u) continue;
            candidates.Add(def);
            totalWeight += def.Weight;
        }

        if (candidates.Count == 0)
        {
            ChaosLog.Warn(LogChannel.Config, "[掉落] 掉落组 " + dropGroup + " 没有可用行（Enable/Weight 全被过滤）");
            return false;
        }

        var random = new System.Random((int)(runSeed * 31u + killSeq));
        uint roll = (uint)random.Next(0, (int)totalWeight);   //[0, totalWeight)

        uint accumulated = 0u;
        DropConfig picked = candidates[candidates.Count - 1];
        for (int i = 0; i < candidates.Count; i++)
        {
            accumulated += candidates[i].Weight;
            if (roll < accumulated) { picked = candidates[i]; break; }
        }

        itemId = picked.ItemId;
        uint minCount = picked.MinCount == 0u ? 1u : picked.MinCount;
        uint maxCount = picked.MaxCount < minCount ? minCount : picked.MaxCount;
        count = (uint)random.Next((int)minCount, (int)maxCount + 1);

        ChaosLog.Info(LogChannel.Config,
            "[掉落] 组 " + dropGroup + " 抽 1（总权重 " + totalWeight + "，roll=" + roll + "）→ 物品 " + itemId
            + " ×" + count + "（seed=" + runSeed + " killSeq=" + killSeq + "）");
        return true;
    }

    public string DumpState()
    {
        EnsureLoaded();
        var sb = new System.Text.StringBuilder();
        sb.Append("[掉落] 表中共 ").Append(_defs.Count).Append(" 行");
        for (int i = 0; i < _defs.Count; i++)
        {
            DropConfig def = _defs[i];
            if (def == null) continue;
            sb.Append("\n    ").Append(def.Index).Append(" 组").Append(def.DropGroup)
              .Append(" 物品").Append(def.ItemId).Append(" ×").Append(def.MinCount).Append("~").Append(def.MaxCount)
              .Append(" 权重").Append(def.Weight).Append(def.Enable ? "" : "（禁用）");
        }
        return sb.ToString();
    }
}
