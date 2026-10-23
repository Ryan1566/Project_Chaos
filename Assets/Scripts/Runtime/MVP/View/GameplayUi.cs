using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Small shared widgets for the formal gameplay navigation.</summary>
public static class GameplayUi
{
    public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size = 22)
    {
        var rect = Rect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 4);
        rect.offsetMax = new Vector2(-12, -4);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = RunTeamHud.ResolveFont();
        label.fontSize = size;
        label.color = Color.white;
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    public static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size,
        UnityAction click)
    {
        var rect = Rect(parent, name, new Vector2(0, 1), new Vector2(0, 1), position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.18f, .32f, .48f, 1);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(click);
        Text(rect, "Text", text);
        return button;
    }
}
