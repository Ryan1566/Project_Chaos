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
/// 旧 `finishUtcTicks` 保留但**恒 0 不读**（同门客重伤字段的处理方式）。
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
    private SavePayload _timerPayload;
    private float _buildRemainder;
    public enum Command { Build, Upgrade, Cancel, Harvest }

    /// <summary>Formal commands commit resources and building state together, or restore the same payload on failure.</summary>
    public bool Execute(Command command, uint id, out uint amount, out string reason)
    {
        uint earned = 0;
        bool ok = MetaCommandRunner.Execute("Village." + command, (out string why) => {
            switch (command)
            {
                case Command.Build: return StartBuildCore(id, out why);
                case Command.Upgrade: return StartUpgradeCore(id, out why);
                case Command.Cancel: return CancelBuildCore(id, out earned, out why);
                case Command.Harvest: return HarvestCore(id, out earned, out why);
                default: why = "未知村庄命令"; return false;
            }
        }, out reason);
        amount = ok ? earned : 0; return ok;
    }

    public bool StartBuild(uint id, out string reason) => Execute(Command.Build, id, out _, out reason);
    public bool StartUpgrade(uint id, out string reason) => Execute(Command.Upgrade, id, out _, out reason);
    public bool CancelBuild(uint id, out uint amount, out string reason) => Execute(Command.Cancel, id, out amount, out reason);
    public bool Harvest(uint id, out uint amount, out string reason) => Execute(Command.Harvest, id, out amount, out reason);

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
    private bool StartBuildCore(uint buildingId, out string reason)
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
            remainSeconds = BuildDuration(lv1), finishUtcTicks = 0L,
        });

        ChaosLog.Info(LogChannel.Config,
            "[村庄] 开始建造 " + buildingId + "（" + lv1.NameKey + "），扣 "
            + InventoryService.ResourceDisplayName(kind) + " " + lv1.CostAmount + "，耗时 " + lv1.BuildSeconds + "s");
        return true;
    }

    /// <summary>升级到下一级；扣资源、占队列。</summary>
    private bool StartUpgradeCore(uint buildingId, out string reason)
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
        save.remainSeconds = BuildDuration(def);

        ChaosLog.Info(LogChannel.Config,
            "[村庄] 开始升级 " + buildingId + " → Lv" + targetLevel + "，扣 "
            + InventoryService.ResourceDisplayName(kind) + " " + def.CostAmount + "，耗时 " + def.BuildSeconds + "s");
        return true;
    }

    /// <summary>取消建造/升级，返还 50%（向下取整）。</summary>
    private bool CancelBuildCore(uint buildingId, out uint refunded, out string reason)
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
        if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        var payload = MetaProgressService.Instance.Payload;
        if (!ReferenceEquals(payload, _timerPayload)) { _timerPayload = payload; _buildRemainder = 0; }
        _buildRemainder += deltaTime;
        int seconds = Mathf.FloorToInt(_buildRemainder);
        if (seconds < 1) return;
        _buildRemainder -= seconds;
        deltaTime = seconds;
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

    /// <summary>Withdraw only accumulated, worked output. Repeated clicks never create a new cycle.</summary>
    private bool HarvestCore(uint buildingId, out uint amount, out string reason)
    {
        amount = 0u;
        reason = null;
        BuildingSave save = Find(buildingId);
        if (save == null) { reason = "还没建造 " + buildingId; return false; }
        if (!save.completed) { reason = "建筑还在建造中"; return false; }

        BuildingConfig def = GetDef(buildingId, save.level);
        if (def == null) { reason = "表里没有 " + buildingId + " Lv" + save.level; return false; }

        if (save.pendingOutput == 0) { reason = VillageText.Get("NoYield"); return false; }
        InventoryService.ResourceKind kind = ResourceKindOf(save.pendingResourceType == 0 ? def.OutputResourceType : save.pendingResourceType);
        uint capacity = uint.MaxValue - InventoryService.Instance.GetResource(kind);
        amount = System.Math.Min(capacity, save.pendingOutput);
        if (amount == 0) { reason = "资源已达到上限"; return false; }
        uint now = InventoryService.Instance.AddResource(kind, amount);
        save.pendingOutput -= amount;

        ChaosLog.Info(LogChannel.Config,
            "[村庄] 收取 " + buildingId + "（Lv" + save.level + "）→ "
            + InventoryService.ResourceDisplayName(kind) + " +" + amount + "，当前 " + now);
        return true;
    }

    public void TickProduction(uint buildingId, float deltaTime, object worker)
    {
        if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)
            || !VillageWorkforce.CanProduce(buildingId, worker)) return;
        var save = Find(buildingId);
        if (save == null || !save.completed) return;
        var def = GetDef(buildingId, save.level);
        if (def == null || !def.Enable || def.HarvestSeconds == 0) return;
        if (save.pendingOutput > 0 && save.pendingResourceType != def.OutputResourceType) return;
        double total = System.Math.Max(0, save.productionSeconds) + deltaTime;
        double cycles = System.Math.Floor(total / def.HarvestSeconds);
        save.productionSeconds = (float)(total % def.HarvestSeconds);
        if (cycles < 1) return;
        int bonus = buildingId == 2001u ? EffectAggregator.Instance.Snapshot.FarmOutputDelta : 0;
        double output = System.Math.Max(0, (double)def.OutputAmount + bonus) * cycles;
        save.pendingOutput = (uint)System.Math.Min(uint.MaxValue, save.pendingOutput + output);
        save.pendingResourceType = def.OutputResourceType;
        MetaRuntimeDriver.NotifyChanged();
    }
    private static uint BuildDuration(BuildingConfig definition)
    {
        int change = definition.BuildingId == 2001u ? EffectAggregator.Instance.Snapshot.FarmBuildSecondsDelta : 0;
        return (uint)System.Math.Max(1L, (long)definition.BuildSeconds + change);
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
