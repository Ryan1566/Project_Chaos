using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 世界观地图绘制工具的**草稿模型 + 落盘**。
///
/// ══════════════════ 为什么要有草稿 ══════════════════
/// 工具产出的是两张配表的 `.xlsx`，但**绘制过程本身**需要保存与恢复（关掉窗口再打开不能全丢）。
/// 草稿放在 `Assets/Excel/Chaos_excel/_worldmap_tool/WorldMapDraft.json` —— 这个目录
/// **不参与导出**（导出器只扫顶层、不递归子目录），所以草稿和分表都可以安心放在那里。
///
/// ⚠ 本文件**不引用 `UnityEditor`**：草稿模型是纯数据 + `JsonUtility`，
/// 这样任何地方（包括非编辑器上下文）都能读它，也方便自测。
/// 写盘后需要 `AssetDatabase.Refresh()` 的调用放在窗口/导出器里。
/// </summary>
[Serializable]
public class WorldMapPointEntry
{
    /// <summary>条目类别：`1` = 固定地点、`2` = 兴趣点、`3+` 预留（见 `05_WorldMap_字段规格.md` §4.2）。</summary>
    public uint kind = 1;

    /// <summary>分类值。`Kind=1` 时查地点表、`Kind=2` 时查兴趣点表（本工具不做外键校验）。</summary>
    public uint type = 1;

    /// <summary>对应表的主键。`(Kind, Type, RefId)` 三元组唯一定位一个条目。</summary>
    public uint refId = 1001;

    /// <summary>归一化横坐标（自左向右，0~1）。由 <see cref="WorldMapToolGeometry"/> 换算得到。</summary>
    public float x;

    /// <summary>归一化纵坐标（**自下向上**，0~1）。⚠ 翻转只在 <see cref="WorldMapToolGeometry"/> 里做。</summary>
    public float y;

    /// <summary>图标缩放倍数，默认 1。</summary>
    public float scale = 1f;

    /// <summary>显示层级，数值越大越靠上，默认 0。</summary>
    public uint layer;

    /// <summary>是否启用（`false` = 保留在表里但不显示）。</summary>
    public bool enable = true;

    public Vector2 Position { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }

    public WorldMapPointEntry Clone()
    {
        return new WorldMapPointEntry
        {
            kind = kind, type = type, refId = refId,
            x = x, y = y, scale = scale, layer = layer, enable = enable,
        };
    }
}

/// <summary>
/// 势力范围多边形的**一个顶点**。同一个 `(Stage, FactionId, RegionId)` 下的所有顶点按
/// **插入顺序**即 `PointOrder`（1 起连续，**不重复首点**，闭合由运行时自动完成）。
/// </summary>
[Serializable]
public class WorldMapFactionVertex
{
    /// <summary>时期 1~5（1 战国末期 / 2 秦朝 / 3 秦末起义初期 / 4 楚汉争霸 / 5 汉刘邦时期）。</summary>
    public uint stage = 1;

    /// <summary>势力标识（时期 1 的建议配色见 <see cref="WorldMapToolDataIO.SuggestedFactionColors"/>）。</summary>
    public uint factionId = 1;

    /// <summary>同一势力的一块独立区域一个 ID；不相连的领地要用不同 `RegionId`。</summary>
    public uint regionId = 1;

    /// <summary>归一化横坐标（自左向右，0~1）。</summary>
    public float x;

    /// <summary>归一化纵坐标（**自下向上**，0~1）。</summary>
    public float y;

    /// <summary>`#RRGGBB`（7 字符）。同一区域内每一行写同一个颜色。</summary>
    public string color = "#2F3A4F";

    /// <summary>该顶点是否参与绘制（**顶点级**开关，不是整块区域开关）。</summary>
    public bool enable = true;

    public Vector2 Position { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }

