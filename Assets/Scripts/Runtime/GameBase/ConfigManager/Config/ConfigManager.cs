using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using ChaosDebug;

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

//[InitializeOnLoad]
/// <summary>
/// 配置管理器，用于Excel配置表的读取和序列化，导出对应的Json和class
/// </summary>
public class ConfigManager
{
    /// <summary>
    /// 备注行
    /// </summary>
    private const int remarkIndex = 3;
    /// <summary>
    /// 属性行
    /// </summary>
    private const int propertyIndex = 4;
    /// <summary>
    /// 端配置行 判断是server还是client
    /// </summary>
    private const int endIndex = 5;
    /// <summary>
    /// 类型行
    /// </summary>
    private const int typeIndex = 6;
    /// <summary>
    /// 值行
    /// </summary>
    private const int valueIndex = 7;

    //[MenuItem("Tool/ClearExcelConfigs")]
    private static void ClearConfigs()
    {

    }

#if UNITY_EDITOR 

    [MenuItem("ExcelTool/ExportExcel")]
    private static void ExportConfigsAndModels()
    {
        ExportConfigs();
        ExportModels();
    }

    /// <summary>
    /// 导出配置
    /// </summary>
    [MenuItem("ExcelTool/ExportExcelConfigs")]
    private static void ExportConfigs()
    {
        try
        {
            FileInfo[] files = FileUtil.LoadFiles(GlobalPath.data_ExcelPath);

            foreach (var file in files)
            {
                if (file.Extension != ".xlsx") continue;//过滤不是Excel的文件

                ExcelPackage ep = new ExcelPackage(file);
                ExcelWorksheets workSheets = ep.Workbook.Worksheets;

                ExcelWorksheet workSheet = workSheets[1];//只导入第一个页签

                if (workSheet.Dimension.End.Row == 0)//空表不处理
                    return;

                ExportJson(workSheet, Path.GetFileNameWithoutExtension(GetFileName(file.Name)),ExporterMode.Config);//导出Json
                ExportClass(workSheet, Path.GetFileNameWithoutExtension(GetFileName(file.Name)), ExporterMode.Config);//导出类
            }

            AssetDatabase.Refresh();

            ChaosLog.Info(LogChannel.Config, $"——————————————————--------------------------------------已导出所有Config文件");
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Config, e.ToString());
        }
    }

    /// <summary>
    /// 导出MVP数据Model类
    /// </summary>
    [MenuItem("ExcelTool/ExportExcelModels")]
    private static void ExportModels()
    {
        try
        {
            FileInfo[] files = FileUtil.LoadFiles(GlobalPath.data_ExcelPath);

            foreach (var file in files)
            {
                //过滤文件
                if (file.Extension != ".xlsx") continue;
                ExcelPackage excelPackage = new ExcelPackage(file);
                ExcelWorksheets workSheets = excelPackage.Workbook.Worksheets;
                //只导表1
                ExcelWorksheet workSheet = workSheets[1];

                if (workSheet.Dimension.End.Row == 0)//空表不处理
                    return;

                ExportJson(workSheet, Path.GetFileNameWithoutExtension(GetFileName(file.Name)), ExporterMode.Data);
                ExportClass(workSheet, Path.GetFileNameWithoutExtension(GetFileName(file.Name)), ExporterMode.Data);

            }
            AssetDatabase.Refresh();

            ChaosLog.Info(LogChannel.Config, $"——————————————————--------------------------------------已导出所有Model文件");
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Config, e.ToString());
        }
    }
