using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OfficeOpenXml;
using System.IO;

public class ImpTableTest : MonoBehaviour 
{
    //表路径记得加后缀
    string filePath = Application.dataPath + "/" + GlobalPath.data_ExcelPath + 
        "/TestTable_测试表" + ".xlsx";
    FileInfo fileInfo = null;

    private void Start()
    {
        fileInfo = new FileInfo(filePath);
        //Debug.Log(ep.File.Name);
        //Debug.Log(ep.Workbook.Worksheets.Count);
        
        ReadFile();
    }

    private void ReadFile()
    {
        // 通过Excel表格的文件信息，打开Excel表格
        using (ExcelPackage ep = new ExcelPackage(fileInfo))
        {
            // 获取Excel表格(从第1张表开始)//指定工作表
            ExcelWorksheet workSheet = ep.Workbook.Worksheets[1];//epplus读取的索引从1开始而非0
            //ExcelWorksheet worksheet1 = excelPackage.Workbook.Worksheets["TestSheet0"];//也可以指定名字

            //读取某段数据
            //string s = workSheet.Cells[7,1].Value.ToString();// 获取表中 第7行第1列的值
            //Debug.Log(s);

            //读取所有数据
            for(int i = 7;i <= workSheet.Dimension.End.Row;i++)
            {
                Debug.Log("编号" + workSheet.Cells[i, 1].Value.ToString() +
              "角色" + workSheet.Cells[i, 2].Value.ToString() +
              "职业" + workSheet.Cells[i, 3].Value.ToString());
            }

            int row = workSheet.Dimension.End.Row;//获得最大行数
            int column = workSheet.Dimension.End.Column;//获得最大列数

            //写入数据
            //workSheet.Cells[4, 1].Value = "50";//指定行和列写入数据

            //workSheet.Cells["C1"].Value = "超级无敌大列巴";//指定单元格写入数据

            //删除表格
            //ep.Workbook.Worksheets.Delete("Sheet2");//删除名为Sheet2，如果Excel文件里没有的话会报错

            //创建表格
            //ep.Workbook.Worksheets.Add("Table");
            /*
            添加名字叫Table的表格（可以自己改名字）
            如果文件存在，则会在表内创建出一张叫Table的表格
            如果文件不存在，就会创建出Excel文件，他最少会有一张表，叫Table
            */

            //保存表格
            //ep.Save(); // 保存表格
        }
    }
}
