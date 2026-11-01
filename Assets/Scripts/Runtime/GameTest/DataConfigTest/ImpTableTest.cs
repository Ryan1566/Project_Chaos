using UnityEngine;
#if UNITY_EDITOR
using OfficeOpenXml;
using System.IO;
using ChaosDebug;
#endif

/// <summary>仅在编辑器中演示按位置读取测试工作簿，不依赖中文页签名称。</summary>
public class ImpTableTest : MonoBehaviour
{
#if UNITY_EDITOR
    private void Start()
    {
        string path = Path.Combine(Application.dataPath, "Excel/Chaos_excel/TestTable_测试表.xlsx");
        if (!File.Exists(path))
        {
            ChaosLog.Error(LogChannel.Config, "测试工作簿不存在：" + path);
            return;
        }
        using (var package = new ExcelPackage(new FileInfo(path)))
        {
            var sheet = ExcelWorksheetAccess.GetByIndex(package.Workbook, 1);
            if (sheet.Dimension == null) return;
            for (int row = 7; row <= sheet.Dimension.End.Row; row++)
            {
                ChaosLog.Info(LogChannel.Config, "序号：" + sheet.Cells[row, 1].Text
                    + "，角色：" + sheet.Cells[row, 2].Text + "，职业：" + sheet.Cells[row, 3].Text);
            }
        }
    }
#endif
}
