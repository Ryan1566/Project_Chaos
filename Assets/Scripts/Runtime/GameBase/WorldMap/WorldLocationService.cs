using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 世界地点服务：聚合配置、当前时代权限与存档状态；买卖/临时奖励全部走
/// MetaCommandRunner 原子提交。查询不产生生产、奖励或计数变化。
/// </summary>
public class WorldLocationService : SingletonBase<WorldLocationService>
{
    private sealed class FamilyExtension
    {
        public bool trade;
        public bool resource;
    }

    // 会话级家族扩展许可：只影响家族在此地的贸易/经营，不影响官员权限与历史关系。
    private readonly Dictionary<uint, FamilyExtension> _extensions = new Dictionary<uint, FamilyExtension>();
    int _extensionContext=-1;
    void EnsureContext()
    {int current=GameSession.Instance.ContextId;if(_extensionContext==current)return;_extensions.Clear();_extensionContext=current;}

    public void Reload()
    {
        WorldLocationCatalog.Reload();
        _extensions.Clear();
        _extensionContext=GameSession.Instance.ContextId;
    }

    /// <summary>会话级授予；不写存档，重载/换会话即失效，永不改变官员事务权限。</summary>
    public void GrantFamilyAccess(uint locationId, bool trade, bool resource)
    {
        EnsureContext();
        FamilyExtension ext;
        if (!_extensions.TryGetValue(locationId, out ext)) { ext = new FamilyExtension(); _extensions.Add(locationId, ext); }
        ext.trade = trade;
        ext.resource = resource;
    }

    public void RevokeFamilyAccess(uint locationId) { _extensions.Remove(locationId); }

    public List<WorldLocationSnapshot> GetLocations(uint era, WorldLocationIdentity identity)
    {
        EnsureContext();
        WorldLocationCatalog.EnsureLoaded();
        List<WorldLocationSnapshot> result = new List<WorldLocationSnapshot>();
        SavePayload payload = GameSession.Instance.LoadedPayload;
        for (int i = 0; i < WorldLocationCatalog.Eras.Count; i++)
        {
            WorldLocationTable_LocationEraConfig row = WorldLocationCatalog.Eras[i];
            if (row == null || row.Era != era || !row.Visible || !row.Enable) continue;
            WorldLocationTable_LocationConfig loc = WorldLocationCatalog.Location(row.LocationId);
            if (loc == null) continue;
            if(loc.Kind==5&&!CheckTemporary(WorldLocationCatalog.TemporaryConfig(loc.LocationId),payload,out _))continue;
            result.Add(BuildSnapshot(row, loc, identity, payload));
        }
        return result;
    }

    public WorldLocationSnapshot GetLocation(uint era, uint id, WorldLocationIdentity identity)
    {
        EnsureContext();
        WorldLocationCatalog.EnsureLoaded();
        WorldLocationTable_LocationEraConfig row = WorldLocationCatalog.Era(era, id);
        if (row == null || !row.Visible || !row.Enable) return null;
        WorldLocationTable_LocationConfig loc = WorldLocationCatalog.Location(id);
        if (loc == null) return null;
        if(loc.Kind==5&&!CheckTemporary(WorldLocationCatalog.TemporaryConfig(id),GameSession.Instance.LoadedPayload,out _))return null;
        return BuildSnapshot(row, loc, identity, GameSession.Instance.LoadedPayload);
    }

    /// <summary>生成供主脑地图合并的临时点位；不改既有 WorldMapService 点位与疆域。</summary>
    public List<WorldMapPoint> BuildMapPoints(uint era, WorldLocationIdentity identity)
    {
        List<WorldMapPoint> points = new List<WorldMapPoint>();
        List<WorldLocationSnapshot> snaps = GetLocations(era, identity);
        for (int i = 0; i < snaps.Count; i++)
        {
            WorldLocationSnapshot snap = snaps[i];
            WorldLocationTable_LocationConfig loc = WorldLocationCatalog.Location(snap.Id);
            if (loc == null) continue;
            points.Add(new WorldMapPoint
            {
                index = snap.Id,
                mapId = loc.MapId,
                kind = snap.Kind == WorldLocationKind.Capital || snap.Kind == WorldLocationKind.Trade || snap.Kind == WorldLocationKind.Tribe ? 1u : 2u,
                type = (uint)snap.Kind,
                refId = snap.Id,
                position = snap.Position,
                scale = 1f,
                layer = LayerOf(snap.Kind),
            });
        }
        return points;
    }

