using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Optional authoring metadata. Exported WorldFaction columns remain unchanged.</summary>
public static class WorldMapHistoricalAtlas
{
    public const string AssetPath = WorldMapToolDataIO.ToolFolderAssetPath + "/WorldMapHistoricalAtlas.json";
    [Serializable] public sealed class Faction
    {
        public uint id;
        public string name, capital, notes, allegiance;
        public float labelX, labelY, capitalX, capitalY;
    }
    [Serializable] public sealed class Stage
    {
        public uint stage;
        public int yearBce;
        public string title, summary;
        public Faction[] factions;
    }
    [Serializable] private sealed class Atlas { public Stage[] stages; }
    private static Atlas _atlas;
    private static DateTime _stamp;
    private static string _loadedPath;

    public static Stage Read(uint stage)
    {
        string path = WorldMapToolDataIO.AssetPathToAbsolute(AssetPath);
        if (!File.Exists(path)) return null;
        DateTime stamp = File.GetLastWriteTimeUtc(path);
        if (_atlas == null || _stamp != stamp || path != _loadedPath)
        {
            try { _atlas = JsonUtility.FromJson<Atlas>(File.ReadAllText(path)); _stamp = stamp; _loadedPath = path; }
            catch { return null; }
        }
        if (_atlas?.stages != null) foreach (var item in _atlas.stages) if (item.stage == stage) return item;
        return null;
    }
    public static string Name(uint stage, uint id)
    {
        var data = Read(stage);
        if (data?.factions != null) foreach (var faction in data.factions) if (faction.id == id) return faction.name;
        return "势力 " + id;
    }

    public static void Draw(WorldMapDraft draft, uint stage, Rect imageRect)
    {
        if (draft.sourceImagePath != WorldMapToolDataIO.DefaultSourceImagePath) return;
        var data = Read(stage);
        if (data?.factions == null) return;
        var style = new GUIStyle(EditorStyles.whiteBoldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        foreach (var faction in data.factions)
        {
            bool exists = draft.factionVertices.Exists(v => v != null && v.enable && v.stage == stage && v.factionId == faction.id);
            if (!exists) continue;
            Vector2 point = WorldMapToolGeometry.NormalizedToCanvas(new Vector2(faction.labelX, faction.labelY), imageRect);
            string label = faction.name;
            if (label.Contains("（")) label = label.Substring(0, label.IndexOf('（'));
            var content = new GUIContent(label, faction.name + "\n" + faction.capital + "\n" + faction.notes);
            Vector2 size = style.CalcSize(content) + new Vector2(10, 4);
            var rect = new Rect(point.x - size.x / 2, point.y - size.y / 2, size.x, size.y);
            EditorGUI.DrawRect(rect, new Color(.08f, .07f, .05f, .62f)); GUI.Label(rect, content, style);
        }
    }
}
