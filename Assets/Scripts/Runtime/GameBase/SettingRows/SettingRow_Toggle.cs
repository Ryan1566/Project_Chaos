using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开关行：Label / Toggle。用于垂直同步、鼠标水平/垂直反转等二值设置。
///
/// 回填同样必须用 SetIsOnWithoutNotify —— 理由见 SettingRow_Slider 的注释。
/// </summary>
public class SettingRow_Toggle : SettingRowBase
{
    /// <summary>玩家拨动开关时回调。</summary>
    public Action<bool> OnValueChanged;

    private Toggle _toggle;

    private void Awake()
    {
        _toggle = Find<Toggle>("Toggle");
        if (_toggle != null) _toggle.onValueChanged.AddListener(HandleToggleChanged);
    }

    /// <summary>回填开关状态，不触发 OnValueChanged。</summary>
    public void SetValueWithoutNotify(bool value)
    {
        if (_toggle != null) _toggle.SetIsOnWithoutNotify(value);
    }

    /// <summary>可点性落在开关上。整行置灰的颜色部分由基类 SetInteractable 负责。</summary>
    public override void SetClickable(bool clickable)
    {
        base.SetClickable(clickable);
        if (_toggle != null) _toggle.interactable = clickable;
    }

    private void HandleToggleChanged(bool value)
    {
        if (OnValueChanged != null) OnValueChanged(value);
    }
}