    private static uint LayerOf(WorldLocationKind kind)
    {
        switch (kind)
        {
            case WorldLocationKind.Capital: return 40u;
            case WorldLocationKind.Trade: return 30u;
            case WorldLocationKind.Resource: return 20u;
            case WorldLocationKind.Temporary: return 10u;
            default: return 5u;
        }
    }

    private WorldLocationSnapshot BuildSnapshot(WorldLocationTable_LocationEraConfig row,
        WorldLocationTable_LocationConfig loc, WorldLocationIdentity identity, SavePayload payload)
    {
        WorldLocationTable_LocationRelationConfig relation = WorldLocationCatalog.Relation(row.RelationId);
        WorldLocationTable_LocationActionConfig action = WorldLocationCatalog.Action(loc.Kind);
        WorldLocationTable_LocationTextConfig text = WorldLocationCatalog.Text(loc.LocationId);
        WorldResourceTable_ResourceFacilityConfig facility = WorldLocationCatalog.Facility(loc.LocationId);
        WorldLocationSave state = FindState(payload, loc.LocationId);
        WorldLocationKind kind = (WorldLocationKind)loc.Kind;

        FamilyExtension ext;
        bool extended = _extensions.TryGetValue(loc.LocationId, out ext) && ext != null;
        bool relationTrade = relation != null && relation.CanTrade && relation.BuyPercent > 0;
        bool familyTrade = relationTrade || (extended && ext.trade);
        bool familyResource = relationTrade || (extended && ext.resource);
        int buyPercent = PercentFor(loc.LocationId, relation);

        bool canTrade = identity == WorldLocationIdentity.Family && action != null && action.CanTrade
            && (kind == WorldLocationKind.Trade || kind == WorldLocationKind.Tribe) && familyTrade
            && WorldLocationCatalog.Market(loc.MarketId)!=null;
        bool configuredFacility=facility!=null&&facility.CycleSeconds>0&&facility.OutputPerCycle>0&&facility.StockLimit>0
            &&WorldLocationCatalog.Output(facility.OutputItemId)!=null;
        bool canBuild = identity == WorldLocationIdentity.Family && configuredFacility && action != null && action.CanBuild
            && kind == WorldLocationKind.Resource && familyResource && (state == null || !state.built);
        bool canHarvest = identity == WorldLocationIdentity.Family && configuredFacility && action != null && action.CanHarvest
            && kind == WorldLocationKind.Resource && familyResource && state != null && state.built&&state.pendingOutput>0;
        bool canOpenOffice = identity == WorldLocationIdentity.Official && action != null
            && action.CanOpenOffice && relation != null && relation.CanEnterOffice;

        bool tempOk = false;
        string tempReason = null;
        WorldLocationTable_LocationTemporaryConfig temp = null;
        if (kind == WorldLocationKind.Temporary)
        {
            temp = WorldLocationCatalog.TemporaryConfig(loc.LocationId);
            tempOk = identity == WorldLocationIdentity.Family && action != null && action.CanResolve
                && CheckTemporary(temp, payload, out tempReason);
        }
        bool canResolve = kind == WorldLocationKind.Temporary && tempOk;

        WorldLocationSnapshot snap = new WorldLocationSnapshot
        {
            Id = loc.LocationId,
            Era = row.Era,
            FactionId = row.FactionId,
            Name = row.Name,
            FactionName = row.FactionName,
            RelationName = relation == null ? "未知" : relation.RelationName,
            Kind = kind,
            Position = WorldMapService.Instance.Points(loc.MapId).Find(p=>p.refId==loc.LocationId)?.position??new Vector2(loc.X, loc.Y),
            Description = text == null ? (row.Name ?? "") : text.Description,
            ConditionText = text == null ? "" : text.ConditionText,
            IconKey = kind==WorldLocationKind.Resource
                ?WorldLocationCatalog.Output(facility?.OutputItemId??0)?.IconKey??"iron_mine"
                :kind==WorldLocationKind.Temporary?(temp!=null&&temp.Repeatable?"rumor":"easter_egg"):IconKey(kind),
            Built = state != null && state.built,
            PendingOutput = state != null && state.built ? state.pendingOutput : 0,
            ProductionSeconds = state == null ? 0.0 : state.productionSeconds,
            OutputItemId = facility == null ? 0u : facility.OutputItemId,
            BuyPercent = buyPercent,
            CanTrade = canTrade,
            CanBuild = canBuild,
            CanHarvest = canHarvest,
            CanResolve = canResolve,
            CanOpenOffice = canOpenOffice,
        };
        snap.LockReason = BuildLockReason(snap, identity, relation, temp, tempOk, tempReason, text);
        if(kind==WorldLocationKind.Resource&&configuredFacility)
        {
            var output=WorldLocationCatalog.Output(facility.OutputItemId);
            snap.Description+="\n\n设施："+facility.FacilityName+"\n建造：钱财 "+facility.BuildMoney+"、材料 "+facility.BuildMaterial
                +"\n产出："+output.Name+" ×"+facility.OutputPerCycle+"／"+facility.CycleSeconds+"秒　暂存上限 "+facility.StockLimit;
            if(state!=null&&state.built&&familyResource&&state.pendingOutput==0)snap.LockReason="设施运行中，尚无可领取产物。";
        }
        return snap;
    }

