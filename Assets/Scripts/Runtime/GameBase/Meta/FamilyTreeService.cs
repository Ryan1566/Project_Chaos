using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 家族树（M3）：5 板块 × 3 节点 = 15 节点。
/// 点亮规则：**已点亮不可重复**、**前置必须已点亮**、**资源足够**；不做洗点。
/// 点亮后立刻触发一次效果重算（`EffectAggregator.MarkDirty()`）。
/// </summary>
public class FamilyTreeService : SingletonBase<FamilyTreeService>
{
    private List<FamilyTreeConfig> _defs;
    private bool _loaded;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        string path = GlobalPath.data_JsonPathToRead + "FamilyTreeConfig";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[家族树] 找不到 " + path);
            _defs = new List<FamilyTreeConfig>();
            return;
        }
        DataList<FamilyTreeConfig> list = JsonUtility.FromJson<DataList<FamilyTreeConfig>>(asset.text);
        _defs = (list == null || list.datas == null) ? new List<FamilyTreeConfig>() : list.datas;
    }

    public void Reload() { _loaded = false; _defs = null; EnsureLoaded(); }

    public List<FamilyTreeConfig> GetAll() { EnsureLoaded(); return _defs; }

    public FamilyTreeConfig GetDef(uint nodeId)
    {
        EnsureLoaded();
        for (int i = 0; i < _defs.Count; i++)
            if (_defs[i] != null && _defs[i].Index == nodeId) return _defs[i];
        return null;
    }

    public List<uint> Unlocked
    {
        get
        {
            SavePayload p = MetaProgressService.Instance.Payload;
            if (p.familyTreeNodes == null) p.familyTreeNodes = new List<uint>();
            return p.familyTreeNodes;
        }
    }

    public bool IsUnlocked(uint nodeId) { return Unlocked.Contains(nodeId); }

    public int UnlockedCount { get { return Unlocked.Count; } }

    /// <summary>能否点亮：启用 → 未点亮 → 前置已点亮 → 资源足够（08 §3.4 优先级）。</summary>
    public bool CanUnlock(uint nodeId, out string reason)
    {
        reason = null;
        FamilyTreeConfig def = GetDef(nodeId);
        if (def == null) { reason = "节点 " + nodeId + " 不存在"; return false; }
        if (!def.Enable) { reason = "节点 " + nodeId + " 已禁用"; return false; }
        if (IsUnlocked(nodeId)) { reason = "节点 " + nodeId + " 已经点亮过了"; return false; }
        if (def.PrereqNodeId != 0u && !IsUnlocked(def.PrereqNodeId))
        {
            reason = "前置节点 " + def.PrereqNodeId + " 还没点亮";
            return false;
        }
        uint have = InventoryService.Instance.GetResource(VillageService.ResourceKindOf(def.CostType));
        if (have < def.CostValue)
        {
            reason = "资源不足：需要 " + InventoryService.ResourceDisplayName(VillageService.ResourceKindOf(def.CostType))
                + " " + def.CostValue + "，当前 " + have;
            return false;
        }
        return true;
    }

    /// <summary>点亮节点：扣资源 → 写入存档 → 触发效果重算。</summary>
    public bool Unlock(uint nodeId, out string reason)
    { return MetaCommandRunner.Execute("FamilyTree.Unlock", (out string why) => UnlockCore(nodeId, out why), out reason); }

    private bool UnlockCore(uint nodeId, out string reason)
    {
        if (!CanUnlock(nodeId, out reason)) return false;

        FamilyTreeConfig def = GetDef(nodeId);
        InventoryService.ResourceKind kind = VillageService.ResourceKindOf(def.CostType);
        string spendReason;
        if (!InventoryService.Instance.SpendResource(kind, def.CostValue, out spendReason))
        {
            reason = "扣资源失败：" + spendReason;
            return false;
        }

        Unlocked.Add(nodeId);
        EffectAggregator.Instance.MarkDirty();
        MetaEffectSnapshot snap = EffectAggregator.Instance.Snapshot;

        ChaosLog.Success(LogChannel.Config,
            "[家族树] 点亮节点 " + nodeId + "（" + def.NameKey + "），消耗 " + InventoryService.ResourceDisplayName(kind) + " " + def.CostValue
            + "；当前 " + Unlocked.Count + "/15。" + snap.Dump());
        return true;
    }

    /// <summary>调试：清空已点亮节点并重算。</summary>
    public void ResetAll()
    {
        Unlocked.Clear();
        EffectAggregator.Instance.MarkDirty();
        ChaosLog.Info(LogChannel.Config, "[家族树] 已重置（全部节点熄灭）");
    }

    public string DumpState()
    {
        EnsureLoaded();
        var sb = new System.Text.StringBuilder();
        sb.Append("[家族树] 已点亮 ").Append(Unlocked.Count).Append("/").Append(_defs.Count).Append("：");
        for (int i = 0; i < _defs.Count; i++)
        {
            FamilyTreeConfig def = _defs[i];
            if (def == null) continue;
            sb.Append("\n  ").Append(def.Index)
              .Append(" 板块").Append(def.Board).Append("#").Append(def.OrderInBoard)
              .Append(" ").Append(IsUnlocked(def.Index) ? "✔已点亮" : "未点亮")
              .Append(" 消耗 ").Append(InventoryService.ResourceDisplayName(VillageService.ResourceKindOf(def.CostType)))
              .Append(" ").Append(def.CostValue);
        }
        return sb.ToString();
    }
}
