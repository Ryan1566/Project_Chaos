using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>玩家界面的共用贴图索引。Resources 只存引用，位图由美术目录维护。</summary>
public sealed class GameplayUiSkin : ScriptableObject
{
    [SerializeField] private Sprite[] sprites;
    private static Dictionary<string, Sprite> _byName;
    private static Dictionary<string, Sprite> _locations;

    public static readonly Color Ink = new Color32(56, 38, 27, 255);
    public static readonly Color DarkPaper = new Color32(100, 79, 62, 255);
    public static readonly Color LightInk = new Color32(246, 228, 194, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIndex()
    {
        _byName = null;
        _locations = null;
    }

    private static Dictionary<string, Sprite> LoadIndex(string resource)
    {
        var index = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        var skin = Resources.Load<GameplayUiSkin>(resource);
        if (skin != null && skin.sprites != null)
            foreach (var sprite in skin.sprites)
                if (sprite != null) index[sprite.name] = sprite;
        return index;
    }

    public static Sprite Get(string name)
    {
        if (_byName == null) _byName = LoadIndex("UI/GameplayUiSkin");
        return _byName.TryGetValue(name, out var found) ? found : null;
    }

    public static Sprite Location(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (_locations == null) _locations = LoadIndex("UI/GameplayLocationSkin");
        return _locations.TryGetValue(key, out var found) ? found : Resources.Load<Sprite>("UI/WorldLocations/" + key);
    }

    public static void Apply(Image image, string name, Color tint)
    {
        if (image == null) return;
        var sprite = Get(name);
        if (sprite != null)
        {
            image.sprite = sprite;
            if (image.type != Image.Type.Filled)
                image.type = sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
        }
        image.color = tint;
    }

    public static void Button(Button button, string family = "UI_Btn_Md")
    {
        if (button == null || !(button.targetGraphic is Image image) || Get(family + "_Normal") == null) return;
        Apply(image, family + "_Normal", Color.white);
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = Get(family + "_Hover"),
            pressedSprite = Get(family + "_Pressed") ?? Get(family + "_Selected"),
            selectedSprite = Get(family + "_Selected") ?? Get(family + "_Hover"),
            disabledSprite = Get(family + "_Disabled")
        };
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.color = Ink;
    }
}
