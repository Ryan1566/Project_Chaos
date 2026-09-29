using System;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 左右箭头选择行：Label / Prev / ValueText / Next。
///
/// 用于分辨率、视窗模式、帧率上限、画质等级、输入设备、攻击触发方式等"从有限几档里挑一个"的设置。
/// 相比 Dropdown 的好处是不需要额外一次点击就能看到"当前值"，手柄导航也更顺（左右即切换）。
///
/// 选项文案由页面通过 Configure 传入，行自己不保存业务含义 —— 它只知道"序号"。
/// 两头会置灰箭头的 Interactable，让玩家一眼看出已经到头了。
/// </summary>
public class SettingRow_Selector : SettingRowBase
{
    /// <summary>切到新序号时回调。</summary>
    public Action<int> OnValueChanged;

    private Button _prevButton;
    private Button _nextButton;
    private TextMeshProUGUI _valueText;

    private string[] _options;
    private int _index;

    private void Awake()
    {
        _prevButton = Find<Button>("Prev");
        _nextButton = Find<Button>("Next");
        _valueText = Find<TextMeshProUGUI>("ValueText");

        if (_prevButton != null) _prevButton.onClick.AddListener(() => Step(-1));
        if (_nextButton != null) _nextButton.onClick.AddListener(() => Step(1));
    }

    /// <summary>
    /// 可点性落在两个箭头上。箭头本来还会因为"到头了"而单独置灰，所以这里重新走一遍 RefreshDisplay，
    /// 由它把两个条件（行能不能点 && 是否到两头）合起来算，免得两处各写一半互相覆盖。
    /// 整行置灰的颜色部分由基类 SetInteractable 负责。
    /// </summary>
    public override void SetClickable(bool clickable)
    {
        base.SetClickable(clickable);
        RefreshDisplay();
    }

    /// <summary>页面在绑定时调用，传入全部档位的显示文案。</summary>
    public void Configure(string[] optionDisplayTexts)
    {
        _options = optionDisplayTexts;
        if (_options == null || _options.Length == 0)
        {
            ChaosLog.Warn(LogChannel.UI, name + " 的选项列表为空，它会一直显示空白");
        }
    }

    /// <summary>回填序号，不触发 OnValueChanged。</summary>
    public void SetIndexWithoutNotify(int index)
    {
        _index = ClampIndex(index);
        RefreshDisplay();
    }

    /// <summary>直接写一段文案、脱离档位体系。按键行那种"值是键名而不是档位"的场合用不到，
    /// 但留个口子给以后要显示"-"或"未绑定"之类状态的场合。</summary>
    public void SetTextWithoutNotify(string text)
    {
        SetText(_valueText, text);
    }

    private void Step(int delta)
    {
        if (_options == null || _options.Length == 0) return;

        int target = ClampIndex(_index + delta);
        if (target == _index) return;//已经到头，箭头本该是灰的，这里再兜一层

        _index = target;
        RefreshDisplay();
        if (OnValueChanged != null) OnValueChanged(_index);
    }

    private int ClampIndex(int index)
    {
        if (_options == null || _options.Length == 0) return 0;
        if (index < 0) return 0;
        if (index >= _options.Length) return _options.Length - 1;
        return index;
    }

    private void RefreshDisplay()
    {
        if (_options != null && _index >= 0 && _index < _options.Length)
        {
            SetText(_valueText, _options[_index]);
        }

        //到头的箭头置灰，比"点了没反应"更容易理解。
        //还要与整行置灰的状态（Interactable）取与：行被置灰时箭头一律不可点
        if (_prevButton != null) _prevButton.interactable = Clickable && (_index > 0);
        if (_nextButton != null)
        {
            _nextButton.interactable = Clickable && (_options != null && _index < _options.Length - 1);
        }
    }
}
