using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>按已核对清单统一迁移英文配置名称，保留资源GUID，并在失败时恢复原文件与引用。</summary>
public static class ConfigEnglishMigration
{
    [Serializable] sealed class Plan {public List<Row> entries;}
    [Serializable] sealed class Row {public string oldFile,newFile,worksheet,oldClass,baseName,className,oldResourcePath,newResourcePath;public int index;}
    [Serializable] sealed class Books {public List<Book> entries;}
    [Serializable] sealed class Book {public string oldFile,newFile,sha256;}
    [Serializable] sealed class Patches {public List<Patch> entries;}
    [Serializable] sealed class Patch {public string path,beforeSha256,afterSha256;}
    const string Excel="Assets/Excel/Chaos_excel/";
    const string Classes="Assets/Scripts/Runtime/MVP/Model/ConfigData/";
    static string Hash(string path)
    {using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    static void RequireAssetPath(string path)
    {
        string root=Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(path).StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("目标超出Assets："+path);
    }
    static void Move(string from,string to,List<KeyValuePair<string,string>> moved)
    {
        RequireAssetPath(from);RequireAssetPath(to);
        string error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new IOException(error);
        moved.Add(new KeyValuePair<string,string>(from,to));
    }
    public static string Apply(string folder)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("迁移前请停止运行");
        var plan=JsonUtility.FromJson<Plan>("{\"entries\":"+File.ReadAllText(Path.Combine(folder,"plan-final.json"))+"}");
        var books=JsonUtility.FromJson<Books>("{\"entries\":"+File.ReadAllText(Path.Combine(folder,"source-final.json"))+"}");
        var patches=JsonUtility.FromJson<Patches>("{\"entries\":"+File.ReadAllText(Path.Combine(folder,"reference-patches.json"))+"}");
        if(plan?.entries?.Count!=31||books?.entries?.Count!=14||patches?.entries==null)throw new InvalidDataException("迁移清单数量不完整");
        if(!File.Exists("Assets/QA/ConfigEnglish/preview-equality.txt"))throw new InvalidDataException("尚未完成独立暂存数据比较");
        foreach(var book in books.entries)
        {
            if(Path.GetFileName(book.oldFile)!=book.oldFile||Path.GetFileName(book.newFile)!=book.newFile)throw new InvalidDataException("工作簿路径非法");
            string source=Excel+book.oldFile,target=Excel+book.newFile;
            if(!File.Exists(source)||Hash(source)!=book.sha256||Hash(Path.Combine(folder,"before/Excel",book.oldFile))!=book.sha256)
                throw new IOException("原表变更或备份不完整："+source);
            if(source!=target&&File.Exists(target))throw new IOException("目标表已存在："+target);
            if(!File.Exists(Path.Combine(folder,"workbooks",book.newFile)))throw new IOException("缺少暂存表："+book.newFile);
        }
        foreach(var row in plan.entries)
        {
            if(row.className!=ConfigExportNaming.GeneratedClassName(row.newFile,row.baseName,"Config")
                ||row.newResourcePath!="Data/Json/Runtime/"+Path.GetFileNameWithoutExtension(ConfigExportNaming.DataFileName(row.newFile,row.baseName,".json")))
                throw new InvalidDataException("清单与最终命名规则不一致："+row.oldClass);
            string oldJson="Assets/Resources/"+row.oldResourcePath+".json",newJson="Assets/Resources/"+row.newResourcePath+".json";
            RequireAssetPath(oldJson);RequireAssetPath(newJson);
            if(!File.Exists(oldJson)||File.Exists(newJson)||!File.Exists(Classes+row.oldClass+".cs")||File.Exists(Classes+row.className+".cs"))
                throw new IOException("配置源缺失或新目标已存在："+row.oldClass);
            if(!File.Exists(Path.Combine(folder,"preview-classes",row.className+".cs")))throw new IOException("缺少暂存Config脚本："+row.className);
        }
        foreach(var patch in patches.entries)
        {
            string path="Assets/"+patch.path;RequireAssetPath(path);
            if(!patch.path.StartsWith("Scripts/",StringComparison.Ordinal)||Hash(path)!=patch.beforeSha256
                ||Hash(Path.Combine(folder,"patched-scripts",patch.path))!=patch.afterSha256)
                throw new IOException("引用文件已变更或暂存不一致："+path);
        }
        var index=new ConfigTableAssets.Index();
        foreach(var row in plan.entries)index.tables.Add(new ConfigTableAssets.Entry{className=row.className,resourcePath=row.newResourcePath,workbook=Path.GetFileNameWithoutExtension(row.newFile),worksheet=row.worksheet});
        var moved=new List<KeyValuePair<string,string>>();bool repairedMeta=false;
        AssetDatabase.StartAssetEditing();
        try
        {
            // 用户从文件管理器改名了样表时，将原meta随文件补齐，避免无意更换工作簿GUID。
            string orphan=Excel+"测试表.xlsx.meta",sampleMeta=Excel+"TestTable_测试表.xlsx.meta";
            if(File.Exists(orphan)&&!File.Exists(sampleMeta)){File.Move(orphan,sampleMeta);repairedMeta=true;}
            foreach(var book in books.entries)
            {
                string target=Excel+book.newFile;
                if(book.oldFile!=book.newFile)Move(Excel+book.oldFile,target,moved);
                File.Copy(Path.Combine(folder,"workbooks",book.newFile),target,true);
            }
            foreach(var row in plan.entries)
            {
                Move("Assets/Resources/"+row.oldResourcePath+".json","Assets/Resources/"+row.newResourcePath+".json",moved);
                Move(Classes+row.oldClass+".cs",Classes+row.className+".cs",moved);
                File.Copy(Path.Combine(folder,"preview-classes",row.className+".cs"),Classes+row.className+".cs",true);
            }
            foreach(var patch in patches.entries)File.Copy(Path.Combine(folder,"patched-scripts",patch.path),"Assets/"+patch.path,true);
            File.WriteAllText("Assets/Resources/"+ConfigTableAssets.IndexResourcePath+".json",JsonUtility.ToJson(index,true));
            File.WriteAllText("Assets/QA/ConfigEnglish/published.txt","14 workbooks; 31 JSON and Config assets; "+patches.entries.Count+" reference files\nBackup: "+folder);
        }
        catch(Exception original)
        {
            var errors=new List<string>();
            Action<Action> restore=action=>{try{action();}catch(Exception ex){errors.Add(ex.Message);}};
            foreach(var patch in patches.entries)restore(()=>File.Copy(Path.Combine(folder,"before/references",patch.path),"Assets/"+patch.path,true));
            foreach(var row in plan.entries)
                if(File.Exists(Classes+row.className+".cs"))restore(()=>File.Copy(Path.Combine(folder,"before/ConfigData",row.oldClass+".cs"),Classes+row.className+".cs",true));
            foreach(var book in books.entries)
                if(File.Exists(Excel+book.newFile))restore(()=>File.Copy(Path.Combine(folder,"before/Excel",book.oldFile),Excel+book.newFile,true));
            foreach(var pair in moved.AsEnumerable().Reverse())restore(()=>{var error=AssetDatabase.MoveAsset(pair.Value,pair.Key);if(!string.IsNullOrEmpty(error))throw new IOException(error);});
            if(repairedMeta)restore(()=>File.Move(Excel+"TestTable_测试表.xlsx.meta",Excel+"测试表.xlsx.meta"));
            restore(()=>File.Copy(Path.Combine(folder,"before/Runtime/ConfigTableIndex.json"),"Assets/Resources/"+ConfigTableAssets.IndexResourcePath+".json",true));
            throw new IOException("迁移失败，已尝试恢复；原始错误："+original.Message+"；恢复问题："+string.Join("；",errors),original);
        }
        finally {AssetDatabase.StopAssetEditing();ConfigTableAssets.Reload();AssetDatabase.Refresh();}
        return "已迁移14本工作簿、31张JSON与Config脚本，更新"+patches.entries.Count+"个引用文件；资源GUID通过重命名保留。";
    }
}
