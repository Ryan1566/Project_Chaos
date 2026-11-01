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

    /// <summary>
    /// 世界观地图表文件名（顶层共享工作簿）：第 1 页 = 地点（WorldMapTable_WorldMapConfig），第 2 页 = 疆域（WorldMapTable_WorldFactionConfig）。
    /// 只保留一张共享表，避免顶层出现同英文前缀的第二张表被静默覆盖。
    /// </summary>
    public const string MapTableFileName = "WorldMapTable_世界观地图表.xlsx";

    /// <summary>势力**合并**表文件名（顶层）—— 现已与地点表合并为同一张共享工作簿，因此与 <see cref="MapTableFileName"/> 同名。</summary>
    public const string FactionMergedFileName = "WorldMapTable_世界观地图表.xlsx";

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

    // ══════════════════ 共享工作簿：固定页位置 ══════════════════

    /// <summary>地点页的工作表位置（从 1 开始）。位置固定，不靠页签名称识别。</summary>
    public const int MapSheetIndex = 1;

    /// <summary>疆域页的工作表位置（从 1 开始）。位置固定，不靠页签名称识别。</summary>
    public const int FactionSheetIndex = 2;

    /// <summary>地点页的展示名称（仅用于改名恢复；识别一律按索引）。</summary>
    public const string MapSheetDisplayName = "地点";

    /// <summary>疆域页的展示名称（仅用于改名恢复；识别一律按索引）。</summary>
    public const string FactionSheetDisplayName = "疆域";

    /// <summary>
    /// 解析本次写盘的目标目录：默认重载写正式顶层目录，临时目录重载只写传入文件夹。
    /// 传入空值视为使用正式顶层目录。
    /// </summary>
    private static string ResolveOutputFolderAbsolute(string outputDirectory)
    {
        if (string.IsNullOrEmpty(outputDirectory)) return TopLevelFolderAbsolute;
        return WorldMapToolDataIO.AssetPathToAbsolute(outputDirectory);
    }

    /// <summary>把磁盘绝对路径转成报告用的工程内路径（不在 Assets 下时原样返回绝对路径）。</summary>
    private static string ToReportPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return string.Empty;

        string root = TopLevelFolderAbsolute;
        if (string.IsNullOrEmpty(root)) return absolutePath;

        string normalizedRoot = root.Replace('\\', '/').TrimEnd('/');
        string normalized = absolutePath.Replace('\\', '/');

        if (string.Equals(Path.GetDirectoryName(normalized), normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return TopLevelFolderAssetPath + "/" + Path.GetFileName(absolutePath);

        return absolutePath;
    }

    /// <summary>工作表名比较：忽略前后空白与大小写，用于判断页签是否已是中文展示名。</summary>
    private static bool SameSheetName(string left, string right)
    {
        if (left == null || right == null) return false;
        return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 在已有工作簿里按**固定位置**取目标页；位置不存在时只能在工作簿**末尾追加**补齐，
    /// 绝不插到别处，保证「1 地点 / 2 疆域」的索引不受额外页影响。
    /// </summary>
    private static ExcelWorksheet GetOrAppendSheetByIndex(ExcelWorkbook workbook, int oneBasedIndex, out string error)
    {
        error = null;
        if (workbook == null)
        {
            error = "工作簿为 null";
            return null;
        }

        int count = workbook.Worksheets.Count;
        if (oneBasedIndex < 1)
        {
            error = "目标页索引非法：" + oneBasedIndex;
            return null;
        }

        if (count >= oneBasedIndex) return ExcelWorksheetAccess.GetByIndex(workbook, oneBasedIndex);

        //缺少目标页：只在末尾补齐中间位置，保证索引对齐
        while (workbook.Worksheets.Count < oneBasedIndex)
        {
            workbook.Worksheets.Add("占位" + (workbook.Worksheets.Count + 1));
        }
        return ExcelWorksheetAccess.GetByIndex(workbook, oneBasedIndex);
    }

    /// <summary>
    /// 表名比较的归一化：忽略前后空白、大小写，并去掉 `Config` 后缀再比一次 ——
    /// 导出器读的是**基名**（`WorldMap`），人工表里常写成**完整类名**（`WorldMapTable_WorldMapConfig`），两者视为同一张表。
    /// </summary>
    private static bool SameTableType(string left, string right)
    {
        if (left == null || right == null) return false;

        string a = NormalizeTableType(left);
        string b = NormalizeTableType(right);
        if (a.Length == 0 || b.Length == 0) return false;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTableType(string value)
    {
        string trimmed = value == null ? string.Empty : value.Trim();
        if (trimmed.EndsWith("Config", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(0, trimmed.Length - "Config".Length).Trim();
        return trimmed;
    }

    /// <summary>
    /// 整页是否为空占位：**所有**已用单元格（值、公式、文本）都为空才算空 —— 不能只看 B1，
    /// 否则 B1 空、别的格有用户数据时会被当占位覆盖掉。
    /// </summary>
    private static bool IsEmptyPlaceholderSheet(ExcelWorksheet sheet)
    {
        if (sheet.Dimension == null) return true;   //完全空白

        ExcelRange cells = sheet.Cells[1, 1, sheet.Dimension.End.Row, sheet.Dimension.End.Column];
        foreach (ExcelRangeBase cell in cells)
        {
            if (cell.Value != null) return false;
            if (!string.IsNullOrEmpty(cell.Formula)) return false;
            if (!string.IsNullOrEmpty(cell.Text) && cell.Text.Trim().Length > 0) return false;
        }
        return true;
    }

    /// <summary>
    /// 校验目标页归属：整页空占位 = 合法（可直接写）；B1 的表类型与本类同名（基名或完整 `Config` 类名）= 同一张表（可覆盖）；
    /// 其它值 = 别类数据（拒绝，绝不覆盖）。返回 false 时 error 给出原因。
    /// </summary>
    private static bool CheckSheetOwnership(ExcelWorksheet sheet, string tableName, int sheetIndex, out string error)
    {
        error = null;
        if (IsEmptyPlaceholderSheet(sheet)) return true;   //整页无数据、无公式 → 空占位，合法

        string existing = sheet.Cells[1, 2].Text;
        if (!string.IsNullOrEmpty(existing) && SameTableType(existing, tableName)) return true;

        error = "工作表索引 " + sheetIndex + "（当前名称「" + sheet.Name + "」）的 B1 表类型为「" +
                (string.IsNullOrEmpty(existing) ? "（空）" : existing) +
                "」，与本次要写的数据（" + tableName + "）不符 —— 该页不属于本次数据，已拒绝覆盖。";
        return false;
    }

    /// <summary>
    /// 保证共享工作簿里**固定两页**都在：1 = 地点、2 = 疆域。
    /// 缺位时只在**末尾**补齐并写成空占位（纯名称页、无任何单元格内容），不能碰既有页的位置与内容，
    /// 额外页原样保留；已有页即使次序不是约定位置也一律不动。
    /// </summary>
    private static void EnsureBothSheets(ExcelWorkbook workbook)
    {
        if (workbook == null) return;

        int required = FactionSheetIndex > MapSheetIndex ? FactionSheetIndex : MapSheetIndex;
        while (workbook.Worksheets.Count < required)
            workbook.Worksheets.Add(workbook.Worksheets.Count==0?MapSheetDisplayName:FactionSheetDisplayName);
    }

    /// <summary>清空目标页整块已用区域，但保留列宽、行高等版面设置；避免旧数据残留在新数据下方。</summary>
    private static void ClearSheetUsedRange(ExcelWorksheet sheet)
    {
        int lastRow = sheet.Dimension != null ? sheet.Dimension.End.Row : 0;
        int lastCol = sheet.Dimension != null ? sheet.Dimension.End.Column : 0;
        if (lastRow < 1 || lastCol < 1) return;
        sheet.Cells[1, 1, lastRow, lastCol].Clear();
    }

    /// <summary>
    /// 事务式保存：先完整写到同目录的临时文件，成功后再替换目标；
    /// 任何一步失败都不动原表。临时文件仅在运行时清理，且只清理本方法自己生成的限定文件。
    /// </summary>
    private static void SaveWorkbookTransactional(ExcelPackage package, string targetAbsolutePath)
    {
        string folder = Path.GetDirectoryName(targetAbsolutePath);
        EnsureFolder(folder);

        string fileName = Path.GetFileName(targetAbsolutePath);
        string tempPath = Path.Combine(folder, "~tmp_worldmap_" + Guid.NewGuid().ToString("N") + "_" + fileName);
        try
        {
            package.SaveAs(new FileInfo(tempPath));   //先写临时文件，失败时原表不受影响
            // 同卷原子替换，失败时原表仍在；备份仅在成功后清理，失败时保留恢复源。
            if(File.Exists(targetAbsolutePath))
            {
                string backupPath=tempPath+".bak";
                File.Replace(tempPath,targetAbsolutePath,backupPath);
                SafeDeleteGeneratedFile(backupPath);
            }
            else File.Move(tempPath,targetAbsolutePath);
        }
        finally
        {
            SafeDeleteGeneratedFile(tempPath);      //只删本方法生成的临时文件
        }
    }

    /// <summary>运行时清理导出事务自己生成的临时文件；文件不存在或删除失败都静默跳过。</summary>
    private static void SafeDeleteGeneratedFile(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return;
        try
        {
            if (File.Exists(absolutePath)) File.Delete(absolutePath);
        }
        catch
        {
            //清理失败不影响导出结果，交给人工处理
        }
    }

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
    /// 导出地点页到**顶层** `世界观地图表.xlsx` 的第 1 页（默认重载，写正式根目录）。
    /// 会在写盘前执行硬约束校验：① ≥1 行数据；② 顶层无同前缀其它表。
    /// </summary>
    public static WorldMapExportResult ExportMapTable(WorldMapDraft draft)
    {
        return ExportMapTable(draft, null);
    }

    /// <summary>
    /// 导出地点页到指定目录下的 `世界观地图表.xlsx` 第 1 页（供主控临时目录独立验证）。
    /// 只更新第 1 页：其余页的值、样式、列宽、行高全部原样保留，绝不整本重建。
    /// </summary>
    public static WorldMapExportResult ExportMapTable(WorldMapDraft draft, string outputDirectory)
    {
        var result = new WorldMapExportResult();

        if (draft == null) { result.Message = "草稿为 null，未导出"; return result; }

        string folderAbsolute = ResolveOutputFolderAbsolute(outputDirectory);

        //硬约束①：≥1 行数据
        result.Issues.AddRange(WorldMapToolValidator.ValidateMapTable(draft, true));
        //硬约束②：同前缀冲突（共享文件自身不算冲突）
        WorldMapToolValidator.CheckTopLevelPrefixCollision(
            folderAbsolute, MapTableFileName, ConfigExportNaming.WorkbookKey(MapTableFileName), result.Issues);

        if (WorldMapToolValidator.HasError(result.Issues))
        {
            result.Success = false;
            result.Message = "地图表**拒绝导出**（存在必须修复的错误）";
            return result;
        }

        string absolutePath = Path.Combine(folderAbsolute, MapTableFileName);
        try
        {
            //已有工作簿直接打开并原地修改，未存在时新建
            using (var package = new ExcelPackage(new FileInfo(absolutePath)))
            {
                EnsureBothSheets(package.Workbook);
                string error;
                ExcelWorksheet sheet = GetOrAppendSheetByIndex(package.Workbook, MapSheetIndex, out error);
                if (sheet == null) throw new InvalidOperationException(error);

                if (!CheckSheetOwnership(sheet, MapTableName, MapSheetIndex, out error))
                {
                    result.Success = false;
                    result.Issues.Add(WorldMapToolIssue.Error(error));
                    result.Message = "地图表**拒绝导出**：" + error;
                    return result;
                }

                ClearSheetUsedRange(sheet);
                //页签名称只是展示；按索引选中后再恢复中文名
                if (!SameSheetName(sheet.Name, MapSheetDisplayName)) sheet.Name = MapSheetDisplayName;

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

                SaveWorkbookTransactional(package, absolutePath);
            }

            result.WrittenFiles.Add(ToReportPath(absolutePath));
            result.Success = true;
            result.Message = "地图表已导出：第 " + MapSheetIndex + " 页「" + MapSheetDisplayName + "」写入 " +
                             Path.GetFileName(absolutePath) + "（数据 " + result.MapRowCount + " 行）";
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
        //★ 同一 (时期,势力) 统一取"第一块区域"的颜色：把"同势力两块不同色"挡在导出这一层
        Dictionary<string, string> factionColors = BuildFactionColorMap(regions);

        for (uint stage = WorldMapToolDataIO.MinStage; stage <= WorldMapToolDataIO.MaxStage; stage++)
        {
            string fileName = FactionStageFileName(stage);
            string absolutePath = Path.Combine(ToolFolderAbsolute, fileName);
            int rows = 0;

            try
            {
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet sheet = package.Workbook.Worksheets.Add("时代" + stage);
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
                            SetText(sheet, rowIndex, 8, ResolveFactionColor(factionColors, vertex.stage, vertex.factionId));
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
    /// 导出**疆域页**到顶层 `世界观地图表.xlsx` 的第 2 页（默认重载，写正式根目录）。
    /// 等价于把 5 张时期分表人工合并：按时期 1→5、区域内按顶点顺序排列，
    /// 并且 **`Index` 全局重新连续编号**（各分表都从 1 起，直接拼会产生重复 —— 见 `06_...字段规格.md` C1）。
    /// </summary>
    public static WorldMapExportResult ExportFactionMergedTable(WorldMapDraft draft)
    {
        return ExportFactionMergedTable(draft, null);
    }

    /// <summary>
    /// 导出**疆域页**到指定目录下的 `世界观地图表.xlsx` 第 2 页（供主控临时目录独立验证）。
    /// 只更新第 2 页：第 1 页地点页与所有额外页原样保留。首次单独导出时会在末尾补一个空地点页占位。
    /// </summary>
    public static WorldMapExportResult ExportFactionMergedTable(WorldMapDraft draft, string outputDirectory)
    {
        var result = new WorldMapExportResult();

        if (draft == null) { result.Message = "草稿为 null，未导出"; return result; }

        string folderAbsolute = ResolveOutputFolderAbsolute(outputDirectory);

        //硬约束①：合并表 ≥1 行数据
        result.Issues.AddRange(WorldMapToolValidator.ValidateFactionTables(draft, true));
        //硬约束②：同前缀冲突（共享文件自身不算冲突）
        WorldMapToolValidator.CheckTopLevelPrefixCollision(
            folderAbsolute, FactionMergedFileName, ConfigExportNaming.WorkbookKey(FactionMergedFileName), result.Issues);

        if (WorldMapToolValidator.HasError(result.Issues))
        {
            result.Success = false;
            result.Message = "势力合并表**拒绝导出**（存在必须修复的错误）";
            return result;
        }

        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(draft);
        //★ 同一 (时期,势力) 统一取"第一块区域"的颜色：把"同势力两块不同色"挡在导出这一层
        Dictionary<string, string> factionColors = BuildFactionColorMap(regions);
        string absolutePath = Path.Combine(folderAbsolute, FactionMergedFileName);

        try
        {
            using (var package = new ExcelPackage(new FileInfo(absolutePath)))
            {
                EnsureBothSheets(package.Workbook);
                string error;
                ExcelWorksheet sheet = GetOrAppendSheetByIndex(package.Workbook, FactionSheetIndex, out error);
                if (sheet == null) throw new InvalidOperationException(error);

                if (!CheckSheetOwnership(sheet, FactionTableName, FactionSheetIndex, out error))
                {
                    result.Success = false;
                    result.Issues.Add(WorldMapToolIssue.Error(error));
                    result.Message = "势力合并表**拒绝导出**：" + error;
                    return result;
                }

                ClearSheetUsedRange(sheet);
                //页签名称只是展示；按索引选中后再恢复中文名
                if (!SameSheetName(sheet.Name, FactionSheetDisplayName)) sheet.Name = FactionSheetDisplayName;

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
                            SetText(sheet, rowIndex, 8, ResolveFactionColor(factionColors, vertex.stage, vertex.factionId));
                            SetBool(sheet, rowIndex, 9, vertex.enable);

                            rowIndex++;
                        }
                    }
                }

                SaveWorkbookTransactional(package, absolutePath);
            }

            result.WrittenFiles.Add(ToReportPath(absolutePath));
            result.Success = true;
            result.Message = "势力合并表已导出：第 " + FactionSheetIndex + " 页「" + FactionSheetDisplayName + "」写入 " +
                             Path.GetFileName(absolutePath) +
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

    /// <summary>`(时期|势力)` → 该势力**第一块区域**的颜色。用于"同势力同色"的统一。</summary>
    private static Dictionary<string, string> BuildFactionColorMap(List<WorldMapFactionRegion> regions)
    {
        var map = new Dictionary<string, string>();
        for (int i = 0; i < regions.Count; i++)
        {
            string key = regions[i].stage + "|" + regions[i].factionId;
            if (!map.ContainsKey(key)) map.Add(key, regions[i].color);   //只在第一次写入 → 取第一块
        }
        return map;
    }

    /// <summary>取该势力统一的颜色；查不到（理论不可达）时回退 null，由调用方保证有值。</summary>
    private static string ResolveFactionColor(Dictionary<string, string> map, uint stage, uint factionId)
    {
        string color;
        return map.TryGetValue(stage + "|" + factionId, out color) ? color : null;
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
