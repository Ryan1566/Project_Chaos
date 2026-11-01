#if UNITY_EDITOR
using System;
using System.IO;

/// <summary>
/// 配置导出命名规则：从工作簿名提取英文表名，并生成数据文件名。
/// 这里只做纯字符串校验，不读取文件、不缓存，也不依赖 Unity API。
/// </summary>
public static class ConfigExportNaming
{
    /// <summary>
    /// 从工作簿路径或文件名提取英文表名。取扩展名前、第一个下划线之前的 ASCII 英文前缀；
    /// 不含下划线的纯英文临时工作簿直接使用整个文件名。
    /// </summary>
    /// <param name="workbookPathOrName">工作簿绝对路径、相对路径或文件名。</param>
    /// <returns>保留原始大小写的英文表名。</returns>
    public static string WorkbookKey(string workbookPathOrName)
    {
        if (string.IsNullOrWhiteSpace(workbookPathOrName))
        {
            throw new ArgumentException("工作簿路径或文件名不能为空", nameof(workbookPathOrName));
        }

        string fileName = Path.GetFileName(workbookPathOrName.Trim());
        string workbookStem = Path.GetFileNameWithoutExtension(fileName);
        int separatorIndex = workbookStem.IndexOf('_');
        string workbookKey = separatorIndex >= 0
            ? workbookStem.Substring(0, separatorIndex)
            : workbookStem;

        if (!IsValidWorkbookKey(workbookKey))
        {
            throw new ArgumentException(
                "工作簿名必须以 ASCII 英文字母开头，且第一个下划线前的表名只能包含 ASCII 英文字母和数字："
                + workbookPathOrName,
                nameof(workbookPathOrName));
        }

        return workbookKey;
    }

    /// <summary>
    /// 规范化 B1 单元格中的原始类名。只去掉首尾空白，保留类名原文，不追加 Config/Data 等后缀。
    /// 结果必须是合法 ASCII C# 标识符；关键字和 Windows 保留名仍由 ConfigManager 校验。
    /// </summary>
    /// <param name="rawClassName">B1 单元格中的原始类名。</param>
    /// <returns>去掉首尾空白后的类名。</returns>
    public static string ClassName(string rawClassName)
    {
        string className = (rawClassName ?? string.Empty).Trim();
        if (className.Length == 0)
        {
            throw new ArgumentException("B1 类名不能为空", nameof(rawClassName));
        }

        char first = className[0];
        if (!IsAsciiLetter(first) && first != '_')
        {
            throw new ArgumentException(
                "B1 类名必须是合法 ASCII C# 标识符（字母或下划线开头，后续仅字母、数字和下划线）："
                + rawClassName,
                nameof(rawClassName));
        }

        for (int index = 1; index < className.Length; index++)
        {
            char current = className[index];
            if (!IsAsciiLetter(current) && !IsAsciiDigit(current) && current != '_')
            {
                throw new ArgumentException(
                    "B1 类名必须是合法 ASCII C# 标识符（字母或下划线开头，后续仅字母、数字和下划线）："
                    + rawClassName,
                    nameof(rawClassName));
            }
        }

        return className;
    }

    /// <summary>
    /// 生成配置数据文件名，格式为“英文表名_B1基础名 + 扩展名”。
    /// 仅支持 .json 和 .record，返回纯 ASCII 文件名。
    /// </summary>
    /// <param name="workbookPathOrName">工作簿绝对路径、相对路径或文件名。</param>
    /// <param name="rawClassName">B1 单元格中的基础类名。</param>
    /// <param name="extension">数据文件扩展名，只允许 .json 或 .record。</param>
    /// <returns>纯 ASCII 数据文件名。</returns>
    public static string DataFileName(string workbookPathOrName, string rawClassName, string extension)
    {
        if (extension != ".json" && extension != ".record")
        {
            throw new ArgumentException("数据文件扩展名只能是 .json 或 .record", nameof(extension));
        }

        string workbookKey = WorkbookKey(workbookPathOrName);
        string className = ClassName(rawClassName);
        return workbookKey + "_" + className + extension;
    }

    /// <summary>
    /// 生成完整的配置类名，格式为“英文表名_B1基础名 + 模式后缀”。
    /// 模式后缀只允许 Config 或 Data，必须由调用端明确传入；
    /// 不剥离 B1 基础名中已有的后缀，也不根据后缀反推模式。
    /// </summary>
    /// <param name="workbookPathOrName">工作簿绝对路径、相对路径或文件名。</param>
    /// <param name="rawBaseName">B1 单元格中的基础类名。</param>
    /// <param name="modeSuffix">模式后缀，只允许 Config 或 Data。</param>
    /// <returns>纯 ASCII 的完整类名。</returns>
    public static string GeneratedClassName(string workbookPathOrName, string rawBaseName, string modeSuffix)
    {
        if (modeSuffix != "Config" && modeSuffix != "Data")
        {
            throw new ArgumentException("配置类名模式后缀只能是 Config 或 Data", nameof(modeSuffix));
        }

        string workbookKey = WorkbookKey(workbookPathOrName);
        string baseName = ClassName(rawBaseName);
        return workbookKey + "_" + baseName + modeSuffix;
    }

    private static bool IsValidWorkbookKey(string workbookKey)
    {
        if (string.IsNullOrEmpty(workbookKey) || !IsAsciiLetter(workbookKey[0]))
        {
            return false;
        }

        for (int index = 1; index < workbookKey.Length; index++)
        {
            char current = workbookKey[index];
            if (!IsAsciiLetter(current) && !IsAsciiDigit(current))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }

    private static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }
}
#endif
