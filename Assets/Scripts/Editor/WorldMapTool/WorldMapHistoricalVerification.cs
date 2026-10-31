using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>Targeted authoring, raster-fill and Excel/JSON/runtime round-trip verification.</summary>
public static class WorldMapHistoricalVerification
{
    [Serializable] private sealed class Rows { public List<WorldFactionConfig> datas; }
    [Serializable] private sealed class Atlas { public WorldMapHistoricalAtlas.Stage[] stages; }
    private static string Qa => Application.dataPath + "/QA/WorldMapHistory/";
    private static int _checks;
    private static readonly List<string> Log = new List<string>();
    private static void Check(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); _checks++; Log.Add("PASS " + message); }
    private static string Hash(string path)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""); }
    private static WorldMapDraft Candidate() => JsonUtility.FromJson<WorldMapDraft>(File.ReadAllText(Qa + "candidate-draft.json"));

    public static string ValidateCandidate()
    {
        _checks = 0; Log.Clear(); var draft = Candidate();
        Check(draft.imageWidth == 4096 && draft.imageHeight == 3138, "original PNG coordinate system");
        Check(WorldMapToolValidator.ValidateFactionTables(draft, false).Count == 0, "all candidate polygons valid and counterclockwise");
        int[] expected = { 0, 7, 1, 8, 19, 11 };
        for (uint stage = 1; stage <= 5; stage++)
            Check(draft.factionVertices.Where(v => v.stage == stage).Select(v => v.factionId).Distinct().Count() == expected[stage], "polity count stage " + stage);
        using (var fill = new WorldMapToolPolygonOverlay())
        {
            var concave = new WorldMapDraft();
            foreach (var p in new[] { new Vector2(.1f,.1f),new Vector2(.9f,.1f),new Vector2(.9f,.4f),new Vector2(.4f,.4f),new Vector2(.4f,.9f),new Vector2(.1f,.9f) })
                concave.factionVertices.Add(new WorldMapFactionVertex { stage = 1, x = p.x, y = p.y, color = "#FF0000" });
            var mask = fill.Get(concave, 1);
            Check(Alpha(mask,.2f,.8f) > 0 && Alpha(mask,.8f,.8f) == 0, "concave bay stays transparent inside bounding box");
            Check(mask == fill.Get(concave, 1), "unchanged mask reuses texture");
            Check(Alpha(fill.Get(concave, 2),.2f,.8f) == 0, "other period never leaks into current fill");
            concave.factionVertices[3].x = .85f;
            Check(Alpha(fill.Get(concave, 1),.6f,.45f) > 0, "editing a vertex invalidates cached fill");
            for (uint stage = 1; stage <= 5; stage++)
            {
                mask = fill.Get(draft, stage);
                foreach (var sea in new[] { new Vector2(119f,39f),new Vector2(121f,40.3f),new Vector2(121.7f,35.6f),new Vector2(122.3f,30f) })
                {
                    Vector2 p = Geographic(sea.x, sea.y);
                    Check(Alpha(mask,p.x,p.y) == 0, "transparent Bohai/Yellow/East sea stage " + stage + " " + sea);
                }
            }
        }
        File.WriteAllLines(Qa + "unity-validation.txt", Log);
        return "PASS " + _checks + " authoring/raster checks";
    }

    private static float Alpha(Texture2D texture, float x, float y)
    { return texture.GetPixel(Mathf.Clamp((int)(x * texture.width),0,texture.width-1),Mathf.Clamp((int)(y * texture.height),0,texture.height-1)).a; }
    private static Vector2 Geographic(float longitude, float latitude)
    {
        // Independently fixed control transform from the checked coast registration.
        return new Vector2((2974.72f + (longitude - 117.2f) * 189.44f) / 4096,
            1 - (671.12f + (39.1f - latitude) * 190.94f) / 3138);
    }

    public static string InstallAndExport()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Requires idle editor");
        foreach (var window in Resources.FindObjectsOfTypeAll<WorldMapToolWindow>())
            if (window.HasUnsavedDraft) throw new InvalidOperationException("Unsaved map draft: refusing overwrite");
        ValidateCandidate();
        var draft = Candidate(); var previous = WorldMapToolDataIO.Load(out _);
        Check(previous != null && previous.points.Count == draft.points.Count
            && previous.points.Select(JsonUtility.ToJson).SequenceEqual(draft.points.Select(JsonUtility.ToJson)), "existing location and POI records preserved");
        string folder = WorldMapToolXlsxExporter.ToolFolderAbsolute;
        string backup = Path.Combine(folder, "_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_historical");
        Directory.CreateDirectory(backup);
        var targets = new List<string> {
            WorldMapToolDataIO.AssetPathToAbsolute(WorldMapToolDataIO.DraftAssetPath),
            WorldMapToolDataIO.AssetPathToAbsolute(WorldMapHistoricalAtlas.AssetPath),
            Path.Combine(WorldMapToolXlsxExporter.TopLevelFolderAbsolute,WorldMapToolXlsxExporter.FactionMergedFileName),
            Application.dataPath + "/Resources/Data/Json/Runtime/WorldFactionConfig.json" };
        for (uint stage = 1; stage <= 5; stage++) targets.Add(Path.Combine(folder,WorldMapToolXlsxExporter.FactionStageFileName(stage)));
        var manifest = new List<string>();
        foreach (var path in targets) if (File.Exists(path))
        { File.Copy(path, Path.Combine(backup, Path.GetFileName(path))); manifest.Add(Hash(path) + " " + path); }
        File.WriteAllLines(Path.Combine(backup,"manifest.txt"),manifest);
        Check(WorldMapToolDataIO.Save(draft,out string reason), reason ?? "draft saved");
        File.Copy(Qa + "candidate-atlas.json",WorldMapToolDataIO.AssetPathToAbsolute(WorldMapHistoricalAtlas.AssetPath),true);
        var stages = WorldMapToolXlsxExporter.ExportFactionStageTables(draft);
        Check(stages.Success, "five stage XLSX files: " + stages.Message);
        var merged = WorldMapToolXlsxExporter.ExportFactionMergedTable(draft);
        Check(merged.Success, "merged XLSX: " + merged.Message);
        // Reuse the existing single-table conversion, rather than regenerate unrelated classes/tables.
        using (var package = new ExcelPackage(new FileInfo(Path.Combine(WorldMapToolXlsxExporter.TopLevelFolderAbsolute,WorldMapToolXlsxExporter.FactionMergedFileName))))
        {
            var sheet = package.Workbook.Worksheets.First();
            typeof(ConfigManager).GetMethod("ExportJson",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[] { sheet,"WorldFaction",ExporterMode.Config });
        }
        AssetDatabase.ImportAsset("Assets/Resources/Data/Json/Runtime/WorldFactionConfig.json",ImportAssetOptions.ForceUpdate);
        WorldMapService.Instance.Reload();
        VerifyRuntime(draft);
        foreach (var window in Resources.FindObjectsOfTypeAll<WorldMapToolWindow>()) window.ReloadSavedDraft();
        File.WriteAllLines(Qa+"unity-validation.txt",Log);
        File.WriteAllText(Qa+"installed.txt","PASS " + _checks + " checks\nbackup="+backup+"\nvertices="+draft.factionVertices.Count);
        AssetDatabase.Refresh(); return File.ReadAllText(Qa+"installed.txt");
    }

    private static void VerifyRuntime(WorldMapDraft draft)
    {
        var rows = JsonUtility.FromJson<Rows>(File.ReadAllText(Application.dataPath + "/Resources/Data/Json/Runtime/WorldFactionConfig.json"));
        Check(rows?.datas != null && rows.datas.Count == draft.factionVertices.Count, "single-table runtime JSON row count equals draft");
        var atlas = JsonUtility.FromJson<Atlas>(File.ReadAllText(Qa+"candidate-atlas.json"));
        foreach (var stage in atlas.stages)
        {
            Check(WorldMapService.Instance.FactionIds(stage.stage).Count == stage.factions.Length, "runtime faction count stage " + stage.stage);
            foreach (var faction in stage.factions)
                Check(WorldMapService.Instance.TryGetFactionAt(stage.stage,new Vector2(faction.capitalX,faction.capitalY),out uint owner,out _) && owner == faction.id,
                    "capital belongs to " + faction.name + " : " + faction.capital);
        }
    }
}
