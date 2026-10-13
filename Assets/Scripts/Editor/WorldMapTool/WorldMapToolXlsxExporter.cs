using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>一次导出的结果（成功与否 + 写出的文件 + 校验清单 + 行数）。</summary>
public class WorldMapExportResult
{
    public bool Success;
    public string Message;
    public readonly List<string> WrittenFiles = new List<string>();
    public readonly List<WorldMapToolIssue> Issues = new List<WorldMapToolIssue>();

    public int MapRowCount;
    public int FactionRowCount;

    /// <summary>各时期分表的行数（下标 1~5；0 未用）。</summary>
    public readonly Dictionary<uint, int> StageRowCounts = new Dictionary<uint, int>();
}

/// <summary>
/// 世界观地图绘制工具的**xlsx 导出器**（EPPlus 4.5.3，`Assets/Plugins/EPPlus.dll`）。
///
/// ══════════════════ 表结构契约（冻结，不得改列名/类型/顺序） ══════════════════
/// 固定 7 行表头，与 `TestTable` / `LoadingTable` 完全一致：
///   行1 = `类名` + 英文表名（装饰，导出器不读）
///   行2 = `是否读取` + `IsConfig`（装饰，导出器不读）
///   行3 = 中文备注（→ 生成 C# 字段的行尾注释）
///   行4 = 英文属性名（→ 字段名与 Json key；**任一列为空会中断整批导出**）
///   行5 = 端标记，**每列都必须填 `client`**（留空 → 该列只进类、不进 Json ← 最静默的错）
///   行6 = 类型（12 类型白名单、必须小写）
///   行7 起 = **正式数据**（第 7 行就是第一条数据）
///
/// ══════════════════ 单元格类型（实测对齐现有表） ══════════════════
/// 导出器**统一用 `.Text` 读值**（`ConfigManager.cs:313`），但写出来的类型要和工程既有表一致
/// （实测 `LoadingTable_读条轮播表.xlsx`：表头 6 行是 String、数据行数字是 Double、bool 是中文文本 `是`）：
///   · `uint` 列 → 数字 + 数字格式 `0`；
///   · `float` 列 → 数字 + 数字格式 **`0.0000`**（**固定小数位，避免出现 1E-05 这类科学计数法**）；
///   · `bool` 列 → 文本 `true` / `false`（归一化后是裸值，最可读）；
///   · `string` 列 → 文本。
/// </summary>
public static class WorldMapToolXlsxExporter
{
    /// <summary>顶层目录（**参与导出**，只扫这一层、不递归）。</summary>
    public const string TopLevelFolderAssetPath = "Assets/Excel/Chaos_excel";

    /// <summary>地图表文件名（顶层，全工程只能有一张 `WorldMap_` 开头的表）。</summary>
    public const string MapTableFileName = "WorldMap_世界观地图表.xlsx";

    /// <summary>势力**合并**表文件名（顶层）。</summary>
    public const string FactionMergedFileName = "WorldFaction_势力范围配置表.xlsx";

    /// <summary>势力**时期分表**文件名（放子目录，不参与导出）。</summary>
    public static string FactionStageFileName(uint stage)
    {
        return "WorldFaction_势力范围配置表_阶段" + stage + ".xlsx";
    }

    // ══════════════════ 列定义（冻结） ══════════════════

    /// <summary>地图表：行3 中文备注。</summary>
    public static readonly string[] MapNotes =
    {
        "序号", "地图ID", "条目类别", "地点类型", "地点ID", "横坐标", "纵坐标", "图标缩放", "显示层级", "是否启用",
    };

    /// <summary>地图表：行4 英文属性名（= 类字段名 = Json key）。</summary>
    public static readonly string[] MapProperties =
    {
        "Index", "MapId", "Kind", "Type", "RefId", "X", "Y", "Scale", "Layer", "Enable",
    };

    /// <summary>地图表：行6 类型。</summary>
    public static readonly string[] MapTypes =
    {
        "uint", "uint", "uint", "uint", "uint", "float", "float", "float", "uint", "bool",
    };