    private static string IconKey(WorldLocationKind kind)
    {
        switch (kind)
        {
            case WorldLocationKind.Capital: return "capital";
            case WorldLocationKind.Trade: return "market_town";
            case WorldLocationKind.Tribe: return "tribe";
            case WorldLocationKind.Resource: return "resource";
            case WorldLocationKind.Temporary: return "temporary";
            default: return "location";
        }
    }

    private static string BuildLockReason(WorldLocationSnapshot snap, WorldLocationIdentity identity,
        WorldLocationTable_LocationRelationConfig relation, WorldLocationTable_LocationTemporaryConfig temp,
        bool tempOk, string tempReason, WorldLocationTable_LocationTextConfig text)
    {
        if (identity == WorldLocationIdentity.Official)
        {
            if (snap.CanOpenOffice) return null;
            if (relation == null) return "此地的势力关系未配置，官员不能进入。";
            if (!relation.CanEnterOffice) return "此地关系为" + relation.RelationName + "，官员不能进入事务。";
            return "此地没有可办理的官员事务。";
        }
        if (snap.Kind == WorldLocationKind.Temporary)
        {
            if (tempOk) return null;
            if (temp == null) return "临时事件配置缺失。";
            if (!string.IsNullOrEmpty(tempReason)) return tempReason;
            return text == null ? "临时事件条件未满足。" : ("条件未满足：" + text.ConditionText);
        }
        if (snap.CanTrade || snap.CanBuild || snap.CanHarvest) return null;
        if (relation != null && !relation.CanTrade)
            return "此地关系为" + relation.RelationName + "，暂不可贸易或经营（仅可查看）。";
        return "此地仅可查看，没有可执行的家族操作。";
    }

    /// <summary>当前存档时代的家族交易；只按当前存档判断，调用方传时代不能绕过。</summary>
    public bool TryBuy(uint locationId, uint itemId, out string reason)
    {
        return MetaCommandRunner.Execute("WorldLocation.Buy", (out string why) => BuyCore(locationId, itemId, out why), out reason);
    }

    private bool BuyCore(uint locationId, uint itemId, out string reason)
    {
        reason = null;
        SavePayload payload = GameSession.Instance.LoadedPayload;
        WorldLocationTable_LocationConfig loc;
        WorldLocationTable_LocationEraConfig eraRow;
        WorldLocationTable_LocationRelationConfig relation;
        if (!ResolveTrade(locationId, payload, out loc, out eraRow, out relation)) { reason = "此地当前不可贸易。"; return false; }
        if (!WorldLocationCatalog.IsOffer(loc.MarketId, itemId)) { reason = "该市场不出售此物品。"; return false; }

        ItemTable_ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        if (def == null || !def.Enable) { reason = "物品不存在或已禁用。"; return false; }
        uint price = BuyPrice(def, locationId, relation);
        if (price == 0u) { reason = "该物品当前不可购买。"; return false; }
        InventoryService inv = InventoryService.Instance;
        uint money = inv.GetResource(InventoryService.ResourceKind.Money);
        if (money < price) { reason = "钱财不足（有 " + money + "，需 " + price + "）。"; return false; }
        if (RunInventory.IsImportant(itemId) && RunBagProgression.Owns(itemId)) { reason = "已永久拥有该重要道具。"; return false; }
        if (!inv.CanReceive(itemId, 1u, out reason)) return false;

        string spendReason;
        if (!inv.SpendResource(InventoryService.ResourceKind.Money, price, out spendReason))
        { reason = spendReason; return false; }
        string addReason;
        if (!inv.AddItem(itemId, 1u, out addReason))
        {
            reason = "入库失败：" + addReason;
            return false;
        }
        return true;
    }

