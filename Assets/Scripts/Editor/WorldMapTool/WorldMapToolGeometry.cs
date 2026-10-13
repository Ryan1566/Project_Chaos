using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 世界观地图绘制工具的**几何与坐标换算**。
///
/// ══════════════════ 本文件是【唯一】做 Y 轴翻转的地方 ══════════════════
/// 「Y 向上」这条约定（`05_WorldMap_字段规格.md` §6 C4 / `06_WorldFaction_字段规格.md` §6 C4）
/// 算错就是**整张图上下镜像**，是这两个表最危险的一处。所以三种坐标之间的换算**全部收拢在本文件**，
/// 其它文件（窗口 / 校验 / 导出）**一律不得自己写 `1 - y`** —— 要换算就调这里的方法。
///
/// 三种坐标：
///   ① **图像像素** `px/py`：以源图**左上角**为原点，`py` 向下增大；
///   ② **归一化** `0~1`：`X` 自左向右、**`Y` 自下向上**（这就是写进配表的取值）；
///   ③ **画布 UI**：IMGUI 坐标，`y` 向下增大，原点在窗口左上（本工具用窗口空间坐标，见窗口注释）。
///
/// 对应关系：
///   `X = px / 图宽`，`Y = 1 - py / 图高`   ← ②由①得到（翻转只在这里）
///   画布 = 归一化映射到「底图在屏幕上的矩形」，`y` 再翻回向下
/// </summary>
public static class WorldMapToolGeometry
{
    /// <summary>浮点相等判定的容差（归一化坐标量级很小，用 1e-6 足够）。</summary>
    public const float Epsilon = 1e-6f;

    // ══════════════════ ① 图像像素 <-> 归一化 ══════════════════

