using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OfficeOpenXml;
using UnityEngine;

/// <summary>在独立临时目录验证共享地图工作簿，重点检查另一页、额外页和发布失败时的数据保留。</summary>
public static class WorldMapWorkbookVerification
{
    static readonly List<string> Lines=new List<string>();
    static void Check(bool condition,string message)
    {if(!condition)throw new Exception(message);Lines.Add("PASS "+message);}
    static string Hash(string path)
    {using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file));}
    static string Snapshot(ExcelWorksheet sheet)
    {
        var parts=new List<string>{sheet.Column(2).Width.ToString(CultureInfo.InvariantCulture),sheet.Row(3).Height.ToString(CultureInfo.InvariantCulture)};
        if(sheet.Dimension!=null)foreach(var cell in sheet.Cells[sheet.Dimension.Address])
            if(cell.Value!=null||!string.IsNullOrEmpty(cell.Formula))
                parts.Add(cell.Address+"|"+Convert.ToString(cell.Value,CultureInfo.InvariantCulture)+"|"+cell.Formula+"|"+cell.Style.Numberformat.Format+"|"+cell.Style.Font.Bold+"|"+cell.Style.Fill.PatternType+"|"+cell.Style.Fill.BackgroundColor.Rgb);
        return string.Join("\n",parts);
    }
    public static string Run()
    {
        Lines.Clear();var root=Path.Combine(Path.GetTempPath(),"ChaosWorldWorkbook-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var draft=new WorldMapDraft();draft.points.Add(new WorldMapPointEntry{x=.4f,y=.4f});
        foreach(var p in new[]{new Vector2(.1f,.1f),new Vector2(.3f,.1f),new Vector2(.2f,.3f)})draft.factionVertices.Add(new WorldMapFactionVertex{x=p.x,y=p.y});
        string path=Path.Combine(root,WorldMapToolXlsxExporter.MapTableFileName);
        Check(WorldMapToolXlsxExporter.MapTableFileName==WorldMapToolXlsxExporter.FactionMergedFileName,"both public constants select the same workbook");
        var result=WorldMapToolXlsxExporter.ExportMapTable(draft,root);Check(result.Success,"map-only first export succeeds: "+result.Message);
        using(var book=new ExcelPackage(new FileInfo(path)))Check(book.Workbook.Worksheets.Count==2,"map-only first export reserves both fixed page positions");
        result=WorldMapToolXlsxExporter.ExportFactionMergedTable(draft,root);Check(result.Success,"faction second export succeeds: "+result.Message);
        string extraBefore,factionBefore,mapBefore;
        using(var book=new ExcelPackage(new FileInfo(path)))
        {
            var extra=book.Workbook.Worksheets.Add("附加说明");extra.Cells[1,2].Value=7;extra.Cells[3,2].Formula="B1*2";extra.Column(2).Width=31;extra.Row(3).Height=43;extra.Cells[1,2].Style.Font.Bold=true;
            extra.Cells[1,2].Style.Fill.PatternType=OfficeOpenXml.Style.ExcelFillStyle.Solid;extra.Cells[1,2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Green);
            var map=ExcelWorksheetAccess.GetByIndex(book.Workbook,1);var faction=ExcelWorksheetAccess.GetByIndex(book.Workbook,2);
            map.Name="临时地点名";faction.Name="临时疆域名";map.Cells[1,2].Value="WorldMapConfig";faction.Cells[1,2].Value="WorldFactionConfig";book.Save();
        }
        using(var book=new ExcelPackage(new FileInfo(path)))
        {factionBefore=Snapshot(ExcelWorksheetAccess.GetByIndex(book.Workbook,2));extraBefore=Snapshot(ExcelWorksheetAccess.GetByIndex(book.Workbook,3));}
        draft.points[0].x=.6f;result=WorldMapToolXlsxExporter.ExportMapTable(draft,root);Check(result.Success,"renamed map page with full class name is selected by index: "+result.Message);
        using(var book=new ExcelPackage(new FileInfo(path)))
        {Check(Snapshot(ExcelWorksheetAccess.GetByIndex(book.Workbook,2))==factionBefore,"map export preserves faction values, styles and layout");Check(Snapshot(ExcelWorksheetAccess.GetByIndex(book.Workbook,3))==extraBefore,"map export preserves extra page values, formula and layout");mapBefore=Snapshot(ExcelWorksheetAccess.GetByIndex(book.Workbook,1));}
        result=WorldMapToolXlsxExporter.ExportFactionMergedTable(draft,root);Check(result.Success,"renamed faction page with full class name is selected by index: "+result.Message);
        using(var book=new ExcelPackage(new FileInfo(path)))
        {Check(Snapshot(ExcelWorksheetAccess.GetByIndex(book.Workbook,1))==mapBefore,"faction export preserves map data");Check(Snapshot(ExcelWorksheetAccess.GetByIndex(book.Workbook,3))==extraBefore,"faction export preserves extra page values, formula and layout");}
        string before=Hash(path);
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
        {result=WorldMapToolXlsxExporter.ExportMapTable(draft,root);Check(!result.Success,"target read-only sharing lock rejects export");}
        Check(Hash(path)==before,"failed publication leaves original workbook byte-identical");
        using(var book=new ExcelPackage(new FileInfo(path))){ExcelWorksheetAccess.GetByIndex(book.Workbook,1).Cells[1,2].Value="OtherConfig";book.Save();}
        before=Hash(path);result=WorldMapToolXlsxExporter.ExportMapTable(draft,root);Check(!result.Success&&Hash(path)==before,"wrong B1 type is rejected without modifying file");
        using(var book=new ExcelPackage(new FileInfo(path))){var page=ExcelWorksheetAccess.GetByIndex(book.Workbook,1);page.Cells.Clear();page.Cells[3,3].Value="需要保留的用户资料";book.Save();}
        before=Hash(path);result=WorldMapToolXlsxExporter.ExportMapTable(draft,root);Check(!result.Success&&Hash(path)==before,"empty B1 with real user content is not treated as blank placeholder");
        var second=Path.Combine(root,"faction-first");Directory.CreateDirectory(second);result=WorldMapToolXlsxExporter.ExportFactionMergedTable(draft,second);Check(result.Success,"faction-only first export succeeds");
        using(var book=new ExcelPackage(new FileInfo(Path.Combine(second,WorldMapToolXlsxExporter.FactionMergedFileName))))
        {Check(book.Workbook.Worksheets.Count==2&&ExcelWorksheetAccess.GetByIndex(book.Workbook,2).Cells[1,2].Text=="WorldFaction","faction-only first export occupies second page");}
        Lines.Add("Temporary directory: "+root);Directory.CreateDirectory(Application.dataPath+"/QA/ConfigModules");string text=string.Join("\n",Lines);File.WriteAllText(Application.dataPath+"/QA/ConfigModules/worldmap-workbook.txt",text);return text;
    }
}