    public bool TrySell(uint locationId, uint itemId, out string reason)
    {
        return MetaCommandRunner.Execute("WorldLocation.Sell", (out string why) => SellCore(locationId, itemId, out why), out reason);
    }

    private bool SellCore(uint locationId, uint itemId, out string reason)
    {
        reason = null;
        SavePayload payload = GameSession.Instance.LoadedPayload;
        WorldLocationTable_LocationConfig loc;
        WorldLocationTable_LocationEraConfig eraRow;
        WorldLocationTable_LocationRelationConfig relation;
        if (!ResolveTrade(locationId, payload, out loc, out eraRow, out relation)) { reason = "此地当前不可贸易。"; return false; }
        if (!WorldLocationCatalog.IsOffer(loc.MarketId, itemId)) { reason = "该市场不收购此物品。"; return false; }
        ItemTable_ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        if (def == null || !def.Enable || def.SellPrice == 0u) { reason = "该物品当前不可出售。"; return false; }
        if (MarketService.Instance.GetCount(itemId) == 0u) { reason = "没有可出售的该物品。"; return false; }
        if(RunInventory.IsImportant(itemId)){reason="永久归属的重要道具不可出售。";return false;}
        if(InventoryService.Instance.ResourceRoom(InventoryService.ResourceKind.Money)<def.SellPrice)
        {reason="钱财已达上限，无法完整收款。";return false;}

        string removeReason;
        if (!InventoryService.Instance.RemoveItem(itemId, 1u, out removeReason))
        { reason = removeReason; return false; }
        return InventoryService.Instance.TryCreditResource(InventoryService.ResourceKind.Money, def.SellPrice, out reason);
    }

    /// <summary>临时点处置：奖励入库与完成/冷却记录在同一命令内提交；满仓失败不标完成。</summary>
    public bool ResolveTemporary(uint locationId, out string reason)
    {
        return MetaCommandRunner.Execute("WorldLocation.ResolveTemporary",
            (out string why) => ResolveCore(locationId, out why), out reason);
    }

    private bool ResolveCore(uint locationId, out string reason)
    {
        reason = null;
        SavePayload payload = GameSession.Instance.LoadedPayload;
        WorldLocationTable_LocationConfig loc = WorldLocationCatalog.Location(locationId);
        WorldLocationTable_LocationEraConfig eraRow = WorldLocationCatalog.Era(CurrentEra(payload), locationId);
        if (loc == null || loc.Kind != 5u || eraRow == null || !eraRow.Visible)
        { reason = "此地当前不在可处置范围。"; return false; }
        WorldLocationTable_LocationActionConfig action = WorldLocationCatalog.Action(loc.Kind);
        if (action == null || !action.CanResolve)
        { reason = "此地当前不可处置。"; return false; }
        WorldLocationTable_LocationTemporaryConfig temp = WorldLocationCatalog.TemporaryConfig(locationId);
        if (!CheckTemporary(temp, payload, out reason)) return false;
        if (temp.RewardItemId != 0u)
        {
            ItemTable_ItemConfig def = InventoryService.Instance.GetDefinition(temp.RewardItemId);
            if (def == null || !def.Enable) { reason = "奖励物品缺失或已禁用（itemId=" + temp.RewardItemId + "）。"; return false; }
            uint count = temp.RewardCount == 0u ? 1u : temp.RewardCount;
            string addReason;
            if (!InventoryService.Instance.AddItem(temp.RewardItemId, count, out addReason))
            { reason = "奖励入库失败：" + addReason; return false; }
        }
        WorldLocationSave state = GetState(payload, locationId, true);
        state.completed = !temp.Repeatable;
        if(state.resolvedCount<uint.MaxValue)state.resolvedCount++;
        state.lastResolvedClock = payload.officeClockSeconds;
        return true;
    }

