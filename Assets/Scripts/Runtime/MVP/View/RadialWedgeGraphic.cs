using UnityEngine;
using UnityEngine.UI;

/// <summary>An annular sector mesh; unlike a filled Image it never covers the center hole.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RadialWedgeGraphic : MaskableGraphic
{
    public float InnerRadius { get; private set; }
    public float OuterRadius { get; private set; }
    public float CenterAngle { get; private set; }
    public float SweepAngle { get; private set; }
    private Sprite _sprite;
    private float _designSize;

    public override Texture mainTexture => _sprite == null ? Texture2D.whiteTexture : _sprite.texture;

    public void SetTexture(Sprite sprite, float designSize)
    {
        _sprite = sprite;
        _designSize = Mathf.Max(1f, designSize);
        SetAllDirty();
    }

    public void Configure(float innerRadius, float outerRadius, float centerAngleDegrees, float sweepAngleDegrees, Color tint, bool interactive = false)
    {
        raycastTarget = interactive;
        if (InnerRadius == innerRadius && OuterRadius == outerRadius && CenterAngle == centerAngleDegrees
            && SweepAngle == sweepAngleDegrees && color == tint) return;
        InnerRadius = Mathf.Max(0, innerRadius); OuterRadius = Mathf.Max(InnerRadius, outerRadius);
        CenterAngle = centerAngleDegrees; SweepAngle = Mathf.Clamp(sweepAngleDegrees, 0, 360); color = tint;
        SetVerticesDirty();
    }

    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 point))
            return false;
        float distance = point.sqrMagnitude;
        float angle = Mathf.Atan2(point.x, point.y) * Mathf.Rad2Deg;
        return distance >= InnerRadius * InnerRadius && distance <= OuterRadius * OuterRadius
            && Mathf.Abs(Mathf.DeltaAngle(CenterAngle, angle)) <= SweepAngle * .5f
            && base.Raycast(screenPoint, eventCamera);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int steps = Mathf.Max(1, Mathf.CeilToInt(SweepAngle / 4));
        for (int i = 0; i <= steps; i++)
        {
            float angle = (90 - CenterAngle + SweepAngle * .5f - SweepAngle * i / steps) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(direction * InnerRadius, color, MapUv(direction, InnerRadius));
            vh.AddVert(direction * OuterRadius, color, MapUv(direction, OuterRadius));
            if (i == 0) continue;
            int v = i * 2; vh.AddTriangle(v - 2, v - 1, v + 1); vh.AddTriangle(v - 2, v + 1, v);
        }
    }

    private Vector2 MapUv(Vector2 direction, float radius)
    {
        if (_sprite == null) return Vector2.zero;
        // 贴图的基准扇区朝上。旋回基准角，24 格可共用一张贴图。
        float angle = CenterAngle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);
        var local = new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
        var uv = local * (radius / _designSize) + Vector2.one * .5f;
        var rect = _sprite.textureRect;
        return new Vector2((rect.x + uv.x * rect.width) / _sprite.texture.width,
            (rect.y + uv.y * rect.height) / _sprite.texture.height);
    }
}
