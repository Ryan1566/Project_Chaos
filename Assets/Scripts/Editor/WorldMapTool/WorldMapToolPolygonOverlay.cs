using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Cached even/odd polygon fill. Concave coastlines never fill their bounding rectangles.</summary>
public sealed class WorldMapToolPolygonOverlay : IDisposable
{
    private Texture2D _texture;
    private Color32[] _pixels;
    private int _hash;
    private readonly List<float> _crossings = new List<float>();
    public Texture2D Get(WorldMapDraft draft, uint stage)
    {
        int width = 2048, height = Mathf.RoundToInt(width * (float)draft.imageHeight / draft.imageWidth);
        int hash = Fingerprint(draft, stage);
        if (_texture != null && hash == _hash && _texture.height == height) return _texture;
        if (_texture == null || _texture.height != height)
        {
            Dispose();
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
                name = "WorldMapEditorPolygonFill", hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _pixels = new Color32[width * height];
        }
        Array.Clear(_pixels, 0, _pixels.Length);
        foreach (var region in WorldMapToolDataIO.GroupRegions(draft))
        {
            if (region.stage != stage || region.vertices.Count < 3) continue;
            if (!ColorUtility.TryParseHtmlString(region.color, out var color)) color = Color.magenta;
            color.a = .32f;
            Rasterize(region.vertices, color, width, height);
        }
        _texture.SetPixels32(_pixels); _texture.Apply(false, false); _hash = hash;
        return _texture;
    }

    private static int Fingerprint(WorldMapDraft draft, uint stage)
    {
        unchecked
        {
            int hash = (int)stage * 397;
            foreach (var v in draft.factionVertices)
            {
                if (v == null || v.stage != stage) continue;
                hash = hash * 31 + v.x.GetHashCode(); hash = hash * 31 + v.y.GetHashCode();
                hash = hash * 31 + (int)v.factionId; hash = hash * 31 + (int)v.regionId;
                hash = hash * 31 + (v.enable ? 1 : 0); hash = hash * 31 + (v.color?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    private void Rasterize(List<Vector2> vertices, Color32 color, int width, int height)
    {
        var bounds = WorldMapToolGeometry.Bounds(vertices);
        int bottom = Mathf.Max(0, Mathf.FloorToInt(bounds.yMin * height));
        int top = Mathf.Min(height - 1, Mathf.CeilToInt(bounds.yMax * height));
        for (int y = bottom; y <= top; y++)
        {
            float scan = (y + .5f) / height; _crossings.Clear();
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 a = vertices[i], b = vertices[(i + 1) % vertices.Count];
                if ((a.y <= scan && b.y > scan) || (b.y <= scan && a.y > scan))
                    _crossings.Add((a.x + (scan - a.y) * (b.x - a.x) / (b.y - a.y)) * width);
            }
            _crossings.Sort();
            for (int i = 0; i + 1 < _crossings.Count; i += 2)
            {
                int left = Mathf.Max(0, Mathf.CeilToInt(_crossings[i] - .5f));
                int right = Mathf.Min(width - 1, Mathf.FloorToInt(_crossings[i + 1] - .5f));
                for (int x = left; x <= right; x++) _pixels[y * width + x] = color;
            }
        }
    }

    public void Dispose()
    {
        if (_texture != null) UnityEngine.Object.DestroyImmediate(_texture);
        _texture = null; _pixels = null;
    }
}
