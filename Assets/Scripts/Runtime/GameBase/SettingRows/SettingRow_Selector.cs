using System;
using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 左右箭头选择行：Label / Prev / ValueText / Next，或 Label / Prev / Options / Next。
///
/// 用于分辨率、视窗模式、帧率上限、画质等级、输入设备、攻击触发方式等"从有限几档里挑一个"的设置。
/// 相比 Dropdown 的好处是不需要额外一次点击就能看到"当前值"，手柄导航也更顺（左右即切换）。
///
/// ══════════════ 两种形态，不能混用 ══════════════
/// 【预摆档位】Options 下每个档位一个节点（Option_0..n），切档只切显隐、【不写文字】。
///   档位文案是生成时烘焙进 prefab 的字面量，每个档位各自挂 LocalizedText，
///   所以切档与切语言互相不干扰：切语言时组件自己更新，行不需要重跑刷新。
///   页面按这一形态绑定时调 ConfigureOptions（"无边框全屏/全屏/边框窗口"这类固定档位）。
/// 【运行时文字】只有一个 ValueText，切档时由本行写文字。
///   适用于"档位文字是算出来的"场合 —— 分辨率档位由显示器能力过滤而来、帧率只是数字。
///   页面按这一形态绑定时调 Configure（老接口）。
///
/// 两种形态在可见性上等价，所以 Cutover 时页面调错了接口的表现是【文字不出现】，
/// 而不是切档失灵；ConfigureOptions 会为此打一条明确的错误日志（见它的注释）。
///
/// 选项文案由页面传入，行自己不保存业务含义 —— 它只知道"序号"。
/// 两头会置灰箭头的 Interactable，让玩家一眼看出已经到头了。
/// </summary>
public class SettingRow_Selector : SettingRowBase
{
    /// <summary>切到新序号时回调。</summary>
    public Action<int> OnValueChanged;

    private Button _prevButton;
    private Button _nextButton;

    /// <summary>运行时文字形态：唯一的文字节点。预摆形态下为 null。</summary>
    private TextMeshProUGUI _valueText;

    /// <summary>预摆形态：Options 下的档位节点，下标 = 档位序号。运行时文字形态下为空。</summary>
    private TextMeshProUGUI[] _optionNodes;

    /// <summary>各档位的显示文字。预摆形态下与 _optionNodes 一一对应（首次读自预制体字面量）。</summary>
    private string[] _optionTexts;

    /// <summary>预摆形态下各档位的本地化 Key，与 _optionNodes 同序。由页面的 ConfigureOptions 传入。</summary>
    private string[] _optionKeys;

    private int _index;

