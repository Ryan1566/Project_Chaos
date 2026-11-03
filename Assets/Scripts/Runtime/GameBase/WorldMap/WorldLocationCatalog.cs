using System;
using System.Collections.Generic;
using UnityEngine;
using ChaosDebug;

/// <summary>
/// 世界地点配置目录：通过项目统一类型索引加载小模块表，全部查询为只读。
/// </summary>
public static class WorldLocationCatalog
{
    private static bool _loaded;
    public static readonly List<WorldLocationTable_LocationConfig> Locations = new List<WorldLocationTable_LocationConfig>();
    public static readonly List<WorldLocationTable_LocationEraConfig> Eras = new List<WorldLocationTable_LocationEraConfig>();
    public static readonly List<WorldLocationTable_LocationTemporaryConfig> Temporary = new List<WorldLocationTable_LocationTemporaryConfig>();
    public static readonly List<WorldResourceTable_ResourceFacilityConfig> Facilities = new List<WorldResourceTable_ResourceFacilityConfig>();

    private static readonly Dictionary<uint, WorldLocationTable_LocationConfig> LocById = new Dictionary<uint, WorldLocationTable_LocationConfig>();
    private static readonly Dictionary<ulong, WorldLocationTable_LocationEraConfig> EraByKey = new Dictionary<ulong, WorldLocationTable_LocationEraConfig>();
    private static readonly Dictionary<uint, WorldLocationTable_LocationActionConfig> ActionByKind = new Dictionary<uint, WorldLocationTable_LocationActionConfig>();
    private static readonly Dictionary<uint, WorldLocationTable_LocationRelationConfig> RelationById = new Dictionary<uint, WorldLocationTable_LocationRelationConfig>();
    private static readonly Dictionary<uint, WorldLocationTable_LocationTextConfig> TextById = new Dictionary<uint, WorldLocationTable_LocationTextConfig>();
    private static readonly Dictionary<uint, WorldLocationTable_LocationTemporaryConfig> TempById = new Dictionary<uint, WorldLocationTable_LocationTemporaryConfig>();
    private static readonly Dictionary<uint, WorldResourceTable_ResourceFacilityConfig> FacilityById = new Dictionary<uint, WorldResourceTable_ResourceFacilityConfig>();
    private static readonly Dictionary<uint, WorldResourceTable_ResourceOutputConfig> OutputById = new Dictionary<uint, WorldResourceTable_ResourceOutputConfig>();
    private static readonly Dictionary<uint, MarketTable_MarketConfig> MarketById = new Dictionary<uint, MarketTable_MarketConfig>();
    private static readonly Dictionary<uint, List<MarketTable_MarketOfferConfig>> OffersByMarket = new Dictionary<uint, List<MarketTable_MarketOfferConfig>>();

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        Locations.Clear(); Eras.Clear(); Temporary.Clear(); Facilities.Clear();
        LocById.Clear(); EraByKey.Clear(); ActionByKind.Clear(); RelationById.Clear();
        TextById.Clear(); TempById.Clear(); FacilityById.Clear(); OutputById.Clear(); MarketById.Clear(); OffersByMarket.Clear();

        Locations.AddRange(Load<WorldLocationTable_LocationConfig>("WorldLocationTable_Location"));
        Eras.AddRange(Load<WorldLocationTable_LocationEraConfig>("WorldLocationTable_LocationEra"));
        List<WorldLocationTable_LocationActionConfig> actions = Load<WorldLocationTable_LocationActionConfig>("WorldLocationTable_LocationAction");
        List<WorldLocationTable_LocationRelationConfig> relations = Load<WorldLocationTable_LocationRelationConfig>("WorldLocationTable_LocationRelation");
        List<WorldLocationTable_LocationTextConfig> texts = Load<WorldLocationTable_LocationTextConfig>("WorldLocationTable_LocationText");
        Temporary.AddRange(Load<WorldLocationTable_LocationTemporaryConfig>("WorldLocationTable_LocationTemporary"));
        Facilities.AddRange(Load<WorldResourceTable_ResourceFacilityConfig>("WorldResourceTable_ResourceFacility"));
        List<WorldResourceTable_ResourceOutputConfig> outputs = Load<WorldResourceTable_ResourceOutputConfig>("WorldResourceTable_ResourceOutput");
        List<MarketTable_MarketConfig> markets = Load<MarketTable_MarketConfig>("MarketTable_Market");
        List<MarketTable_MarketOfferConfig> offers = Load<MarketTable_MarketOfferConfig>("MarketTable_MarketOffer");