    public WorldMapFactionVertex Clone()
    {
        return new WorldMapFactionVertex
        {
            stage = stage, factionId = factionId, regionId = regionId,
            x = x, y = y, color = color, enable = enable,
        };
    }
}

/// <summary>
/// 同一 `(Stage, FactionId, RegionId)` 的一组顶点（= 一个多边形）。
/// 由 <see cref="WorldMapToolDataIO.GroupRegions"/> 从扁平列表聚合出来，只用于展示/校验/导出。
/// </summary>
public class WorldMapFactionRegion
{
    public uint stage;
    public uint factionId;
    public uint regionId;
    public string color;

    /// <summary>按绘制顺序（= `PointOrder`）的顶点。</summary>
    public List<Vector2> vertices = new List<Vector2>();

    /// <summary>每个顶点在草稿扁平列表里的下标，便于删除/定位。</summary>
    public List<int> sourceIndices = new List<int>();

    public string Key { get { return WorldMapToolDataIO.RegionKey(stage, factionId, regionId); } }

    /// <summary>人类可读的标签（显示在侧栏列表里）。</summary>
    public string Label
    {
        get
        {
            return string.Format("时期{0} / 势力{1} / 区域{2}（{3} 顶点）",
                stage, factionId, regionId, vertices.Count);
        }
    }
}

/// <summary>
/// 绘制工具的**整份草稿**：源图信息 + 地图 id + 点位 + 势力顶点。
/// 直接 `JsonUtility` 序列化（`[Serializable]` + public 字段，没有属性 —— 属性会被静默丢弃）。
/// </summary>
[Serializable]
public class WorldMapDraft
{
    /// <summary>源图（`Assets/` 开头的工程内路径）。</summary>
    public string sourceImagePath = WorldMapToolDataIO.DefaultSourceImagePath;

    /// <summary>
    /// 源图**真实像素宽**，从 PNG 文件头 IHDR 读出来 ——
    /// ⚠ 不能用 `Texture2D.width`：美术导入设置会把大图降采样到 2048，用它换算坐标会**整图偏移**。
    /// </summary>
    public int imageWidth = 4096;

    /// <summary>源图**真实像素高**（同上，来自 IHDR）。</summary>
    public int imageHeight = 3138;

    /// <summary>该行属于哪张底图。当前只有 `1` = 天下中原（多图预留）。</summary>
    public uint mapId = 1;

    /// <summary>固定地点 / 兴趣点。</summary>
    public List<WorldMapPointEntry> points = new List<WorldMapPointEntry>();

    /// <summary>势力范围顶点（扁平存放，按 `(Stage,FactionId,RegionId)` 分组）。</summary>
    public List<WorldMapFactionVertex> factionVertices = new List<WorldMapFactionVertex>();

    /// <summary>「有未保存改动」标记。**不写进 Json**（`[NonSerialized]`）。</summary>
    [NonSerialized] public bool dirty;

    /// <summary>结构化克隆（撤销栈用；`JsonUtility` 快照更稳，见窗口的撤销实现）。</summary>
    public WorldMapDraft Clone()
    {
        var copy = new WorldMapDraft
        {
            sourceImagePath = sourceImagePath,
            imageWidth = imageWidth,
            imageHeight = imageHeight,
            mapId = mapId,
        };
        for (int i = 0; i < points.Count; i++) copy.points.Add(points[i].Clone());
        for (int i = 0; i < factionVertices.Count; i++) copy.factionVertices.Add(factionVertices[i].Clone());
        return copy;
    }
}

/// <summary>草稿的读写 + 源图尺寸探测 + 默认值。全部是纯静态工具。</summary>
public static class WorldMapToolDataIO
{
    /// <summary>默认源图（天下中原，4096×3138）。</summary>
    public const string DefaultSourceImagePath = "Assets/Art/PNG/GameWorld_Map/Map_tianxia_zhongyuan.png";

