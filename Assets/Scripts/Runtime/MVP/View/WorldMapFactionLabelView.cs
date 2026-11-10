using System.Collections.Generic;
using Chaos.MVP.Model.WorldMap;
using TMPro;
using UnityEngine;

/// <summary>Passive, reusable country labels in the same normalized coordinate space as POIs.</summary>
public sealed class WorldMapFactionLabelView : MonoBehaviour
{
    readonly Dictionary<uint, TextMeshProUGUI> _labels = new Dictionary<uint, TextMeshProUGUI>();
    readonly HashSet<uint> _active = new HashSet<uint>();
    readonly List<Rect> _occupied = new List<Rect>();
    static readonly Vector2[] Offsets = {
        Vector2.up, Vector2.left, Vector2.right, Vector2.down,
        new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1)
    };
    static readonly float[] OffsetRadii = { 22f, 39f, 56f };
    List<WorldFactionLabel> _data;
    List<WorldFactionArea> _areas;
    List<WorldLocationSnapshot> _places;
    Material _inkMaterial;
    Vector2 _mapSize;
    float _zoom = 1f, _scale = 1f;
    public int CachedCount => _labels.Count;
    public void Render(List<WorldFactionLabel> labels, List<WorldFactionArea> areas = null,
        List<WorldLocationSnapshot> places = null, Vector2 mapSize = default)
    {
        _data = labels;
        _areas = areas;
        _places = places;
        _mapSize = mapSize;
        _active.Clear();
        if (labels != null) foreach (var data in labels)
        {
            if (data == null || string.IsNullOrEmpty(data.Name) || !Finite(data.Position)) continue;
            if (!_labels.TryGetValue(data.FactionId, out var label))
            { label = CreateLabel(data.FactionId); _labels.Add(data.FactionId, label); }
            _active.Add(data.FactionId);
            label.gameObject.SetActive(true);
            label.text = data.Name;
        }
        foreach (var pair in _labels) if (!_active.Contains(pair.Key)) pair.Value.gameObject.SetActive(false);
        PlaceLabels();
    }
    static bool Finite(Vector2 point) => !float.IsNaN(point.x) && !float.IsNaN(point.y)
        && !float.IsInfinity(point.x) && !float.IsInfinity(point.y);
    public void SetZoom(float zoom)
    {
        if (Mathf.Approximately(_zoom, zoom)) return;
        _zoom = zoom;
        _scale = 1f / Mathf.Pow(Mathf.Max(1f, zoom), .85f);
        foreach (var label in _labels.Values) label.rectTransform.localScale = new Vector3(_scale, _scale, 1f);
        PlaceLabels();
    }

    public void SetMapSize(Vector2 size)
    {
        if (_mapSize == size) return;
        _mapSize = size;
        PlaceLabels();
    }

    private void PlaceLabels()
    {
        if (_data == null) return;
        _occupied.Clear();
        Vector2 size = _mapSize * _zoom;
        bool canOffset = _areas != null && size.x > 0f && size.y > 0f;
        if (canOffset && _places != null)
            foreach (var place in _places)
            {
                Vector2 center = Vector2.Scale(place.Position, size);
                bool prominent = place.Kind == WorldLocationKind.Capital || place.Kind == WorldLocationKind.PlayerVillage;
                float radius = prominent ? 19f : 14f;
                _occupied.Add(new Rect(center - Vector2.one * radius, Vector2.one * (radius * 2f)));
                if (prominent && !string.IsNullOrEmpty(place.Name))
                {
                    float width = place.Name.Length * 16f;
                    _occupied.Add(new Rect(center.x - width * .5f, center.y - 38f, width, 20f));
                }
            }
        foreach (var data in _data)
        {
            if (data == null || !_active.Contains(data.FactionId)) continue;
            var label = _labels[data.FactionId];
            Vector2 position = data.Position;
            label.fontSize = data.Name.Length == 1 ? 32 : data.Name.Length <= 3 ? 25 : 20;
            if (canOffset)
            {
                if (!TryPlaceLabel(data, label.fontSize, size, out position, out var bounds))
                {
                    // 密集小国先缩一档字，再用同一组候选点尝试，仍无空位时保留原锚。
                    label.fontSize *= .8f;
                    TryPlaceLabel(data, label.fontSize, size, out position, out bounds);
                }
                _occupied.Add(bounds);
            }
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = position;
            label.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private bool TryPlaceLabel(WorldFactionLabel data, float fontSize, Vector2 size, out Vector2 position, out Rect bounds)
    {
        float glyph = fontSize * _scale * _zoom;
        Vector2 textSize = new Vector2(glyph * data.Name.Length + 6f, glyph + 6f);
        Vector2 original = Vector2.Scale(data.Position, size);
        position = data.Position;
        bounds = new Rect(original - textSize * .5f, textSize);
        if (!Collides(bounds)) return true;

        // 在原锚点附近逐步扩大避让范围，候选点须留在所属势力内。
        foreach (float radius in OffsetRadii)
        foreach (var direction in Offsets)
        {
            Vector2 center = original + direction * radius;
            var candidateBounds = new Rect(center - textSize * .5f, textSize);
            var candidate = new Vector2(center.x / size.x, center.y / size.y);
            if (Collides(candidateBounds) || !WorldFactionLabelCatalog.Contains(_areas, data.FactionId, candidate)) continue;
            position = candidate;
            bounds = candidateBounds;
            return true;
        }
        return false;
    }

    private bool Collides(Rect candidate)
    {
        foreach (var bounds in _occupied)
            if (bounds.Overlaps(candidate)) return true;
        return false;
    }
    TextMeshProUGUI CreateLabel(uint id)
    {
        var rect = GameplayUi.Rect(transform, "FactionLabel_" + id, Vector2.one * .5f,
            Vector2.one * .5f, Vector2.zero, new Vector2(240, 44));
        rect.gameObject.layer = gameObject.layer;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = RunTeamHud.ResolveFont();
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 11;
        label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Overflow;
        label.color = new Color(.18f, .12f, .075f); label.raycastTarget = false;
        rect.localScale = new Vector3(_scale, _scale, 1f);
        if (label.font != null && label.font.material != null)
        {
            // 全部国名共用本面板的描边材质，不修改字体资产。
            if (_inkMaterial == null)
            {
                _inkMaterial = new Material(label.font.material) { name = "WorldFactionLabel_Ink" };
                if (_inkMaterial.HasProperty("_OutlineWidth")) _inkMaterial.SetFloat("_OutlineWidth", .16f);
                if (_inkMaterial.HasProperty("_OutlineColor")) _inkMaterial.SetColor("_OutlineColor", new Color(.97f, .89f, .70f, .92f));
                _inkMaterial.EnableKeyword("OUTLINE_ON");
            }
            label.fontSharedMaterial = _inkMaterial;
        }
        return label;
    }
    public void Clear() { Render(null); }
    void OnDestroy()
    {
        if (_inkMaterial == null) return;
        if (Application.isPlaying) Destroy(_inkMaterial); else DestroyImmediate(_inkMaterial);
    }
}
