#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit authoring sync. The source remains the historical atlas metadata used by the map tool.</summary>
public static class WorldFactionLabelExport
{
    [Serializable] sealed class Atlas { public List<Era> stages; }
    [Serializable] sealed class Era { public uint stage; public List<Faction> factions; }
    [Serializable] sealed class Faction { public uint id; public string name; public float labelX, labelY; }
    [Serializable] sealed class Output { public int schemaVersion = 1; public List<WorldFactionLabelConfig> datas; }
    [MenuItem("Tools/世界观地图/同步运行时势力名称")]
    public static void Export()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请在编辑模式同步势力名称");
        string source = Application.dataPath + "/Excel/Chaos_excel/_worldmap_tool/WorldMapHistoricalAtlas.json";
        var atlas = JsonUtility.FromJson<Atlas>(File.ReadAllText(source));
        var output = new Output { datas = new List<WorldFactionLabelConfig>() };
        var seen = new HashSet<string>();
        foreach (var era in atlas.stages) foreach (var faction in era.factions)
        {
            if (era.stage < 1 || era.stage > 5 || faction.id == 0 || string.IsNullOrWhiteSpace(faction.name)
                || !seen.Add(era.stage + ":" + faction.id)) throw new InvalidDataException("历史名录有非法或重复条目");
            var point = new Vector2(faction.labelX, faction.labelY);
            if (!WorldFactionLabelCatalog.Contains(WorldMapService.Instance.Areas(era.stage), faction.id, point))
                throw new InvalidDataException("势力名称锚点不在本国领地内：" + era.stage + "/" + faction.name);
            output.datas.Add(new WorldFactionLabelConfig { Stage = era.stage, FactionId = faction.id, Name = faction.name, X = point.x, Y = point.y });
        }
        const string path = "Assets/Resources/Data/Json/Runtime/WorldFactionLabelConfig.json";
        File.WriteAllText(path, JsonUtility.ToJson(output, true));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        WorldFactionLabelCatalog.Reload();
        Debug.Log("[世界观地图] 已同步 " + output.datas.Count + " 个时代势力名称");
    }
}
#endif
