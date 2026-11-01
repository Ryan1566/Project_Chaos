using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置导表管线验证。只在本方法被调用时生成系统临时目录下的样表、Json/record 和类文件，
/// 不读取也不写入生产 Excel、Resources、ConfigData 或 ModelData 目录。
/// </summary>
public static class ConfigPipelineVerification
{
    /// <summary>
    /// 从 Unity 菜单执行验证，并在 Console 输出完整摘要。
    /// </summary>
    [MenuItem("ExcelTool/验证配置导表管线（临时样表）")]
    private static void RunFromMenu()
    {
        ConfigPipelineVerificationReport report = Run();
        if (report.Passed)
        {
            Debug.Log(report.ToSummary());
        }
        else
        {
            Debug.LogError(report.ToSummary());
        }
    }

    /// <summary>
    /// 执行全部验证并返回报告；验证失败不会向调用方抛异常。
    /// </summary>
    public static ConfigPipelineVerificationReport Run()
    {
        ConfigPipelineVerificationReport report = new ConfigPipelineVerificationReport();
        try
        {
            string tempRoot = CreateTempRoot();
            report.TempRoot = tempRoot;
            RunChecks(report, tempRoot);
            report.Passed = true;
            report.Message = "全部检查通过";
        }
        catch (Exception ex)
        {
            report.Passed = false;
            report.Message = ex.Message;
            report.Log.Add("异常：" + ex);
        }
        return report;
    }

    /// <summary>
    /// 执行全部验证；只要有一项失败就抛出带完整摘要的异常，便于自动化入口使用。
    /// </summary>
    public static ConfigPipelineVerificationReport RunOrThrow()
    {
        ConfigPipelineVerificationReport report = Run();
        if (!report.Passed)
        {
            throw new InvalidOperationException(report.ToSummary());
        }
        return report;
    }

