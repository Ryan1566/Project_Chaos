using System.Collections.Generic;
using Chaos.MVP.Model.WorldMap;
using TMPro;
using UnityEngine;

/// <summary>Passive, reusable country labels in the same normalized coordinate space as POIs.</summary>
public sealed class WorldMapFactionLabelView : MonoBehaviour
{
    readonly Dictionary<uint, TextMeshProUGUI> _labels = new Dictionary<uint, TextMeshProUGUI>();
    readonly HashSet<uint> _active = new HashSet<uint>();
    readonly List<Material> _ownedMaterials = new List<Material>();
    public int CachedCount => _labels.Count;
    public void Render(List<WorldFactionLabel> labels)
    {
        _active.Clear();
        if (labels != null) foreach (var data in labels)
        {
            if (data == null || string.IsNullOrEmpty(data.Name) || !Finite(data.Position)) continue;
            if (!_labels.TryGetValue(data.FactionId, out var label))
            { label = CreateLabel(data.FactionId); _labels.Add(data.FactionId, label); }
            _active.Add(data.FactionId);
            label.gameObject.SetActive(true);
            label.text = data.Name;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = data.Position;
            label.rectTransform.anchoredPosition = Vector2.zero;
        }
        foreach (var pair in _labels) if (!_active.Contains(pair.Key)) pair.Value.gameObject.SetActive(false);
    }
    static bool Finite(Vector2 point) => !float.IsNaN(point.x) && !float.IsNaN(point.y)
        && !float.IsInfinity(point.x) && !float.IsInfinity(point.y);
    TextMeshProUGUI CreateLabel(uint id)
    {
        var rect = GameplayUi.Rect(transform, "FactionLabel_" + id, Vector2.one * .5f,
            Vector2.one * .5f, Vector2.zero, new Vector2(240, 44));
        rect.gameObject.layer = gameObject.layer;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = RunTeamHud.ResolveFont();
        label.fontSize = 24; label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Overflow;
        label.color = Color.white; label.raycastTarget = false;
        if (label.font != null && label.font.material != null)
        {
            // Each label owns its SDF material; shared font/material assets stay untouched.
            var material = new Material(label.font.material) { name = "WorldFactionLabel_Owned" };
            if (material.HasProperty("_OutlineWidth")) material.SetFloat("_OutlineWidth", .24f);
            if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", new Color(0, 0, 0, .95f));
            label.fontSharedMaterial = material; _ownedMaterials.Add(material);
        }
        return label;
    }
    public void Clear() { Render(null); }
    void OnDestroy()
    {
        foreach (var material in _ownedMaterials) if (material != null)
        { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        _ownedMaterials.Clear();
    }
}
