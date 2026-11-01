using System.Collections.Generic;
using System.Linq;

/// <summary>永久家族成长的解锁事务，图结构和效果有效后才允许扣费。</summary>
public class FamilyTreeService : SingletonBase<FamilyTreeService>
{
    private FamilyTreeCatalog _catalog;
    public FamilyTreeCatalog Catalog { get { EnsureLoaded(); return _catalog; } }
    public void EnsureLoaded() { if (_catalog == null) _catalog = new FamilyTreeCatalog(); }
    public void Reload() { _catalog = null; EnsureLoaded(); }
    public List<MetaSkillTreeTable_FamilyTreeConfig> GetAll() { return Catalog.Nodes; }
    public MetaSkillTreeTable_FamilyTreeConfig GetDef(uint id) { return Catalog.Node(id); }
    public List<uint> Unlocked
    {
        get
        {
            var payload = MetaProgressService.Instance.Payload;
            if (payload.familyTreeNodes == null) payload.familyTreeNodes = new List<uint>();
            return payload.familyTreeNodes;
        }
    }
    public bool IsUnlocked(uint id) { return Unlocked.Contains(id); }
    public int UnlockedCount { get { return Catalog.Nodes.Count(x => x.Enable && IsUnlocked(x.Index)); } }
    public bool CanUnlock(uint id, out string reason)
    {
        reason = null;
        try { Catalog.Validate(); }
        catch (System.Exception error) { reason = "家族树配置错误：" + error.Message; return false; }
        var def = GetDef(id);
        if (def == null || !def.Enable) { reason = "节点未开放"; return false; }
        if (IsUnlocked(id)) { reason = "已解锁"; return false; }
        if (def.PrereqNodeId != 0 && !IsUnlocked(def.PrereqNodeId))
        { reason = "需要先解锁「" + Catalog.Text(GetDef(def.PrereqNodeId).NameKey) + "」"; return false; }
        uint have = InventoryService.Instance.GetResource(VillageService.ResourceKindOf(def.CostType));
        if (have < def.CostValue)
        { reason = "资源不足：需要" + InventoryService.ResourceDisplayName(VillageService.ResourceKindOf(def.CostType)) + " " + def.CostValue + "，持有 " + have; return false; }
        return true;
    }
    public bool Unlock(uint id, out string reason)
    { return MetaCommandRunner.Execute("FamilyTree.Unlock", (out string why) => UnlockCore(id, out why), out reason); }
    private bool UnlockCore(uint id, out string reason)
    {
        if (!CanUnlock(id, out reason)) return false;
        var def = GetDef(id);
        if (!InventoryService.Instance.SpendResource(VillageService.ResourceKindOf(def.CostType), def.CostValue, out reason)) return false;
        Unlocked.Add(id); EffectAggregator.Instance.MarkDirty(); return true;
    }
    public void ResetAll() { Unlocked.Clear(); EffectAggregator.Instance.MarkDirty(); }
    public string DumpState()
    { return "[家族树] 已解锁 " + UnlockedCount + "/" + Catalog.Nodes.Count(x => x.Enable) + "：" + string.Join(",", Unlocked); }
}
