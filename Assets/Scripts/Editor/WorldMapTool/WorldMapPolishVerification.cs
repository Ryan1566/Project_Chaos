#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Chaos.MVP.Model.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Focused authoring/display checks; no player saves or persistent scene changes.</summary>
public static class WorldMapPolishVerification
{
    public static string Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); checks++; };
        int[] expected = { 0, 7, 1, 8, 19, 11 };
        for (uint stage = 1; stage <= 5; stage++)
        {
            var areas = WorldMapService.Instance.Areas(stage);
            var labels = WorldFactionLabelCatalog.Read(stage, areas);
            check(labels.Count == expected[stage] && labels.Select(l => l.FactionId).Distinct().Count() == labels.Count, "unique current-era names " + stage);
            foreach (var label in labels)
                check(!label.Name.StartsWith("势力 ") && WorldFactionLabelCatalog.Contains(areas, label.FactionId, label.Position), "named anchor inside own country " + stage + "/" + label.Name);
        }
        var obj = EditorUtility.CreateGameObjectWithHideFlags("WorldMapPolishCheck", HideFlags.HideAndDontSave, typeof(RectTransform));
        try
        {
            var mask = obj.AddComponent<WorldFactionMask>();
            var left = RectArea(1, .1f, .5f, Color.red);
            var right = RectArea(2, .5f, .9f, Color.blue);
            var areas = new List<WorldFactionArea> { left, right };
            mask.SetAreas(areas, 1);
            var texture = (Texture2D)mask.texture;
            var fill = texture.GetPixel(128, 256);
            check(fill.r > .9f && Mathf.Abs(fill.a - WorldFactionMask.FactionAlpha) < .02f, "interior fill and opacity preserved");
            var border = texture.GetPixel(256, 256);
            check(border.a > .55f && border.r < .3f && border.b < .3f, "common border remains dark after all fills");
            check(texture.GetPixel(128, 51).a > .55f, "closing bottom edge stroked");
            check(texture.GetPixel(8, 8).a == 0, "outside remains transparent");
            int generation = mask.RasterGeneration; mask.SetAreas(areas, 1);
            check(mask.RasterGeneration == generation && ReferenceEquals(mask.texture, texture), "same geometry reuses raster");
            left.vertices[0] += new Vector2(.01f, 0); mask.SetAreas(areas, 1);
            check(mask.RasterGeneration == generation + 1, "changed geometry invalidates raster");
            mask.SetAreas(null, 1);
            check(((Texture2D)mask.texture).GetPixels32().All(p => p.a == 0), "empty era clears all old borders");

            var layer = new GameObject("Names", typeof(RectTransform)); layer.hideFlags = HideFlags.HideAndDontSave;
            layer.transform.SetParent(obj.transform, false);
            var view = layer.AddComponent<WorldMapFactionLabelView>();
            var data = new List<WorldFactionLabel> { new WorldFactionLabel { FactionId = 1, Name = "秦", Position = new Vector2(.3f, .4f) } };
            view.Render(data);
            var label = layer.GetComponentInChildren<TextMeshProUGUI>(); int id = label.GetInstanceID();
            check(label.text == "秦" && label.rectTransform.anchorMin == data[0].Position && !label.raycastTarget, "name coordinate and raycast pass-through");
            check(label.font != null && label.font == RunTeamHud.ResolveFont(), "Chinese font assigned");
            check(label.fontSharedMaterial != label.font.material, "outline material is privately owned");
            data[0].Name = "汉"; view.Render(data);
            check(layer.GetComponentInChildren<TextMeshProUGUI>().GetInstanceID() == id && label.text == "汉", "country label reused and name updated");
            view.Clear(); check(!label.gameObject.activeSelf && view.CachedCount == 1, "clear hides cached label");
            view.Render(data); check(label.gameObject.activeSelf && label.GetInstanceID() == id, "reopen reuses label");
            check(typeof(BasePanel).GetMethod("HandleBack").IsVirtual && typeof(BasePanel).GetMethod("ClosePanel").IsVirtual, "common panel back uses virtual close contract");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
        return "PASS " + checks + " map/panel checks";
    }
    static WorldFactionArea RectArea(uint id, float x0, float x1, Color color)
    {
        var area = new WorldFactionArea { stage = 1, factionId = id, regionId = id, color = color };
        area.vertices.AddRange(new[] { new Vector2(x0, .1f), new Vector2(x1, .1f), new Vector2(x1, .9f), new Vector2(x0, .9f) });
        return area;
    }
}
#endif
