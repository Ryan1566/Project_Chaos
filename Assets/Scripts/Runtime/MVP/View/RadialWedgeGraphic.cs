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
    public void Configure(float innerRadius, float outerRadius, float centerAngleDegrees, float sweepAngleDegrees, Color tint)
    {
        raycastTarget = false;
        if (InnerRadius == innerRadius && OuterRadius == outerRadius && CenterAngle == centerAngleDegrees
            && SweepAngle == sweepAngleDegrees && color == tint) return;
        InnerRadius = Mathf.Max(0, innerRadius); OuterRadius = Mathf.Max(InnerRadius, outerRadius);
        CenterAngle = centerAngleDegrees; SweepAngle = Mathf.Clamp(sweepAngleDegrees, 0, 360); color = tint;
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int steps = Mathf.Max(1, Mathf.CeilToInt(SweepAngle / 4));
        for (int i = 0; i <= steps; i++)
        {
            float angle = (90 - CenterAngle + SweepAngle * .5f - SweepAngle * i / steps) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(direction * InnerRadius, color, Vector2.zero);
            vh.AddVert(direction * OuterRadius, color, Vector2.zero);
            if (i == 0) continue;
            int v = i * 2; vh.AddTriangle(v - 2, v - 1, v + 1); vh.AddTriangle(v - 2, v + 1, v);
        }
    }
}
