using Chaos.MVP.Model.Gameplay;
using RunMap;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Constant-size marker; terrain zoom affects its position, never its icon size.</summary>
public sealed class RunMapMarkerView : MonoBehaviour
{
    private RectTransform _root, _portrait;
    private RunMapGlyph _dot, _border, _image;
    public string MarkerId { get; private set; }
    public void Initialize()
    {
        _root = (RectTransform)transform;
        _dot = Glyph(transform, "Dot", Vector2.zero, 10, RunMapGlyph.Shape.Circle);
        _portrait = GameplayUi.Rect(transform, "Portrait", Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.one * 36);
        _border = Glyph(_portrait, "DiamondFrame", Vector2.zero, 36, RunMapGlyph.Shape.Diamond);
        _image = Glyph(_portrait, "PortraitImage", Vector2.zero, 30, RunMapGlyph.Shape.Portrait);
    }
    public void Render(RunMapMarkerState marker, Vector2 point)
    {
        MarkerId = marker.Id; _root.anchoredPosition = point;
        var tint = ColorFor(marker.Kind); if (marker.Dimmed) tint *= new Color(.55f, .55f, .55f, 1);
        bool avatar = marker.Kind == RunMapMarkerKind.Player || marker.Kind == RunMapMarkerKind.Npc || marker.Kind == RunMapMarkerKind.Interactive;
        _dot.gameObject.SetActive(marker.Kind == RunMapMarkerKind.Player || !avatar);
        _portrait.gameObject.SetActive(avatar);
        _dot.color = tint; _border.color = tint;
        _portrait.anchoredPosition = marker.Kind == RunMapMarkerKind.Player ? new Vector2(0, 27) : Vector2.zero;
        _image.Portrait = marker.Portrait; _image.Chest = marker.Kind == RunMapMarkerKind.Interactive;
        _image.color = marker.Dimmed ? new Color(.55f, .55f, .55f, 1) : Color.white;
        _image.SetAllDirty();
    }
    public static Color ColorFor(RunMapMarkerKind kind)
    {
        switch (kind)
        {
            case RunMapMarkerKind.Player: return new Color(1, .83f, .16f);
            case RunMapMarkerKind.Ally: return new Color(.25f, .65f, 1);
            case RunMapMarkerKind.Enemy: return new Color(1, .23f, .23f);
            case RunMapMarkerKind.Npc: return new Color(.35f, .95f, .74f);
            default: return new Color(.92f, .69f, .33f);
        }
    }
    public static RunMapGlyph Glyph(Transform parent, string name, Vector2 position, float size, RunMapGlyph.Shape shape)
    {
        var rect = GameplayUi.Rect(parent, name, Vector2.one * .5f, Vector2.one * .5f, position, Vector2.one * size);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var glyph = rect.gameObject.AddComponent<RunMapGlyph>(); glyph.Style = shape; glyph.raycastTarget = false;
        return glyph;
    }
}

/// <summary>UI-native dots and diamond-cropped portraits, including explicit person/chest fallback art.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RunMapGlyph : MaskableGraphic
{
    public enum Shape { Circle, Diamond, Portrait }
    public Shape Style;
    public Sprite Portrait;
    public bool Chest;
    public override Texture mainTexture => Portrait != null && Style == Shape.Portrait ? Portrait.texture : Texture2D.whiteTexture;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var r = GetPixelAdjustedRect();
        if (Style == Shape.Circle)
        {
            Circle(vh, r.center, r.size * .75f, new Color(color.r, color.g, color.b, .15f * color.a));
            Circle(vh, r.center, r.size * .5f, color); return;
        }
        Rect uv = Portrait != null && Style == Shape.Portrait
            ? new Rect(Portrait.rect.x / Portrait.texture.width, Portrait.rect.y / Portrait.texture.height,
                Portrait.rect.width / Portrait.texture.width, Portrait.rect.height / Portrait.texture.height) : new Rect(0, 0, 1, 1);
        Diamond(vh, r, Style == Shape.Portrait && Portrait == null ? new Color(.10f, .15f, .20f, 1) : color, uv);
        if (Style != Shape.Portrait || Portrait != null) return;
        var c = color;
        if (Chest)
        {
            Quad(vh, new Rect(r.xMin + r.width * .23f, r.yMin + r.height * .27f, r.width * .54f, r.height * .39f), c);
            Quad(vh, new Rect(r.xMin + r.width * .46f, r.yMin + r.height * .28f, r.width * .08f, r.height * .35f), new Color(.4f, .32f, .14f));
        }
        else
        {
            Circle(vh, r.center + Vector2.up * r.height * .13f, Vector2.one * r.width * .13f, c);
            Quad(vh, new Rect(r.center.x - r.width * .22f, r.center.y - r.height * .23f, r.width * .44f, r.height * .18f), c);
        }
    }
    private static void Diamond(VertexHelper vh, Rect r, Color c, Rect uv)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(r.center.x, r.yMin), c, new Vector2(uv.center.x, uv.yMin));
        vh.AddVert(new Vector3(r.xMin, r.center.y), c, new Vector2(uv.xMin, uv.center.y));
        vh.AddVert(new Vector3(r.center.x, r.yMax), c, new Vector2(uv.center.x, uv.yMax));
        vh.AddVert(new Vector3(r.xMax, r.center.y), c, new Vector2(uv.xMax, uv.center.y));
        vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
    }
    private static void Quad(VertexHelper vh, Rect r, Color c)
    {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector3(r.xMin, r.yMin), c, Vector2.zero); vh.AddVert(new Vector3(r.xMin, r.yMax), c, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), c, Vector2.zero); vh.AddVert(new Vector3(r.xMax, r.yMin), c, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }
    private static void Circle(VertexHelper vh, Vector2 center, Vector2 radius, Color c)
    {
        int i = vh.currentVertCount; vh.AddVert(center, c, Vector2.zero);
        for (int j = 0; j <= 20; j++)
        { float angle = j * Mathf.PI * 2 / 20; vh.AddVert(center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y), c, Vector2.zero); }
        for (int j = 0; j < 20; j++) vh.AddTriangle(i, i + j + 1, i + j + 2);
    }
}
