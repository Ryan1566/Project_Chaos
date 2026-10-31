using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **势力范围蒙版**：把任意多块多边形**光栅化**成一张纹理，再用**一个 quad** 画出来。
///
/// ══════════════ 为什么用光栅化，而不是三角化 ══════════════
/// 三角化（耳切法 / Delaunay）只对**简单多边形**可靠：凹多边形的耳切容易选错耳、
/// 自交多边形更是直接失败或产生翻转三角形。而本项目的势力边界**允许凹多边形**
/// （`06_WorldFaction_字段规格.md` M7 明确"不做自交/凹多边形校验"）。
/// 光栅化用「**扫描线 + 奇偶填充**」，对凹多边形天然正确，也不需要任何三角化库。
///
/// ══════════════ 继承 RawImage（而不是裸 MaskableGraphic） ══════════════
/// `RawImage` 本身就是 `MaskableGraphic` 的子类，而且**已经实现了"一个 quad + texture"** 这件事。
/// 所以这里只做"光栅化 → 赋值 texture"，quad / 材质 / 显隐全部复用基类。
/// → **prefab 上该节点只需挂 `WorldFactionMask` 一个组件**（不要再单独挂 RawImage，否则会画两遍）。
///
/// ⚠ **坐标不要翻转**：`Texture2D.SetPixels32` 的颜色数组是「**从左到右、从下到上**」，
///   而配表里的归一化坐标就是「X 自左向右、**Y 自下向上**」—— 两者方向一致，
///   所以 `px = n.x * W`、`py = n.y * H`，**不要写 `1 - y`**（写了就上下镜像）。
/// </summary>
public class WorldFactionMask : RawImage
{
    /// <summary>光栅化宽度（高度按底图宽高比算）。512 够用且开销小。</summary>
    public const int RasterWidth = 512;

    /// <summary>2×2 超采样抗锯齿（先放大 2 倍画，再盒式降采样）。</summary>
    public const int Supersample = 2;

    /// <summary>势力色块的透明度（MVP 取值；将来可由配置驱动）。</summary>
    public const float FactionAlpha = 0.35f;

    [SerializeField, Range(0, 4)] private float borderWidthPixels = 1.25f;
    [SerializeField] private Color borderColor = new Color(0, 0, 0, .95f);
    public int RasterGeneration { get; private set; }
    private int _lastSignature;
    private bool _hasSignature;
    private Texture2D _texture;
    private Color32[] _hi;              //超采样缓冲（复用，避免每次重建都分配几 MB）
    private int _hiWidth;
    private int _hiHeight;
    private readonly List<float> _scanXs = new List<float>(64);

    /// <summary>
    /// 重设要显示的区域。**可重复调用**（换时期、Reload 都会再调）。
    /// 传 null / 空列表 → 蒙版全透明（不报错）。
    /// </summary>
    /// <param name="areas">该时期的势力区域（归一化顶点，Y 向上）</param>
    /// <param name="mapAspect">底图宽高比 = 高 / 宽（&lt;=0 时按 1 处理）</param>
    public void SetAreas(List<WorldFactionArea> areas, float mapAspect)
    {
        //蒙版是背景层，绝不能吃掉点位的射线
        raycastTarget = false;

        if (mapAspect <= 0f || float.IsNaN(mapAspect) || float.IsInfinity(mapAspect)) mapAspect = 1f;
        int signature = Signature(areas, mapAspect);
        if (_hasSignature && signature == _lastSignature && _texture != null) return;
        _lastSignature = signature; _hasSignature = true;

        int width = RasterWidth;
        int height = Mathf.Clamp(Mathf.RoundToInt(width * mapAspect), 1, 4096);
        int sw = width * Supersample;
        int sh = height * Supersample;

        EnsureHiBuffer(sw, sh);
        System.Array.Clear(_hi, 0, _hi.Length);

        int drawn = 0;
        if (areas != null)
        {
            for (int i = 0; i < areas.Count; i++)
            {
                WorldFactionArea area = areas[i];
                if (area == null || !area.IsValid) continue;

                FillPolygon(area, sw, sh);
                drawn++;
            }
        }

        // Stroke after every fill, so adjacent polygons cannot paint over their common border.
        if (areas != null && borderWidthPixels > 0) foreach (var area in areas)
            if (area != null && area.IsValid) StrokePolygon(area, sw, sh);
        EnsureTexture(width, height);
        Downsample(width, height);
        RasterGeneration++;

        if (drawn == 0)
        {
            ChaosLog.Warn(LogChannel.UI, "[局外地图] 势力蒙版：本时期没有有效区域（顶点不足 3 的一律跳过）");
        }
    }

