#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using OfficeOpenXml;

/// <summary>
/// 配置工作簿工作表访问入口。默认使用从 1 开始的索引（1 为第一张表），
/// 名称访问仅作为显式扩展，不把中文页签名称当作稳定的业务标识。
/// 本类不缓存任何工作表；返回对象只在对应 ExcelPackage 释放前有效。
/// </summary>
public static class ExcelWorksheetAccess
{
    /// <summary>
    /// 按从 1 开始的位置获取工作表。通过遍历顺序定位，不依赖名称或页签内容。
    /// </summary>
    /// <param name="workbook">已打开的工作簿，不能为 null。</param>
    /// <param name="oneBasedIndex">从 1 开始的工作表索引。</param>
    /// <returns>当前位置的工作表。</returns>
    public static ExcelWorksheet GetByIndex(ExcelWorkbook workbook, int oneBasedIndex)
    {
        if (workbook == null)
        {
            throw new ArgumentNullException("workbook", "工作簿不能为 null，无法读取工作表。");
        }

        int count = workbook.Worksheets.Count;
        if (oneBasedIndex < 1 || oneBasedIndex > count)
        {
            throw new ArgumentOutOfRangeException(
                "oneBasedIndex",
                oneBasedIndex,
                "工作表索引必须从 1 开始且不超过当前工作表数量 " + count + "。");
        }

        int position = 0;
        foreach (ExcelWorksheet worksheet in workbook.Worksheets)
        {
            position++;
            if (position == oneBasedIndex)
            {
                return worksheet;
            }
        }

        throw new InvalidOperationException(
            "工作表集合声明有 " + count + " 张表，但遍历到 " + position
            + " 张仍未找到索引 " + oneBasedIndex + "。");
    }

    /// <summary>
    /// 按名称精确获取工作表。仅用于明确知道页签名称的扩展场景；名称不缓存，
    /// 中文页签改名后应改用索引访问。比较区分大小写和空白，不做模糊匹配。
    /// </summary>
    /// <param name="workbook">已打开的工作簿，不能为 null。</param>
    /// <param name="name">完整工作表名称，不能为 null、空串或纯空白。</param>
    /// <returns>名称精确匹配的工作表。</returns>
    public static ExcelWorksheet GetByName(ExcelWorkbook workbook, string name)
    {
        if (workbook == null)
        {
            throw new ArgumentNullException("workbook", "工作簿不能为 null，无法读取工作表。");
        }
        if (name == null)
        {
            throw new ArgumentNullException("name", "工作表名称不能为 null。");
        }
        if (name.Length == 0 || name.Trim().Length == 0)
        {
            throw new ArgumentException("工作表名称不能为空或纯空白。", "name");
        }

        foreach (ExcelWorksheet worksheet in workbook.Worksheets)
        {
            if (worksheet != null && string.Equals(worksheet.Name, name, StringComparison.Ordinal))
            {
                return worksheet;
            }
        }

        throw new KeyNotFoundException("找不到名称为「" + name + "」的工作表；名称必须精确匹配。");
    }
}
#endif
