using System;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 局外世界观地图的**数据服务**：把两张配表读成运行时可用的视图对象
/// （<see cref="WorldMapPoint"/> / <see cref="WorldFactionArea"/>）。
///
/// ══════════════ 契约要点 ══════════════
/// · **懒加载 + 一次性缓存**：第一次访问时读表，之后走内存；`Reload()` 强制重读（改表后调试用）。
/// · **表缺失 / 为空 / Json 损坏 一律不抛异常**：只 `ChaosLog.Warn` 并返回空集 ——
///   这样**删掉 Json 后面板仍然能打开**（验收 A7），只是没有点位与势力范围。
/// · 输出已做过滤与排序：点位丢掉 `Enable = false`、按 `Layer` 升序（数值大的后画 = 在上面）；
///   势力按 `(Stage, FactionId, RegionId)` 分组、组内按 `PointOrder` 升序、**顶点数 <3 的整块丢弃**。
///
/// ⚠ **为什么不直接调 `ConfigLoader.Instance.LoadData<T>()`**：
///   它内部是 `ResManager.Instance.Load<TextAsset>(...).text` —— **没有 null 检查**，
///   表被删/改名时会直接 **NRE**，与"表缺失只 Warn"的契约（A7）冲突。
///   所以这里自己 `Resources.Load<TextAsset>` + 判空 + `JsonUtility`，
///   路径仍取自 `GlobalPath.data_JsonPathToRead`（单一真相源不变）。
/// </summary>
public class WorldMapService : SingletonBase<WorldMapService>
{
    /// <summary>元数据提供者（默认占位实现恒返回 false）。落地地点表后换实现即可。</summary>
    public IWorldMapMetaProvider MetaProvider = new WorldMapMetaProvider();

    private Dictionary<uint, List<WorldMapPoint>> _pointsByMap;
    private Dictionary<uint, List<WorldFactionArea>> _areasByStage;
    private bool _loaded;

    private static readonly List<WorldMapPoint> EmptyPoints = new List<WorldMapPoint>();
    private static readonly List<WorldFactionArea> EmptyAreas = new List<WorldFactionArea>();

    // ══════════════════ 对外查询 ══════════════════

    /// <summary>取某张底图上所有启用的点位（按 `Layer` 升序）。没有数据时返回**空列表**（不是 null）。</summary>
    public List<WorldMapPoint> Points(uint mapId)
    {
        EnsureLoaded();

        List<WorldMapPoint> list;
        if (_pointsByMap != null && _pointsByMap.TryGetValue(mapId, out list)) return list;
        return EmptyPoints;
    }

    /// <summary>取某时期的所有势力领地（按分组首次出现顺序）。没有数据时返回**空列表**（不是 null）。</summary>
    public List<WorldFactionArea> Areas(uint stage)
    {
        EnsureLoaded();

        List<WorldFactionArea> list;
        if (_areasByStage != null && _areasByStage.TryGetValue(stage, out list)) return list;
        return EmptyAreas;
    }

    /// <summary>强制重新读表（改完配表后调试用）。</summary>
    public void Reload()
    {
        _loaded = false;
        _pointsByMap = null;
        _areasByStage = null;
        EnsureLoaded();
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;   //先置位：即使读表过程出问题也不要每次访问都重试
        Load();
    }

    // ══════════════════ 读表 ══════════════════

    private void Load()
    {
        _pointsByMap = new Dictionary<uint, List<WorldMapPoint>>();
        _areasByStage = new Dictionary<uint, List<WorldFactionArea>>();

        LoadPoints(LoadTable<WorldMapConfig>("WorldMap"));
        LoadAreas(LoadTable<WorldFactionConfig>("WorldFaction"));
    }

    private void LoadPoints(List<WorldMapConfig> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            ChaosLog.Warn(LogChannel.Config, "[局外地图] 地图表没有可用数据（或表缺失）→ 本轮没有点位");
            return;
        }

        int skipped = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            WorldMapConfig row = rows[i];
            if (row == null) continue;
            if (!row.Enable) { skipped++; continue; }

            var point = new WorldMapPoint
            {
                index = row.Index,
                mapId = row.MapId,
                kind = row.Kind,
                type = row.Type,
                refId = row.RefId,
                position = new Vector2(row.X, row.Y),
                scale = row.Scale <= 0f ? 1f : row.Scale,
                layer = row.Layer,
            };

