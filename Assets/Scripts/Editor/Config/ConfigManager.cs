using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

//[InitializeOnLoad]
//配置管理器，用于Excel配置表的读取和序列化，导出对应的Json和class
public class ConfigManager
{
    /// <summary>
    /// 备注行
    /// </summary>
    private const int RemarkIndex = 3;
    /// <summary>
    /// 属性行
    /// </summary>
    private const int propertyIndex = 4;
    /// <summary>
    /// 类型行
    /// </summary>
    private const int typeIndex = 5;
    /// <summary>
    /// 值行
    /// </summary>
    private const int valueIndex = 6;

    /// <summary>
    /// 导出配置
    /// </summary>
    [MenuItem("Tool/ExportExcel")]
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

                ExportJson(workSheet, Path.GetFileNameWithoutExtension(GetFileName(file.Name)));//导出Json
                ExportClass(workSheet, Path.GetFileNameWithoutExtension(GetFileName(file.Name)));//导出类
            }

            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    //导出类
    private static void ExportClass(ExcelWorksheet workSheet,string fileName)
    {
        string[] properties = GetProperties(workSheet);
        StringBuilder sb = new StringBuilder();
        sb.Append("using System;\t\n\n");
        sb.Append("[Serializable]\t\n");
        sb.Append($"public class {fileName}Config\n");//类名
        sb.Append("{\n");

        for (int col = 1; col <= properties.Length; col++)
        {
            string fieldType = GetType(workSheet, col);
            string fieldName = properties[col - 1];
            string remarkContent = GetRemark(workSheet, col);
            sb.Append($"\tpublic {fieldType} {fieldName};");
            sb.Append($"//{remarkContent}\n");
        }

        sb.Append("}\n\n");
        FileUtil.SaveFile(GlobalPath.data_ClassPath, string.Format("{0}Config.cs", fileName), sb.ToString());
    }

    //导出Json
    private static void ExportJson(ExcelWorksheet workSheet, string fileName)
    {
        string str = "";
        int num = 0;
        string[] properties = GetProperties(workSheet);
        for (int col = 1; col <= properties.Length; col++)
        {
            string[] temp = GetValues(workSheet, col);
            num = temp.Length;
            foreach (var value in temp)
            {
                str += GetJsonK_VFromKeyAndValues
                    (
                    properties[col - 1],Convert(GetType(workSheet, col), value)
                    ) + ',';
            }
        }
        //获取key:value的字符串
        str = str.Substring(0, str.Length - 1);//去除最小单位语句的最后一个","
        str = GetJsonFromJsonK_V(str, num);//组装成Json列表，并去除列表中最后一个","
        str = GetUnityJsonFromJson(str);//修改为适应JsonUtility的格式

        //用 Unity 自带 JsonUtility（需手动加换行）
        //string json1 = JsonUtility.ToJson(fileContent); // 紧凑格式
        //str = JsonUtility.ToJson(str, true); // 带缩进的格式化格式

        FileUtil.SaveFile(GlobalPath.data_JsonPath, string.Format("{0}Config.json", fileName), str);
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
        return workSheet.Cells[RemarkIndex, col].Text;
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

    /// <summary>
    /// 获取值
    /// </summary>
    /// <param name="workSheet"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    private static string[] GetValues(ExcelWorksheet workSheet,int col)
    {
        string[] values = new string[workSheet.Dimension.End.Row - valueIndex];//实际值数组的容量
        for(int row = valueIndex + 1;row <= workSheet.Dimension.End.Row; row++)
        {
            values[row - valueIndex - 1] = workSheet.Cells[row,col].Text;
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
        return workSheet.Cells[typeIndex, col].Text;
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static string Convert(string type,string value)
    {
        string res = "";
        switch (type)
        {
            case "int":
                res = value;
                break;
            case "int32":
                res = value;
                break;
            case "int64":
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
            case "long long":
                res = value;
                break;
            case "float":
                res = value;
                break;
            case "double":
                res = value;
                break;
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
}