    /// <summary>势力表：行3 中文备注。</summary>
    public static readonly string[] FactionNotes =
    {
        "序号", "时期", "势力ID", "区域ID", "顶点序号", "横坐标", "纵坐标", "填充颜色", "是否启用",
    };

    /// <summary>势力表：行4 英文属性名。</summary>
    public static readonly string[] FactionProperties =
    {
        "Index", "Stage", "FactionId", "RegionId", "PointOrder", "X", "Y", "Color", "Enable",
    };

    /// <summary>势力表：行6 类型。</summary>
    public static readonly string[] FactionTypes =
    {
        "uint", "uint", "uint", "uint", "uint", "float", "float", "string", "bool",
    };

    private const string MapTableName = "WorldMap";
    private const string FactionTableName = "WorldFaction";

    // ══════════════════ 单元格写入 ══════════════════

    private static void SetText(ExcelWorksheet sheet, int row, int col, string value)
    {
        sheet.Cells[row, col].Value = value;
    }

    private static void SetUInt(ExcelWorksheet sheet, int row, int col, uint value)
    {
        ExcelRange cell = sheet.Cells[row, col];
        cell.Value = (double)value;
        cell.Style.Numberformat.Format = "0";
    }

    /// <summary>浮点写**固定 4 位小数**（`0.0000`）—— 归一化坐标量级小，默认 General 格式会写出科学计数法。</summary>
    private static void SetFloat(ExcelWorksheet sheet, int row, int col, float value)
    {
        ExcelRange cell = sheet.Cells[row, col];
        cell.Value = (double)value;
        cell.Style.Numberformat.Format = "0.0000";
    }

    private static void SetBool(ExcelWorksheet sheet, int row, int col, bool value)
    {
        SetText(sheet, row, col, value ? "true" : "false");
    }

    /// <summary>写 1~6 行的固定表头。</summary>
    private static void WriteHeader(ExcelWorksheet sheet, string tableName, string[] notes, string[] properties, string[] types)
    {
        SetText(sheet, 1, 1, "类名");
        SetText(sheet, 1, 2, tableName);
        SetText(sheet, 2, 1, "是否读取");
        SetText(sheet, 2, 2, "IsConfig");

        for (int i = 0; i < properties.Length; i++)
        {
            int col = i + 1;
            SetText(sheet, 3, col, i < notes.Length ? notes[i] : string.Empty);
            SetText(sheet, 4, col, properties[i]);
            SetText(sheet, 5, col, "client");            //每列都必须填 client
            SetText(sheet, 6, col, types[i]);
        }
    }

    // ══════════════════ 路径工具 ══════════════════

    /// <summary>顶层目录的磁盘绝对路径。</summary>
    public static string TopLevelFolderAbsolute
    {
        get { return WorldMapToolDataIO.AssetPathToAbsolute(TopLevelFolderAssetPath); }
    }

    /// <summary>工具子目录的磁盘绝对路径（分表与草稿都放这里）。</summary>
    public static string ToolFolderAbsolute
    {
        get { return WorldMapToolDataIO.AssetPathToAbsolute(WorldMapToolDataIO.ToolFolderAssetPath); }
    }

    private static void EnsureFolder(string absoluteFolder)
    {
        if (!string.IsNullOrEmpty(absoluteFolder) && !Directory.Exists(absoluteFolder))
            Directory.CreateDirectory(absoluteFolder);
    }

    // ══════════════════ 导出：地图表（顶层） ══════════════════