            List<WorldMapPoint> list;
            if (!_pointsByMap.TryGetValue(row.MapId, out list))
            {
                list = new List<WorldMapPoint>();
                _pointsByMap.Add(row.MapId, list);
            }
            list.Add(point);
        }

        //Layer 升序：数值大的**后画** → 压在上面（与字段规格"数值越大越靠上"一致）
        foreach (KeyValuePair<uint, List<WorldMapPoint>> pair in _pointsByMap)
        {
            pair.Value.Sort((a, b) => a.layer.CompareTo(b.layer));
        }

        ChaosLog.Info(LogChannel.Config,
            "[局外地图] 点位已加载：底图 " + _pointsByMap.Count + " 张，共 " + CountPoints() + " 个点位（跳过 Enable=false " + skipped + " 个）");
    }

    private void LoadAreas(List<WorldFactionConfig> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            ChaosLog.Warn(LogChannel.Config, "[局外地图] 势力表没有可用数据（或表缺失）→ 本轮没有势力范围");
            return;
        }

        //先按 (Stage,FactionId,RegionId) 分组，保持首次出现顺序 —— 顺序稳定，便于排查
        var groups = new Dictionary<string, List<WorldFactionConfig>>();
        var order = new List<string>();

        for (int i = 0; i < rows.Count; i++)
        {
            WorldFactionConfig row = rows[i];
            if (row == null || !row.Enable) continue;

            string key = row.Stage + "|" + row.FactionId + "|" + row.RegionId;
            List<WorldFactionConfig> group;
            if (!groups.TryGetValue(key, out group))
            {
                group = new List<WorldFactionConfig>();
                groups.Add(key, group);
                order.Add(key);
            }
            group.Add(row);
        }

        int dropped = 0;
        for (int i = 0; i < order.Count; i++)
        {
            List<WorldFactionConfig> group = groups[order[i]];
            if (group.Count < 3)
            {
                //规格 M2：少于 3 个顶点不构成多边形 → 跳过并记 warning（不得崩溃）
                dropped++;
                ChaosLog.Warn(LogChannel.Config,
                    "[局外地图] 区域 " + order[i] + " 只有 " + group.Count + " 个顶点，不足 3 个已跳过");
                continue;
            }

            //规格 M1：按 PointOrder 升序连成闭合多边形
            group.Sort((a, b) => a.PointOrder.CompareTo(b.PointOrder));

            WorldFactionConfig first = group[0];
            var area = new WorldFactionArea
            {
                stage = first.Stage,
                factionId = first.FactionId,
                regionId = first.RegionId,
                color = ParseColor(first.Color),
            };

            for (int k = 0; k < group.Count; k++)
            {
                area.vertices.Add(new Vector2(group[k].X, group[k].Y));
            }

            List<WorldFactionArea> list;
            if (!_areasByStage.TryGetValue(area.stage, out list))
            {
                list = new List<WorldFactionArea>();
                _areasByStage.Add(area.stage, list);
            }
            list.Add(area);
        }

        ChaosLog.Info(LogChannel.Config,
            "[局外地图] 势力范围已加载：时期 " + _areasByStage.Count + " 个，共 " + CountAreas() + " 块区域（跳过顶点不足 " + dropped + " 块）");
    }

    /// <summary>`#RRGGBB` → Color；解析失败回退品红并记 Warn（不抛异常）。</summary>
    private static Color ParseColor(string hex)
    {
        Color parsed;
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out parsed)) return parsed;

        ChaosLog.Warn(LogChannel.Config, "[局外地图] 颜色值非法（应为 #RRGGBB）：「" + hex + "」→ 暂用品红");
        return Color.magenta;
    }

    /// <summary>
    /// 读一张 Config 模式的表。**任何失败都只 Warn 并返回 null**，绝不抛异常（A7 要求）。
    /// </summary>
    private static List<T> LoadTable<T>(string tableName) where T : class
    {
        //Config 模式的产物名 = 表名 + "Config.json"，路径取自 GlobalPath（单一真相源）
        string path = GlobalPath.data_JsonPathToRead + tableName + "Config";

        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[局外地图] 配置表缺失：Resources/" + path + ".json（按空表处理）");
            return null;
        }

        try
        {
            DataList<T> list = JsonUtility.FromJson<DataList<T>>(asset.text);
            if (list == null || list.datas == null)
            {
                ChaosLog.Warn(LogChannel.Config, "[局外地图] 配置表解析结果为空：" + path);
                return null;
            }
            return list.datas;
        }
        catch (Exception e)
        {
            //Json 被手改坏时 FromJson 可能抛异常；这里兜住，面板照样能开
            ChaosLog.Warn(LogChannel.Config, "[局外地图] 配置表解析失败：" + path + " → " + e.Message + "（按空表处理）");
            return null;
        }
    }

    private int CountPoints()
    {
        int total = 0;
        foreach (KeyValuePair<uint, List<WorldMapPoint>> pair in _pointsByMap) total += pair.Value.Count;
        return total;
    }

    private int CountAreas()
    {
        int total = 0;
        foreach (KeyValuePair<uint, List<WorldFactionArea>> pair in _areasByStage) total += pair.Value.Count;
        return total;
    }
}