        for (int i = 0; i < Locations.Count; i++)
        {
            WorldLocationTable_LocationConfig row = Locations[i];
            if (row != null && row.Enable && !LocById.ContainsKey(row.LocationId)) LocById.Add(row.LocationId, row);
        }
        for (int i = 0; i < Eras.Count; i++)
        {
            WorldLocationTable_LocationEraConfig row = Eras[i];
            if (row == null || !row.Enable) continue;
            ulong key = Key(row.Era, row.LocationId);
            if (!EraByKey.ContainsKey(key)) EraByKey.Add(key, row);
        }
        for (int i = 0; i < actions.Count; i++)
            if (actions[i] != null && actions[i].Enable) ActionByKind[actions[i].Kind] = actions[i];
        for (int i = 0; i < relations.Count; i++)
            if (relations[i] != null && relations[i].Enable) RelationById[relations[i].RelationId] = relations[i];
        for (int i = 0; i < texts.Count; i++)
            if (texts[i] != null && texts[i].Enable) TextById[texts[i].LocationId] = texts[i];
        for (int i = 0; i < Temporary.Count; i++)
            if (Temporary[i] != null && Temporary[i].Enable) TempById[Temporary[i].LocationId] = Temporary[i];
        for (int i = 0; i < Facilities.Count; i++)
            if (Facilities[i] != null && Facilities[i].Enable) FacilityById[Facilities[i].LocationId] = Facilities[i];
        foreach(var output in outputs)if(output!=null&&output.Enable)OutputById[output.OutputItemId]=output;
        for (int i = 0; i < markets.Count; i++)
            if (markets[i] != null && markets[i].Enable) MarketById[markets[i].MarketId] = markets[i];
        for (int i = 0; i < offers.Count; i++)
        {
            MarketTable_MarketOfferConfig row = offers[i];
            if (row == null || !row.Enable) continue;
            List<MarketTable_MarketOfferConfig> list;
            if (!OffersByMarket.TryGetValue(row.MarketId, out list))
            {
                list = new List<MarketTable_MarketOfferConfig>();
                OffersByMarket.Add(row.MarketId, list);
            }
            list.Add(row);
        }
        // 物品定义仍由统一物品表提供，产物表只保存经营分类与图标。
        if (outputs.Count == 0) ChaosLog.Warn(LogChannel.Config, "[世界地点] 资源产物表为空");
        _loaded = true;
    }

    public static void Reload() { _loaded = false; EnsureLoaded(); }

    private static List<T> Load<T>(string table) where T : class
    {
        string path = GlobalPath.data_JsonPathToRead + table;
        TextAsset asset = ConfigTableAssets.Load(path+"Config");
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[世界地点] 找不到配置 " + path);
            return new List<T>();
        }
        DataList<T> doc = JsonUtility.FromJson<DataList<T>>(asset.text);
        return doc == null || doc.datas == null ? new List<T>() : doc.datas;
    }

    private static ulong Key(uint era, uint locationId) { return ((ulong)era << 32) | locationId; }

    public static WorldLocationTable_LocationConfig Location(uint locationId)
    { EnsureLoaded(); WorldLocationTable_LocationConfig row; return LocById.TryGetValue(locationId, out row) ? row : null; }

    public static WorldLocationTable_LocationEraConfig Era(uint era, uint locationId)
    { EnsureLoaded(); WorldLocationTable_LocationEraConfig row; return EraByKey.TryGetValue(Key(era, locationId), out row) ? row : null; }

    public static WorldLocationTable_LocationActionConfig Action(uint kind)
    { EnsureLoaded(); WorldLocationTable_LocationActionConfig row; return ActionByKind.TryGetValue(kind, out row) ? row : null; }

    public static WorldLocationTable_LocationRelationConfig Relation(uint relationId)
    { EnsureLoaded(); WorldLocationTable_LocationRelationConfig row; return RelationById.TryGetValue(relationId, out row) ? row : null; }

    public static WorldLocationTable_LocationTextConfig Text(uint locationId)
    { EnsureLoaded(); WorldLocationTable_LocationTextConfig row; return TextById.TryGetValue(locationId, out row) ? row : null; }

    public static WorldLocationTable_LocationTemporaryConfig TemporaryConfig(uint locationId)
    { EnsureLoaded(); WorldLocationTable_LocationTemporaryConfig row; return TempById.TryGetValue(locationId, out row) ? row : null; }

    public static WorldResourceTable_ResourceFacilityConfig Facility(uint locationId)
    { EnsureLoaded(); WorldResourceTable_ResourceFacilityConfig row; return FacilityById.TryGetValue(locationId, out row) ? row : null; }

    public static WorldResourceTable_ResourceOutputConfig Output(uint itemId)
    { EnsureLoaded();return OutputById.TryGetValue(itemId,out var row)?row:null; }

    public static MarketTable_MarketConfig Market(uint marketId)
    { EnsureLoaded(); MarketTable_MarketConfig row; return MarketById.TryGetValue(marketId, out row) ? row : null; }

    public static List<MarketTable_MarketOfferConfig> Offers(uint marketId)
    {
        EnsureLoaded();
        List<MarketTable_MarketOfferConfig> list;
        return OffersByMarket.TryGetValue(marketId, out list) ? list : null;
    }

    public static bool IsOffer(uint marketId, uint itemId)
    {
        List<MarketTable_MarketOfferConfig> list = Offers(marketId);
        if (list == null) return false;
        for (int i = 0; i < list.Count; i++) if (list[i].ItemId == itemId) return true;
        return false;
    }
}
