using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>显式执行已核对的小模块配置迁移。发布和退役旧表分开，旧数据先备份再逐项校验。</summary>
public static class ConfigModuleMigration
{
    const string ExcelRoot="Assets/Excel/Chaos_excel";
    [Serializable] sealed class Plan { public List<Row> entries; }
    [Serializable] sealed class Row { public string className,oldFile,oldSheet,newFile,sheet;public int index; }
    [Serializable] sealed class Sources { public List<Source> entries; }
    [Serializable] sealed class Source { public string file,sha256; }
    static Plan ReadPlan(string folder)
    {
        var plan=JsonUtility.FromJson<Plan>("{\"entries\":"+File.ReadAllText(Path.Combine(folder,"plan.json"))+"}");
        if(plan?.entries==null||plan.entries.Count!=30||plan.entries.Select(r=>r.className).Distinct().Count()!=30)
            throw new InvalidDataException("迁移清单必须完整覆盖30张不同配置表");
        foreach(var row in plan.entries)
            if(Path.GetFileName(row.newFile)!=row.newFile||!row.newFile.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase)
                ||string.IsNullOrWhiteSpace(row.sheet)||row.index<1||!File.Exists(Path.Combine(folder,"workbooks",row.newFile)))
                throw new InvalidDataException("迁移目标或页签位置非法："+row.newFile);
        if(plan.entries.Select(r=>r.newFile).Distinct().Count()!=14)throw new InvalidDataException("迁移清单应有14本工作簿");
        return plan;
    }
    static string Hash(string path)
    {
        using(var sha=SHA256.Create())using(var file=File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
    }
    static void RequireEditMode()
    {if(EditorApplication.isPlaying)throw new InvalidOperationException("请停止运行后迁移配置");}
    // 使用本轮已验证的持久备份，不依赖可能被系统清理的临时目录副本。
    static string BackupRoot(string staging)
    {return File.ReadAllText(Path.Combine(staging,"backup-location.txt")).Trim();}
    static void RequireSourceBackup(string staging,string relativePath)
    {
        string root=Path.GetFullPath(ExcelRoot)+Path.DirectorySeparatorChar;
        string actual=Path.GetFullPath(Path.Combine(ExcelRoot,relativePath));
        string backup=Path.Combine(BackupRoot(staging),"Excel",relativePath);
        if(!actual.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!File.Exists(backup)||Hash(actual)!=Hash(backup))
            throw new IOException("原表已变更或缺少相同备份，停止迁移："+actual);
    }
    public static string Publish(string staging)
    {
        RequireEditMode();var plan=ReadPlan(staging);
        var sources=JsonUtility.FromJson<Sources>("{\"entries\":"+File.ReadAllText(Path.Combine(staging,"source.json"))+"}");
        foreach(var source in sources.entries)
        {
            RequireSourceBackup(staging,source.file);
            if(Hash(Path.Combine(ExcelRoot,source.file))!=source.sha256)throw new IOException("暂存表已过时："+source.file);
        }
        var files=plan.entries.Select(r=>r.newFile).Distinct().ToArray();
        foreach(var file in files)
            if(File.Exists(Path.Combine(ExcelRoot,file))&&!sources.entries.Any(s=>s.file==file))
                throw new IOException("目标已经存在，拒绝覆盖："+file);
        var pathByType=plan.entries.ToDictionary(r=>r.className);
        string indexPath="Assets/Resources/"+ConfigTableAssets.IndexResourcePath+".json";
        var index=JsonUtility.FromJson<ConfigTableAssets.Index>(File.ReadAllText(indexPath));
        if(index.tables.Count!=31)throw new InvalidDataException("迁移前索引应为31项");
        var moves=new List<KeyValuePair<string,string>>();
        foreach(var entry in index.tables)
        {
            if(!pathByType.TryGetValue(entry.className,out var row))
            {if(entry.className!="WorldFactionLabelConfig")throw new InvalidDataException("未知配置类型："+entry.className);}
            string target="Data/Json/Runtime/"+(row==null?"世界观地图表":Path.GetFileNameWithoutExtension(row.newFile))+"_"+entry.className;
            if(target!=entry.resourcePath)
            {
                string old="Assets/Resources/"+entry.resourcePath+".json",next="Assets/Resources/"+target+".json";
                if(!File.Exists(old)||File.Exists(next))throw new IOException("配置迁移源缺失或目标冲突："+entry.className);
                moves.Add(new KeyValuePair<string,string>(old,next));
            }
        }
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var file in files)File.Copy(Path.Combine(staging,"workbooks",file),Path.Combine(ExcelRoot,file),true);
            foreach(var move in moves)
            {var error=AssetDatabase.MoveAsset(move.Key,move.Value);if(!string.IsNullOrEmpty(error))throw new IOException(error);}
            foreach(var entry in index.tables)
                if(pathByType.TryGetValue(entry.className,out var row))
                {entry.resourcePath="Data/Json/Runtime/"+Path.GetFileNameWithoutExtension(row.newFile)+"_"+row.className;entry.workbook=Path.GetFileNameWithoutExtension(row.newFile);entry.worksheet=row.sheet;}
                else if(entry.className=="WorldFactionLabelConfig")
                {entry.resourcePath="Data/Json/Runtime/世界观地图表_WorldFactionLabelConfig";entry.workbook="世界观地图表";entry.worksheet="历史名录派生";}
            File.WriteAllText(indexPath,JsonUtility.ToJson(index,true));
        }
        finally {AssetDatabase.StopAssetEditing();ConfigTableAssets.Reload();AssetDatabase.Refresh();}
        return "已发布14本中文页签工作簿并保留GUID迁移"+moves.Count+"张JSON；旧表尚未退役，须先导出并比较31张JSON。";
    }
    public static string RetireVerifiedSources(string staging)
    {
        RequireEditMode();var plan=ReadPlan(staging);
        var targets=new HashSet<string>(plan.entries.Select(r=>r.newFile),StringComparer.OrdinalIgnoreCase);
        var oldFiles=plan.entries.Select(r=>r.oldFile).Distinct().Where(f=>!targets.Contains(f)).ToArray();
        foreach(var file in oldFiles){RequireSourceBackup(staging,file);RequireSourceBackup(staging,file+".meta");}
        string archive=ExcelRoot+"/_archive_single_sheet";
        if(Directory.Exists(archive))
        {
            // 先核对整个归档目录，不允许递归操作未知内容或越出配置根目录。
            var archiveFull=Path.GetFullPath(archive);
            if(archiveFull!=Path.Combine(Path.GetFullPath(ExcelRoot),"_archive_single_sheet"))throw new IOException("归档路径不匹配");
            foreach(var file in Directory.GetFiles(archive,"*",SearchOption.AllDirectories))
                RequireSourceBackup(staging,file.Substring(ExcelRoot.Length+1));
            RequireSourceBackup(staging,"_archive_single_sheet.meta");
        }
        if(!File.Exists("Assets/QA/ConfigModules/data-equality.txt"))throw new IOException("缺少主控独立数据比较记录");
        foreach(var file in oldFiles)
            if(!AssetDatabase.DeleteAsset(ExcelRoot+"/"+file))throw new IOException("无法退役旧表："+file);
        if(Directory.Exists(archive)&&!AssetDatabase.DeleteAsset(archive))throw new IOException("无法退役重复归档目录");
        AssetDatabase.Refresh();
        return "已退役6本旧分组表和25本重复归档表，原文件与元数据保存在本轮外部备份目录。";
    }
}