    /// <summary>
    /// 图像像素（**左上角原点、py 向下**）→ 归一化（**Y 向上**）。
    /// 公式：`X = px / 图宽`、`Y = 1 - py / 图高`。
    /// </summary>
    public static Vector2 PixelToNormalized(float px, float py, int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0) return Vector2.zero;
        return new Vector2(px / imageWidth, 1f - py / imageHeight);
    }

    /// <summary>归一化（Y 向上）→ 图像像素（左上角原点、py 向下）。上式的逆运算，翻转同样只在这里。</summary>
    public static Vector2 NormalizedToPixel(Vector2 normalized, int imageWidth, int imageHeight)
    {
        return new Vector2(normalized.x * imageWidth, (1f - normalized.y) * imageHeight);
    }

    // ══════════════════ ② 归一化 <-> 画布 UI ══════════════════

    /// <summary>
    /// 归一化（Y 向上）→ 画布坐标（Y 向下），`imageRect` = 底图当前在屏幕上的矩形。
    /// </summary>
    public static Vector2 NormalizedToCanvas(Vector2 normalized, Rect imageRect)
    {
        return new Vector2(
            imageRect.x + normalized.x * imageRect.width,
            imageRect.y + (1f - normalized.y) * imageRect.height);
    }

    /// <summary>画布坐标（Y 向下）→ 归一化（Y 向上）。鼠标落点用它。</summary>
    public static Vector2 CanvasToNormalized(Vector2 canvas, Rect imageRect)
    {
        if (imageRect.width <= 0f || imageRect.height <= 0f) return Vector2.zero;
        return new Vector2(
            (canvas.x - imageRect.x) / imageRect.width,
            1f - (canvas.y - imageRect.y) / imageRect.height);
    }

    /// <summary>把归一化坐标夹到 `[0,1]`（防止点位/顶点跑到图外）。</summary>
    public static Vector2 ClampNormalized(Vector2 v)
    {
        return new Vector2(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y));
    }

    /// <summary>归一化坐标是否在图内（含边界，带一点容差）。</summary>
    public static bool IsInsideUnitSquare(Vector2 v, float tolerance = 1e-4f)
    {
        return v.x >= -tolerance && v.x <= 1f + tolerance
            && v.y >= -tolerance && v.y <= 1f + tolerance;
    }

    // ══════════════════ 多边形：面积 / 朝向 ══════════════════

    /// <summary>
    /// 鞋带公式求**有符号面积**。在「X 向右、Y 向上」的坐标系里：
    /// **正 = 逆时针（CCW）**，负 = 顺时针（CW）。顶点少于 3 个返回 0。
    /// </summary>
    public static float SignedArea(IList<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3) return 0f;

        float sum = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current = polygon[i];
            Vector2 next = polygon[(i + 1) % polygon.Count];
            sum += current.x * next.y - next.x * current.y;
        }
        return sum * 0.5f;
    }

    /// <summary>顶点是否按逆时针排列（本工具的产出约定是统一逆时针）。</summary>
    public static bool IsCounterClockwise(IList<Vector2> polygon)
    {
        return SignedArea(polygon) > 0f;
    }

    /// <summary>
    /// 把顶点顺序**规范化为逆时针**（已经是 CCW 就原样返回副本；CW 则反转）。
    /// 注意：本工具**不重复首点**（闭合由运行时按 `PointOrder` 连完自动完成，见 `06_...字段规格.md` M5）。
    /// </summary>
    public static List<Vector2> EnsureCounterClockwise(IList<Vector2> polygon)
    {
        var result = new List<Vector2>();
        if (polygon == null) return result;

        result.AddRange(polygon);
        if (result.Count >= 3 && SignedArea(result) < 0f) result.Reverse();
        return result;
    }

    /// <summary>多边形的包围盒（归一化坐标）。点少于 1 个返回 `Rect.zero`。</summary>
    public static Rect Bounds(IList<Vector2> polygon)
    {
        if (polygon == null || polygon.Count == 0) return Rect.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 p = polygon[i];
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    // ══════════════════ 多边形：自交 / 内外判定 ══════════════════

    /// <summary>
    /// 多边形是否**自交**（边与边发生真正的穿越）。
    ///
    /// 判定口径（很重要，决定误报率）：
    ///   · 只测**不相邻**的两条边。相邻边共享一个顶点，是正常的多边形转角，**不算自交**；
    ///   · 只认**严格穿越**（两条边互相跨过），**共线重叠 / 端点相接不算** —— 凹多边形合法
    ///     （`06_...字段规格.md` M7 明确说凹多边形不阻断），共线接触也不该报自交。
    /// 顶点少于 4 个不可能自交，直接返回 false。
    /// </summary>
    public static bool IsSelfIntersecting(IList<Vector2> polygon)
    {
        if (polygon == null) return false;
        int n = polygon.Count;
        if (n < 4) return false;

        for (int i = 0; i < n; i++)
        {
            int iNext = (i + 1) % n;
            for (int j = i + 1; j < n; j++)
            {
                int jNext = (j + 1) % n;

                // 相邻边（共享顶点）跳过：i==j、iNext==j、jNext==i 都不是自交
                if (i == j || iNext == j || jNext == i) continue;

                if (SegmentsProperlyIntersect(polygon[i], polygon[iNext], polygon[j], polygon[jNext]))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 点是否在多边形内（**射线法**，偶数交点在外、奇数在内）。
    /// 落在边上时判为「在内」（工具交互上更符合直觉：点边框算选中）。
    /// </summary>
    public static bool PointInPolygon(Vector2 point, IList<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3) return false;

        int n = polygon.Count;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];

            // 点落在边 a-b 上 → 视为在内
            if (IsPointOnSegment(point, a, b)) return true;

            bool crossesRay = (a.y > point.y) != (b.y > point.y);
            if (crossesRay)
            {
                float intersectX = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < intersectX) inside = !inside;
            }
        }
        return inside;
    }

    /// <summary>点是否落在线段 `a-b` 上（含端点，带容差）。</summary>
    public static bool IsPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        float cross = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
        if (Mathf.Abs(cross) > 1e-4f) return false;

        float dot = (point.x - a.x) * (b.x - a.x) + (point.y - a.y) * (b.y - a.y);
        if (dot < 0f) return false;

        float lengthSq = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
        return dot <= lengthSq;
    }

    /// <summary>两条线段是否**严格穿越**（不含共线重叠、端点相接、仅触碰）。</summary>
    private static bool SegmentsProperlyIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float d1 = Cross(c, d, a);
        float d2 = Cross(c, d, b);
        float d3 = Cross(a, b, c);
        float d4 = Cross(a, b, d);

        bool straddle1 = (d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f);
        bool straddle2 = (d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f);
        return straddle1 && straddle2;
    }

    /// <summary>叉积 `(b-a) × (p-a)`：>0 表示 p 在 ab 左侧。</summary>
    private static float Cross(Vector2 a, Vector2 b, Vector2 p)
    {
        return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    }
}
