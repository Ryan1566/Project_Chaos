using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>类型名与物理配置文件名之间的索引，不让业务层依赖工作簿的组织方式。</summary>
public static class ConfigTableAssets
{
    public const string IndexResourcePath = "Data/Json/Runtime/ConfigTableIndex";
    [Serializable] public sealed class Entry
    {
        public string className;
        public string resourcePath;
        public string workbook;
        public string worksheet;
    }
    [Serializable] public sealed class Index
    {
        public int schemaVersion = 1;
        public List<Entry> tables = new List<Entry>();
    }
    static Dictionary<string,string> paths;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reload() { paths = null; }

    /// <summary>接受类型名或旧配置资源路径，统一解析为当前物理资源路径。</summary>
    public static string Resolve(string classNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(classNameOrPath)) throw new ArgumentException("配置类型名为空");
        var className = classNameOrPath.StartsWith(GlobalPath.data_JsonPathToRead,StringComparison.Ordinal)
            ? classNameOrPath.Substring(GlobalPath.data_JsonPathToRead.Length) : classNameOrPath;
        EnsureIndex();
        if (paths.TryGetValue(className,out var path)) return path;
        // 未发布索引的旧工程和测试仍能读取旧文件；索引已发布时缺表必须明确报错。
        if (paths.Count == 0 && Resources.Load<TextAsset>(IndexResourcePath) == null)
            return GlobalPath.data_JsonPathToRead + className;
        throw new InvalidOperationException("配置索引中缺少类型：" + className);
    }
    public static TextAsset Load(string classNameOrPath) { return Resources.Load<TextAsset>(Resolve(classNameOrPath)); }
    static void EnsureIndex()
    {
        if (paths != null) return;
        var next = new Dictionary<string,string>(StringComparer.Ordinal);
        var asset = Resources.Load<TextAsset>(IndexResourcePath);
        if (asset != null)
        {
            var index = JsonUtility.FromJson<Index>(asset.text);
            if (index == null || index.schemaVersion != 1 || index.tables == null)
                throw new InvalidOperationException("配置索引格式或版本不受支持");
            foreach (var entry in index.tables)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.className) || string.IsNullOrWhiteSpace(entry.resourcePath)
                    || !entry.resourcePath.StartsWith(GlobalPath.data_JsonPathToRead,StringComparison.Ordinal)
                    || entry.resourcePath.Contains("..") || entry.resourcePath.Contains("\\") || next.ContainsKey(entry.className))
                    throw new InvalidOperationException("配置索引含非法路径或重复类型");
                next.Add(entry.className,entry.resourcePath);
            }
        }
        // 整个索引验证完成后才发布，坏索引不会留下部分缓存。
        paths = next;
    }
}
