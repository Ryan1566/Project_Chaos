using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using OfficeOpenXml;
using UnityEditor;
using ChaosDebug;
#endif

/// <summary>
/// 导出模式枚举
/// </summary>
public enum ExporterMode
{
    /// <summary>
    /// 表格数据，策划配置的默认常量数据
    /// </summary>
    Config,
    /// <summary>
    /// 模型数据，服务器或者本地可以修改的数据
    /// </summary>
    Data,
}

#if UNITY_EDITOR

/// <summary>
/// 批量导表报告。调用方可以直接读取产物、跳过页签、警告和类型索引状态。
/// </summary>
[Serializable]
public sealed class ConfigExportReport
{
    /// <summary>本次导出模式</summary>
    public ExporterMode Mode;
    /// <summary>输入工作簿数量</summary>
    public int WorkbookCount;
    /// <summary>扫描到的工作表数量</summary>
    public int ScannedWorksheetCount;
    /// <summary>成功导出的工作表数量</summary>
    public int ExportedWorksheetCount;
    /// <summary>跳过的空页、说明页或模式不匹配页数量</summary>
    public int SkippedWorksheetCount;
    /// <summary>导出的数据行总数</summary>
    public int DataRowCount;
    /// <summary>每个已导出工作表对应的产物信息</summary>
    public List<ConfigExportArtifact> Artifacts = new List<ConfigExportArtifact>();
    /// <summary>被跳过的工作表及原因</summary>
    public List<ConfigExportSkip> Skipped = new List<ConfigExportSkip>();
    /// <summary>未阻断导出的警告</summary>
    public List<string> Warnings = new List<string>();
    /// <summary>本批是否发布了类型索引</summary>
    public bool TypeIndexPublished;
    /// <summary>类型索引的绝对路径；未发布时为 null</summary>
    public string TypeIndexPath;
    /// <summary>发布后的类型索引条目总数</summary>
    public int TypeIndexEntryCount;
    /// <summary>本批耗时（毫秒）</summary>
    public double ElapsedMilliseconds;

    /// <summary>生成便于日志查看的中文摘要</summary>
    public string ToSummary()
    {
        string indexState = TypeIndexPublished
            ? $"已发布（{TypeIndexEntryCount} 条）"
            : "未发布";
        return $"模式 {Mode}：工作簿 {WorkbookCount} 个，扫描页签 {ScannedWorksheetCount} 个，"
            + $"导出 {ExportedWorksheetCount} 个，跳过 {SkippedWorksheetCount} 个，"
            + $"数据行 {DataRowCount} 行，类型索引{indexState}";
    }
}

/// <summary>
/// 单个工作表的导出产物信息。
/// </summary>
[Serializable]
public sealed class ConfigExportArtifact
{
    public string WorkbookPath;
    public string WorkbookName;
    public string WorksheetName;
    public string ClassName;
    public ExporterMode Mode;
    /// <summary>Config 模式为 .json，Data 模式为 .record</summary>
    public string DataFileName;
    public string DataFilePath;
    public string ClassFileName;
    public string ClassFilePath;
    /// <summary>不带扩展名的运行时资源路径，以 Data/Json/Runtime/ 开头</summary>
    public string ResourcePath;
    public int DataRowCount;
}

/// <summary>
/// 被跳过的工作表及原因。
/// </summary>
[Serializable]
public sealed class ConfigExportSkip
{
    public string WorkbookPath;
    public string WorkbookName;
    public string WorksheetName;
    public string Reason;
}

/// <summary>
/// 带文件、页签、Excel 行列和字段定位的导表异常。
/// </summary>
public sealed class ConfigExportException : Exception
{
    /// <summary>工作簿绝对路径；批次级错误时可能为空</summary>
    public string WorkbookPath { get; private set; }
    /// <summary>工作表名；批次级错误时可能为空</summary>
    public string WorksheetName { get; private set; }
    /// <summary>Excel 行号，从 1 开始；0 表示无具体行</summary>
    public int Row { get; private set; }
    /// <summary>Excel 列号，从 1 开始；0 表示无具体列</summary>
    public int Column { get; private set; }
    /// <summary>字段名；无字段时为空</summary>
    public string FieldName { get; private set; }

    public ConfigExportException(
        string message,
        string workbookPath,
        string worksheetName,
        int row,
        int column,
        string fieldName,
        Exception innerException = null)
        : base(message, innerException)
    {
        WorkbookPath = workbookPath;
        WorksheetName = worksheetName;
        Row = row;
        Column = column;
        FieldName = fieldName;
    }
}

/// <summary>
/// 配置管理器，用于 Excel 配置表的批量校验、序列化导出和类型索引发布。
/// 所有实现都放在 UNITY_EDITOR 分支内，Player 程序集不会引用 UnityEditor。
/// </summary>
public class ConfigManager
{
    // 新版表头约定：第 1 行 B 列类名，第 2 行 B 列模式，第 3 行备注，第 4 行字段，
    // 第 5 行端标记，第 6 行类型，第 7 行起数据。
    private const int ClassNameRow = 1;
    private const int ModeRow = 2;
    private const int RemarkRow = 3;
    private const int PropertyRow = 4;
    private const int EndFlagRow = 5;
    private const int TypeRow = 6;
    private const int FirstDataRow = 7;
    private const int ClassNameColumn = 2;
    private const int ModeColumn = 2;

