using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OfficeOpenXml;
using System.IO;
using System;

public class FileUtil : MonoBehaviour
{
    /// <summary>
    /// 加载该路径下所有文件
    /// </summary>
    /// <param name="path">文件夹路径</param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public static FileInfo[] LoadFiles(string path)
    {
        path = string.Format("{0}/{1}",Application.dataPath,path);
        Debug.Log("读取文件路径：" + path);

        if (Directory.Exists(path))
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            List<FileInfo> files = new List<FileInfo>();

            foreach (var file in directory.GetFiles("*"))//遍历该目录下的所有文件
            {
                if (file.Name.EndsWith(".meta"))
                    continue;
                if (file.Name.StartsWith("~")) continue;

                files.Add(file);
            }

            Debug.Log("已获取该路径下所有.xlsx类型文件:\t" + path);
            return files.ToArray();
        }
        else
        {
            throw new System.Exception("路径不存在");
        }
    }

    public static void SaveFile(string path, string fileName, string fileContent)
    {
        path = string.Format("{0}/{1}", Application.dataPath, path);

        if (Directory.Exists(path))
        {
            path = string.Format("{0}/{1}", path, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, fileContent);
        }
        else
        {
            throw new System.Exception("路径不存在");
        }
    }
}