    private void Awake()
    {
        _prevButton = Find<Button>("Prev");
        _nextButton = Find<Button>("Next");
        _valueText = Find<TextMeshProUGUI>("ValueText", false);

        Transform optionsNode = transform.Find("Options");
        if (optionsNode != null)
        {
            List<TextMeshProUGUI> nodes = new List<TextMeshProUGUI>();
            for (int i = 0; i < optionsNode.childCount; i++)
            {
                TextMeshProUGUI t = optionsNode.GetChild(i).GetComponent<TextMeshProUGUI>();
                if (t != null) nodes.Add(t);
            }
            _optionNodes = nodes.ToArray();

            //把预制体里的字面量记下来：ConfigureOptions 只传 Key、不传文字，
            //这样"预制体里写什么"就是唯一事实来源，代码里不会藏着第二份文案。
            _optionTexts = new string[_optionNodes.Length];
            for (int i = 0; i < _optionNodes.Length; i++) _optionTexts[i] = _optionNodes[i].text;
        }

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

    /// <summary>页面在绑定时调用，传入全部档位的显示文案（运行时文字形态）。</summary>
    public void Configure(string[] optionDisplayTexts)
    {
        _optionTexts = optionDisplayTexts;
        if (_optionTexts == null || _optionTexts.Length == 0)
        {
            ChaosLog.Warn(LogChannel.UI, name + " 的选项列表为空，它会一直显示空白");
        }
    }

    /// <summary>
    /// 页面在绑定时调用，只为【预摆档位】形态登记各档的本地化 Key。
    ///
    /// ══════════════ 为什么只传 Key、不传文案 ══════════════
    /// 文案在预制体里、由每个档位自己的 LocalizedText 负责显示。这里记下的 Key 只有一个用途：
    /// 切档时知道"没有 Key"的档位该显示预制体里的原文（兜底），不会把 Key 当文字印在界面上。
    ///
    /// keys 必须与 Options 下的档位【按同一顺序】一一对应 —— 对错了会让档位显示隔壁那一档的文字。
    ///
    /// ══════════════ 形态不匹配时会明确报错 ══════════════
    /// 如果这行在预制体里是 ValueText 形态（没有 Options 节点），本方法会报一条错误并返回。
    /// 不报的话表现是"点了箭头，文字毫无变化"，从现象完全看不出是绑错了接口。
    /// </summary>
    public void ConfigureOptions(string[] keys)
    {
        if (_optionNodes == null || _optionNodes.Length == 0)
        {
            ChaosLog.Error(LogChannel.UI,
                name + " 在预制体里没有 Options 子节点，无法使用预摆档位。" +
                "要么改用 Configure（运行时写 ValueText），要么重新生成设置面板预制体。");
            return;
        }

        if (keys != null && keys.Length != _optionNodes.Length)
        {
            ChaosLog.Warn(LogChannel.UI,
                name + " 的档位 Key 有 " + keys.Length + " 个，而预制体里摆了 " +
                _optionNodes.Length + " 个档位节点，多出来的档位会显示预制体里的原文。");
        }

        _optionKeys = keys;
        //预制体里第一个档位是默认激活的，索引也归零，两边对齐
        SetIndexWithoutNotify(0);
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
        if (_optionTexts == null || _optionTexts.Length == 0) return;

        int target = ClampIndex(_index + delta);
        if (target == _index) return;//已经到头，箭头本该是灰的，这里再兜一层

        _index = target;
        RefreshDisplay();
        if (OnValueChanged != null) OnValueChanged(_index);
    }

    private int ClampIndex(int index)
    {
        int count = CountOptions();
        if (count == 0) return 0;
        if (index < 0) return 0;
        if (index >= count) return count - 1;
        return index;
    }

    /// <summary>
    /// 档位数的权威来源。两种形态不会同时有值：预摆形态只有节点，运行时形态只有文字表。
    /// </summary>
    private int CountOptions()
    {
        if (_optionNodes != null && _optionNodes.Length > 0) return _optionNodes.Length;
        return _optionTexts != null ? _optionTexts.Length : 0;
    }

    private void RefreshDisplay()
    {
        ShowCurrentOption();

        //到头的箭头置灰，比"点了没反应"更容易理解。
        //还要与整行置灰的状态（Interactable）取与：行被置灰时箭头一律不可点
        int count = CountOptions();
        if (_prevButton != null) _prevButton.interactable = Clickable && (_index > 0);
        if (_nextButton != null)
        {
            _nextButton.interactable = Clickable && (_index < count - 1);
        }
    }

    private void ShowCurrentOption()
    {
        if (_optionNodes != null && _optionNodes.Length > 0)
        {
            for (int i = 0; i < _optionNodes.Length; i++)
            {
                if (_optionNodes[i] == null) continue;
                bool on = (i == _index);

                //档位节点被关掉时它的 LocalizedText.Start 不跑，所以每次点亮都补一次取词，
                //否则从"从没显示过"的档位切过来会停在 Key 或中文原文上。
                if (on)
                {
                    string key = (_optionKeys != null && i < _optionKeys.Length) ? _optionKeys[i] : null;
                    if (!string.IsNullOrEmpty(key)) _optionNodes[i].GetComponent<LocalizedText>().SetKey(key);
                }

                if (_optionNodes[i].gameObject.activeSelf != on) _optionNodes[i].gameObject.SetActive(on);
            }
            return;
        }

        if (_optionTexts != null && _index >= 0 && _index < _optionTexts.Length)
        {
            SetText(_valueText, _optionTexts[_index]);
        }
    }
}