#endif

    //导出类
    private static void ExportClass(ExcelWorksheet workSheet,string fileName,ExporterMode mode)
    {
        string[] endflags = GetEnd(workSheet);
        string[] properties = GetProperties(workSheet);
        StringBuilder sb = new StringBuilder();
        sb.Append("using System;\t\n\n");
        sb.Append("[Serializable]\t\n");
        if (mode == ExporterMode.Data)//添加用于持久化的数据类备注
            sb.Append($"//用于{fileName}Model类进行持久化的数据类\n");
        sb.Append($"public class {fileName}{mode.ToString()}");//类名
        /*if (mode == ExporterMode.Data)//模型类继承模型接口
            sb.Append(": IModel");*/
        sb.Append("\n");
        sb.Append("{\n");

        for (int col = 1; col <= properties.Length; col++)
        {
            if (endflags[col - 1] == "server")
                continue;

            string fieldType = GetType(workSheet, col);
            string fieldName = properties[col - 1];
            string remarkContent = GetRemark(workSheet, col);
            sb.Append($"\tpublic {fieldType} {fieldName};");
            sb.Append($"//{remarkContent}\n");
        }

        sb.Append("}\n\n");

        string savePath = mode == ExporterMode.Config ? GlobalPath.data_ExcelClassPath : GlobalPath.data_ExcelModelPath;

        FileUtil.SaveFile(savePath
            , string.Format("{0}{1}.cs", fileName,mode.ToString())
            , sb.ToString());

        ChaosLog.Info(LogChannel.Config, $"Class: 已导出对应{mode.ToString()}文件{string.Format("{0}{1}.cs", fileName, mode.ToString())}至文件夹 {savePath}");
    }

    //导出Json
    private static void ExportJson(ExcelWorksheet workSheet, string fileName,ExporterMode mode)
    {
        string str = "";
        int num = 0;

        string[] endflags = GetEnd(workSheet);//判断是不是client可用的字段
        string[] properties = GetProperties(workSheet);
        for (int col = 1; col <= properties.Length; col++)
        {
            if (endflags[col - 1] == "server" || endflags[col - 1] == "")
                continue;

            string colType = GetType(workSheet, col);
            string colField = properties[col - 1];
            string[] temp = GetValues(workSheet, col);
            num = temp.Length;
            for (int i = 0; i < temp.Length; i++)
            {
                string converted;
                try
                {
                    converted = Convert(colType, temp[i]);
                }
                catch (Exception ex)
                {
                    // 把出错位置的上下文一并抛出：表 / 页签 / Excel 行列 / 字段 / 类型 / 原始取值。
                    // Convert 只拿得到 (type, value)，定位不到是哪一格；而本项目已经被
                    // 「静默失败 / 定位不到」坑过多次（空表 return、路径错位、空列 Json 无 key），
                    // 所以「不支持此类型」「bool 列取值非法」必须能直接指到那一格。
                    throw new Exception(
                        $"导出失败：表「{fileName}」页签「{workSheet.Name}」第 {valueIndex + i} 行第 {col} 列"
                        + $"（字段 {colField}，类型 {colType}）的取值「{temp[i]}」无法转换：{ex.Message}", ex);
                }
                str += GetJsonK_VFromKeyAndValues(colField, converted) + ',';
            }
        }

        if(str == "")
        {
            ChaosLog.Info(LogChannel.Config, "该表无任何导出项");
            return;
        }

        //获取key:value的字符串
        str = str.Substring(0, str.Length - 1);//去除最小单位语句的最后一个","
        str = GetJsonFromJsonK_V(str, num);//组装成Json列表，并去除列表中最后一个","
        str = GetUnityJsonFromJson(str);//修改为适应JsonUtility的格式
        str = FormatJson(str);//新增：导出前统一美化，便于人工查看

        //用 Unity 自带 JsonUtility（需手动加换行）
        //string json1 = JsonUtility.ToJson(fileContent); // 紧凑格式
        //str = JsonUtility.ToJson(str, true); // 带缩进的格式化格式

        string savePath = mode == ExporterMode.Config ? GlobalPath.data_JsonPath : GlobalPath.data_RecordPath;

        FileUtil.SaveFile(savePath
            , string.Format("{0}{1}.{2}"
            , fileName, mode.ToString(), mode == ExporterMode.Config ? "json" : "record"), str);

        ChaosLog.Info(LogChannel.Config, $"Json: 已导出对应{mode.ToString()}文件" +
            $"{string.Format("{0}{1}.{2}", fileName, mode.ToString(), mode == ExporterMode.Config ? "json" : "record")}" +
            $"至文件夹 {savePath}");
    }

    /// <summary>
    /// 获取Excel表对应的类名
    /// </summary>
    /// <param name="fullName">文件全名</param>
    /// <returns></returns>
    private static string GetFileName(string fullName)
    {
        fullName = fullName.Split('_')[0];
        return fullName;
    }

    /// <summary>
    /// 获取备注
    /// </summary>
    /// <param name="workSheet"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    private static string GetRemark(ExcelWorksheet workSheet, int col)
    {
        return workSheet.Cells[remarkIndex, col].Text;
    }

    /// <summary>
    /// 获取属性
    /// </summary>
    /// <param name="workSheet">读取的表</param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    private static string[] GetProperties(ExcelWorksheet workSheet)
    {
        string[] properties = new string[workSheet.Dimension.End.Column];
        for(int col = 1;col <= workSheet.Dimension.End.Column;col++)
        {
            if (workSheet.Cells[propertyIndex, col].Text == "")
            {
                throw new System.Exception(string.Format("第{0}行第{1}列为空", propertyIndex, col));
            }
            properties[col - 1] = workSheet.Cells[propertyIndex, col].Text;
        }
        return properties;
    }

    private static string[] GetEnd(ExcelWorksheet workSheet)
    {
        string[] end = new string[workSheet.Dimension.End.Column];
        for (int col = 1; col <= workSheet.Dimension.End.Column; col++)
        {
            if (workSheet.Cells[propertyIndex, col].Text == "")
            {
                ChaosLog.Warn(string.Format("第{0}行第{1}列为空", propertyIndex, col));
            }
            end[col - 1] = workSheet.Cells[endIndex, col].Text;
        }
        return end;
    }

    /// <summary>
    /// 获取值
    /// </summary>
    /// <param name="workSheet"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    private static string[] GetValues(ExcelWorksheet workSheet,int col)
    {
        string[] values = new string[workSheet.Dimension.End.Row - valueIndex + 1];//实际值数组的容量
        for(int row = valueIndex;row <= workSheet.Dimension.End.Row; row++)
        {
            values[row - valueIndex] = workSheet.Cells[row,col].Text;
        }
        return values;
    }

    /// <summary>
    /// 获取类型
    /// </summary>
    /// <param name="workSheet"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    private static string GetType(ExcelWorksheet workSheet,int col)
    {
        return workSheet.Cells[typeIndex, col].Text.Trim();
    }

    /// <summary>
    /// 类型转换
    ///
    /// ══════════════ 类型必须同时过两道关 ══════════════
    /// 1) 必须是【合法的 C# 类型名】—— ExportClass 会把 Excel 第 6 行的文本【原样】当字段类型拼成
    ///    `public {类型} {字段};`，拼出 `public int32 X;` 这种就直接编译不过；
    /// 2) 必须能被【UnityEngine.JsonUtility 正确往返】—— 运行时读取链用的是 JsonUtility.FromJson。
    ///
    /// 【已移除】int32 / int64 / long long：
    ///   它们曾是本方法的 case，但 C# 里并不存在这些类型名，生成的类必然编译失败。
    ///   移除后它们落到 default 分支，在【导出阶段】就以「不支持此类型」明确失败 ——
    ///   早失败好过"导出看起来成功、实际拿到一堆莫名其妙的编译错误"。
    ///
    /// 【已新增】byte / sbyte / short / ushort / bool（均经 JsonUtility 往返实测）。
    ///
    /// 【实测后否决，禁止重新加入】
    ///   · decimal —— JsonUtility 会【静默丢弃】该字段：ToJson 输出里根本没有它，往返后恒为 0。
    ///     这是"无声错值"，比报错危险得多，所以连试都不要试。
    ///   · char    —— 能往返，但 JSON 里存成【数字】（'A' → 65），语义极易被误读。
    ///
    /// 【bool 为什么要归一化】—— 理由【不是】"JsonUtility 要小写"这么简单。
    ///   Lead 2026-10-05 实测（此前"大小写不敏感"的说法【只对带引号形式成立】）：
    ///     · 裸值 {"b":true} / {"b":false}          → 解析成功
    ///     · 裸值 {"b":TRUE} / {"b":True}           → 抛 JSON parse error: Invalid value
    ///     · 带引号 {"b":"true"} / {"b":"TRUE"}     → 解析成功（仅此形式大小写不敏感）
    ///     · {"b":1} / {"b":0}                      → 解析成功（数字）
    ///     · {"b":是} 抛异常；{"b":"是"} 不报错但【静默变 false】（无声错值）
    ///   导出器产出的是【裸值】，所以必须归一化成恰好小写的 true / false：
    ///   否则大写会直接抛异常，中文「是」在语法上就不是合法 JSON。
    ///   绝不能与 string 归到一组加引号，否则 "是" 会解析失败并静默回退 false。
    /// </summary>
    /// <param name="type">Excel 第 6 行的类型文本（须是小写类型名）</param>
    /// <param name="value">单元格文本</param>
    /// <returns>可直接拼进 Json 的片段（string 会加双引号；bool 归一化为裸的 true/false）</returns>
    /// <exception cref="Exception">类型不在白名单内；或 bool 列取值无法识别</exception>
    private static string Convert(string type,string value)
    {
        string res = "";
        switch (type)
        {
            case "int":
                res = value;
                break;
            case "uint":
                res = value;
                break;
            case "long":
                res = value;
                break;
            case "ulong":
                res = value;
                break;
            case "byte":
            case "sbyte":
            case "short":
            case "ushort":
                res = value;
                break;
            case "float":
                res = value;
                break;
            case "double":
                res = value;
                break;
            case "bool":
                {
                    // 归一化成规范的【裸值】true / false（不加引号，不能与 string 同组）。
                    // 取值刻意只留 true/false、1/0、是/否（不区分大小写）：
                    // 多一套同义词（yes/y/no/n）就多一种填错方式。
                    string v = (value ?? "").Trim().ToLowerInvariant();
                    if (v == "true" || v == "1" || v == "是") res = "true";
                    else if (v == "false" || v == "0" || v == "否") res = "false";
                    else throw new Exception($"bool 列取值非法：'{value}'（应为 true/false、1/0 或 是/否）");
                    break;
                }
            case "string":
                res = $"\"{value}\"";
                break;
            default:
                throw new Exception($"不支持此类型: {type}");
        }
        return res;
    }

    /// <summary>
    /// 返回key:value
    /// </summary>
    private static string GetJsonK_VFromKeyAndValues(string key, string value)
    {
        return string.Format("\"{0}\":{1}", key, value);
    }

    /// <summary>
    ///获取[key:value]转换为{key:value,key:value},再变成[{key:value,key:value},{key:value,key:value}]
    /// </summary>
    private static string GetJsonFromJsonK_V(string json, int valueNum)
    {
        string str = "";
        string[] strs;
        List<string> listStr = new List<string>();
        strs = json.Split(',');
        listStr.Clear();
        for (int j = 0; j < valueNum; j++)
        {
            string tempStr = "";
            for (int i = 0;i < strs.Length / valueNum; i++)
            {
                tempStr += strs[j + i * valueNum];
                tempStr += ",";
            }
            tempStr = tempStr.Substring(0, tempStr.Length - 1);
            
            //listStr.Add("{" + string.Format("{0},{1}", strs[j], strs[j + valueNum]) + "}");
            listStr.Add("{" + tempStr + "}");
        }
        str = "[";
        foreach (var l in listStr)
        {
            str += l + ',';
        }
        str = str.Substring(0, str.Length - 1);
        str += ']';
        return str;
    }

    /// <summary>
    /// 适应JsonUtility.FromJson函数的转换格式
    /// </summary>
    private static string GetUnityJsonFromJson(string json)
    {
        return "{" + "\"datas\":" + json + "}";
    }

    /// <summary>
    /// 把紧凑的 Json 文本整理成带缩进与换行的形式，便于人工查看与比较。
    ///
    /// 只影响导出产物的排版：.json 与 .record 的内容完全一致，只是扩展名不同；
    /// JsonUtility.FromJson 不关心空白，所以美化不会影响运行时读取。
    ///
    /// 排版风格与 JsonUtility.ToJson(obj, true) 一致（4 空格缩进、冒号后留空格），
    /// 这样工程里的 Json 文件看起来是同一种格式。
    ///
    /// 注意：必须区分「字符串内部」与「结构符号」。中文数据里如果出现逗号、
    /// 冒号、花括号，不区分就会在字符串中间插换行，把 Json 写坏。
    /// </summary>
    private static string FormatJson(string compact)
    {
        if (string.IsNullOrEmpty(compact)) return compact;

        const char QUOTE = (char)34;      //双引号
        const char BACKSLASH = (char)92;  //反斜杠

        StringBuilder sb = new StringBuilder(compact.Length * 2);
        int depth = 0;//嵌套层级，1 层 = 4 个空格
        bool inString = false;

        for (int i = 0; i < compact.Length; i++)
        {
            char c = compact[i];

            if (inString)//字符串内部原样搬运，只处理转义与结束引号
            {
                sb.Append(c);
                if (c == BACKSLASH && i + 1 < compact.Length)
                    sb.Append(compact[++i]);
                else if (c == QUOTE)
                    inString = false;
                continue;
            }

            switch (c)
            {
                case QUOTE:
                    inString = true;
                    sb.Append(c);
                    break;

                case '{':
                case '[':
                    sb.Append(c);
                    //空容器 {} 或 [] 不换行，免得出现只有一对括号的两行
                    if (i + 1 < compact.Length && compact[i + 1] != '}' && compact[i + 1] != ']')
                    {
                        depth++;
                        sb.Append('\n');
                        sb.Append(' ', depth * 4);
                    }
                    break;

                case '}':
                case ']':
                    //与上面的空容器判断保持对称
                    if (depth > 0 && i > 0 && compact[i - 1] != '{' && compact[i - 1] != '[')
                    {
                        depth--;
                        sb.Append('\n');
                        sb.Append(' ', depth * 4);
                    }
                    sb.Append(c);
                    break;

                case ',':
                    sb.Append(c).Append('\n').Append(' ', depth * 4);
                    break;

                case ':':
                    sb.Append(c).Append(' ');
                    break;

                case ' '://紧凑文本里不该有空白，真有也丢掉，免得出现多余空行
                case '\t':
                case '\r':
                case '\n':
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
