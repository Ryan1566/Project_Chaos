using System;
using System.Collections.Generic;
using System.Reflection;
using OfficeOpenXml;

/// <summary>
/// 工作表访问入口的内存验证。仅在显式调用 Run 时创建临时 ExcelPackage，
/// 不读写磁盘、不读取工程配置，也不修改任何生产表或产物。
/// </summary>
public static class WorksheetAccessVerification
{
    /// <summary>
    /// 验证索引、中文页签改名、名称精确匹配、非法参数和静态引用。
    /// 全部通过时返回中文摘要；任一检查失败时抛出异常。
    /// </summary>
    public static string Run()
    {
        try
        {
            List<string> lines = new List<string>();
            VerifyIndexAndRename(lines);
            VerifyNameLookup(lines);
            VerifyInvalidArguments(lines);
            VerifyNoStaticCache(lines);
            lines.Add("PASS 工作表访问接口内存验证全部通过");
            return "工作表访问接口内存验证\n" + string.Join("\n", lines.ToArray());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("工作表访问接口内存验证失败：" + ex.Message, ex);
        }
    }

    /// <summary>
    /// 验证从 1 开始的索引，以及中文工作表改名后索引仍按位置选中。
    /// </summary>
    private static void VerifyIndexAndRename(List<string> lines)
    {
        using (ExcelPackage package = new ExcelPackage())
        {
            ExcelWorksheet first = package.Workbook.Worksheets.Add("配置封面");
            ExcelWorksheet second = package.Workbook.Worksheets.Add("中文数据页");
            ExcelWorksheet third = package.Workbook.Worksheets.Add("运行日志");

            AssertSame(first, ExcelWorksheetAccess.GetByIndex(package.Workbook, 1),
                "索引 1 应选中第一张工作表");
            AssertSame(second, ExcelWorksheetAccess.GetByIndex(package.Workbook, 2),
                "索引 2 应选中第二张工作表");
            AssertSame(third, ExcelWorksheetAccess.GetByIndex(package.Workbook, 3),
                "索引 3 应选中第三张工作表");
            lines.Add("PASS 索引 1/2 分别选中第一张、第二张工作表");

            second.Name = "改名后的数据页";
            AssertSame(second, ExcelWorksheetAccess.GetByIndex(package.Workbook, 2),
                "中文工作表改名后索引 2 应仍选中同一张表");
            AssertSame(third, ExcelWorksheetAccess.GetByIndex(package.Workbook, 3),
                "中文工作表改名后索引 3 应仍选中第三张表");
            lines.Add("PASS 中文工作表改名后按从 1 开始的索引仍选中正确");
        }
    }

    /// <summary>
    /// 验证名称接口只做完整名称精确匹配：同时存在含空格与不含空格的表时，
    /// 各自命中各自的对象；只有确实不存在的空白/大小写变体才拒绝，且不回退第一张表。
    /// </summary>
    private static void VerifyNameLookup(List<string> lines)
    {
        using (ExcelPackage package = new ExcelPackage())
        {
            ExcelWorksheet spacedName = package.Workbook.Worksheets.Add("中文 数据页");
            ExcelWorksheet compactName = package.Workbook.Worksheets.Add("中文数据页");
            ExcelWorksheet asciiName = package.Workbook.Worksheets.Add("ConfigSheet");

            AssertSame(spacedName, ExcelWorksheetAccess.GetByName(package.Workbook, "中文 数据页"),
                "名称接口应精确访问含空格的中文工作表");
            AssertSame(compactName, ExcelWorksheetAccess.GetByName(package.Workbook, "中文数据页"),
                "名称接口应精确访问无空格的中文工作表，不得因含空格表存在而混淆");
            AssertSame(asciiName, ExcelWorksheetAccess.GetByName(package.Workbook, "ConfigSheet"),
                "名称接口应精确访问大小写完全一致的工作表");
            lines.Add("PASS 含空格、无空格及 ASCII 名称分别精确选中各自工作表");

            ExpectException<KeyNotFoundException>(
                () => ExcelWorksheetAccess.GetByName(package.Workbook, "中文  数据页"),
                "双空格名称");
            ExpectException<KeyNotFoundException>(
                () => ExcelWorksheetAccess.GetByName(package.Workbook, "中文数据页 "),
                "末尾空格名称");
            ExpectException<KeyNotFoundException>(
                () => ExcelWorksheetAccess.GetByName(package.Workbook, " 中文 数据页"),
                "前导空格名称");
            ExpectException<KeyNotFoundException>(
                () => ExcelWorksheetAccess.GetByName(package.Workbook, "configsheet"),
                "大小写不一致名称");
            ExpectException<KeyNotFoundException>(
                () => ExcelWorksheetAccess.GetByName(package.Workbook, "不存在的工作表"),
                "缺失名称");
            lines.Add("PASS 不存在的空白变体、大小写变体和缺失名称均明确拒绝，不回退到第一张工作表");
        }
    }