    private static readonly HashSet<string> SupportedTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "int", "uint", "long", "ulong", "byte", "sbyte", "short", "ushort",
        "float", "double", "bool", "string",
    };

    private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while",
    };

    private static readonly HashSet<string> ReservedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// 保留原菜单：同时导出 Config 和 Data。两次批处理各自先校验后发布。
    /// </summary>
    [MenuItem("ExcelTool/ExportExcel")]
    private static void ExportConfigsAndModels()
    {
        try
        {
            ConfigExportReport configReport = ExportMenuBatch(ExporterMode.Config, true);
            ConfigExportReport dataReport = ExportMenuBatch(ExporterMode.Data, false);
            ChaosLog.Info(LogChannel.Config, "已导出所有 Config/Data：" + configReport.ToSummary() + "；" + dataReport.ToSummary());
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Config, e.ToString());
        }
    }

    /// <summary>
    /// 保留原菜单：按真实目录导出 Config。
    /// </summary>
    [MenuItem("ExcelTool/ExportExcelConfigs")]
    private static void ExportConfigs()
    {
        try
        {
            ConfigExportReport report = ExportMenuBatch(ExporterMode.Config, true);
            ChaosLog.Info(LogChannel.Config, "已导出所有 Config：" + report.ToSummary());
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Config, e.ToString());
        }
    }

    /// <summary>
    /// 保留原菜单：按真实目录导出 Data（.record）。
    /// </summary>
    [MenuItem("ExcelTool/ExportExcelModels")]
    private static void ExportModels()
    {
        try
        {
            ConfigExportReport report = ExportMenuBatch(ExporterMode.Data, false);
            ChaosLog.Info(LogChannel.Config, "已导出所有 Data：" + report.ToSummary());
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Config, e.ToString());
        }
    }

    /// <summary>
    /// 按真实 Excel 目录执行菜单导出。
    /// </summary>
    private static ConfigExportReport ExportMenuBatch(ExporterMode mode, bool publishTypeIndex)
    {
        FileInfo[] files = FileUtil.LoadFiles(GlobalPath.data_ExcelPath);
        List<string> paths = new List<string>();
        foreach (FileInfo file in files)
        {
            if (string.Equals(file.Extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                paths.Add(file.FullName);
            }
        }

        paths.Sort(StringComparer.OrdinalIgnoreCase);
        if (paths.Count == 0)
        {
            throw Error(null, null, 0, 0, null, "目录「" + GlobalPath.data_ExcelPath + "」下没有 .xlsx 工作簿");
        }

        return ExportExcelBatch(paths, mode, null, null, publishTypeIndex);
    }

    /// <summary>
    /// 批量导出 Excel 配置的公开入口。先解析并校验整批工作簿，确认全部合法后才发布
    /// JSON/record、类文件和可选类型索引；所有失败都会转换为 ConfigExportException。
    /// </summary>
    /// <param name="xlsxAbsolutePaths">绝对路径的 .xlsx 工作簿集合</param>
    /// <param name="exportMode">本批导出模式；页签模式与它不一致时跳过</param>
    /// <param name="jsonOutputDirectory">
    /// 可选的 JSON/record 输出目录，支持绝对路径、Assets/ 开头路径或相对 Assets 的路径；
    /// 为空时 Config 写 GlobalPath.data_JsonPath，Data 写 GlobalPath.data_RecordPath。
    /// </param>
    /// <param name="classOutputDirectory">
    /// 可选的类文件输出目录；为空时 Config 写 GlobalPath.data_ExcelClassPath，
    /// Data 写 GlobalPath.data_ExcelModelPath。
    /// </param>
    /// <param name="publishTypeIndex">
    /// 是否发布类型索引。仅 Config 模式且 JSON 输出目录为真实 Resources/Data/Json/Runtime
    /// 时允许为 true；独立临时目录测试必须传 false。
    /// </param>
    /// <returns>包含产物、跳过页签、警告和类型索引状态的报告</returns>
    /// <exception cref="ConfigExportException">任一工作簿、页签、字段或发布步骤失败</exception>
    public static ConfigExportReport ExportExcelBatch(
        IList<string> xlsxAbsolutePaths,
        ExporterMode exportMode,
        string jsonOutputDirectory = null,
        string classOutputDirectory = null,
        bool publishTypeIndex = true)
    {
        try
        {
            return ExportExcelBatchInternal(
                xlsxAbsolutePaths,
                exportMode,
                jsonOutputDirectory,
                classOutputDirectory,
                publishTypeIndex);
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error(null, null, 0, 0, null, "批量导出失败：" + ex.Message, ex);
        }
    }

    /// <summary>
    /// 批量导出的内部实现。工作表规则：遍历所有有效页签；B1 为类名，B2 为 IsConfig/IsData。
    /// B1 已是完整类名（以 Config/Data 结尾）时不再追加后缀；B2 为空时按 B1 后缀或本批
    /// exportMode 推断。第 3 行备注、第 4 行字段、第 5 行端标记、第 6 行类型、第 7 行起数据保持不变。
    /// </summary>
    private static ConfigExportReport ExportExcelBatchInternal(
        IList<string> xlsxAbsolutePaths,
        ExporterMode exportMode,
        string jsonOutputDirectory,
        string classOutputDirectory,
        bool publishTypeIndex)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (exportMode != ExporterMode.Config && exportMode != ExporterMode.Data)
        {
            throw Error(null, null, 0, 0, null, "不支持的导出模式：" + exportMode);
        }
        if (xlsxAbsolutePaths == null || xlsxAbsolutePaths.Count == 0)
        {
            throw Error(null, null, 0, 0, null, "没有传入任何 .xlsx 工作簿绝对路径");
        }
        if (publishTypeIndex && exportMode != ExporterMode.Config)
        {
            throw Error(null, null, 0, 0, null,
                "Data 模式输出 .record，不在 Resources/Data/Json/Runtime 下，不能发布类型索引；请将 publishTypeIndex 设为 false");
        }

        string defaultJsonPath = exportMode == ExporterMode.Config
            ? GlobalPath.data_JsonPath
            : GlobalPath.data_RecordPath;
        string defaultClassPath = exportMode == ExporterMode.Config
            ? GlobalPath.data_ExcelClassPath
            : GlobalPath.data_ExcelModelPath;
        string jsonDirectory = ResolveOutputDirectory(jsonOutputDirectory, defaultJsonPath);
        string classDirectory = ResolveOutputDirectory(classOutputDirectory, defaultClassPath);

        if (publishTypeIndex)
        {
            string productionJsonDirectory = ResolveOutputDirectory(null, GlobalPath.data_JsonPath);
            if (!PathsEqual(jsonDirectory, productionJsonDirectory))
            {
                throw Error(null, null, 0, 0, null,
                    "发布类型索引时 JSON 输出目录必须是「" + productionJsonDirectory
                    + "」；独立临时目录测试请将 publishTypeIndex 设为 false");
            }
        }

        List<string> workbookPaths = NormalizeWorkbookPaths(xlsxAbsolutePaths);
        List<SheetPlan> plans = new List<SheetPlan>();
        List<ConfigExportSkip> skipped = new List<ConfigExportSkip>();
        List<string> warnings = new List<string>();
        Dictionary<string, SheetPlan> classPlans = new Dictionary<string, SheetPlan>(StringComparer.Ordinal);
        Dictionary<string, SheetPlan> outputPlans = new Dictionary<string, SheetPlan>(StringComparer.OrdinalIgnoreCase);
        int scannedWorksheetCount = 0;

        foreach (string workbookPath in workbookPaths)
        {
            string workbookName = GetWorkbookName(workbookPath);
            try
            {
                using (ExcelPackage package = new ExcelPackage(new FileInfo(workbookPath)))
                {
                    ExcelWorksheets worksheets = package.Workbook.Worksheets;
                    for (int index = 1; index <= worksheets.Count; index++)
                    {
                        scannedWorksheetCount++;
                        ExcelWorksheet worksheet = ExcelWorksheetAccess.GetByIndex(package.Workbook, index);
                        SheetPlan plan = AnalyzeWorksheet(
                            worksheet,
                            workbookPath,
                            workbookName,
                            exportMode,
                            jsonDirectory,
                            classDirectory,
                            skipped,
                            warnings,
                            classPlans,
                            outputPlans);
                        if (plan != null)
                        {
                            plans.Add(plan);
                        }
                    }
                }
            }
            catch (ConfigExportException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw Error(workbookPath, null, 0, 0, null, "读取工作簿失败：" + ex.Message, ex);
            }
        }

        PlannedFile indexFile = null;
        int indexEntryCount = 0;
        string indexFullPath = null;
        if (publishTypeIndex)
        {
            indexFile = BuildIndexPlan(plans, jsonDirectory, out indexEntryCount, out indexFullPath);
        }

        List<PlannedFile> filesToPublish = new List<PlannedFile>();
        foreach (SheetPlan plan in plans)
        {
            filesToPublish.Add(new PlannedFile
            {
                TargetPath = plan.DataFilePath,
                Content = plan.DataContent,
                Description = plan.Mode == ExporterMode.Config ? "配置 Json" : "数据 record",
                WorkbookPath = plan.WorkbookPath,
                WorksheetName = plan.WorksheetName,
            });
        }
        foreach (SheetPlan plan in plans)
        {
            filesToPublish.Add(new PlannedFile
            {
                TargetPath = plan.ClassFilePath,
                Content = plan.ClassContent,
                Description = "配置类文件",
                WorkbookPath = plan.WorkbookPath,
                WorksheetName = plan.WorksheetName,
            });
        }
        if (indexFile != null)
        {
            filesToPublish.Add(indexFile);
        }

        PublishPlannedFiles(filesToPublish);
        RefreshAfterPublish(filesToPublish, publishTypeIndex, indexFullPath);

        ConfigExportReport report = new ConfigExportReport
        {
            Mode = exportMode,
            WorkbookCount = workbookPaths.Count,
            ScannedWorksheetCount = scannedWorksheetCount,
            ExportedWorksheetCount = plans.Count,
            SkippedWorksheetCount = skipped.Count,
            TypeIndexPublished = publishTypeIndex && indexFile != null,
            TypeIndexPath = indexFullPath,
            TypeIndexEntryCount = indexEntryCount,
        };
        foreach (SheetPlan plan in plans)
        {
            report.DataRowCount += plan.DataRowCount;
            report.Artifacts.Add(new ConfigExportArtifact
            {
                WorkbookPath = plan.WorkbookPath,
                WorkbookName = plan.WorkbookName,
                WorksheetName = plan.WorksheetName,
                ClassName = plan.ClassName,
                Mode = plan.Mode,
                DataFileName = plan.DataFileName,
                DataFilePath = plan.DataFilePath,
                ClassFileName = plan.ClassFileName,
                ClassFilePath = plan.ClassFilePath,
                ResourcePath = plan.ResourcePath,
                DataRowCount = plan.DataRowCount,
            });
        }
        report.Skipped.AddRange(skipped);
        report.Warnings.AddRange(warnings);
        stopwatch.Stop();
        report.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        return report;
    }

    /// <summary>
    /// 校验并构建单个工作表的导出计划；空页、说明页和模式不匹配页返回 null 并记录跳过原因。
    /// </summary>
    private static SheetPlan AnalyzeWorksheet(
        ExcelWorksheet worksheet,
        string workbookPath,
        string workbookName,
        ExporterMode requestedMode,
        string jsonDirectory,
        string classDirectory,
        List<ConfigExportSkip> skipped,
        List<string> warnings,
        Dictionary<string, SheetPlan> classPlans,
        Dictionary<string, SheetPlan> outputPlans)
    {
        string worksheetName = worksheet.Name ?? string.Empty;
        ExcelAddressBase dimension = worksheet.Dimension;
        if (dimension == null || dimension.End.Row == 0 || dimension.End.Column == 0)
        {
            AddSkip(skipped, workbookPath, workbookName, worksheetName, "空页");
            return null;
        }

        string rawClassName = TrimmedCell(worksheet, ClassNameRow, ClassNameColumn);
        string rawMode = TrimmedCell(worksheet, ModeRow, ModeColumn);
        ExporterMode? markedMode = ParseModeMarker(rawMode, workbookPath, worksheetName);
        bool hasFieldHeader = HasFieldHeader(worksheet);

        if (rawClassName.Length == 0 && !hasFieldHeader && !markedMode.HasValue)
        {
            AddSkip(skipped, workbookPath, workbookName, worksheetName, "空页或说明页（B1 无类名且第4行无字段）");
            return null;
        }
        if (rawClassName.Length == 0)
        {
            throw Error(workbookPath, worksheetName, ClassNameRow, ClassNameColumn, null,
                "有效页签缺少 B1 类名");
        }
        if (!hasFieldHeader)
        {
            if (markedMode.HasValue)
            {
                throw Error(workbookPath, worksheetName, PropertyRow, 0, null,
                    "B2 已标记 " + (markedMode.Value == ExporterMode.Config ? "IsConfig" : "IsData")
                    + "，但第4行没有字段名");
            }
            AddSkip(skipped, workbookPath, workbookName, worksheetName, "说明页（第4行没有字段名）");
            return null;
        }

        // B1是数据基础名，导出模式只由B2或本次请求决定，不从基础名末尾猜测。
        ExporterMode mode = markedMode ?? requestedMode;
        if (mode != requestedMode)
        {
            AddSkip(skipped, workbookPath, workbookName, worksheetName,
                "页签模式为 " + mode + "，本批导出模式为 " + requestedMode);
            return null;
        }

        string className = BuildClassName(rawClassName, mode, workbookPath, worksheetName);
        List<ColumnPlan> columns = AnalyzeColumns(worksheet, workbookPath, worksheetName, className, warnings);
        int dataRowCount;
        string dataContent = BuildDataContent(worksheet, workbookPath, worksheetName, columns, out dataRowCount);
        string classContent = BuildClassContent(columns, className, mode, workbookName);

        string dataFileName = ConfigExportNaming.DataFileName(workbookName, rawClassName,
            mode == ExporterMode.Config ? ".json" : ".record");
        string classFileName = className + ".cs";
        ValidateFileNamePart(dataFileName, "数据文件名", workbookPath, worksheetName, 0, 0);
        ValidateFileNamePart(classFileName, "类文件名", workbookPath, worksheetName, 0, 0);

        string dataFilePath = Path.Combine(jsonDirectory, dataFileName);
        string classFilePath = Path.Combine(classDirectory, classFileName);
        string resourcePath = BuildResourcePath(workbookName, rawClassName);

        SheetPlan plan = new SheetPlan
        {
            WorkbookPath = workbookPath,
            WorkbookName = workbookName,
            WorksheetName = worksheetName,
            Mode = mode,
            ClassName = className,
            DataFileName = dataFileName,
            DataFilePath = dataFilePath,
            DataContent = dataContent,
            ClassFileName = classFileName,
            ClassFilePath = classFilePath,
            ClassContent = classContent,
            ResourcePath = resourcePath,
            DataRowCount = dataRowCount,
        };

        SheetPlan previous;
        if (classPlans.TryGetValue(className, out previous))
        {
            throw Error(workbookPath, worksheetName, ClassNameRow, ClassNameColumn, className,
                "类名重复：与表「" + Path.GetFileName(previous.WorkbookPath) + "」页签「"
                + previous.WorksheetName + "」使用了同一个完整类名");
        }
        RegisterOutput(outputPlans, dataFilePath, plan);
        RegisterOutput(outputPlans, classFilePath, plan);
        classPlans.Add(className, plan);
        return plan;
    }

    /// <summary>
    /// 读取并校验字段列。完全空白的列跳过；有数据但没有字段名/类型的列必须报错。
    /// </summary>
    private static List<ColumnPlan> AnalyzeColumns(
        ExcelWorksheet worksheet,
        string workbookPath,
        string worksheetName,
        string className,
        List<string> warnings)
    {
        if (worksheet.Dimension == null)
        {
            throw Error(workbookPath, worksheetName, 0, 0, null, "工作表为空，无法读取字段");
        }

        List<ColumnPlan> columns = new List<ColumnPlan>();
        Dictionary<string, ColumnPlan> fieldNames = new Dictionary<string, ColumnPlan>(StringComparer.Ordinal);
        int lastColumn = worksheet.Dimension.End.Column;

        for (int column = 1; column <= lastColumn; column++)
        {
            string fieldName = TrimmedCell(worksheet, PropertyRow, column);
            string fieldType = TrimmedCell(worksheet, TypeRow, column);
            string endFlag = TrimmedCell(worksheet, EndFlagRow, column);
            string remark = CellText(worksheet, RemarkRow, column);
            bool hasData = ColumnHasData(worksheet, column);

            if (fieldName.Length == 0 && fieldType.Length == 0 && endFlag.Length == 0 && !hasData)
            {
                continue;
            }
            if (fieldName.Length == 0)
            {
                throw Error(workbookPath, worksheetName, PropertyRow, column, null,
                    "字段名为空（该列存在类型、端标记或数据）");
            }
            if (fieldType.Length == 0)
            {
                throw Error(workbookPath, worksheetName, TypeRow, column, fieldName,
                    "类型为空");
            }

            bool inClass = !string.Equals(endFlag, "server", StringComparison.OrdinalIgnoreCase);
            if (!inClass)
            {
                continue;
            }
            bool inJson = endFlag.Length > 0;

            if (!SupportedTypes.Contains(fieldType))
            {
                throw Error(workbookPath, worksheetName, TypeRow, column, fieldName,
                    "不支持此类型「" + fieldType + "」，可用类型：int/uint/long/ulong/byte/sbyte/short/ushort/float/double/bool/string");
            }
            if (!IsValidIdentifier(fieldName))
            {
                throw Error(workbookPath, worksheetName, PropertyRow, column, fieldName,
                    "字段名不是合法的 C# 标识符");
            }
            if (string.Equals(fieldName, className, StringComparison.Ordinal))
            {
                throw Error(workbookPath, worksheetName, PropertyRow, column, fieldName,
                    "字段名不能与类名相同");
            }

            ColumnPlan previous;
            if (fieldNames.TryGetValue(fieldName, out previous))
            {
                throw Error(workbookPath, worksheetName, PropertyRow, column, fieldName,
                    "字段名重复：与第 " + previous.Column + " 列使用了同一个字段名");
            }

            if (endFlag.Length > 0
                && !string.Equals(endFlag, "client", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("表「" + Path.GetFileName(workbookPath) + "」页签「" + worksheetName
                    + "」第" + EndFlagRow + "行第" + column + "列端标记「" + endFlag
                    + "」不是 server/client，按客户端字段处理");
            }

            ColumnPlan columnPlan = new ColumnPlan
            {
                Column = column,
                FieldName = fieldName,
                Type = fieldType,
                EndFlag = endFlag,
                Remark = remark,
                InClass = inClass,
                InJson = inJson,
            };
            fieldNames.Add(fieldName, columnPlan);
            columns.Add(columnPlan);
        }

        if (columns.Count == 0)
        {
            throw Error(workbookPath, worksheetName, PropertyRow, 0, null,
                "没有可导出字段（所有列都是 server 或空列）");
        }

        bool hasJsonColumn = false;
        foreach (ColumnPlan column in columns)
        {
            if (column.InJson)
            {
                hasJsonColumn = true;
                break;
            }
        }
        if (!hasJsonColumn)
        {
            throw Error(workbookPath, worksheetName, EndFlagRow, 0, null,
                "没有客户端字段：第" + EndFlagRow + "行端标记为空或 server，无法生成 Json/record");
        }

        return columns;
    }

    /// <summary>
    /// 构建带 datas 容器的 Json/record 内容，并在转换每个值时保留精确单元格定位。
    /// </summary>
    private static string BuildDataContent(
        ExcelWorksheet worksheet,
        string workbookPath,
        string worksheetName,
        List<ColumnPlan> columns,
        out int dataRowCount)
    {
        List<ColumnPlan> jsonColumns = new List<ColumnPlan>();
        foreach (ColumnPlan column in columns)
        {
            if (column.InJson)
            {
                jsonColumns.Add(column);
            }
        }

        List<List<string>> rows = new List<List<string>>();
        if (worksheet.Dimension != null)
        {
            int lastRow = worksheet.Dimension.End.Row;
            for (int row = FirstDataRow; row <= lastRow; row++)
            {
                bool hasValue = false;
                foreach (ColumnPlan column in jsonColumns)
                {
                    if (TrimmedCell(worksheet, row, column.Column).Length > 0)
                    {
                        hasValue = true;
                        break;
                    }
                }
                if (!hasValue)
                {
                    continue;
                }

                List<string> values = new List<string>(jsonColumns.Count);
                foreach (ColumnPlan column in jsonColumns)
                {
                    string rawValue = CellText(worksheet, row, column.Column);
                    try
                    {
                        values.Add(EncodeJsonValue(column.Type, rawValue));
                    }
                    catch (ConfigExportException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw Error(workbookPath, worksheetName, row, column.Column, column.FieldName,
                            "JSON 值转换失败：" + ex.Message, ex);
                    }
                }
                rows.Add(values);
            }
        }

        dataRowCount = rows.Count;
        StringBuilder builder = new StringBuilder();
        builder.Append("{\n    \"datas\": [");
        if (rows.Count == 0)
        {
            builder.Append("]\n}");
            return builder.ToString();
        }

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            builder.Append("\n        {");
            for (int columnIndex = 0; columnIndex < jsonColumns.Count; columnIndex++)
            {
                if (columnIndex > 0)
                {
                    builder.Append(',');
                }
                builder.Append("\n            ");
                builder.Append(EscapeJson(jsonColumns[columnIndex].FieldName));
                builder.Append(": ");
                builder.Append(rows[rowIndex][columnIndex]);
            }
            builder.Append("\n        }");
            if (rowIndex < rows.Count - 1)
            {
                builder.Append(',');
            }
        }
        builder.Append("\n    ]\n}");
        return builder.ToString();
    }

    /// <summary>
    /// 生成类文件内容。完整类名已含英文表名与模式后缀，脚本文件名与类名保持一致。
    /// </summary>
    private static string BuildClassContent(
        List<ColumnPlan> columns,
        string className,
        ExporterMode mode,
        string workbookName)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("using System;\n\n");
        builder.Append("[Serializable]\n");
        if (mode == ExporterMode.Data)
        {
            builder.Append("//用于持久化的数据类，来源工作簿：").Append(workbookName).Append("\n");
        }
        builder.Append("public class ").Append(className).Append("\n{\n");
        foreach (ColumnPlan column in columns)
        {
            string remark = SanitizeComment(column.Remark);
            if (remark.Length > 0)
            {
                builder.Append("    /// <summary>\n");
                builder.Append("    /// ").Append(remark).Append("\n");
                builder.Append("    /// </summary>\n");
            }
            builder.Append("    public ").Append(column.Type).Append(' ')
                .Append(column.FieldName).Append(";\n");
        }
        builder.Append("}\n");
        return builder.ToString();
    }

    /// <summary>
    /// 把单元格文本按第 6 行类型转换成可直接拼进 Json 的片段。
    /// </summary>
    private static string EncodeJsonValue(string type, string rawValue)
    {
        string value = rawValue ?? string.Empty;
        switch (type)
        {
            case "string":
                return EscapeJson(value);
            case "bool":
                {
                    string normalized = value.Trim().ToLowerInvariant();
                    if (normalized == "true" || normalized == "1" || normalized == "是")
                    {
                        return "true";
                    }
                    if (normalized == "false" || normalized == "0" || normalized == "否")
                    {
                        return "false";
                    }
                    throw new FormatException("bool 取值「" + value + "」非法，应为 true/false、1/0 或 是/否");
                }
            case "int":
                {
                    int parsed;
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "uint":
                {
                    uint parsed;
                    if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "long":
                {
                    long parsed;
                    if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "ulong":
                {
                    ulong parsed;
                    if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "byte":
                {
                    byte parsed;
                    if (!byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "sbyte":
                {
                    sbyte parsed;
                    if (!sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "short":
                {
                    short parsed;
                    if (!short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "ushort":
                {
                    ushort parsed;
                    if (!ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString(CultureInfo.InvariantCulture);
                }
            case "float":
                {
                    float parsed;
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                        || float.IsNaN(parsed) || float.IsInfinity(parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString("R", CultureInfo.InvariantCulture);
                }
            case "double":
                {
                    double parsed;
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                        || double.IsNaN(parsed) || double.IsInfinity(parsed))
                    {
                        throw NumberError(type, value);
                    }
                    return parsed.ToString("R", CultureInfo.InvariantCulture);
                }
            default:
                throw new FormatException("不支持此类型: " + type);
        }
    }

    /// <summary>
    /// 把字符串转成 JSON 字符串字面量，处理引号、反斜杠、控制字符和代理项。
    /// </summary>
    private static string EscapeJson(string text)
    {
        if (text == null)
        {
            return "\"\"";
        }

        StringBuilder builder = new StringBuilder(text.Length + 8);
        builder.Append('"');
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            switch (current)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (current < ' ')
                    {
                        AppendUnicodeEscape(builder, current);
                    }
                    else if (char.IsHighSurrogate(current))
                    {
                        if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                        {
                            throw new FormatException("字符串包含孤立的高代理项，无法生成合法 JSON");
                        }
                        builder.Append(current);
                        builder.Append(text[++index]);
                    }
                    else if (char.IsLowSurrogate(current))
                    {
                        throw new FormatException("字符串包含孤立的低代理项，无法生成合法 JSON");
                    }
                    else if (current == '\u2028' || current == '\u2029')
                    {
                        AppendUnicodeEscape(builder, current);
                    }
                    else
                    {
                        builder.Append(current);
                    }
                    break;
            }
        }
        builder.Append('"');
        return builder.ToString();
    }

    private static void AppendUnicodeEscape(StringBuilder builder, char value)
    {
        builder.Append("\\u").Append(((int)value).ToString("x4", CultureInfo.InvariantCulture));
    }

    private static FormatException NumberError(string type, string value)
    {
        return new FormatException(type + " 取值「" + value + "」不是合法数值或超出目标类型范围");
    }

    /// <summary>
    /// 构建并合并类型索引。本批覆盖的同类名条目原位替换，未覆盖的既有条目原样保留。
    /// </summary>
    private static PlannedFile BuildIndexPlan(
        List<SheetPlan> plans,
        string jsonDirectory,
        out int entryCount,
        out string indexFullPath)
    {
        string indexResourcePath = ConfigTableAssets.IndexResourcePath;
        if (string.IsNullOrWhiteSpace(indexResourcePath)
            || !indexResourcePath.StartsWith(GlobalPath.data_JsonPathToRead, StringComparison.Ordinal))
        {
            throw Error(null, null, 0, 0, null,
                "ConfigTableAssets.IndexResourcePath 非法：" + indexResourcePath);
        }

        string indexFileName = Path.GetFileName(indexResourcePath) + ".json";
        indexFullPath = Path.Combine(jsonDirectory, indexFileName);
        ConfigTableAssets.Index index = new ConfigTableAssets.Index();

        if (File.Exists(indexFullPath))
        {
            string text;
            try
            {
                text = File.ReadAllText(indexFullPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw Error(null, null, 0, 0, null,
                    "读取类型索引失败：" + indexFullPath + "；" + ex.Message, ex);
            }

            try
            {
                index = JsonUtility.FromJson<ConfigTableAssets.Index>(text);
            }
            catch (Exception ex)
            {
                throw Error(null, null, 0, 0, null,
                    "类型索引解析失败：" + indexFullPath + "；" + ex.Message, ex);
            }
            if (index == null)
            {
                throw Error(null, null, 0, 0, null, "类型索引解析结果为空：" + indexFullPath);
            }
            if (index.schemaVersion != 1)
            {
                throw Error(null, null, 0, 0, null,
                    "类型索引版本不受支持：" + index.schemaVersion + "，路径：" + indexFullPath);
            }
            if (index.tables == null)
            {
                index.tables = new List<ConfigTableAssets.Entry>();
            }
        }

        HashSet<string> oldClassNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConfigTableAssets.Entry oldEntry in index.tables)
        {
            ValidateIndexEntry(oldEntry, indexFullPath);
            if (!oldClassNames.Add(oldEntry.className))
            {
                throw Error(null, null, 0, 0, null,
                    "既有类型索引含重复类型「" + oldEntry.className + "」：" + indexFullPath);
            }
        }

        List<ConfigTableAssets.Entry> newEntries = new List<ConfigTableAssets.Entry>();
        Dictionary<string, ConfigTableAssets.Entry> newEntryByClass =
            new Dictionary<string, ConfigTableAssets.Entry>(StringComparer.Ordinal);
        foreach (SheetPlan plan in plans)
        {
            if (plan.Mode != ExporterMode.Config)
            {
                continue;
            }

            ConfigTableAssets.Entry entry = new ConfigTableAssets.Entry
            {
                className = plan.ClassName,
                resourcePath = plan.ResourcePath,
                workbook = plan.WorkbookName,
                worksheet = plan.WorksheetName,
            };
            ValidateIndexEntry(entry, indexFullPath);
            if (newEntryByClass.ContainsKey(entry.className))
            {
                throw Error(plan.WorkbookPath, plan.WorksheetName, ClassNameRow, ClassNameColumn,
                    plan.ClassName, "本批类型索引出现重复类型");
            }
            newEntryByClass.Add(entry.className, entry);
            newEntries.Add(entry);
        }

        List<ConfigTableAssets.Entry> merged = new List<ConfigTableAssets.Entry>();
        foreach (ConfigTableAssets.Entry oldEntry in index.tables)
        {
            ConfigTableAssets.Entry replacement;
            if (newEntryByClass.TryGetValue(oldEntry.className, out replacement))
            {
                merged.Add(replacement);
                newEntryByClass.Remove(oldEntry.className);
            }
            else
            {
                merged.Add(oldEntry);
            }
        }
        foreach (ConfigTableAssets.Entry entry in newEntries)
        {
            if (newEntryByClass.ContainsKey(entry.className))
            {
                merged.Add(entry);
                newEntryByClass.Remove(entry.className);
            }
        }

        HashSet<string> classNames = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> resourcePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConfigTableAssets.Entry entry in merged)
        {
            ValidateIndexEntry(entry, indexFullPath);
            if (!classNames.Add(entry.className))
            {
                throw Error(null, null, 0, 0, null,
                    "类型索引含重复类型「" + entry.className + "」：" + indexFullPath);
            }
            if (!resourcePaths.Add(entry.resourcePath))
            {
                throw Error(null, null, 0, 0, null,
                    "类型索引含重复资源路径「" + entry.resourcePath + "」：" + indexFullPath);
            }
        }

        index = new ConfigTableAssets.Index
        {
            schemaVersion = 1,
            tables = merged,
        };
        string content = JsonUtility.ToJson(index, true);
        if (string.IsNullOrEmpty(content))
        {
            throw Error(null, null, 0, 0, null, "类型索引序列化结果为空：" + indexFullPath);
        }

        entryCount = merged.Count;
        return new PlannedFile
        {
            TargetPath = indexFullPath,
            Content = content + "\n",
            Description = "类型索引",
        };
    }

    /// <summary>
    /// 校验一条类型索引记录，确保运行时 ConfigTableAssets 能直接使用。
    /// </summary>
    private static void ValidateIndexEntry(ConfigTableAssets.Entry entry, string indexFilePath)
    {
        if (entry == null)
        {
            throw Error(null, null, 0, 0, null, "类型索引含空条目：" + indexFilePath);
        }
        if (string.IsNullOrWhiteSpace(entry.className) || !IsValidIdentifier(entry.className))
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 className 非法「" + entry.className + "」：" + indexFilePath);
        }
        if (string.IsNullOrWhiteSpace(entry.resourcePath))
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 resourcePath 为空：" + indexFilePath);
        }
        if (!entry.resourcePath.StartsWith(GlobalPath.data_JsonPathToRead, StringComparison.Ordinal))
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 resourcePath 必须以「" + GlobalPath.data_JsonPathToRead
                + "」开头：「" + entry.resourcePath + "」：" + indexFilePath);
        }
        if (entry.resourcePath.Length <= GlobalPath.data_JsonPathToRead.Length)
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 resourcePath 缺少文件名：「" + entry.resourcePath + "」：" + indexFilePath);
        }
        if (entry.resourcePath.IndexOf(".json", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 resourcePath 不能带 .json：「" + entry.resourcePath + "」：" + indexFilePath);
        }
        if (entry.resourcePath.IndexOf('\\') >= 0
            || entry.resourcePath.IndexOf("..", StringComparison.Ordinal) >= 0
            || entry.resourcePath.IndexOf("//", StringComparison.Ordinal) >= 0
            || entry.resourcePath.EndsWith("/", StringComparison.Ordinal))
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 resourcePath 含非法路径片段：「" + entry.resourcePath + "」：" + indexFilePath);
        }
        if (string.IsNullOrWhiteSpace(entry.workbook))
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 workbook 为空：" + indexFilePath);
        }
        if (string.IsNullOrWhiteSpace(entry.worksheet))
        {
            throw Error(null, null, 0, 0, null,
                "类型索引 worksheet 为空：" + indexFilePath);
        }
    }

    /// <summary>
    /// 发布计划内的产物。先全部写暂存文件，再逐个替换；任一步失败都恢复既有文件。
    /// </summary>
    private static void PublishPlannedFiles(List<PlannedFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return;
        }

        HashSet<string> targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (PlannedFile file in files)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(file.TargetPath);
            }
            catch (Exception ex)
            {
                throw Error(file.WorkbookPath, file.WorksheetName, 0, 0, null,
                    "发布目标路径非法：" + file.TargetPath + "；" + ex.Message, ex);
            }
            if (!targets.Add(fullPath))
            {
                throw Error(file.WorkbookPath, file.WorksheetName, 0, 0, null,
                    "发布目标重复：" + fullPath);
            }
            file.TargetPath = fullPath;
        }

        string backupRoot = Path.Combine(
            Path.GetTempPath(),
            "ChaosConfigExport",
            "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(backupRoot);

        List<StagedFile> stagedFiles = new List<StagedFile>();
        try
        {
            foreach (PlannedFile file in files)
            {
                // 内容完全一致时不触碰目标，避免无意义的资源重导入与文件替换冲突。
                if (HasIdenticalOutput(file)) continue;
                string directory = Path.GetDirectoryName(file.TargetPath);
                if (string.IsNullOrEmpty(directory))
                {
                    throw new InvalidOperationException("无法确定目标目录：" + file.TargetPath);
                }
                Directory.CreateDirectory(directory);
                string stagePath = file.TargetPath + ".chaos_export_" + Guid.NewGuid().ToString("N") + ".tmp";
                stagedFiles.Add(new StagedFile
                {
                    File = file,
                    StagePath = stagePath,
                });
                File.WriteAllText(stagePath, file.Content ?? string.Empty, new UTF8Encoding(false));
            }
        }
        catch (Exception ex)
        {
            CleanupStagedFiles(stagedFiles);
            throw Error(null, null, 0, 0, null,
                "导出暂存失败，未发布任何文件；备份目录「" + backupRoot + "」；" + ex.Message, ex);
        }

        List<CommittedFile> committedFiles = new List<CommittedFile>();
        try
        {
            for (int index = 0; index < stagedFiles.Count; index++)
            {
                StagedFile staged = stagedFiles[index];
                bool existed = File.Exists(staged.File.TargetPath);
                string backupPath = null;
                if (existed)
                {
                    backupPath = Path.Combine(
                        backupRoot,
                        index.ToString(CultureInfo.InvariantCulture) + "_"
                        + Path.GetFileName(staged.File.TargetPath) + ".bak");
                    File.Copy(staged.File.TargetPath, backupPath, true);
                }

                if (existed)
                {
                    File.Replace(staged.StagePath, staged.File.TargetPath, null);
                }
                else
                {
                    File.Move(staged.StagePath, staged.File.TargetPath);
                }

                committedFiles.Add(new CommittedFile
                {
                    File = staged.File,
                    Existed = existed,
                    BackupPath = backupPath,
                });
            }
        }
        catch (Exception ex)
        {
            List<string> rollbackErrors = RollbackCommittedFiles(committedFiles);
            string detail = "导出发布失败，已回滚 " + committedFiles.Count + " 个已发布文件；备份目录「" + backupRoot + "」";
            if (rollbackErrors.Count > 0)
            {
                detail += "；回滚异常：" + string.Join("；", rollbackErrors.ToArray());
            }
            detail += "；原始错误：" + ex.Message;
            throw Error(null, null, 0, 0, null, detail, ex);
        }
        finally { CleanupStagedFiles(stagedFiles); }
    }

    /// <summary>按实际输出编码比较字节；无法读取时继续原发布流程，让其报告真实的访问失败。</summary>
    private static bool HasIdenticalOutput(PlannedFile file)
    {
        if (!File.Exists(file.TargetPath)) return false;
        try
        {
            byte[] existing=File.ReadAllBytes(file.TargetPath);
            byte[] expected=new UTF8Encoding(false).GetBytes(file.Content??string.Empty);
            if(existing.Length!=expected.Length)return false;
            for(int i=0;i<existing.Length;i++)if(existing[i]!=expected[i])return false;
            return true;
        }
        catch(IOException){return false;}
        catch(UnauthorizedAccessException){return false;}
    }

    /// <summary>只清理本次生成并登记的暂存文件，不动目标产物与回滚备份。</summary>
    private static void CleanupStagedFiles(List<StagedFile> staged)
    {
        foreach(var file in staged)
        {
            try
            {
                if(File.Exists(file.StagePath))File.Delete(file.StagePath);
                if(File.Exists(file.StagePath+".meta"))File.Delete(file.StagePath+".meta");
            }
            catch(Exception ex){Debug.LogWarning("导出暂存文件清理失败："+file.StagePath+"；"+ex.Message);}
        }
    }

    /// <summary>
    /// 回滚已提交文件：既有文件从备份恢复；本批新文件删除。不会删除或替换任何既有旧产物。
    /// </summary>
    private static List<string> RollbackCommittedFiles(List<CommittedFile> committedFiles)
    {
        List<string> errors = new List<string>();
        for (int index = committedFiles.Count - 1; index >= 0; index--)
        {
            CommittedFile committed = committedFiles[index];
            try
            {
                if (committed.Existed)
                {
                    if (string.IsNullOrEmpty(committed.BackupPath) || !File.Exists(committed.BackupPath))
                    {
                        errors.Add("缺少备份，无法恢复：" + committed.File.TargetPath);
                    }
                    else
                    {
                        File.Copy(committed.BackupPath, committed.File.TargetPath, true);
                    }
                }
                else if (File.Exists(committed.File.TargetPath))
                {
                    File.Delete(committed.File.TargetPath);
                }
            }
            catch (Exception ex)
            {
                errors.Add(committed.File.TargetPath + "：" + ex.Message);
            }
        }
        return errors;
    }

    /// <summary>
    /// 发布后刷新 AssetDatabase，并在发布索引时显式 Reload 运行时索引缓存。
    /// </summary>
    private static void RefreshAfterPublish(List<PlannedFile> files, bool publishTypeIndex, string indexFullPath)
    {
        bool hasAssetFile = false;
        foreach (PlannedFile file in files)
        {
            if (IsUnderAssets(file.TargetPath))
            {
                hasAssetFile = true;
                break;
            }
        }
        if (hasAssetFile)
        {
            AssetDatabase.Refresh();
        }
        if (publishTypeIndex && !string.IsNullOrEmpty(indexFullPath))
        {
            string indexAssetPath = AbsoluteToAssetPath(indexFullPath);
            AssetDatabase.ImportAsset(indexAssetPath, ImportAssetOptions.ForceUpdate);
        }
        ConfigTableAssets.Reload();
    }

    /// <summary>
    /// 保留旧工具的单表调用入口，文件与类名统一使用当前英文命名规则。
    /// </summary>
    private static void ExportJson(ExcelWorksheet workSheet, string fileName, ExporterMode mode)
    {
        try
        {
            string baseName=TrimmedCell(workSheet,ClassNameRow,ClassNameColumn);
            string className = BuildClassName(baseName, mode, fileName, workSheet.Name);
            List<ColumnPlan> columns = AnalyzeColumns(workSheet, fileName, workSheet.Name, className, new List<string>());
            int dataRowCount;
            string content = BuildDataContent(workSheet, fileName, workSheet.Name, columns, out dataRowCount);
            string outputDirectory = ResolveOutputDirectory(
                null,
                mode == ExporterMode.Config ? GlobalPath.data_JsonPath : GlobalPath.data_RecordPath);
            string oldFileName = ConfigExportNaming.DataFileName(fileName,baseName,
                mode == ExporterMode.Config ? ".json" : ".record");
            ValidateFileNamePart(oldFileName, "旧版数据文件名", fileName, workSheet.Name, 0, 0);

            List<PlannedFile> files = new List<PlannedFile>
            {
                new PlannedFile
                {
                    TargetPath = Path.Combine(outputDirectory, oldFileName),
                    Content = content,
                    Description = "旧版单表数据",
                    WorkbookPath = fileName,
                    WorksheetName = workSheet.Name,
                },
            };
            PublishPlannedFiles(files);
            RefreshAfterPublish(files, false, null);
            ChaosLog.Info(LogChannel.Config, "旧版单表数据已导出：" + oldFileName);
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error(fileName, workSheet.Name, 0, 0, null, "旧版单表数据导出失败：" + ex.Message, ex);
        }
    }

    /// <summary>
    /// 保留旧工具的单表类调用入口，脚本文件名始终与当前完整类名一致。
    /// </summary>
    private static void ExportClass(ExcelWorksheet workSheet, string fileName, ExporterMode mode)
    {
        try
        {
            string baseName=TrimmedCell(workSheet,ClassNameRow,ClassNameColumn);
            string className = BuildClassName(baseName, mode, fileName, workSheet.Name);
            List<ColumnPlan> columns = AnalyzeColumns(workSheet, fileName, workSheet.Name, className, new List<string>());
            string content = BuildClassContent(columns, className, mode, fileName);
            string outputDirectory = ResolveOutputDirectory(
                null,
                mode == ExporterMode.Config ? GlobalPath.data_ExcelClassPath : GlobalPath.data_ExcelModelPath);
            string oldFileName = className + ".cs";
            ValidateFileNamePart(oldFileName, "旧版类文件名", fileName, workSheet.Name, 0, 0);

            List<PlannedFile> files = new List<PlannedFile>
            {
                new PlannedFile
                {
                    TargetPath = Path.Combine(outputDirectory, oldFileName),
                    Content = content,
                    Description = "旧版单表类文件",
                    WorkbookPath = fileName,
                    WorksheetName = workSheet.Name,
                },
            };
            PublishPlannedFiles(files);
            RefreshAfterPublish(files, false, null);
            ChaosLog.Info(LogChannel.Config, "旧版单表类已导出：" + oldFileName);
        }
        catch (ConfigExportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error(fileName, workSheet.Name, 0, 0, null, "旧版单表类导出失败：" + ex.Message, ex);
        }
    }

    /// <summary>
    /// 解析并校验工作簿绝对路径集合，拒绝相对路径、非 .xlsx 和重复输入。
    /// </summary>
    private static List<string> NormalizeWorkbookPaths(IList<string> xlsxAbsolutePaths)
    {
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string rawPath in xlsxAbsolutePaths)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                throw Error(null, null, 0, 0, null, "工作簿路径为空");
            }
            if (!Path.IsPathRooted(rawPath))
            {
                throw Error(null, null, 0, 0, null, "工作簿路径必须是绝对路径：" + rawPath);
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(rawPath);
            }
            catch (Exception ex)
            {
                throw Error(null, null, 0, 0, null, "工作簿路径非法：" + rawPath + "；" + ex.Message, ex);
            }

            if (!string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw Error(fullPath, null, 0, 0, null, "工作簿扩展名必须是 .xlsx");
            }
            if (!File.Exists(fullPath))
            {
                throw Error(fullPath, null, 0, 0, null, "工作簿不存在");
            }
            if (!seen.Add(fullPath))
            {
                throw Error(fullPath, null, 0, 0, null, "同一工作簿被重复传入");
            }
            result.Add(fullPath);
        }
        return result;
    }

    /// <summary>
    /// 解析输出目录；空值使用 GlobalPath 默认目录，相对路径优先按项目根目录/Assets 解析。
    /// </summary>
    private static string ResolveOutputDirectory(string supplied, string defaultRelativePath)
    {
        try
        {
            string path;
            if (string.IsNullOrWhiteSpace(supplied))
            {
                path = Path.Combine(Application.dataPath, defaultRelativePath);
            }
            else
            {
                string raw = supplied.Trim();
                if (Path.IsPathRooted(raw))
                {
                    path = raw;
                }
                else
                {
                    string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    if (raw.Equals("Assets", StringComparison.OrdinalIgnoreCase)
                        || raw.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                        || raw.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
                    {
                        path = Path.Combine(projectRoot, raw);
                    }
                    else
                    {
                        path = Path.Combine(Application.dataPath, raw);
                    }
                }
            }

            string fullPath = Path.GetFullPath(path);
            string root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrEmpty(root) && fullPath.Length > root.Length)
            {
                fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            return fullPath;
        }
        catch (Exception ex)
        {
            throw Error(null, null, 0, 0, null,
                "输出目录无效：" + (supplied ?? defaultRelativePath) + "；" + ex.Message, ex);
        }
    }

    private static string GetWorkbookName(string workbookPath)
    {
        string name = Path.GetFileNameWithoutExtension(workbookPath);
        ValidateFileNamePart(name, "工作簿名", workbookPath, null, 0, 0);
        try { ConfigExportNaming.WorkbookKey(workbookPath); }
        catch(ArgumentException ex){throw Error(workbookPath,null,0,0,null,"工作簿名缺少合法英文表名："+ex.Message,ex);}
        return name;
    }

    /// <summary>
    /// 生成完整类名：英文表名、B1基础名和导出模式后缀共同构成脚本及类名。
    /// </summary>
    private static string BuildClassName(
        string rawClassName,
        ExporterMode mode,
        string workbookPath,
        string worksheetName)
    {
        string baseName;
        try { baseName=ConfigExportNaming.ClassName(rawClassName); }
        catch(ArgumentException ex){throw Error(workbookPath,worksheetName,ClassNameRow,ClassNameColumn,rawClassName,"B1基础名必须是合法的 C# 标识符，且仅允许ASCII英文字母、数字和下划线："+ex.Message,ex);}
        if(!IsValidIdentifier(baseName)||ReservedFileNames.Contains(baseName))
            throw Error(workbookPath,worksheetName,ClassNameRow,ClassNameColumn,baseName,"B1基础名不是可用的英文C#标识符");
        string className = ConfigExportNaming.GeneratedClassName(workbookPath,baseName,mode.ToString());
        if (!IsValidIdentifier(className))
        {
            throw Error(workbookPath, worksheetName, ClassNameRow, ClassNameColumn, className,
                "类名不是合法的 C# 标识符");
        }
        if (ReservedFileNames.Contains(className))
        {
            throw Error(workbookPath, worksheetName, ClassNameRow, ClassNameColumn, className,
                "类名是 Windows 保留文件名，无法生成类文件");
        }
        return className;
    }

    private static ExporterMode? ParseModeMarker(string rawMode, string workbookPath, string worksheetName)
    {
        if (rawMode.Length == 0)
        {
            return null;
        }
        if (string.Equals(rawMode, "IsConfig", StringComparison.OrdinalIgnoreCase))
        {
            return ExporterMode.Config;
        }
        if (string.Equals(rawMode, "IsData", StringComparison.OrdinalIgnoreCase))
        {
            return ExporterMode.Data;
        }
        throw Error(workbookPath, worksheetName, ModeRow, ModeColumn, null,
            "B2 模式标记「" + rawMode + "」无法识别，应为 IsConfig 或 IsData");
    }

    private static bool HasFieldHeader(ExcelWorksheet worksheet)
    {
        if (worksheet.Dimension == null)
        {
            return false;
        }
        int lastColumn = worksheet.Dimension.End.Column;
        for (int column = 1; column <= lastColumn; column++)
        {
            if (TrimmedCell(worksheet, PropertyRow, column).Length > 0)
            {
                return true;
            }
        }
        return false;
    }

    private static bool ColumnHasData(ExcelWorksheet worksheet, int column)
    {
        if (worksheet.Dimension == null)
        {
            return false;
        }
        int lastRow = worksheet.Dimension.End.Row;
        for (int row = FirstDataRow; row <= lastRow; row++)
        {
            if (TrimmedCell(worksheet, row, column).Length > 0)
            {
                return true;
            }
        }
        return false;
    }

    private static void RegisterOutput(
        Dictionary<string, SheetPlan> outputPlans,
        string path,
        SheetPlan plan)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            throw Error(plan.WorkbookPath, plan.WorksheetName, 0, 0, null,
                "输出路径非法：" + path + "；" + ex.Message, ex);
        }

        SheetPlan previous;
        if (outputPlans.TryGetValue(fullPath, out previous))
        {
            throw Error(plan.WorkbookPath, plan.WorksheetName, ClassNameRow, ClassNameColumn, plan.ClassName,
                "输出路径重复：与表「" + Path.GetFileName(previous.WorkbookPath) + "」页签「"
                + previous.WorksheetName + "」生成同一文件「" + fullPath + "」");
        }
        outputPlans.Add(fullPath, plan);
    }

    private static string BuildResourcePath(string workbookName, string className)
    {
        string prefix = GlobalPath.data_JsonPathToRead;
        if (!prefix.EndsWith("/", StringComparison.Ordinal))
        {
            prefix += "/";
        }
        return prefix + ConfigExportNaming.WorkbookKey(workbookName) + "_" + ConfigExportNaming.ClassName(className);
    }

    private static void AddSkip(
        List<ConfigExportSkip> skipped,
        string workbookPath,
        string workbookName,
        string worksheetName,
        string reason)
    {
        skipped.Add(new ConfigExportSkip
        {
            WorkbookPath = workbookPath,
            WorkbookName = workbookName,
            WorksheetName = worksheetName,
            Reason = reason,
        });
    }

    private static string CellText(ExcelWorksheet worksheet, int row, int column)
    {
        string text = worksheet.Cells[row, column].Text;
        return text ?? string.Empty;
    }

    private static string TrimmedCell(ExcelWorksheet worksheet, int row, int column)
    {
        return CellText(worksheet, row, column).Trim();
    }

    private static string SanitizeComment(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        StringBuilder builder = new StringBuilder(text.Length);
        foreach (char current in text)
        {
            if (current == '\r' || current == '\n' || current == '\u2028' || current == '\u2029'
                || char.IsControl(current))
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append(current);
            }
        }
        return builder.ToString().Trim();
    }

    private static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value) || CSharpKeywords.Contains(value))
        {
            return false;
        }
        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }
        for (int index = 1; index < value.Length; index++)
        {
            char current = value[index];
            if (!(char.IsLetterOrDigit(current) || current == '_'))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 校验工作簿名、数据文件名和类文件名中的非法字符。
    /// </summary>
    private static void ValidateFileNamePart(
        string name,
        string label,
        string workbookPath,
        string worksheetName,
        int row,
        int column)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Error(workbookPath, worksheetName, row, column, null, label + "为空");
        }
        if (name != name.Trim())
        {
            throw Error(workbookPath, worksheetName, row, column, null, label + "首尾不能有空白");
        }
        if (name == "." || name == "..")
        {
            throw Error(workbookPath, worksheetName, row, column, null, label + "不能是 . 或 ..");
        }
        if (name.IndexOf("..", StringComparison.Ordinal) >= 0)
        {
            throw Error(workbookPath, worksheetName, row, column, null, label + "不能包含 ..");
        }
        if (name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal))
        {
            throw Error(workbookPath, worksheetName, row, column, null, label + "不能以点或空格结尾");
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char current in name)
        {
            if (char.IsControl(current) || Array.IndexOf(invalidChars, current) >= 0)
            {
                throw Error(workbookPath, worksheetName, row, column, null,
                    label + "包含非法文件名字符：「" + current + "」");
            }
        }

        string stem = name;
        int dotIndex = name.IndexOf('.');
        if (dotIndex >= 0)
        {
            stem = name.Substring(0, dotIndex);
        }
        if (ReservedFileNames.Contains(stem))
        {
            throw Error(workbookPath, worksheetName, row, column, null,
                label + "使用了 Windows 保留文件名：「" + stem + "」");
        }
    }

    private static bool IsUnderAssets(string absolutePath)
    {
        string assetsRoot = Path.GetFullPath(Application.dataPath);
        string fullPath = Path.GetFullPath(absolutePath);
        string prefix = assetsRoot + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string AbsoluteToAssetPath(string absolutePath)
    {
        string assetsRoot = Path.GetFullPath(Application.dataPath);
        string fullPath = Path.GetFullPath(absolutePath);
        string prefix = assetsRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw Error(null, null, 0, 0, null,
                "路径不在 Assets 下，无法导入 AssetDatabase：" + absolutePath);
        }
        return "Assets/" + fullPath.Substring(prefix.Length).Replace('\\', '/');
    }

    private static bool PathsEqual(string first, string second)
    {
        string firstPath = Path.GetFullPath(first);
        string secondPath = Path.GetFullPath(second);
        string firstRoot = Path.GetPathRoot(firstPath);
        string secondRoot = Path.GetPathRoot(secondPath);
        if (!string.IsNullOrEmpty(firstRoot) && firstPath.Length > firstRoot.Length)
        {
            firstPath = firstPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        if (!string.IsNullOrEmpty(secondRoot) && secondPath.Length > secondRoot.Length)
        {
            secondPath = secondPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return string.Equals(firstPath, secondPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 构造带定位信息的导出异常，消息使用文件/页签/行/列/字段格式。
    /// </summary>
    private static ConfigExportException Error(
        string workbookPath,
        string worksheetName,
        int row,
        int column,
        string fieldName,
        string detail,
        Exception innerException = null)
    {
        StringBuilder message = new StringBuilder("配置导出失败");
        if (!string.IsNullOrEmpty(workbookPath))
        {
            message.Append("：表「").Append(Path.GetFileName(workbookPath)).Append("」");
        }
        if (!string.IsNullOrEmpty(worksheetName))
        {
            message.Append("页签「").Append(worksheetName).Append("」");
        }
        if (row > 0)
        {
            message.Append("第 ").Append(row).Append(" 行");
        }
        if (column > 0)
        {
            message.Append("第 ").Append(column).Append(" 列");
        }
        if (!string.IsNullOrEmpty(fieldName))
        {
            message.Append("（字段「").Append(fieldName).Append("」）");
        }
        message.Append("：").Append(detail);
        return new ConfigExportException(
            message.ToString(),
            workbookPath,
            worksheetName,
            row,
            column,
            fieldName,
            innerException);
    }

    /// <summary>单个工作表的完整导出计划。</summary>
    private sealed class SheetPlan
    {
        public string WorkbookPath;
        public string WorkbookName;
        public string WorksheetName;
        public ExporterMode Mode;
        public string ClassName;
        public string DataFileName;
        public string DataFilePath;
        public string DataContent;
        public string ClassFileName;
        public string ClassFilePath;
        public string ClassContent;
        public string ResourcePath;
        public int DataRowCount;
    }

    /// <summary>单列字段计划。</summary>
    private sealed class ColumnPlan
    {
        public int Column;
        public string FieldName;
        public string Type;
        public string EndFlag;
        public string Remark;
        public bool InClass;
        public bool InJson;
    }

    /// <summary>待发布文件。</summary>
    private sealed class PlannedFile
    {
        public string TargetPath;
        public string Content;
        public string Description;
        public string WorkbookPath;
        public string WorksheetName;
    }

    /// <summary>已写入暂存文件、等待提交的产物。</summary>
    private sealed class StagedFile
    {
        public PlannedFile File;
        public string StagePath;
    }

    /// <summary>已提交、可用于失败回滚的产物。</summary>
    private sealed class CommittedFile
    {
        public PlannedFile File;
        public bool Existed;
        public string BackupPath;
    }
}
#endif