    /// <summary>
    /// 导出地图表到**顶层** `WorldMap_世界观地图表.xlsx`。
    /// 会在写盘前执行硬约束校验：① ≥1 行数据；② 顶层无同前缀其它表。
    /// </summary>
    public static WorldMapExportResult ExportMapTable(WorldMapDraft draft)
    {
        var result = new WorldMapExportResult();

        //硬约束①：≥1 行数据
        result.Issues.AddRange(WorldMapToolValidator.ValidateMapTable(draft, true));
        //硬约束②：顶层同前缀冲突
        WorldMapToolValidator.CheckTopLevelPrefixCollision(
            TopLevelFolderAbsolute, MapTableFileName, MapTableName, result.Issues);

        if (draft == null) { result.Message = "草稿为 null，未导出"; return result; }

        if (WorldMapToolValidator.HasError(result.Issues))
        {
            result.Success = false;
            result.Message = "地图表**拒绝导出**（存在必须修复的错误）";
            return result;
        }

        string absolutePath = Path.Combine(TopLevelFolderAbsolute, MapTableFileName);
        try
        {
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet sheet = package.Workbook.Worksheets.Add("WorldMapSheet0");
                WriteHeader(sheet, MapTableName, MapNotes, MapProperties, MapTypes);

                int rowIndex = 7;   //第 7 行起是数据
                for (int i = 0; i < draft.points.Count; i++)
                {
                    WorldMapPointEntry p = draft.points[i];
                    if (p == null) continue;

                    SetUInt(sheet, rowIndex, 1, (uint)(i + 1));   //Index 由工具连续编号（1 起）
                    SetUInt(sheet, rowIndex, 2, draft.mapId);
                    SetUInt(sheet, rowIndex, 3, p.kind);
                    SetUInt(sheet, rowIndex, 4, p.type);
                    SetUInt(sheet, rowIndex, 5, p.refId);
                    SetFloat(sheet, rowIndex, 6, p.x);
                    SetFloat(sheet, rowIndex, 7, p.y);
                    SetFloat(sheet, rowIndex, 8, p.scale);
                    SetUInt(sheet, rowIndex, 9, p.layer);
                    SetBool(sheet, rowIndex, 10, p.enable);

                    rowIndex++;
                    result.MapRowCount++;
                }

                package.SaveAs(new FileInfo(absolutePath));
            }

            result.WrittenFiles.Add(TopLevelFolderAssetPath + "/" + MapTableFileName);
            result.Success = true;
            result.Message = "地图表已导出：" + MapTableFileName + "（数据 " + result.MapRowCount + " 行）";
        }
        catch (Exception e)
        {
            result.Success = false;
            result.Issues.Add(WorldMapToolIssue.Error("写 " + MapTableFileName + " 失败：" + e.Message));
            result.Message = "地图表导出失败：" + e.Message;
        }

