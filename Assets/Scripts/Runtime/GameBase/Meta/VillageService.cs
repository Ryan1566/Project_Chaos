using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 村庄（M1）：4 个建筑（BuildingId 2001~2004）× 最多 2 级。
///
/// ══════════════ 资源类型映射（**统一一份**，CostType 与产出类型共用）══════════════
/// `1 = 物资(resGoods)` / `2 = 材料(resMaterial)` / `3 = 图纸(resBlueprint)` / `4 = 钱财(resMoney)`
///
/// ══════════════ 状态机 ══════════════
/// 未建造 → 建造中(`completed=false`) → 已建成(`completed=true`) → 可升级(level<2) → 升级中 → 已建成
/// **同时只允许 1 条建造/升级队列**；**可取消，返还 50%（向下取整）**。
///
/// ══════════════ 计时口径（U2）══════════════
/// **只用运行中时间**：`BuildingSave.remainSeconds`（本轮**新增字段**，零迁移）逐帧递减；
/// 旧 `finishUtcTicks` 保留但**恒 0 不读**（同族人重伤字段的处理方式）。
/// 离线不产、不推进 —— 与"仅运行中计时"一致。
/// </summary>
public class VillageService : SingletonBase<VillageService>
{
    public const int MaxLevel = 2u == 0u ? 2 : 2;
    public const uint CancelRefundPercent = 50u;

    public enum BuildingState
    {
        NotBuilt,
        Building,
        Built,
        Upgradable,
        MaxLevel,
    }

