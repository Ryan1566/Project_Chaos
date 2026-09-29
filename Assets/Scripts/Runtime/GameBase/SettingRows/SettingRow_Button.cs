using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按钮行：Label / Button。用于"恢复所有按键绑定"这类一次性动作。
/// 它不承载任何值，所以没有 SetValueWithoutNotify。
/// </summary>
public class SettingRow_Button : SettingRowBase
{
    /// <summary>点击回调。</summary>
    public Action OnClick;

    private Button _button;

    private void Awake()
    {
        _button = Find<Button>("Button");
        if (_button != null) _button.onClick.AddListener(HandleClick);
    }

    /// <summary>可点性落在按钮上。整行置灰的颜色部分由基类 SetInteractable 负责。</summary>
    public override void SetClickable(bool clickable)
    {
        base.SetClickable(clickable);
        if (_button != null) _button.interactable = clickable;
    }

    private void HandleClick()
    {
        if (OnClick != null) OnClick();
    }
}