        AssetDatabase.Refresh();
        return result;
    }

    // ══════════════════ 导出：势力时期分表（子目录） ══════════════════

    /// <summary>
    /// 导出 5 张**时期分表**到子目录 `_worldmap_tool/`（**不参与导出**，所以可以同名前缀、也可以为空表）。
    /// 每张分表的 `Index` 都从 1 重新编号（合并时才会全局重编号）。
    /// </summary>
    public static WorldMapExportResult ExportFactionStageTables(WorldMapDraft draft)
    {
        var result = new WorldMapExportResult();
        if (draft == null)
        {
            result.Message = "草稿为 null，未导出";
            return result;
        }

        //子目录里的表不参与导出，"0 行"不阻断；但逐顶点的问题仍然报出来
        result.Issues.AddRange(WorldMapToolValidator.ValidateFactionTables(draft, false));

        EnsureFolder(ToolFolderAbsolute);

        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(draft);

        for (uint stage = WorldMapToolDataIO.MinStage; stage <= WorldMapToolDataIO.MaxStage; stage++)
        {
            string fileName = FactionStageFileName(stage);
            string absolutePath = Path.Combine(ToolFolderAbsolute, fileName);
            int rows = 0;

            try
            {
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet sheet = package.Workbook.Worksheets.Add("WorldFactionSheet" + stage);
                    WriteHeader(sheet, FactionTableName, FactionNotes, FactionProperties, FactionTypes);

                    int rowIndex = 7;
                    for (int r = 0; r < regions.Count; r++)
                    {
                        WorldMapFactionRegion region = regions[r];
                        if (region.stage != stage) continue;

                        //★ 统一逆时针：任务裁决要求顶点顺序一律 CCW，导出时规范化
                        List<WorldMapFactionVertex> ordered = GetOrderedRegionVertices(draft, region);
                        for (int k = 0; k < ordered.Count; k++)
                        {
                            WorldMapFactionVertex vertex = ordered[k];
                            if (vertex == null) continue;

                            rows++;
                            SetUInt(sheet, rowIndex, 1, (uint)rows);          //分表内从 1 起
                            SetUInt(sheet, rowIndex, 2, vertex.stage);
                            SetUInt(sheet, rowIndex, 3, vertex.factionId);
                            SetUInt(sheet, rowIndex, 4, vertex.regionId);
                            SetUInt(sheet, rowIndex, 5, (uint)(k + 1));       //PointOrder = 组内顺序，1 起连续
                            SetFloat(sheet, rowIndex, 6, vertex.x);
                            SetFloat(sheet, rowIndex, 7, vertex.y);
                            SetText(sheet, rowIndex, 8, vertex.color);
                            SetBool(sheet, rowIndex, 9, vertex.enable);

                            rowIndex++;
                        }
                    }

                    package.SaveAs(new FileInfo(absolutePath));
                }

                result.WrittenFiles.Add(WorldMapToolDataIO.ToolFolderAssetPath + "/" + fileName);
                result.StageRowCounts[stage] = rows;
                result.FactionRowCount += rows;
            }
            catch (Exception e)
            {
                result.Issues.Add(WorldMapToolIssue.Error("写 " + fileName + " 失败：" + e.Message));
            }
        }

        result.Success = !WorldMapToolValidator.HasError(result.Issues);
        result.Message = "势力时期分表已导出 5 张（子目录 _worldmap_tool/，不参与导出）：合计 " +
                         result.FactionRowCount + " 行";
        AssetDatabase.Refresh();
        return result;
    }

    // ══════════════════ 导出：势力合并表（顶层） ══════════════════

    /// <summary>
    /// 导出**合并表**到顶层（等价于把 5 张时期分表人工合并）：按时期 1→5、区域内按顶点顺序排列，
    /// 并且 **`Index` 全局重新连续编号**（各分表都从 1 起，直接拼会产生重复 —— 见 `06_...字段规格.md` C1）。
    /// </summary>
    public static WorldMapExportResult ExportFactionMergedTable(WorldMapDraft draft)
    {
        var result = new WorldMapExportResult();

        //硬约束①：合并表 ≥1 行数据
        result.Issues.AddRange(WorldMapToolValidator.ValidateFactionTables(draft, true));
        //硬约束②：顶层同前缀冲突
        WorldMapToolValidator.CheckTopLevelPrefixCollision(
            TopLevelFolderAbsolute, FactionMergedFileName, FactionTableName, result.Issues);

        if (draft == null) { result.Message = "草稿为 null，未导出"; return result; }

        if (WorldMapToolValidator.HasError(result.Issues))
        {
            result.Success = false;
            result.Message = "势力合并表**拒绝导出**（存在必须修复的错误）";
            return result;
        }

        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(draft);
        string absolutePath = Path.Combine(TopLevelFolderAbsolute, FactionMergedFileName);

        try
        {
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet sheet = package.Workbook.Worksheets.Add("WorldFactionSheet0");
                WriteHeader(sheet, FactionTableName, FactionNotes, FactionProperties, FactionTypes);

                int rowIndex = 7;
                //时期 1→5 依次输出，保证合并结果稳定可复现
                for (uint stage = WorldMapToolDataIO.MinStage; stage <= WorldMapToolDataIO.MaxStage; stage++)
                {
                    for (int r = 0; r < regions.Count; r++)
                    {
                        WorldMapFactionRegion region = regions[r];
                        if (region.stage != stage) continue;

                        //★ 统一逆时针（与分表同一口径）
                        List<WorldMapFactionVertex> ordered = GetOrderedRegionVertices(draft, region);
                        for (int k = 0; k < ordered.Count; k++)
                        {
                            WorldMapFactionVertex vertex = ordered[k];
                            if (vertex == null) continue;

                            result.FactionRowCount++;
                            SetUInt(sheet, rowIndex, 1, (uint)result.FactionRowCount);   //★ 合并后全局重编号
                            SetUInt(sheet, rowIndex, 2, vertex.stage);
                            SetUInt(sheet, rowIndex, 3, vertex.factionId);
                            SetUInt(sheet, rowIndex, 4, vertex.regionId);
                            SetUInt(sheet, rowIndex, 5, (uint)(k + 1));
                            SetFloat(sheet, rowIndex, 6, vertex.x);
                            SetFloat(sheet, rowIndex, 7, vertex.y);
                            SetText(sheet, rowIndex, 8, vertex.color);
                            SetBool(sheet, rowIndex, 9, vertex.enable);

                            rowIndex++;
                        }
                    }
                }

                package.SaveAs(new FileInfo(absolutePath));
            }

            result.WrittenFiles.Add(TopLevelFolderAssetPath + "/" + FactionMergedFileName);
            result.Success = true;
            result.Message = "势力合并表已导出：" + FactionMergedFileName +
                             "（数据 " + result.FactionRowCount + " 行，Index 已全局连续重编号）";
        }
        catch (Exception e)
        {
            result.Success = false;
            result.Issues.Add(WorldMapToolIssue.Error("写 " + FactionMergedFileName + " 失败：" + e.Message));
            result.Message = "势力合并表导出失败：" + e.Message;
        }

        AssetDatabase.Refresh();
        return result;
    }

    /// <summary>
    /// 取某个区域**按规范顺序**（统一**逆时针**）排列的顶点。
    ///
    /// 任务裁决要求"顶点顺序统一逆时针"，所以导出时按有符号面积判定、必要时反转 ——
    /// 这样写出的 `PointOrder` 天生满足约定，运行时按 `PointOrder` 连线即可（不重复首点，自动闭合）。
    /// </summary>
    private static List<WorldMapFactionVertex> GetOrderedRegionVertices(WorldMapDraft draft, WorldMapFactionRegion region)
    {
        var ordered = new List<WorldMapFactionVertex>();
        var points = new List<Vector2>();

        for (int k = 0; k < region.sourceIndices.Count; k++)
        {
            WorldMapFactionVertex vertex = draft.factionVertices[region.sourceIndices[k]];
            if (vertex == null) continue;
            ordered.Add(vertex);
            points.Add(vertex.Position);
        }

        //归一化坐标是 Y 向上，所以有符号面积为负 = 顺时针 → 反转成逆时针
        if (points.Count >= 3 && WorldMapToolGeometry.SignedArea(points) < 0f) ordered.Reverse();
        return ordered;
    }

    // ══════════════════ (Kind, Type, RefId) 使用清单 ══════════════════

    /// <summary>
    /// 打印 `(Kind, Type, RefId)` 使用清单到 Console —— 供后续**地点表 / 兴趣点表**落地时对照
    /// （那两张表本次未建，所以本工具**不做外键硬校验**，只出这份清单）。
    /// </summary>
    public static void PrintUsageList(WorldMapDraft draft)
    {
        if (draft == null || draft.points == null)
        {
            Debug.Log("[局外地图] (Kind,Type,RefId) 使用清单：草稿为空。");
            return;
        }

        //按三元组聚合，保持首次出现顺序
        var order = new List<string>();
        var counts = new Dictionary<string, int>();
        var kindCounts = new Dictionary<uint, int>();

        for (int i = 0; i < draft.points.Count; i++)
        {
            WorldMapPointEntry p = draft.points[i];
            if (p == null) continue;

            string key = "Kind=" + p.kind + ", Type=" + p.type + ", RefId=" + p.refId;
            if (!counts.ContainsKey(key)) { counts.Add(key, 0); order.Add(key); }
            counts[key]++;

            if (!kindCounts.ContainsKey(p.kind)) kindCounts.Add(p.kind, 0);
            kindCounts[p.kind]++;
        }

        var builder = new StringBuilder();
        builder.AppendLine("[局外地图] (Kind,Type,RefId) 使用清单 —— 供地点表/兴趣点表落地时对照（本工具不做外键校验）");
        builder.AppendLine("  点位总数 = " + draft.points.Count + "，不同三元组 = " + order.Count);

        foreach (var pair in kindCounts)
        {
            string name = pair.Key == 1 ? "固定地点" : (pair.Key == 2 ? "兴趣点" : "预留类别");
            builder.AppendLine("  Kind=" + pair.Key + "（" + name + "）：" + pair.Value + " 个点位");
        }

        for (int i = 0; i < order.Count; i++)
        {
            builder.AppendLine("    " + order[i] + "  ×" + counts[order[i]]);
        }

        Debug.Log(builder.ToString());
    }
}