    /// <summary>工具专属子目录：**不参与导出**（导出器只扫顶层、不递归），草稿与时期分表都放这里。</summary>
    public const string ToolFolderAssetPath = "Assets/Excel/Chaos_excel/_worldmap_tool";

    /// <summary>草稿文件名。</summary>
    public const string DraftFileName = "WorldMapDraft.json";

    /// <summary>草稿的工程内路径。</summary>
    public const string DraftAssetPath = ToolFolderAssetPath + "/" + DraftFileName;

    /// <summary>时期 1 的建议配色（`06_WorldFaction_字段规格.md` §4.2；正式配色表落地后以其为准）。</summary>
    public static readonly Dictionary<uint, string> SuggestedFactionColors = new Dictionary<uint, string>
    {
        { 1, "#2F3A4F" },   // 秦
        { 2, "#4A6FA5" },   // 赵
        { 3, "#B23A48" },   // 楚
        { 4, "#2E8B57" },   // 齐
        { 5, "#5B7C99" },   // 燕
        { 6, "#A0522D" },   // 魏
        { 7, "#6B5B95" },   // 韩
        { 8, "#8C6239" },   // 匈奴
        { 9, "#808080" },   // 中立 / 无主
    };

    /// <summary>时期的中文名（1~5），用于侧栏显示。</summary>
    public static readonly string[] StageNames =
    {
        "未使用",           // 下标 0 占位，时期从 1 开始
        "战国末期",
        "秦朝",
        "秦末起义初期",
        "楚汉争霸",
        "汉刘邦时期",
    };

    /// <summary>时期号的合法区间（含端点）。</summary>
    public const uint MinStage = 1;
    public const uint MaxStage = 5;

    /// <summary>某个时期的中文名（越界时返回 "时期N"）。</summary>
    public static string StageName(uint stage)
    {
        if (stage >= MinStage && stage < (uint)StageNames.Length) return StageNames[stage];
        return "时期" + stage;
    }

    /// <summary>区域分组键。</summary>
    public static string RegionKey(uint stage, uint factionId, uint regionId)
    {
        return stage + "|" + factionId + "|" + regionId;
    }

    /// <summary>`Assets/...` 工程内路径 → 磁盘绝对路径（基于 `Application.dataPath`）。</summary>
    public static string AssetPathToAbsolute(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return null;

        string normalized = assetPath.Replace('\\', '/');
        if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            string relative = normalized.Substring("Assets/".Length);
            return Path.Combine(Application.dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
        }
        //已经是绝对路径就原样返回
        return normalized;
    }

    // ══════════════════ 源图真实像素（PNG IHDR） ══════════════════