    private List<BuildingConfig> _defs;
    private bool _loaded;

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        string path = GlobalPath.data_JsonPathToRead + "BuildingConfig";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[村庄] 找不到 " + path);
            _defs = new List<BuildingConfig>();
            return;
        }
        DataList<BuildingConfig> list = JsonUtility.FromJson<DataList<BuildingConfig>>(asset.text);
        _defs = (list == null || list.datas == null) ? new List<BuildingConfig>() : list.datas;
    }

    public void Reload() { _loaded = false; _defs = null; EnsureLoaded(); }

    /// <summary>资源类型码 → 资源种类（1 物资 / 2 材料 / 3 图纸 / 4 钱财）。</summary>
    public static InventoryService.ResourceKind ResourceKindOf(uint typeCode)
    {
        switch (typeCode)
        {
            case 2u: return InventoryService.ResourceKind.Material;
            case 3u: return InventoryService.ResourceKind.Blueprint;
            case 4u: return InventoryService.ResourceKind.Money;
            default: return InventoryService.ResourceKind.Goods;
        }
    }

    public List<BuildingConfig> GetAllDefs() { EnsureLoaded(); return _defs; }

    public BuildingConfig GetDef(uint buildingId, uint level)
    {
        EnsureLoaded();
        for (int i = 0; i < _defs.Count; i++)
            if (_defs[i] != null && _defs[i].BuildingId == buildingId && _defs[i].Level == level) return _defs[i];
        return null;
    }

    public List<BuildingSave> GetAll()
    {
        SavePayload p = MetaProgressService.Instance.Payload;
        if (p.buildings == null) p.buildings = new List<BuildingSave>();
        return p.buildings;
    }

    public BuildingSave Find(uint buildingId)
    {
        List<BuildingSave> all = GetAll();
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].buildingType == buildingId) return all[i];
        return null;
    }

    public bool IsQueueBusy
    {
        get
        {
            List<BuildingSave> all = GetAll();
            for (int i = 0; i < all.Count; i++) if (all[i] != null && !all[i].completed) return true;
            return false;
        }
    }

    public BuildingState GetState(uint buildingId)
    {
        BuildingSave save = Find(buildingId);
        if (save == null) return BuildingState.NotBuilt;
        if (!save.completed) return BuildingState.Building;
        if (save.level >= MaxLevel) return BuildingState.MaxLevel;
        return BuildingState.Upgradable;
    }

    /// <summary>开始建造（Lv1）；扣资源、占队列。</summary>
    public bool StartBuild(uint buildingId, out string reason)
    {
        reason = null;
        BuildingConfig lv1 = GetDef(buildingId, 1u);
        if (lv1 == null) { reason = "建筑 " + buildingId + " 在表里不存在"; return false; }
        if (Find(buildingId) != null) { reason = "建筑 " + buildingId + " 已经建过了"; return false; }
        if (IsQueueBusy) { reason = "已有建筑在建造/升级中（同时只能 1 条队列）"; return false; }

        InventoryService.ResourceKind kind = ResourceKindOf(lv1.CostResourceType);
        string spendReason;
        if (!InventoryService.Instance.SpendResource(kind, lv1.CostAmount, out spendReason))
        {
            reason = "建造资源不足：" + spendReason;
            return false;
        }

        uint nextId = 1;
        List<BuildingSave> all = GetAll();
        for (int i = 0; i < all.Count; i++) if (all[i] != null && all[i].id >= nextId) nextId = all[i].id + 1;

        all.Add(new BuildingSave
        {
            id = nextId, buildingType = buildingId, level = 1u, completed = false,
            remainSeconds = lv1.BuildSeconds, finishUtcTicks = 0L,
        });

        ChaosLog.Info(LogChannel.Config,
            "[村庄] 开始建造 " + buildingId + "（" + lv1.NameKey + "），扣 "
            + InventoryService.ResourceDisplayName(kind) + " " + lv1.CostAmount + "，耗时 " + lv1.BuildSeconds + "s");
        return true;
    }

    /// <summary>升级到下一级；扣资源、占队列。</summary>
    public bool StartUpgrade(uint buildingId, out string reason)
    {
        reason = null;
        BuildingSave save = Find(buildingId);
        if (save == null) { reason = "还没建造 " + buildingId; return false; }
        if (!save.completed) { reason = "建筑还在建造中"; return false; }
        if (save.level >= MaxLevel) { reason = "已经是最高级（MVP 只到 " + MaxLevel + " 级）"; return false; }
        if (IsQueueBusy) { reason = "已有建筑在建造/升级中（同时只能 1 条队列）"; return false; }

        uint targetLevel = save.level + 1u;
        BuildingConfig def = GetDef(buildingId, targetLevel);
        if (def == null) { reason = "表里没有 " + buildingId + " 的 Lv" + targetLevel; return false; }

        InventoryService.ResourceKind kind = ResourceKindOf(def.CostResourceType);
        string spendReason;
        if (!InventoryService.Instance.SpendResource(kind, def.CostAmount, out spendReason))
        {
            reason = "升级资源不足：" + spendReason;
            return false;
        }

        save.level = targetLevel;
        save.completed = false;
        save.remainSeconds = def.BuildSeconds;

        ChaosLog.Info(LogChannel.Config,
            "[村庄] 开始升级 " + buildingId + " → Lv" + targetLevel + "，扣 "
            + InventoryService.ResourceDisplayName(kind) + " " + def.CostAmount + "，耗时 " + def.BuildSeconds + "s");
        return true;
    }

    /// <summary>取消建造/升级，返还 50%（向下取整）。</summary>
    public bool CancelBuild(uint buildingId, out uint refunded, out string reason)
    {
        refunded = 0u;
        reason = null;
        BuildingSave save = Find(buildingId);
        if (save == null) { reason = "没有可取消的建造"; return false; }
        if (save.completed) { reason = "建筑已建成，无需取消"; return false; }

        BuildingConfig def = GetDef(buildingId, save.level);
        uint cost = def == null ? 0u : def.CostAmount;
        refunded = cost * CancelRefundPercent / 100u;   //向下取整

        if (def != null && refunded > 0u)
            InventoryService.Instance.AddResource(ResourceKindOf(def.CostResourceType), refunded);

        //如果取消的是 Lv1 建造 → 整个建筑移除；若是升级 → 回退到上一级
        if (save.level <= 1u) GetAll().Remove(save);
        else { save.level -= 1u; save.completed = true; save.remainSeconds = 0u; }

        ChaosLog.Info(LogChannel.Config,
            "[村庄] 取消建造 " + buildingId + "，返还 " + refunded + "（原消耗 " + cost + " 的 50%）");
        return true;
    }

    /// <summary>逐帧推进（**仅运行中时间**）；归零 → `completed = true`。</summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        List<BuildingSave> all = GetAll();
        for (int i = 0; i < all.Count; i++)
        {
            BuildingSave save = all[i];
            if (save == null || save.completed) continue;
            if (save.remainSeconds <= deltaTime)
            {
                save.remainSeconds = 0u;
                save.completed = true;
                ChaosLog.Success(LogChannel.Config, "[村庄] 建筑 " + save.buildingType + " Lv" + save.level + " 建造完成");
            }
            else save.remainSeconds -= (uint)Mathf.CeilToInt(deltaTime);
        }
    }

    /// <summary>收取产出（按一个周期结算；**全部收取、不设待收取上限**）。</summary>
    public bool Harvest(uint buildingId, out uint amount, out string reason)
    {
        amount = 0u;
        reason = null;
        BuildingSave save = Find(buildingId);
        if (save == null) { reason = "还没建造 " + buildingId; return false; }
        if (!save.completed) { reason = "建筑还在建造中"; return false; }

        BuildingConfig def = GetDef(buildingId, save.level);
        if (def == null) { reason = "表里没有 " + buildingId + " Lv" + save.level; return false; }

        int output = (int)def.OutputAmount;
        //家族树 EffectType=7（收获产量）只作用于农田 2001
        if (buildingId == 2001u) output += EffectAggregator.Instance.Snapshot.FarmOutputDelta;
        if (output <= 0) { reason = "产出为 0"; return false; }

        amount = (uint)output;
        InventoryService.ResourceKind kind = ResourceKindOf(def.OutputResourceType);
        uint now = InventoryService.Instance.AddResource(kind, amount);

        ChaosLog.Info(LogChannel.Config,
            "[村庄] 收取 " + buildingId + "（Lv" + save.level + "）→ "
            + InventoryService.ResourceDisplayName(kind) + " +" + amount + "，当前 " + now);
        return true;
    }

    public string DumpState()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[村庄] 队列=").Append(IsQueueBusy ? "忙" : "空闲");
        foreach (uint id in new uint[] { 2001u, 2002u, 2003u, 2004u })
        {
            BuildingSave save = Find(id);
            BuildingState state = GetState(id);
            sb.Append("\n  ").Append(id).Append(" ").Append(state);
            if (save != null) sb.Append(" Lv").Append(save.level).Append(" 剩余").Append(save.remainSeconds).Append("s");
        }
        return sb.ToString();
    }
}
