using System;
using System.Collections.Generic;
using Chaos.MVP.Model.WorldMap;
using UnityEngine;

/// <summary>Stage-specific names and anchors. Views receive complete display data and never read authoring files.</summary>
public static class WorldFactionLabelCatalog
{
    static List<WorldFactionLabelConfig> _entries;
    public static void Reload() { _entries = null; }
    static void Load()
    {
        if (_entries != null) return;
        _entries = new List<WorldFactionLabelConfig>();
        var asset = Resources.Load<TextAsset>(GlobalPath.data_JsonPathToRead + "WorldFactionLabelConfig");
        if (asset == null) return;
        try { _entries = JsonUtility.FromJson<DataList<WorldFactionLabelConfig>>(asset.text)?.datas ?? _entries; }
        catch (Exception error) { Debug.LogWarning("[世界观地图] 势力名称配置无效：" + error.Message); }
    }
    public static List<WorldFactionLabel> Read(uint stage, List<WorldFactionArea> areas)
    {
        Load();
        var result = new List<WorldFactionLabel>();
        if (areas == null) return result;
        var seen = new HashSet<uint>();
        foreach (var area in areas)
        {
            if (area == null || !area.IsValid || !seen.Add(area.factionId)) continue;
            var entry = _entries.Find(e => e != null && e.Stage == stage && e.FactionId == area.factionId);
            Vector2 position = entry == null ? Vector2.zero : new Vector2(entry.X, entry.Y);
            if (!Contains(areas, area.factionId, position)) position = FindInterior(areas, area.factionId);
            result.Add(new WorldFactionLabel { FactionId = area.factionId,
                Name = entry == null || string.IsNullOrWhiteSpace(entry.Name) ? "势力 " + area.factionId : entry.Name,
                Position = position });
        }
        return result;
    }
    public static bool Contains(List<WorldFactionArea> areas, uint id, Vector2 point)
    {
        if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y)
            || point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) return false;
        foreach (var area in areas) if (area != null && area.IsValid && area.factionId == id && area.Contains(point)) return true;
        return false;
    }
    static Vector2 FindInterior(List<WorldFactionArea> areas, uint id)
    {
        WorldFactionArea largest = null; float largestSize = -1;
        foreach (var area in areas)
        {
            if (area == null || !area.IsValid || area.factionId != id) continue;
            float sum = 0;
            for (int i = 0; i < area.vertices.Count; i++)
            { var a = area.vertices[i]; var b = area.vertices[(i + 1) % area.vertices.Count]; sum += a.x * b.y - b.x * a.y; }
            if (Mathf.Abs(sum) > largestSize) { largest = area; largestSize = Mathf.Abs(sum); }
        }
        if (largest == null) return Vector2.zero;
        Vector2 min = Vector2.one, max = Vector2.zero, center = Vector2.zero;
        foreach (var vertex in largest.vertices) { min = Vector2.Min(min, vertex); max = Vector2.Max(max, vertex); center += vertex; }
        center /= largest.vertices.Count;
        if (largest.Contains(center)) return center;
        // A small deterministic interior search handles concavity; no per-frame raster or scene query.
        float best = float.MaxValue; Vector2 selected = largest.vertices[0];
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            var point = new Vector2(Mathf.Lerp(min.x, max.x, (x + .5f) / 32), Mathf.Lerp(min.y, max.y, (y + .5f) / 32));
            float distance = (point - center).sqrMagnitude;
            if (distance < best && largest.Contains(point)) { best = distance; selected = point; }
        }
        return selected;
    }
}