    private static void RunChecks(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CheckStaticContract(report);

        CaseDirectories main = CreateCase(tempRoot, "主流程");
        string buildingWorkbook = CreateBuildingWorkbook(main);
        string legacyWorkbook = CreateLegacyWorkbook(main);

        List<string> configWorkbooks = new List<string> { buildingWorkbook, legacyWorkbook };
        ConfigExportReport configReport = ConfigManager.ExportExcelBatch(
            configWorkbooks,
            ExporterMode.Config,
            main.Json,
            main.Class,
            false);
        Check(report, configReport.WorkbookCount == 2, "Config 应处理 2 个工作簿，实际 " + configReport.WorkbookCount);
        Check(report, configReport.ScannedWorksheetCount == 6, "Config 应扫描 6 个页签，实际 " + configReport.ScannedWorksheetCount);
        Check(report, configReport.ExportedWorksheetCount == 3, "Config 应导出 3 个页签，实际 " + configReport.ExportedWorksheetCount);
        Check(report, configReport.SkippedWorksheetCount == 3, "Config 应跳过 3 个页签，实际 " + configReport.SkippedWorksheetCount);
        Check(report, configReport.DataRowCount == 4, "Config 应导出 4 行数据，实际 " + configReport.DataRowCount);
        Check(report, !configReport.TypeIndexPublished, "临时目录不应发布类型索引");
        Check(report, configReport.Artifacts.Count == 3, "Config 产物数量应为 3，实际 " + configReport.Artifacts.Count);

        string buildingJson = Path.Combine(main.Json, "建筑表_BuildingConfig.json");
        string inferredJson = Path.Combine(main.Json, "建筑表_InferredConfig.json");
        string legacyJson = Path.Combine(main.Json, "旧表_副标题_LegacyConfig.json");
        string buildingClass = Path.Combine(main.Class, "BuildingConfig.cs");
        string inferredClass = Path.Combine(main.Class, "InferredConfig.cs");
        string legacyClass = Path.Combine(main.Class, "LegacyConfig.cs");

        Check(report, File.Exists(buildingJson), "缺少完整工作簿名命名的 Json：" + buildingJson);
        Check(report, File.Exists(inferredJson), "缺少 B2 为空时按请求模式命名的 Json：" + inferredJson);
        Check(report, File.Exists(legacyJson), "缺少带下划线工作簿的完整基名 Json：" + legacyJson);
        Check(report, !File.Exists(Path.Combine(main.Json, "建筑_BuildingConfig.json")), "不应截断工作簿名");
        Check(report, !File.Exists(Path.Combine(main.Json, "旧表_LegacyConfig.json")), "不应把旧表_副标题截成旧表");
        Check(report, !File.Exists(Path.Combine(main.Json, "旧表_副标题_LegacyConfigConfig.json")), "完整类名不应重复追加 Config 后缀");
        Check(report, File.Exists(buildingClass), "缺少类文件：" + buildingClass);
        Check(report, File.Exists(inferredClass), "缺少推断模式类文件：" + inferredClass);
        Check(report, File.Exists(legacyClass), "缺少完整类名类文件：" + legacyClass);
        Check(report, !File.Exists(Path.Combine(main.Class, "LegacyConfigConfig.cs")), "完整类名不应重复追加 Config 后缀（类文件）");

        ConfigExportArtifact buildingArtifact = FindArtifact(configReport, "BuildingConfig");
        Check(report, buildingArtifact != null, "报告中缺少 BuildingConfig 产物");
        Check(report, buildingArtifact.DataFileName == "建筑表_BuildingConfig.json", "BuildingConfig 数据文件名错误：" + buildingArtifact.DataFileName);
        Check(report, buildingArtifact.ResourcePath == "Data/Json/Runtime/建筑表_BuildingConfig", "BuildingConfig 资源路径错误：" + buildingArtifact.ResourcePath);
        Check(report, buildingArtifact.DataRowCount == 2, "BuildingConfig 数据行数错误：" + buildingArtifact.DataRowCount);

        string buildingJsonText = File.ReadAllText(buildingJson, Encoding.UTF8);
        Check(report, buildingJsonText.IndexOf("\"datas\"", StringComparison.Ordinal) >= 0, "Json 必须保留 datas 容器");
        Check(report, buildingJsonText.IndexOf("serverOnly", StringComparison.Ordinal) < 0, "server 列不应写入 Json");

        VerificationDataList buildingData = ReadDataList(buildingJson);
        Check(report, buildingData != null && buildingData.datas != null && buildingData.datas.Count == 2,
            "BuildingConfig Json 解析行数错误");
        string expectedSpecial = "引号\"反斜杠\\换行\n逗号,冒号:花括号{}";
        Check(report, buildingData.datas[0].id == 1, "BuildingConfig 第一行 id 错误");
        Check(report, buildingData.datas[0].price == 100, "BuildingConfig 第一行 price 错误");
        string actualSpecial = buildingData.datas[0].name ?? string.Empty;
        Check(report, actualSpecial.Replace("\r\n", "\n") == expectedSpecial, "特殊字符串未正确往返");
        Check(report, buildingData.datas[1].id == 2 && buildingData.datas[1].name == "第二行", "BuildingConfig 第二行数据错误");

        string buildingClassText = File.ReadAllText(buildingClass, Encoding.UTF8);
        Check(report, buildingClassText.IndexOf("public int id;", StringComparison.Ordinal) >= 0, "BuildingConfig 缺少 id 字段");
        Check(report, buildingClassText.IndexOf("public string name;", StringComparison.Ordinal) >= 0, "BuildingConfig 缺少 name 字段");
        Check(report, buildingClassText.IndexOf("public int price;", StringComparison.Ordinal) >= 0, "BuildingConfig 缺少 price 字段");
        Check(report, buildingClassText.IndexOf("serverOnly", StringComparison.Ordinal) < 0, "server 列不应写入类文件");

        VerificationDataList legacyData = ReadDataList(legacyJson);
        Check(report, legacyData != null && legacyData.datas != null && legacyData.datas.Count == 1,
            "LegacyConfig Json 解析行数错误");
        string legacyClassText = File.ReadAllText(legacyClass, Encoding.UTF8);
        Check(report, legacyClassText.IndexOf("public class LegacyConfig", StringComparison.Ordinal) >= 0,
            "LegacyConfig 类名错误");

        ConfigExportReport dataReport = ConfigManager.ExportExcelBatch(
            new List<string> { buildingWorkbook, legacyWorkbook },
            ExporterMode.Data,
            main.Record,
            main.DataClass,
            false);
        Check(report, dataReport.WorkbookCount == 2, "Data 应处理 2 个工作簿，实际 " + dataReport.WorkbookCount);
        Check(report, dataReport.ScannedWorksheetCount == 6, "Data 应扫描 6 个页签，实际 " + dataReport.ScannedWorksheetCount);
        Check(report, dataReport.ExportedWorksheetCount == 2, "Data 应导出 2 个页签，实际 " + dataReport.ExportedWorksheetCount);
        Check(report, dataReport.SkippedWorksheetCount == 4, "Data 应跳过 4 个页签，实际 " + dataReport.SkippedWorksheetCount);
        Check(report, dataReport.DataRowCount == 2, "Data 应导出 2 行数据，实际 " + dataReport.DataRowCount);
        string buildingRecord = Path.Combine(main.Record, "建筑表_BuildingData.record");
        string inferredRecord = Path.Combine(main.Record, "建筑表_InferredData.record");
        string buildingDataClass = Path.Combine(main.DataClass, "BuildingData.cs");
        string inferredDataClass = Path.Combine(main.DataClass, "InferredData.cs");
        Check(report, File.Exists(buildingRecord), "缺少 Data record：" + buildingRecord);
        Check(report, File.Exists(inferredRecord), "缺少 B2 为空时按请求模式命名的 Data record：" + inferredRecord);
        Check(report, File.Exists(buildingDataClass), "缺少 Data 类文件：" + buildingDataClass);
        Check(report, File.Exists(inferredDataClass), "缺少推断模式 Data 类文件：" + inferredDataClass);
        Check(report, !File.Exists(Path.Combine(main.Class, "BuildingData.cs")), "Data 类文件不应写入 Config 类目录");
        Check(report, !File.Exists(Path.Combine(main.DataClass, "BuildingDataData.cs")), "Data 类名不应重复追加 Data 后缀");
        string recordText = File.ReadAllText(buildingRecord, Encoding.UTF8);
        Check(report, recordText.IndexOf("\"datas\"", StringComparison.Ordinal) >= 0, "record 必须保留 datas 容器");
        VerificationDataList recordData = ReadDataList(buildingRecord);
        Check(report, recordData != null && recordData.datas != null && recordData.datas.Count == 1,
            "BuildingData record 解析行数错误");
        Check(report, recordData.datas[0].id == 1 && recordData.datas[0].name == "模型行", "BuildingData record 数据错误");
        VerificationDataList inferredData = ReadDataList(inferredRecord);
        Check(report, inferredData != null && inferredData.datas != null && inferredData.datas.Count == 1,
            "InferredData record 解析行数错误");

        CheckTypeIndexGuard(report, buildingWorkbook, tempRoot);
        CheckAtomicity(report, tempRoot);
        CheckDuplicateClass(report, tempRoot);
        CheckDuplicateField(report, tempRoot);
        CheckOverflow(report, tempRoot);
        CheckInvalidType(report, tempRoot);
        CheckInvalidIdentifier(report, tempRoot);
        CheckInvalidBool(report, tempRoot);
        CheckWriteRollback(report, tempRoot);
    }