    public List<uint> GetMarketItems(uint locationId)
    {
        List<uint> result = new List<uint>();
        WorldLocationTable_LocationConfig loc;
        WorldLocationTable_LocationEraConfig eraRow;
        WorldLocationTable_LocationRelationConfig relation;
        if (!ResolveTrade(locationId, GameSession.Instance.LoadedPayload, out loc, out eraRow, out relation)) return result;
        List<MarketTable_MarketOfferConfig> offers = WorldLocationCatalog.Offers(loc.MarketId);
        if (offers == null) return result;
        for (int i = 0; i < offers.Count; i++)
        {
            uint id = offers[i].ItemId;
            if (MarketService.Instance.IsTradable(id)) result.Add(id);
        }
        return result;
    }

    public uint GetBuyPrice(uint locationId, uint itemId)
    {
        WorldLocationTable_LocationConfig loc;
        WorldLocationTable_LocationEraConfig eraRow;
        WorldLocationTable_LocationRelationConfig relation;
        if (!ResolveTrade(locationId, GameSession.Instance.LoadedPayload, out loc, out eraRow, out relation)) return 0u;
        if (!WorldLocationCatalog.IsOffer(loc.MarketId, itemId)) return 0u;
        ItemTable_ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        return def == null ? 0u : BuyPrice(def, locationId, relation);
    }

    public uint GetSellPrice(uint locationId, uint itemId)
    {
        WorldLocationTable_LocationConfig loc;
        WorldLocationTable_LocationEraConfig eraRow;
        WorldLocationTable_LocationRelationConfig relation;
        if (!ResolveTrade(locationId, GameSession.Instance.LoadedPayload, out loc, out eraRow, out relation)) return 0u;
        if (!WorldLocationCatalog.IsOffer(loc.MarketId, itemId)) return 0u;
        ItemTable_ItemConfig def = InventoryService.Instance.GetDefinition(itemId);
        return def == null ? 0u : def.SellPrice;
    }

    private uint BuyPrice(ItemTable_ItemConfig def, uint locationId, WorldLocationTable_LocationRelationConfig relation)
    {
        if (def == null || def.BuyPrice == 0u) return 0u;
        int percent = PercentFor(locationId, relation);
        if (percent <= 0) return 0u;
        double exact=Math.Ceiling(def.BuyPrice*(double)percent/100.0);
        if(exact>uint.MaxValue)return 0;
        uint price = (uint)exact;
        return price == 0u ? 1u : price;
    }

    /// <summary>关系价格；会话级家族贸易扩展在敌对 0 价格时按中立 100% 计算，不改历史关系。</summary>
    private int PercentFor(uint locationId, WorldLocationTable_LocationRelationConfig relation)
    {
        int percent = relation == null ? 0 : relation.BuyPercent;
        FamilyExtension ext;
        if (percent <= 0 && _extensions.TryGetValue(locationId, out ext) && ext != null && ext.trade)
            percent = 100;
        return percent;
    }

    private bool ResolveTrade(uint locationId, SavePayload payload, out WorldLocationTable_LocationConfig loc,
        out WorldLocationTable_LocationEraConfig eraRow, out WorldLocationTable_LocationRelationConfig relation)
    {
        EnsureContext();
        loc = null; eraRow = null; relation = null;
        WorldLocationCatalog.EnsureLoaded();
        loc = WorldLocationCatalog.Location(locationId);
        if (loc == null || (loc.Kind != 2u && loc.Kind != 3u)) return false;
        // 配置开关在领域层重新核验，不能只靠界面隐藏按钮。
        var action=WorldLocationCatalog.Action(loc.Kind);
        if(action==null||!action.CanTrade||WorldLocationCatalog.Market(loc.MarketId)==null)return false;
        eraRow = WorldLocationCatalog.Era(CurrentEra(payload), locationId);
        if (eraRow == null || !eraRow.Visible) return false;
        relation = WorldLocationCatalog.Relation(eraRow.RelationId);
        if (relation == null) return false;
        FamilyExtension ext;
        bool extended = _extensions.TryGetValue(locationId, out ext) && ext != null && ext.trade;
        return extended || (relation.CanTrade && relation.BuyPercent > 0);
    }

