using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资源经营：一次建成、按秒产出、库存封顶；时代失权时停产停领取但保留资产。
/// 建造/领取走原子命令，Tick 只由运行驱动在未暂停的一秒分支调用。
/// </summary>
public class WorldResourceService : SingletonBase<WorldResourceService>
{
    public bool Build(uint locationId, out string reason)
    {
        return MetaCommandRunner.Execute("WorldResource.Build",
            (out string why) => BuildCore(locationId, out why), out reason);
    }

    private bool BuildCore(uint locationId, out string reason)
    {
        reason = null;
        SavePayload payload = GameSession.Instance.LoadedPayload;
        WorldLocationTable_LocationConfig loc = WorldLocationCatalog.Location(locationId);
        if (loc == null || loc.Kind != 4u) { reason = "此地不是资源经营点。"; return false; }
        if(WorldLocationCatalog.Action(loc.Kind)?.CanBuild!=true)
        {reason="当前配置不允许建造。";return false;}
        if (!WorldLocationService.Instance.IsFamilyResourceAllowed(locationId))
        { reason = "当前时代关系不允许在此经营。"; return false; }
        WorldResourceTable_ResourceFacilityConfig facility = WorldLocationCatalog.Facility(locationId);
        if (facility == null||WorldLocationCatalog.Output(facility.OutputItemId)==null
            ||InventoryService.Instance.GetDefinition(facility.OutputItemId)?.Enable!=true
            ||facility.CycleSeconds==0||facility.StockLimit==0||facility.OutputPerCycle==0)
        { reason = "资源设施或产物配置缺失、无效。"; return false; }
        WorldLocationSave state = WorldLocationService.FindState(payload, locationId);
        if (state != null && state.built) { reason = "设施已建成，无需重复建造。"; return false; }

        InventoryService inv = InventoryService.Instance;
        uint money = inv.GetResource(InventoryService.ResourceKind.Money);
        uint material = inv.GetResource(InventoryService.ResourceKind.Material);
        if (money < facility.BuildMoney)
        { reason = "钱财不足（有 " + money + "，需 " + facility.BuildMoney + "）。"; return false; }
        if (material < facility.BuildMaterial)
        { reason = "材料不足（有 " + material + "，需 " + facility.BuildMaterial + "）。"; return false; }
        string spend;
        if (!inv.SpendResource(InventoryService.ResourceKind.Money, facility.BuildMoney, out spend))
        { reason = spend; return false; }
        if (!inv.SpendResource(InventoryService.ResourceKind.Material, facility.BuildMaterial, out spend))
        { reason = spend; return false; }

        state = WorldLocationService.GetState(payload, locationId, true);
        state.built = true;
        state.productionSeconds = 0.0;
        state.pendingOutput = 0u;
        return true;
    }

    public bool Harvest(uint locationId, out uint amount, out string reason)
    {
        uint earned = 0u;
        bool ok = MetaCommandRunner.Execute("WorldResource.Harvest",
            (out string why) => HarvestCore(locationId, out earned, out why), out reason);
        amount = ok ? earned : 0u;
        return ok;
    }

    private bool HarvestCore(uint locationId, out uint earned, out string reason)
    {
        earned = 0u;
        reason = null;
        SavePayload payload = GameSession.Instance.LoadedPayload;
        WorldLocationTable_LocationConfig loc = WorldLocationCatalog.Location(locationId);
        if (loc == null || loc.Kind != 4u) { reason = "此地不是资源经营点。"; return false; }
        if(WorldLocationCatalog.Action(loc.Kind)?.CanHarvest!=true)
        {reason="当前配置不允许领取。";return false;}
        if (!WorldLocationService.Instance.IsFamilyResourceAllowed(locationId))
        { reason = "当前时代关系不允许领取产出。"; return false; }
        WorldResourceTable_ResourceFacilityConfig facility = WorldLocationCatalog.Facility(locationId);
        WorldLocationSave state = WorldLocationService.FindState(payload, locationId);
        if (facility == null || state == null || !state.built)
        { reason = "设施尚未建成。"; return false; }
        if (state.pendingOutput == 0u) { reason = "暂无可领取产出。"; return false; }

        ItemTable_ItemConfig def = InventoryService.Instance.GetDefinition(facility.OutputItemId);
        if (def == null || !def.Enable || WorldLocationCatalog.Output(facility.OutputItemId)==null
            || !InventoryService.TryGetResourceCommodity(facility.OutputItemId, out var resource))
        { reason = "产物物品缺失或已禁用（itemId=" + facility.OutputItemId + "）。"; return false; }
        InventoryService inv = InventoryService.Instance;
        uint addable = Math.Min(inv.ResourceRoom(resource), state.pendingOutput);
        if (addable == 0u) { reason = "该资源仓储已满，产出仍留在经营点。"; return false; }
        if (!inv.TryCreditResource(resource, addable, out reason)) return false;
        state.pendingOutput -= addable;
        earned = addable;
        return true;
    }

    /// <summary>
    /// 推进所有已建成资源设施。只在当前时代关系允许时累计；库存满即停产，
    /// 不保留溢出秒数；失权期间不累计也不没收已有库存。
    /// </summary>
    public void Tick(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0.0) return;
        if (MetaCommandRunner.IsExecuting) return;
        SavePayload payload = GameSession.Instance.LoadedPayload;
        if (payload == null) return;
        WorldLocationCatalog.EnsureLoaded();
        bool changed = false;
        for (int i = 0; i < WorldLocationCatalog.Facilities.Count; i++)
        {
            WorldResourceTable_ResourceFacilityConfig facility = WorldLocationCatalog.Facilities[i];
            if (facility == null || !facility.Enable) continue;
            if(WorldLocationCatalog.Output(facility.OutputItemId)==null
                ||InventoryService.Instance.GetDefinition(facility.OutputItemId)?.Enable!=true)continue;
            WorldLocationSave state = WorldLocationService.FindState(payload, facility.LocationId);
            if (state == null || !state.built) continue;
            if (!WorldLocationService.Instance.IsFamilyResourceAllowed(facility.LocationId)) continue;

            uint cap = facility.StockLimit == 0u ? 10u : facility.StockLimit;
            if (state.pendingOutput >= cap)
            {
                if (state.productionSeconds != 0.0) { state.productionSeconds = 0.0; changed = true; }
                continue;
            }
            uint cycle = facility.CycleSeconds == 0u ? 60u : facility.CycleSeconds;
            uint perCycle = facility.OutputPerCycle == 0u ? 1u : facility.OutputPerCycle;
            double previous=double.IsNaN(state.productionSeconds)||double.IsInfinity(state.productionSeconds)?0:Math.Max(0,state.productionSeconds);
            double total = previous + seconds;
            double cycles = Math.Floor(total / cycle);
            double produced = cycles * perCycle;
            if (produced < 1) { state.productionSeconds = total; changed = true; continue; }
            uint room = cap - state.pendingOutput;
            if (produced >= room)
            {
                state.pendingOutput = cap;
                state.productionSeconds = 0.0;
            }
            else
            {
                state.pendingOutput += (uint)produced;
                state.productionSeconds = total - cycles * cycle;
            }
            changed = true;
        }
        if (changed) MetaRuntimeDriver.NotifyChanged();
    }
}