    /// <summary>
    /// 检查公开契约、旧入口兼容性和 Editor 隔离的静态证据。
    /// </summary>
    private static void CheckStaticContract(ConfigPipelineVerificationReport report)
    {
        MethodInfo loadData = typeof(ConfigLoader).GetMethod(
            "LoadData",
            BindingFlags.Public | BindingFlags.Instance);
        Check(report, loadData != null && loadData.IsGenericMethodDefinition, "ConfigLoader.LoadData<T> 必须存在");

        MethodInfo loadDataAsync = typeof(ConfigLoader).GetMethod(
            "LoadDataAsync",
            BindingFlags.Public | BindingFlags.Instance);
        Check(report, loadDataAsync != null && loadDataAsync.IsGenericMethodDefinition, "ConfigLoader.LoadDataAsync<T> 必须存在");

        MethodInfo batchMethod = typeof(ConfigManager).GetMethod(
            "ExportExcelBatch",
            BindingFlags.Public | BindingFlags.Static);
        Check(report, batchMethod != null && batchMethod.ReturnType == typeof(ConfigExportReport),
            "ConfigManager.ExportExcelBatch 必须公开并返回 ConfigExportReport");

        MethodInfo legacyJson = typeof(ConfigManager).GetMethod(
            "ExportJson",
            BindingFlags.NonPublic | BindingFlags.Static);
        Check(report, legacyJson != null, "旧版单表 ExportJson 兼容入口必须保留");

        string loaderPath = Path.Combine(
            Application.dataPath,
            "Scripts/Runtime/GameBase/ConfigManager/Config/ConfigLoader.cs");
        string managerPath = Path.Combine(
            Application.dataPath,
            "Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs");
        Check(report, File.Exists(loaderPath), "找不到 ConfigLoader.cs：" + loaderPath);
        Check(report, File.Exists(managerPath), "找不到 ConfigManager.cs：" + managerPath);

        string loaderSource = File.ReadAllText(loaderPath, Encoding.UTF8);
        Check(report, CountOccurrences(loaderSource, "ConfigTableAssets.Resolve(typeName)") >= 2,
            "同步和异步加载都应调用同一个 ConfigTableAssets.Resolve");
        Check(report, loaderSource.IndexOf("UnityEditor", StringComparison.Ordinal) < 0,
            "ConfigLoader 不应引用 UnityEditor");

        string managerSource = File.ReadAllText(managerPath, Encoding.UTF8);
        int editorDefineIndex = managerSource.IndexOf("#if UNITY_EDITOR", StringComparison.Ordinal);
        int editorUsingIndex = managerSource.IndexOf("using UnityEditor;", StringComparison.Ordinal);
        Check(report, editorDefineIndex >= 0, "ConfigManager 必须使用 #if UNITY_EDITOR 隔离 Editor 代码");
        Check(report, editorUsingIndex > editorDefineIndex, "using UnityEditor 必须位于 #if UNITY_EDITOR 内");
        Check(report, managerSource.IndexOf("#endif", editorUsingIndex, StringComparison.Ordinal) > editorUsingIndex,
            "ConfigManager 的 Editor 分支必须有 #endif");
        Check(report, managerSource.IndexOf("using OfficeOpenXml;", StringComparison.Ordinal) > editorDefineIndex,
            "using OfficeOpenXml 必须位于 #if UNITY_EDITOR 内");
    }