    /// <summary>
    /// 验证 null 工作簿、索引 0/负数/Count+1 和空名称均有明确异常。
    /// </summary>
    private static void VerifyInvalidArguments(List<string> lines)
    {
        using (ExcelPackage package = new ExcelPackage())
        {
            package.Workbook.Worksheets.Add("边界表");
            ExcelWorkbook workbook = package.Workbook;
            int count = workbook.Worksheets.Count;

            ExpectException<ArgumentNullException>(
                () => ExcelWorksheetAccess.GetByIndex(null, 1), "null 工作簿的索引访问");
            ExpectException<ArgumentNullException>(
                () => ExcelWorksheetAccess.GetByName(null, "边界表"), "null 工作簿的名称访问");
            ExpectException<ArgumentOutOfRangeException>(
                () => ExcelWorksheetAccess.GetByIndex(workbook, 0), "索引 0");
            ExpectException<ArgumentOutOfRangeException>(
                () => ExcelWorksheetAccess.GetByIndex(workbook, -1), "负索引");
            ExpectException<ArgumentOutOfRangeException>(
                () => ExcelWorksheetAccess.GetByIndex(workbook, count + 1), "Count+1 索引");
            lines.Add("PASS null 工作簿、索引 0、负数、Count+1 均被明确拒绝");

            ExpectException<ArgumentNullException>(
                () => ExcelWorksheetAccess.GetByName(workbook, null), "null 名称");
            ExpectException<ArgumentException>(
                () => ExcelWorksheetAccess.GetByName(workbook, string.Empty), "空名称");
            ExpectException<ArgumentException>(
                () => ExcelWorksheetAccess.GetByName(workbook, "   "), "纯空白名称");
            lines.Add("PASS 名称 null、空串、纯空白均被明确拒绝");
        }
    }

    /// <summary>
    /// 验证访问类没有静态字段，不会把已释放 ExcelPackage 内的工作表留在静态引用中。
    /// </summary>
    private static void VerifyNoStaticCache(List<string> lines)
    {
        FieldInfo[] staticFields = typeof(ExcelWorksheetAccess).GetFields(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (staticFields.Length != 0)
        {
            throw new InvalidOperationException(
                "ExcelWorksheetAccess 不应保留静态字段，实际存在 " + staticFields.Length + " 个。");
        }
        lines.Add("PASS 访问类没有静态字段，不会缓存已释放包内的工作表");
    }

    /// <summary>
    /// 断言返回对象与预期是同一张工作表。
    /// </summary>
    private static void AssertSame(ExcelWorksheet expected, ExcelWorksheet actual, string scenario)
    {
        if (!object.ReferenceEquals(expected, actual))
        {
            string actualName = actual == null ? "null" : "「" + actual.Name + "」";
            throw new InvalidOperationException(scenario + "，实际返回 " + actualName + "。");
        }
    }

    /// <summary>
    /// 断言操作抛出指定类型的异常，并保留不符合预期时的实际行为。
    /// </summary>
    private static void ExpectException<T>(Action action, string scenario) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                scenario + " 抛出了 " + ex.GetType().Name + "，预期为 " + typeof(T).Name + "。", ex);
        }

        throw new InvalidOperationException(
            scenario + " 未抛出异常，预期为 " + typeof(T).Name + "。");
    }
}
