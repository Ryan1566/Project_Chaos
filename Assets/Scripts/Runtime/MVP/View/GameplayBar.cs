using UnityEngine;
using UnityEngine.UI;

/// <summary>用独立的轨底、填充和外框显示进度。进度只裁切中间的填充层。</summary>
public static class GameplayBar
{
    public static Image Build(RectTransform root, string family, float insetX = 6f, float insetY = 3f)
    {
        Layer(root, "Track", family + "_Track", new Vector2(insetX, insetY));
        var fill = Layer(root, "Fill", family + "_Fill", new Vector2(insetX, insetY));
        Layer(root, "Frame", family + "_Frame", Vector2.zero);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        return fill;
    }

    private static Image Layer(RectTransform parent, string name, string sprite, Vector2 inset)
    {
        var rect = GameplayUi.Rect(parent, name, Vector2.zero, Vector2.one * .5f, Vector2.zero, Vector2.zero);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
        var image = rect.gameObject.AddComponent<Image>();
        GameplayUiSkin.Apply(image, sprite, Color.white);
        image.raycastTarget = false;
        return image;
    }

    public static string HealthText(int mode, float ratio, int current, int maximum)
    {
        if (mode == 1) return Mathf.RoundToInt(ratio * 100f) + "%";
        if (mode == 2) return current + "/" + maximum;
        return string.Empty;
    }
}