    private static void CheckTypeIndexGuard(
        ConfigPipelineVerificationReport report,
        string workbookPath,
        string tempRoot)
    {
        CaseDirectories guard = CreateCase(tempRoot, "索引保护");
        ConfigExportException exception = ExpectFailure(
            report,
            "临时目录发布类型索引应被拒绝",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    guard.Json,
                    guard.Class,
                    true);
            },
            "发布类型索引时 JSON 输出目录必须是");
        string productionPath = "Resources" + Path.DirectorySeparatorChar + "Data"
            + Path.DirectorySeparatorChar + "Json" + Path.DirectorySeparatorChar + "Runtime";
        Check(report, exception.Message.IndexOf(productionPath, StringComparison.OrdinalIgnoreCase) >= 0,
            "索引保护错误信息应包含真实 Resources/Data/Json/Runtime 路径");
        Check(report, Directory.GetFiles(guard.Json, "*.json", SearchOption.TopDirectoryOnly).Length == 0,
            "索引保护失败时不应写入任何 Json");
    }

    private static void CheckAtomicity(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories atomic = CreateCase(tempRoot, "整批校验");
        string workbookPath = Path.Combine(atomic.Xlsx, "原子表.xlsx");
        using (ExcelPackage package = new ExcelPackage())
        {
            AddTableSheet(package, "有效", "Atomic", "IsConfig",
                new[] { "id" }, new[] { "int" }, new[] { "client" }, new[] { "编号" },
                new[] { "1" });
            AddTableSheet(package, "非法", "AtomicBad", "IsConfig",
                new[] { "id" }, new[] { "int" }, new[] { "client" }, new[] { "编号" },
                new[] { "abc" });
            package.SaveAs(new FileInfo(workbookPath));
        }

        string oldJson = Path.Combine(atomic.Json, "原子表_AtomicConfig.json");
        WriteUtf8(oldJson, "OLD");

        ConfigExportException exception = ExpectFailure(
            report,
            "整批校验失败应阻止发布",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    atomic.Json,
                    atomic.Class,
                    false);
            },
            "原子表.xlsx",
            "非法",
            "第 7 行",
            "第 2 列",
            "字段「id」");
        CheckExceptionLocation(report, exception, "原子表.xlsx", "非法", 7, 2, "id");

        Check(report, File.ReadAllText(oldJson, Encoding.UTF8) == "OLD", "校验失败不得覆盖旧 Json");
        Check(report, !File.Exists(Path.Combine(atomic.Json, "原子表_AtomicBadConfig.json")), "校验失败不得写入非法页签 Json");
        Check(report, Directory.GetFiles(atomic.Class, "*.cs", SearchOption.TopDirectoryOnly).Length == 0,
            "校验失败不得写入任何类文件");
    }

    private static void CheckDuplicateClass(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories duplicateClass = CreateCase(tempRoot, "重复类");
        string workbookPath = Path.Combine(duplicateClass.Xlsx, "重复类.xlsx");
        using (ExcelPackage package = new ExcelPackage())
        {
            AddTableSheet(package, "一", "Dup", "IsConfig",
                new[] { "id" }, new[] { "int" }, new[] { "client" }, new[] { "编号" },
                new[] { "1" });
            AddTableSheet(package, "二", "Dup", "IsConfig",
                new[] { "id" }, new[] { "int" }, new[] { "client" }, new[] { "编号" },
                new[] { "1" });
            package.SaveAs(new FileInfo(workbookPath));
        }

        ConfigExportException exception = ExpectFailure(
            report,
            "同批重复类名应失败",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    duplicateClass.Json,
                    duplicateClass.Class,
                    false);
            },
            "重复类.xlsx",
            "类名重复",
            "第 1 行",
            "第 2 列");
        CheckExceptionLocation(report, exception, "重复类.xlsx", "二", 1, 2, "DupConfig");
        CheckNoOutput(report, duplicateClass);
    }

    private static void CheckDuplicateField(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories duplicateField = CreateCase(tempRoot, "重复字段");
        string workbookPath = Path.Combine(duplicateField.Xlsx, "重复字段.xlsx");
        using (ExcelPackage package = new ExcelPackage())
        {
            AddTableSheet(package, "字段", "DupField", "IsConfig",
                new[] { "id", "id" },
                new[] { "int", "string" },
                new[] { "client", "client" },
                new[] { "编号", "重复编号" },
                new[] { "1", "x" });
            package.SaveAs(new FileInfo(workbookPath));
        }

        ConfigExportException exception = ExpectFailure(
            report,
            "重复字段名应失败",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    duplicateField.Json,
                    duplicateField.Class,
                    false);
            },
            "重复字段.xlsx",
            "字段名重复",
            "第 4 行",
            "第 3 列");
        CheckExceptionLocation(report, exception, "重复字段.xlsx", "字段", 4, 3, "id");
        CheckNoOutput(report, duplicateField);
    }

    private static void CheckOverflow(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories overflow = CreateCase(tempRoot, "数值溢出");
        string workbookPath = CreateSimpleWorkbook(
            overflow,
            "溢出.xlsx",
            "溢出",
            "Overflow",
            "IsConfig",
            new[] { "id" },
            new[] { "int" },
            new[] { "client" },
            new[] { "编号" },
            new[] { "2147483648" });

        ConfigExportException exception = ExpectFailure(
            report,
            "int 溢出应失败",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    overflow.Json,
                    overflow.Class,
                    false);
            },
            "溢出.xlsx",
            "第 7 行",
            "第 2 列");
        CheckExceptionLocation(report, exception, "溢出.xlsx", "溢出", 7, 2, "id");
        CheckNoOutput(report, overflow);
    }

    private static void CheckInvalidType(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories invalidType = CreateCase(tempRoot, "非法类型");
        string workbookPath = CreateSimpleWorkbook(
            invalidType,
            "类型.xlsx",
            "类型",
            "BadType",
            "IsConfig",
            new[] { "id" },
            new[] { "int32" },
            new[] { "client" },
            new[] { "编号" },
            new[] { "1" });

        ConfigExportException exception = ExpectFailure(
            report,
            "非法类型应失败",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    invalidType.Json,
                    invalidType.Class,
                    false);
            },
            "类型.xlsx",
            "不支持此类型",
            "第 6 行",
            "第 2 列");
        CheckExceptionLocation(report, exception, "类型.xlsx", "类型", 6, 2, "id");
        CheckNoOutput(report, invalidType);
    }

    private static void CheckInvalidIdentifier(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories invalidIdentifier = CreateCase(tempRoot, "非法标识符");
        string workbookPath = CreateSimpleWorkbook(
            invalidIdentifier,
            "标识符.xlsx",
            "标识符",
            "1Bad",
            "IsConfig",
            new[] { "id" },
            new[] { "int" },
            new[] { "client" },
            new[] { "编号" },
            new[] { "1" });

        ConfigExportException exception = ExpectFailure(
            report,
            "非法类名应失败",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    invalidIdentifier.Json,
                    invalidIdentifier.Class,
                    false);
            },
            "标识符.xlsx",
            "合法的 C# 标识符",
            "第 1 行",
            "第 2 列");
        CheckExceptionLocation(report, exception, "标识符.xlsx", "标识符", 1, 2, null);
        CheckNoOutput(report, invalidIdentifier);
    }

    private static void CheckInvalidBool(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories invalidBool = CreateCase(tempRoot, "非法布尔");
        string workbookPath = CreateSimpleWorkbook(
            invalidBool,
            "布尔.xlsx",
            "布尔",
            "BadBool",
            "IsConfig",
            new[] { "flag" },
            new[] { "bool" },
            new[] { "client" },
            new[] { "开关" },
            new[] { "maybe" });

        ConfigExportException exception = ExpectFailure(
            report,
            "非法 bool 应失败",
            () =>
            {
                ConfigManager.ExportExcelBatch(
                    new List<string> { workbookPath },
                    ExporterMode.Config,
                    invalidBool.Json,
                    invalidBool.Class,
                    false);
            },
            "布尔.xlsx",
            "bool 取值",
            "第 7 行",
            "第 2 列");
        CheckExceptionLocation(report, exception, "布尔.xlsx", "布尔", 7, 2, "flag");
        CheckNoOutput(report, invalidBool);
    }

    private static void CheckWriteRollback(ConfigPipelineVerificationReport report, string tempRoot)
    {
        CaseDirectories rollback = CreateCase(tempRoot, "写盘回滚");
        string workbookA = CreateSimpleWorkbook(
            rollback,
            "回滚A.xlsx",
            "A",
            "RollbackA",
            "IsConfig",
            new[] { "id" },
            new[] { "int" },
            new[] { "client" },
            new[] { "编号" },
            new[] { "1" });
        string workbookB = CreateSimpleWorkbook(
            rollback,
            "回滚B.xlsx",
            "B",
            "RollbackB",
            "IsConfig",
            new[] { "id" },
            new[] { "int" },
            new[] { "client" },
            new[] { "编号" },
            new[] { "2" });

        string oldA = Path.Combine(rollback.Json, "回滚A_RollbackAConfig.json");
        string oldB = Path.Combine(rollback.Json, "回滚B_RollbackBConfig.json");
        WriteUtf8(oldA, "OLD_A");
        WriteUtf8(oldB, "OLD_B");

        using (FileStream lockStream = new FileStream(oldB, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            ExpectFailure(
                report,
                "发布中途失败应回滚",
                () =>
                {
                    ConfigManager.ExportExcelBatch(
                        new List<string> { workbookA, workbookB },
                        ExporterMode.Config,
                        rollback.Json,
                        rollback.Class,
                        false);
                },
                "导出发布失败",
                "已回滚");
        }

        Check(report, File.ReadAllText(oldA, Encoding.UTF8) == "OLD_A", "回滚后旧 Json A 必须恢复");
        Check(report, File.ReadAllText(oldB, Encoding.UTF8) == "OLD_B", "回滚后旧 Json B 必须保持");
        Check(report, !File.Exists(Path.Combine(rollback.Class, "回滚A_RollbackAConfig.cs")),
            "写盘失败时不应提交类文件");
    }

    private static void CheckNoOutput(ConfigPipelineVerificationReport report, CaseDirectories dirs)
    {
        Check(report, Directory.GetFiles(dirs.Json, "*.json", SearchOption.TopDirectoryOnly).Length == 0,
            "失败用例不应写入 Json：" + dirs.Json);
        Check(report, Directory.GetFiles(dirs.Class, "*.cs", SearchOption.TopDirectoryOnly).Length == 0,
            "失败用例不应写入类文件：" + dirs.Class);
    }

    private static ConfigExportException ExpectFailure(
        ConfigPipelineVerificationReport report,
        string label,
        Action action,
        params string[] expectedMessageParts)
    {
        try
        {
            action();
        }
        catch (ConfigExportException ex)
        {
            foreach (string part in expectedMessageParts)
            {
                Check(report, ex.Message.IndexOf(part, StringComparison.Ordinal) >= 0,
                    label + " 的异常应包含「" + part + "」；实际：" + ex.Message);
            }
            report.Log.Add("通过：" + label);
            return ex;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                label + " 期望 ConfigExportException，实际 " + ex.GetType().Name + "：" + ex.Message,
                ex);
        }

        throw new InvalidOperationException(label + " 期望失败，但调用成功");
    }

    private static void CheckExceptionLocation(
        ConfigPipelineVerificationReport report,
        ConfigExportException exception,
        string workbookFileName,
        string worksheetName,
        int row,
        int column,
        string fieldName)
    {
        Check(report, string.Equals(Path.GetFileName(exception.WorkbookPath), workbookFileName, StringComparison.OrdinalIgnoreCase),
            "异常工作簿定位错误：" + exception.WorkbookPath);
        Check(report, string.Equals(exception.WorksheetName, worksheetName, StringComparison.Ordinal),
            "异常页签定位错误：" + exception.WorksheetName);
        Check(report, exception.Row == row, "异常行号应为 " + row + "，实际 " + exception.Row);
        Check(report, exception.Column == column, "异常列号应为 " + column + "，实际 " + exception.Column);
        if (fieldName != null)
        {
            Check(report, string.Equals(exception.FieldName, fieldName, StringComparison.Ordinal),
                "异常字段定位错误：" + exception.FieldName);
        }
    }

    private static ConfigExportArtifact FindArtifact(ConfigExportReport report, string className)
    {
        foreach (ConfigExportArtifact artifact in report.Artifacts)
        {
            if (string.Equals(artifact.ClassName, className, StringComparison.Ordinal))
            {
                return artifact;
            }
        }
        return null;
    }

    private static VerificationDataList ReadDataList(string path)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);
        return JsonUtility.FromJson<VerificationDataList>(json);
    }

    private static string CreateBuildingWorkbook(CaseDirectories dirs)
    {
        string path = Path.Combine(dirs.Xlsx, "建筑表.xlsx");
        const string special = "引号\"反斜杠\\换行\n逗号,冒号:花括号{}";
        using (ExcelPackage package = new ExcelPackage())
        {
            ExcelWorksheet cover = package.Workbook.Worksheets.Add("封面");
            cover.Cells[1, 1].Value = "建筑配置说明";

            ExcelWorksheet blank = package.Workbook.Worksheets.Add("空白");
            blank.Cells[1, 1].Value = " ";

            AddTableSheet(
                package,
                "建筑",
                "Building",
                "IsConfig",
                new[] { "id", "name", "price", "serverOnly" },
                new[] { "int", "string", "int", "string" },
                new[] { "client", "client", "client", "server" },
                new[] { "编号", "名称", "价格", "仅服务器" },
                new[] { "1", special, "100", "secret" },
                new[] { "2", "第二行", "0", "" });

            AddTableSheet(
                package,
                "模型",
                "Building",
                "IsData",
                new[] { "id", "name" },
                new[] { "int", "string" },
                new[] { "client", "client" },
                new[] { "编号", "名称" },
                new[] { "1", "模型行" });

            AddTableSheet(
                package,
                "推断配置",
                "Inferred",
                null,
                new[] { "id" },
                new[] { "int" },
                new[] { "client" },
                new[] { "编号" },
                new[] { "1" });

            package.SaveAs(new FileInfo(path));
        }
        return path;
    }

    private static string CreateLegacyWorkbook(CaseDirectories dirs)
    {
        string path = Path.Combine(dirs.Xlsx, "旧表_副标题.xlsx");
        using (ExcelPackage package = new ExcelPackage())
        {
            AddTableSheet(
                package,
                "旧表",
                "LegacyConfig",
                "IsConfig",
                new[] { "id" },
                new[] { "int" },
                new[] { "client" },
                new[] { "编号" },
                new[] { "1" });
            package.SaveAs(new FileInfo(path));
        }
        return path;
    }

    private static string CreateSimpleWorkbook(
        CaseDirectories dirs,
        string fileName,
        string sheetName,
        string className,
        string mode,
        string[] fields,
        string[] types,
        string[] endFlags,
        string[] remarks,
        params string[][] dataRows)
    {
        string path = Path.Combine(dirs.Xlsx, fileName);
        using (ExcelPackage package = new ExcelPackage())
        {
            AddTableSheet(package, sheetName, className, mode, fields, types, endFlags, remarks, dataRows);
            package.SaveAs(new FileInfo(path));
        }
        return path;
    }

    private static void AddTableSheet(
        ExcelPackage package,
        string sheetName,
        string className,
        string mode,
        string[] fields,
        string[] types,
        string[] endFlags,
        string[] remarks,
        params string[][] dataRows)
    {
        ExcelWorksheet sheet = package.Workbook.Worksheets.Add(sheetName);
        sheet.Cells[1, 1].Value = "类名";
        sheet.Cells[1, 2].Value = className;
        sheet.Cells[2, 1].Value = "是否读取";
        if (!string.IsNullOrEmpty(mode))
        {
            sheet.Cells[2, 2].Value = mode;
        }

        for (int index = 0; index < fields.Length; index++)
        {
            int column = index + 2;
            if (remarks != null && index < remarks.Length)
            {
                sheet.Cells[3, column].Value = remarks[index];
            }
            sheet.Cells[4, column].Value = fields[index];
            sheet.Cells[5, column].Value = endFlags != null && index < endFlags.Length
                ? endFlags[index]
                : "client";
            sheet.Cells[6, column].Value = types[index];
        }

        if (dataRows != null)
        {
            for (int rowIndex = 0; rowIndex < dataRows.Length; rowIndex++)
            {
                string[] values = dataRows[rowIndex];
                int row = 7 + rowIndex;
                for (int columnIndex = 0; columnIndex < fields.Length && columnIndex < values.Length; columnIndex++)
                {
                    sheet.Cells[row, columnIndex + 2].Value = values[columnIndex] ?? string.Empty;
                }
            }
        }
    }

    private static CaseDirectories CreateCase(string tempRoot, string name)
    {
        CaseDirectories dirs = new CaseDirectories();
        dirs.Root = Path.Combine(tempRoot, name);
        dirs.Xlsx = Path.Combine(dirs.Root, "xlsx");
        dirs.Json = Path.Combine(dirs.Root, "json");
        dirs.Class = Path.Combine(dirs.Root, "class");
        dirs.Record = Path.Combine(dirs.Root, "record");
        dirs.DataClass = Path.Combine(dirs.Root, "dataClass");
        Directory.CreateDirectory(dirs.Xlsx);
        Directory.CreateDirectory(dirs.Json);
        Directory.CreateDirectory(dirs.Class);
        Directory.CreateDirectory(dirs.Record);
        Directory.CreateDirectory(dirs.DataClass);
        return dirs;
    }

    private static string CreateTempRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChaosConfigPipelineVerification",
            DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteUtf8(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static void Check(ConfigPipelineVerificationReport report, bool condition, string message)
    {
        if (!condition)
        {
            report.Log.Add("失败：" + message);
            throw new InvalidOperationException(message);
        }
        // 通过项只记录简短标记，失败信息才携带具体断言，避免成功日志出现“找不到/缺少”等误导文字。
        report.Log.Add("通过");
    }

    /// <summary>单个验证用例的临时目录集合。</summary>
    private sealed class CaseDirectories
    {
        public string Root;
        public string Xlsx;
        public string Json;
        public string Record;
        public string Class;
        public string DataClass;
    }
}

/// <summary>
/// 配置导表管线验证报告。
/// </summary>
[Serializable]
public sealed class ConfigPipelineVerificationReport
{
    public bool Passed;
    public string Message;
    public string TempRoot;
    public List<string> Log = new List<string>();

    /// <summary>生成可直接输出到 Console 的中文摘要。</summary>
    public string ToSummary()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Passed ? "配置导表管线验证：通过" : "配置导表管线验证：失败").Append('\n');
        if (!string.IsNullOrEmpty(TempRoot))
        {
            builder.Append("临时目录：").Append(TempRoot).Append('\n');
        }
        builder.Append("消息：").Append(Message).Append('\n');
        builder.Append("日志：\n");
        foreach (string line in Log)
        {
            builder.Append(line).Append('\n');
        }
        return builder.ToString();
    }
}

/// <summary>验证用 Json 容器，字段与生成的 datas 结构一致。</summary>
[Serializable]
public sealed class VerificationDataList
{
    public List<VerificationRow> datas = new List<VerificationRow>();
}

/// <summary>验证用数据行。</summary>
[Serializable]
public sealed class VerificationRow
{
    public int id;
    public string name;
    public int price;
}