    private int Signature(List<WorldFactionArea> areas, float aspect)
    {
        unchecked
        {
            int hash = aspect.GetHashCode() * 31 + borderWidthPixels.GetHashCode();
            hash = hash * 31 + borderColor.GetHashCode();
            if (areas != null) foreach (var area in areas)
            {
                if (area == null) { hash *= 31; continue; }
                hash = hash * 31 + area.factionId.GetHashCode(); hash = hash * 31 + area.color.GetHashCode();
                hash = hash * 31 + area.vertices.Count;
                foreach (var point in area.vertices) hash = hash * 31 + point.GetHashCode();
            }
            return hash;
        }
    }
    private void StrokePolygon(WorldFactionArea area, int width, int height)
    {
        float radius = Mathf.Clamp(borderWidthPixels, 0, 4) * Supersample * .5f;
        for (int i = 0; i < area.vertices.Count; i++)
        {
            Vector2 a = Vector2.Scale(area.vertices[i], new Vector2(width, height));
            Vector2 b = Vector2.Scale(area.vertices[(i + 1) % area.vertices.Count], new Vector2(width, height));
            if (float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(b.x) || float.IsNaN(b.y)
                || float.IsInfinity(a.x) || float.IsInfinity(a.y) || float.IsInfinity(b.x) || float.IsInfinity(b.y)) continue;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1), 0, width - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1), 0, width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1), 0, height - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1), 0, height - 1);
            Vector2 segment = b - a; float length = segment.sqrMagnitude;
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                Vector2 point = new Vector2(x + .5f, y + .5f);
                float t = length <= .00001f ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, segment) / length);
                float coverage = Mathf.Clamp01(radius + .5f - (point - a - segment * t).magnitude);
                if (coverage <= 0) continue;
                Color color = borderColor; color.a *= coverage;
                BlendSourceOver(y * _hiWidth + x, (Color32)color);
            }
        }
    }

    private void EnsureHiBuffer(int sw, int sh)
    {
        if (_hi == null || _hiWidth != sw || _hiHeight != sh)
        {
            _hiWidth = sw;
            _hiHeight = sh;
            _hi = new Color32[sw * sh];
        }
    }

    private void EnsureTexture(int width, int height)
    {
        if (_texture != null && _texture.width == width && _texture.height == height) return;

        ReleaseTexture();

        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        _texture.wrapMode = TextureWrapMode.Clamp;
        _texture.filterMode = FilterMode.Bilinear;
        _texture.name = "WorldFactionMask_" + width + "x" + height;
        texture = _texture;   //RawImage.texture：赋值即刷新材质
    }

    // ══════════════════ 扫描线 + 奇偶填充 ══════════════════

    private void FillPolygon(WorldFactionArea area, int sw, int sh)
    {
        List<Vector2> pts = area.vertices;
        int n = pts.Count;
        if (n < 3) return;

        //归一化 → 超采样像素。⚠ 不翻转 Y（见类注释）
        var px = new Vector2[n];
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            Vector2 v = pts[i];
            float x = Mathf.Clamp01(v.x) * sw;
            float y = Mathf.Clamp01(v.y) * sh;
            px[i] = new Vector2(x, y);
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        Color src = area.color;
        src.a = FactionAlpha;
        Color32 src32 = src;

        int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
        int y1 = Mathf.Min(sh - 1, Mathf.CeilToInt(maxY));

        for (int y = y0; y <= y1; y++)
        {
            float scanY = y + 0.5f;
            _scanXs.Clear();

            for (int i = 0; i < n; i++)
            {
                Vector2 a = px[i];
                Vector2 b = px[(i + 1) % n];   //不重复首点 → 闭合边由这里补

                //半开区间判定：顶点正好落在扫描线上时只算一次，避免重复计数
                if ((a.y <= scanY && b.y > scanY) || (b.y <= scanY && a.y > scanY))
                {
                    float t = (scanY - a.y) / (b.y - a.y);
                    _scanXs.Add(a.x + t * (b.x - a.x));
                }
            }

            if (_scanXs.Count < 2) continue;
            _scanXs.Sort();

            int rowBase = y * _hiWidth;
            for (int k = 0; k + 1 < _scanXs.Count; k += 2)
            {
                int xa = Mathf.Max(0, Mathf.CeilToInt(_scanXs[k] - 0.5f));
                int xb = Mathf.Min(_hiWidth - 1, Mathf.FloorToInt(_scanXs[k + 1] - 0.5f));
                for (int x = xa; x <= xb; x++) BlendSourceOver(rowBase + x, src32);
            }
        }
    }

    /// <summary>直线 alpha 的 source-over 混合（半透明势力块叠加时不会互相"吃掉"）。</summary>
    private void BlendSourceOver(int index, Color32 src)
    {
        float sa = src.a / 255f;
        if (sa <= 0f) return;

        Color32 dst = _hi[index];
        float da = dst.a / 255f;
        float outA = sa + da * (1f - sa);
        if (outA <= 0.0001f) { _hi[index] = new Color32(0, 0, 0, 0); return; }

        float inv = 1f / outA;
        float r = (src.r * sa + dst.r * da * (1f - sa)) * inv;
        float g = (src.g * sa + dst.g * da * (1f - sa)) * inv;
        float b = (src.b * sa + dst.b * da * (1f - sa)) * inv;

        _hi[index] = new Color32(
            (byte)Mathf.Clamp(r, 0f, 255f),
            (byte)Mathf.Clamp(g, 0f, 255f),
            (byte)Mathf.Clamp(b, 0f, 255f),
            (byte)Mathf.Clamp(outA * 255f, 0f, 255f));
    }

    // ══════════════════ 降采样（盒式，预乘平均避免黑边） ══════════════════

    private void Downsample(int width, int height)
    {
        int s = Supersample;
        var outPixels = new Color32[width * height];
        float count = s * s;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;

                for (int dy = 0; dy < s; dy++)
                {
                    int rowBase = (y * s + dy) * _hiWidth + x * s;
                    for (int dx = 0; dx < s; dx++)
                    {
                        Color32 c = _hi[rowBase + dx];
                        float ca = c.a / 255f;
                        //预乘后平均：边缘半透明像素不会把颜色拉向黑色
                        r += c.r / 255f * ca;
                        g += c.g / 255f * ca;
                        b += c.b / 255f * ca;
                        a += ca;
                    }
                }

                a /= count;
                if (a <= 0.0001f) outPixels[y * width + x] = new Color32(0, 0, 0, 0);
                else
                {
                    float inv = 1f / (a * count);
                    outPixels[y * width + x] = new Color32(
                        (byte)Mathf.Clamp(r * inv * 255f, 0f, 255f),
                        (byte)Mathf.Clamp(g * inv * 255f, 0f, 255f),
                        (byte)Mathf.Clamp(b * inv * 255f, 0f, 255f),
                        (byte)Mathf.Clamp(a * 255f, 0f, 255f));
                }
            }
        }

        _texture.SetPixels32(outPixels);
        _texture.Apply(false, false);
    }

    // ══════════════════ 生命周期 ══════════════════

    protected override void OnDestroy()
    {
        ReleaseTexture();
        _hi = null;
        base.OnDestroy();
    }

    private void ReleaseTexture()
    {
        if (_texture == null) return;

        if (Application.isPlaying) Destroy(_texture);
        else DestroyImmediate(_texture);

        _texture = null;
        texture = null;
    }
}