    private bool CheckTemporary(WorldLocationTable_LocationTemporaryConfig temp, SavePayload payload, out string reason)
    {
        reason = null;
        if (temp == null || !temp.Enable) { reason = "临时事件配置缺失。"; return false; }
        uint era = CurrentEra(payload);
        if (temp.TriggerEra != 0u && era != temp.TriggerEra)
        { reason = "需在第 " + temp.TriggerEra + " 时代才能处置。"; return false; }
        if (payload == null) { reason = "没有活动存档会话。"; return false; }
        if (payload.runs < temp.MinRuns)
        { reason = "累计进入次数不足（" + payload.runs + "/" + temp.MinRuns + "）。"; return false; }
        if (payload.clears < temp.MinClears)
        { reason = "累计通关数不足（" + payload.clears + "/" + temp.MinClears + "）。"; return false; }
        if(temp.RequiredTaskId!=0)
        {
            var task=payload.officeTasks?.Find(t=>t!=null&&t.taskId==temp.RequiredTaskId);
            if(task==null||(task.status!=OfficeTaskStatus.Completed&&task.completedCycles==0))
            {reason="需先办结关联事务。";return false;}
        }
        if (temp.PrerequisiteLocationId != 0u)
        {
            WorldLocationSave pre = FindState(payload, temp.PrerequisiteLocationId);
            if (pre == null||(!pre.completed&&pre.resolvedCount==0))
            { reason = "需先完成前置地点。"; return false; }
        }
        WorldLocationSave state = FindState(payload, temp.LocationId);
        if (state != null && state.completed) { reason = "该临时事件已完成。"; return false; }
        uint cooldown=Math.Max(1u,temp.CooldownSeconds);
        if (temp.Repeatable && state != null
            && payload.officeClockSeconds < state.lastResolvedClock + cooldown)
        {
            double remain = state.lastResolvedClock + cooldown - payload.officeClockSeconds;
            reason = "冷却中，还需 " + Math.Ceiling(remain) + " 秒。";
            return false;
        }
        return true;
    }

    private static uint CurrentEra(SavePayload payload)
    {
        if (payload == null) return 1u;
        return (uint)Mathf.Clamp((int)payload.era, 1, 5);
    }

    /// <summary>家族资源经营许可：当前时代可见且关系允许，或会话级扩展许可；不影响官员。</summary>
    public bool IsFamilyResourceAllowed(uint locationId)
    {
        EnsureContext();
        WorldLocationCatalog.EnsureLoaded();
        WorldLocationTable_LocationConfig loc = WorldLocationCatalog.Location(locationId);
        if (loc == null || loc.Kind != 4u) return false;
        WorldLocationTable_LocationEraConfig row = WorldLocationCatalog.Era(CurrentEra(GameSession.Instance.LoadedPayload), locationId);
        if (row == null || !row.Visible) return false;
        WorldLocationTable_LocationRelationConfig relation = WorldLocationCatalog.Relation(row.RelationId);
        FamilyExtension ext;
        bool extended = _extensions.TryGetValue(locationId, out ext) && ext != null && ext.resource;
        return extended || (relation != null && relation.CanTrade && relation.BuyPercent > 0);
    }

    internal static WorldLocationSave FindState(SavePayload payload, uint locationId)
    {
        if (payload == null || payload.worldLocations == null) return null;
        for (int i = 0; i < payload.worldLocations.Count; i++)
            if (payload.worldLocations[i] != null && payload.worldLocations[i].locationId == locationId)
                return payload.worldLocations[i];
        return null;
    }

    internal static WorldLocationSave GetState(SavePayload payload, uint locationId, bool create)
    {
        WorldLocationSave state = FindState(payload, locationId);
        if (state != null || !create || payload == null) return state;
        if (payload.worldLocations == null) payload.worldLocations = new List<WorldLocationSave>();
        state = new WorldLocationSave { locationId = locationId };
        payload.worldLocations.Add(state);
        return state;
    }
}
