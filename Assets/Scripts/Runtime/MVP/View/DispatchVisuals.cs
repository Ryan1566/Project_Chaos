using UnityEngine;

/// <summary>派遣界面的可替换表现素材；缺图时界面使用纯色与文字回退。</summary>
public sealed class DispatchVisuals : ScriptableObject
{
    public Sprite CardNormal;
    public Sprite CardSelected;
    public Sprite CardDisabled;
    public Sprite SlotEmpty;
    public Sprite SlotFilled;
    public Sprite SlotSelected;
    public Sprite DetailBackground;
    public Sprite PortraitFrame;
}
