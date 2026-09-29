using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 拉条行：Label / Slider / ValueText。
///
/// 用于四路音量（0~100，步进 1）、鼠标灵敏度（1~10）等。
///
/// ══════════ 为什么必须用 SetValueWithoutNotify ══════════
/// 页面刷新（把 Pending 的值灌回控件）时会走 SetValueWithoutNotify。
/// 若用 slider.value = x，Unity 会派发 onValueChanged，而那是"玩家改了值"的回调 ——
/// 于是变成 刷新→回调→写数据→通知刷新→回调… 的死循环。
/// 这条在 Unity UI 里是经典坑，凡是"代码回填控件"一律用 WithoutNotify 版本。
/// </summary>
public class SettingRow_Slider : SettingRowBase
{
    /// <summary>玩家拖动时回调，参数是取整后的值。</summary>
    public Action<int> OnValueChanged;

    private Slider _slider;
    private TextMeshProUGUI _valueText;

    private void Awake()
    {
        _slider = Find<Slider>("Slider");
        _valueText = Find<TextMeshProUGUI>("ValueText", false);

        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(HandleSliderValueChanged);
        }
    }

    /// <summary>可点性落在拉条上。整行置灰的颜色部分由基类 SetInteractable 负责。</summary>
    public override void SetClickable(bool clickable)
    {
        base.SetClickable(clickable);
        if (_slider != null) _slider.interactable = clickable;
    }

    /// <summary>页面在绑定时调用，设定取值域。</summary>
    public void Configure(int min, int max)
    {
        if (_slider == null) return;
        _slider.minValue = min;
        _slider.maxValue = max;
        //wholeNumbers 让 Slider 内部就吸附到整数，配合下面的 RoundToInt 双保险 ——
        //需求要的是"每次拉动改变 1 点"，靠这个按钮才能保证不会出现 37.4 这种值。
        _slider.wholeNumbers = true;
    }

    /// <summary>回填数值，不触发 OnValueChanged。</summary>
    public void SetValueWithoutNotify(int value)
    {
        if (_slider != null) _slider.SetValueWithoutNotify(value);
        SetText(_valueText, value.ToString());
    }

    private void HandleSliderValueChanged(float raw)
    {
        int value = Mathf.RoundToInt(raw);
        SetText(_valueText, value.ToString());
        if (OnValueChanged != null) OnValueChanged(value);
    }
}
