using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按键行：Label / KeyButton / KeyText / ResetButton。
///
/// ══════════ 这一行不碰 InputActionAsset ══════════
/// 行只抛两个意图（"玩家想重绑"、"玩家想恢复这一项的默认"），
/// 真正的交互式重绑定由页面转交给 InputManager 执行。
/// 这样行不必知道 Action 名、绑定序号、设备类型这一堆输入域概念，
/// 而输入域的逻辑也只有一处（InputManager），不会散进 UI 代码里。
///
/// KeyText 是动态的：显示的键名随当前选中的"输入设备"变化（键盘显示 A/D，手柄显示 ✕/□），
/// 所以它的文本由页面在刷新时写进来，而不是在 prefab 里写死。
/// </summary>
public class SettingRow_Keybind : SettingRowBase
{
    /// <summary>玩家点了按键按钮（请求开始重绑定）。</summary>
    public Action OnRebindRequested;

    /// <summary>玩家点了这一行的小"恢复默认"。</summary>
    public Action OnResetRequested;

    private Button _keyButton;
    private TextMeshProUGUI _keyText;
    private Button _resetButton;

    /// <summary>"正在等待按键"时按钮上显示这段文案，替代键名。</summary>
    private const string ListeningPrompt = "请按键…";

    /// <summary>当前该显示的键名。等待按键时被临时顶替，退出等待后要还原回来。</summary>
    private string _keyDisplay = "";

    private void Awake()
    {
        _keyButton = Find<Button>("KeyButton");
        _keyText = Find<TextMeshProUGUI>("KeyText", false);
        _resetButton = Find<Button>("ResetButton", false);

        if (_keyButton != null) _keyButton.onClick.AddListener(HandleKeyButtonClick);
        if (_resetButton != null) _resetButton.onClick.AddListener(HandleResetClick);
    }

    /// <summary>显示当前绑定（键名 / 手柄按键名）。</summary>
    public void SetKeyText(string text)
    {
        _keyDisplay = text ?? "";
        SetText(_keyText, _keyDisplay);
    }

    /// <summary>
    /// 进入 / 退出"等待按键"状态。
    /// 期间把按钮置为不可点，避免玩家连点导致叠出多个重绑定操作
    /// （Input System 同一时间只允许一个 PerformInteractiveRebinding 在跑，叠起来会互相取消）。
    ///
    /// prompt 用于带方向的重绑定：移动键是 1DAxis 复合绑定，要分两次采集左右方向，
    /// 只说"请按键…"玩家不知道该按左还是按右，所以由页面传入"左移：请按键…"这样的整句。
    /// 不传则用默认提示词。
    /// </summary>
    public void SetListening(bool listening, string prompt = null)
    {
        string text = _keyDisplay;
        if (listening) text = string.IsNullOrEmpty(prompt) ? ListeningPrompt : prompt;
        SetText(_keyText, text);

        if (_keyButton != null) _keyButton.interactable = !listening;
        if (_resetButton != null) _resetButton.interactable = !listening;
    }

    /// <summary>整行置灰（用于"绑定了手柄之外的设备"之类不该点的场合）。</summary>
    public void SetInteractable(bool interactable)
    {
        if (_keyButton != null) _keyButton.interactable = interactable;
        if (_resetButton != null) _resetButton.interactable = interactable;
    }

    private void HandleKeyButtonClick()
    {
        if (OnRebindRequested != null) OnRebindRequested();
    }

    private void HandleResetClick()
    {
        if (OnResetRequested != null) OnResetRequested();
    }
}