    /// <summary>
    /// 读 PNG **文件头**里的真实像素宽高（IHDR 块）。
    ///
    /// ⚠ 为什么必须读文件头：`Texture2D.width` 是**导入后**的尺寸，美术的导入设置会把大图
    /// 降采样（本项目 maxSize 常是 2048），拿它当"图宽"会让全部归一化坐标系统性偏小/偏移。
    ///
    /// PNG 结构：8 字节签名 + IHDR 块（4 字节长度 + `"IHDR"` + 4 字节宽 + 4 字节高，均为**大端**）。
    /// </summary>
    public static bool TryReadPngSize(string assetPath, out int width, out int height)
    {
        width = 0;
        height = 0;

        string absolute = AssetPathToAbsolute(assetPath);
        if (string.IsNullOrEmpty(absolute) || !File.Exists(absolute)) return false;

        try
        {
            using (var stream = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var header = new byte[24];
                if (stream.Read(header, 0, header.Length) < header.Length) return false;

                //PNG 签名 89 50 4E 47 0D 0A 1A 0A
                if (header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47) return false;
                //紧接着必须是 IHDR
                if (header[12] != 'I' || header[13] != 'H' || header[14] != 'D' || header[15] != 'R') return false;

                width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                return width > 0 && height > 0;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ══════════════════ 草稿读写 ══════════════════

    /// <summary>读取草稿。不存在 / 解析失败时返回 `null` 并给出可读原因（调用方决定是否新建默认草稿）。</summary>
    public static WorldMapDraft Load(out string message)
    {
        message = null;
        string absolute = AssetPathToAbsolute(DraftAssetPath);

        if (!File.Exists(absolute))
        {
            message = "草稿不存在（首次使用）：" + DraftAssetPath;
            return null;
        }

        try
        {
            string json = File.ReadAllText(absolute, Encoding.UTF8);
            var draft = JsonUtility.FromJson<WorldMapDraft>(json);
            if (draft == null)
            {
                message = "草稿解析为 null：" + DraftAssetPath;
                return null;
            }
            if (draft.points == null) draft.points = new List<WorldMapPointEntry>();
            if (draft.factionVertices == null) draft.factionVertices = new List<WorldMapFactionVertex>();
            draft.dirty = false;
            message = "已载入草稿：" + DraftAssetPath;
            return draft;
        }
        catch (Exception e)
        {
            message = "草稿读取失败：" + e.Message;
            return null;
        }
    }

    /// <summary>保存草稿（自动建目录；UTF-8 无 BOM）。</summary>
    public static bool Save(WorldMapDraft draft, out string message)
    {
        message = null;
        if (draft == null)
        {
            message = "草稿为 null，未保存";
            return false;
        }

        try
        {
            string absolute = AssetPathToAbsolute(DraftAssetPath);
            string folder = Path.GetDirectoryName(absolute);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string json = JsonUtility.ToJson(draft, true);
            //UTF-8 **无 BOM**（与工程里手写 .cs/.json 的约定一致）
            File.WriteAllText(absolute, json, new UTF8Encoding(false));
            draft.dirty = false;
            message = "草稿已保存：" + DraftAssetPath;
            return true;
        }
        catch (Exception e)
        {
            message = "草稿保存失败：" + e.Message;
            return false;
        }
    }

    /// <summary>新建一份默认草稿（源图用默认图，并尝试读它的 IHDR 填真实尺寸）。</summary>
    public static WorldMapDraft CreateDefault(string sourceImagePath = null)
    {
        var draft = new WorldMapDraft();
        if (!string.IsNullOrEmpty(sourceImagePath)) draft.sourceImagePath = sourceImagePath;

        int width, height;
        if (TryReadPngSize(draft.sourceImagePath, out width, out height))
        {
            draft.imageWidth = width;
            draft.imageHeight = height;
        }
        return draft;
    }

    // ══════════════════ 势力区域分组 ══════════════════

    /// <summary>
    /// 把扁平的顶点列表按 `(Stage, FactionId, RegionId)` 聚合成区域列表。
    /// **保持首次出现的顺序**（这样侧栏列表与导出顺序稳定、可复现）。
    /// </summary>
    public static List<WorldMapFactionRegion> GroupRegions(WorldMapDraft draft)
    {
        var regions = new List<WorldMapFactionRegion>();
        if (draft == null || draft.factionVertices == null) return regions;

        var lookup = new Dictionary<string, WorldMapFactionRegion>();
        for (int i = 0; i < draft.factionVertices.Count; i++)
        {
            WorldMapFactionVertex vertex = draft.factionVertices[i];
            if (vertex == null) continue;

            string key = RegionKey(vertex.stage, vertex.factionId, vertex.regionId);
            WorldMapFactionRegion region;
            if (!lookup.TryGetValue(key, out region))
            {
                region = new WorldMapFactionRegion
                {
                    stage = vertex.stage,
                    factionId = vertex.factionId,
                    regionId = vertex.regionId,
                    color = vertex.color,
                };
                lookup.Add(key, region);
                regions.Add(region);
            }

            region.vertices.Add(vertex.Position);
            region.sourceIndices.Add(i);
        }
        return regions;
    }
}
