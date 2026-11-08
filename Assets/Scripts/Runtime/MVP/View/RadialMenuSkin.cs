using System;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/快捷轮盘皮肤", fileName = "RadialMenuSkin")]
public sealed class RadialMenuSkin : ScriptableObject
{
    public const int MaxEntries = 12;
    public const string ResourcePath = "UI/RadialMenu/DefaultSkin";

    [Serializable]
    public struct Slice
    {
        public Sprite normal;
        public Sprite selected;
        public Sprite disabled;
    }

    [SerializeField, Tooltip("内环。第 1 到 12 个元素分别用于 1 到 12 项。扇区朝上，圆心在画布中心。")]
    private Slice[] primary = new Slice[MaxEntries];
    [SerializeField, Tooltip("外环。第 1 到 12 个元素分别用于 1 到 12 项，与内环共用画布尺寸。")]
    private Slice[] secondary = new Slice[MaxEntries];
    [SerializeField] private Color normalTint = new Color(.07f, .14f, .23f, .96f);
    [SerializeField] private Color selectedTint = new Color(.7f, .48f, .12f, .96f);
    [SerializeField] private Color disabledTint = new Color(.13f, .14f, .16f, .95f);
    [SerializeField] private Sprite pointer;
    [SerializeField] private Color pointerTint = new Color(1f, .86f, .4f);

    public Sprite Pointer => pointer;
    public Color PointerTint => pointerTint;

    public Sprite GetSlice(int tier, int count, bool isSelected, bool isEnabled, out Color tint)
    {
        var ring = tier == 0 ? primary : secondary;
        tint = isSelected ? selectedTint : isEnabled ? normalTint : disabledTint;
        if (ring == null || count < 1 || count > ring.Length) return null;
        var slice = ring[count - 1];
        // 状态专用 Sprite 使用原色。未配置时，共用普通 Sprite 和对应状态色。
        Sprite variant = isSelected ? slice.selected : isEnabled ? null : slice.disabled;
        if (variant == null) return slice.normal;
        tint = Color.white;
        return variant;
    }
}
