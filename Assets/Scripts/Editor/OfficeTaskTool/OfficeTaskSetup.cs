#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>显式执行已核对的配置迁移；保留旧工作簿归档与资源GUID。</summary>
public static class OfficeTaskSetup
{
    [Serializable] sealed class Plan {public List<Row> entries;}
    [Serializable] sealed class Row {public string oldFile,newFile,className,worksheet;}
    const string Source="Assets/Excel/Chaos_excel";
    const string JsonRoot="Assets/Resources/Data/Json/Runtime";
    public static string Install(string stagingDirectory)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("请停止运行后迁移配置");
        var plan=JsonUtility.FromJson<Plan>("{\"entries\":"+File.ReadAllText(Path.Combine(stagingDirectory,"workbook-migration.json"))+"}");
        if(plan?.entries==null||plan.entries.Count!=28)throw new InvalidDataException("合并计划应含25张旧表及3张任务表");
        var files=plan.entries.Select(e=>e.newFile).Distinct().ToArray();
        if(files.Length!=4||files.Any(f=>Path.GetFileName(f)!=f||!File.Exists(Path.Combine(stagingDirectory,"new-workbooks",f))))
            throw new InvalidDataException("缺少四个已验证的合并工作簿");
        var archive=Source+"/_archive_single_sheet";
        var moves=new List<KeyValuePair<string,string>>();
        foreach(var old in plan.entries.Where(e=>!string.IsNullOrEmpty(e.oldFile)).Select(e=>e.oldFile).Distinct())
        {
            if(Path.GetFileName(old)!=old)throw new InvalidDataException("工作簿路径非法");
            if(File.Exists(Source+"/"+old)&&File.Exists(archive+"/"+old))throw new IOException("归档目标已有同名文件："+old);
        }
        foreach(var file in files)if(File.Exists(Source+"/"+file))throw new IOException("合并目标已存在，请先独立核对："+file);
        if(!AssetDatabase.IsValidFolder(archive))AssetDatabase.CreateFolder(Source,"_archive_single_sheet");
        var index=new ConfigTableAssets.Index();
        foreach(var row in plan.entries)index.tables.Add(new ConfigTableAssets.Entry {
            className=row.className,resourcePath=GlobalPath.data_JsonPathToRead+Path.GetFileNameWithoutExtension(row.newFile)+"_"+row.className,
            workbook=row.newFile,worksheet=row.worksheet});
        foreach(var pair in new[]{new[]{"WorldMapConfig","WorldMap_世界观地图表.xlsx","WorldMapSheet0"},new[]{"WorldFactionConfig","WorldFaction_势力范围配置表.xlsx","WorldFactionSheet0"},new[]{"WorldFactionLabelConfig","WorldFaction_势力范围配置表.xlsx","历史名录派生"}})
            index.tables.Add(new ConfigTableAssets.Entry {className=pair[0],resourcePath=GlobalPath.data_JsonPathToRead+Path.GetFileNameWithoutExtension(pair[1])+"_"+pair[0],workbook=pair[1],worksheet=pair[2]});
        foreach(var entry in index.tables)
        {
            string old=JsonRoot+"/"+entry.className+".json",target="Assets/Resources/"+entry.resourcePath+".json";
            if(File.Exists(old)&&File.Exists(target))throw new IOException("新旧配置资源同时存在："+entry.className);
        }
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var row in plan.entries.Where(e=>!string.IsNullOrEmpty(e.oldFile)).GroupBy(e=>e.oldFile).Select(g=>g.First()))
                Move(Source+"/"+row.oldFile,archive+"/"+row.oldFile,moves);
            foreach(var file in files)File.Copy(Path.Combine(stagingDirectory,"new-workbooks",file),Source+"/"+file,false);
            foreach(var entry in index.tables)
            {
                string old=JsonRoot+"/"+entry.className+".json";
                if(File.Exists(old))Move(old,"Assets/Resources/"+entry.resourcePath+".json",moves);
            }
            File.WriteAllText(JsonRoot+"/ConfigTableIndex.json",JsonUtility.ToJson(index,true));
        }
        catch
        {
            // 仅回退本操作已确认移动的资源，不删除或覆盖任何原有文件。
            foreach(var move in moves.AsEnumerable().Reverse())if(File.Exists(move.Value))AssetDatabase.MoveAsset(move.Value,move.Key);
            throw;
        }
        finally {AssetDatabase.StopAssetEditing();AssetDatabase.Refresh();ConfigTableAssets.Reload();}
        InstallPanel();
        return "已合并4本工作簿、归档25本原表、重命名28个旧JSON；下一步显式执行新导出并逐表比较";
    }
    static void Move(string source,string target,List<KeyValuePair<string,string>> moved)
    {
        var root=Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(source).StartsWith(root,StringComparison.OrdinalIgnoreCase)||!Path.GetFullPath(target).StartsWith(root,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("资源迁移超出Assets目录");
        var error=AssetDatabase.MoveAsset(source,target);if(!string.IsNullOrEmpty(error))throw new IOException(error);
        moved.Add(new KeyValuePair<string,string>(source,target));
    }
    public static void InstallPanel()
    {
        InstallPanelPrefab(typeof(OfficeTasksPanel));InstallPanelPrefab(typeof(PausePanel));
    }
    static void InstallPanelPrefab(Type type)
    {
        string path="Assets/Resources/UIPanels/"+type.Name+".prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
        var original=SceneManager.GetActiveScene();var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(temporary);
            var go=new GameObject(type.Name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(UIPanelAnimator),type);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            PrefabUtility.SaveAsPrefabAsset(go,path);
        }
        finally {if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(temporary,true);}
    }
}
#endif
