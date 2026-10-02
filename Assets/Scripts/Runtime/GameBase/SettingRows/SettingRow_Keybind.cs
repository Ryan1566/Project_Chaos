using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按键行：Label / KeyButton / KeyText / ResetButton，另可带【第二格】MouseButton / MouseText。
///
/// ══════════ 两格是怎么回事 ══════════
/// 键鼠方案的每个操作要能同时映射键盘与鼠标（点鼠标左键和按 J 都能攻击），
/// 所以这一行有左右两格：左格 = 键盘键，右格 = 鼠标键，两格【共存、同时生效】。
/// 手柄方案的每个操作只有一个设备，行里就不摆 MouseButton / MouseText 这两个节点，
/// 本类用 Find(..., required: false) 去找它们，找不到就整个第二格逻辑都不参与。
///
/// ══════════ 这一行不碰 InputActionAsset ══════════
/// 行只抛三个意图（"想重绑这一格"×2、"想恢复这一行的默认"），
/// 真正的交互式重绑定由页面转交给 InputManager 执行。
/// 这样行不必知道 Action 名、绑定序号、设备类型这一堆输入域概念，
/// 而输入域的逻辑也只有一处（InputManager），不会散进 UI 代码里。
///
/// KeyText / MouseText 都是动态的：显示的键名随当前选中的设备变化（键盘显示 A/D，手柄显示 ✕/□），
/// 所以它们的文本由页面在刷新时写进来，而不是在 prefab 里写死。
///
/// ══════════ 图标格：KeyText / MouseText 上的 KeyIconText ══════════
/// 键名的字符串长度随语言剧烈变化（"空格" ↔ "Mouse Left Button" ↔ 韩文长句），
/// 而键格宽度是摆 prefab 时定死的，于是切语言就溢出。所以这一格改成
/// "有图标就显示图标、没有就显示文字"，由 KeyText / MouseText 上的 KeyIconText 负责。
///
/// 契约（页面也要照这个来）：
///   · 图标由页面按输入域算好传进来（SetKeyDisplay），行【不查】图标映射表；
///   · 显示图标时行会把键格的白色底（KeyButton.image）交给图标格去隐藏 —— 图标自带键帽外形；
///   · "等待按键"期间强制显示提示词文字，退出时按记下的键名 + 图标还原。
/// </summary>
public class SettingRow_Keybind : SettingRowBase
{
    /// <summary>玩家点了主格（键鼠方案的键盘格 / 手柄方案唯一的那一格）。</summary>
    public Action OnRebindRequested;

    /// <summary>玩家点了第二格（键鼠方案的鼠标格）。单格行上永远不会触发。</summary>
    public Action OnSecondRebindRequested;

    /// <summary>玩家点了这一行的小"恢复默认"。会同时恢复本行的两格。</summary>
    public Action OnResetRequested;

    private Button _keyButton;
    private TextMeshProUGUI _keyText;
    private KeyIconText _keyIconText;
    private Button _secondButton;
    private TextMeshProUGUI _secondText;
    private KeyIconText _secondIconText;
    private Button _resetButton;

    /// <summary>"正在等待按键"时按钮上显示这段文案，替代键名。</summary>
    private const string ListeningPrompt = "请按键…";

    /// <summary>当前该显示的键名。等待按键时被临时顶替，退出等待后要还原回来。</summary>
    private string _keyDisplay = "";
    private string _secondDisplay = "";

    /// <summary>
    /// 当前该显示的图标，与上面的键名【成对】。
    ///
    /// 为什么必须一起记住：退出"等待按键"时要还原成"改键前那一格的样子"，
    /// 只还原文字的话图标就永久消失了 —— 那是改完一次键、这一格再也看不到图标的经典症状。
    /// null = 这个键没有图标，显示键名文字。
    /// </summary>
    private Sprite _keyDisplayIcon;
    private Sprite _secondDisplayIcon;

    /// <summary>图标颜色。由调用方按映射表算好传进来（彩色面键不染色），行本身不查表。</summary>
    private Color _keyDisplayIconColor = Color.white;
    private Color _secondDisplayIconColor = Color.white;

    /// <summary>本行有没有第二格。手柄方案的三个行没有，那时相关的成员都是 null。</summary>
    public bool HasSecondSlot { get { return _secondButton != null || _secondText != null; } }

    private void Awake()
    {
        _keyButton = Find<Button>("KeyButton");
        _keyText = Find<TextMeshProUGUI>("KeyText", false);
        //图标格是可选的：行改成图标显示是后加的能力，没铺到的 prefab 上它不存在，
        //那时本类退化成"只会写文字"，行为与改造前完全一致
        _keyIconText = Find<KeyIconText>("KeyText", false);

        //第二格是可选的：只有键鼠方案的按键行摆了两个节点，所以 required 传 false
        _secondButton = Find<Button>("MouseButton", false);
        _secondText = Find<TextMeshProUGUI>("MouseText", false);
        _secondIconText = Find<KeyIconText>("MouseText", false);

        _resetButton = Find<Button>("ResetButton", false);

        //把键格的白色底注入给图标格：显示图标时要把它关掉（图标自带键帽，叠起来是双层边框）。
        //这件事由行来做而不是让图标格自己去父子树上找 —— 行本来就是按约定名找 KeyButton 的那一方，
        //而页签上的图标格【不该】隐藏底图（页签底图是圆角条，不是键帽）
        if (_keyIconText != null && _keyButton != null) _keyIconText.frameImage = _keyButton.image;
        if (_secondIconText != null && _secondButton != null) _secondIconText.frameImage = _secondButton.image;

        if (_keyButton != null) _keyButton.onClick.AddListener(HandleKeyButtonClick);
        if (_secondButton != null) _secondButton.onClick.AddListener(HandleSecondButtonClick);
        if (_resetButton != null) _resetButton.onClick.AddListener(HandleResetClick);
    }

    /// <summary>显示主格当前绑定（键名 / 手柄按键名）。没有对应图标时用这个。</summary>
    public void SetKeyText(string text)
    {
        SetKeyDisplay(text, null, Color.white);
    }

    /// <summary>
    /// 显示主格当前绑定：给了图标就显示图标，否则显示文字。
    ///
    /// 参数全部由页面按输入域算好（InputManager.GetBindingVisual）——
    /// 行不查 InputActionAsset，也不查图标映射表，职责边界与重绑定那边保持一致。
    /// </summary>
    public void SetKeyDisplay(string text, Sprite icon, Color iconColor)
    {
        _keyDisplay = text ?? "";
        _keyDisplayIcon = icon;
        _keyDisplayIconColor = iconColor;

        if (_keyIconText != null) _keyIconText.SetDisplay(_keyDisplay, icon, iconColor);
        else SetText(_keyText, _keyDisplay);
    }

    /// <summary>显示第二格当前绑定（鼠标键名，未绑定时是"未绑定"）。单格行上是空操作。</summary>
    public void SetSecondText(string text)
    {
        SetSecondDisplay(text, null, Color.white);
    }

    /// <summary>显示第二格当前绑定。语义与 SetKeyDisplay 完全一致，只是落在鼠标格上。</summary>
    public void SetSecondDisplay(string text, Sprite icon, Color iconColor)
    {
        _secondDisplay = text ?? "";
        _secondDisplayIcon = icon;
        _secondDisplayIconColor = iconColor;

        if (_secondIconText != null) _secondIconText.SetDisplay(_secondDisplay, icon, iconColor);
        else SetText(_secondText, _secondDisplay);
    }

    /// <summary>
    /// 进入 / 退出主格的"等待按键"状态。
    /// 期间把按钮置为不可点，避免玩家连点导致叠出多个重绑定操作
    /// （Input System 同一时间只允许一个 PerformInteractiveRebinding 在跑，叠起来会互相取消）。
    ///
    /// prompt 用于带方向的重绑定：移动键是 1DAxis 复合绑定，要分两次采集左右方向，
    /// 只说"请按键…"玩家不知道该按左还是按右，所以由页面传入"左移：请按键…"这样的整句。
    /// 不传则用默认提示词。
    /// </summary>
    public void SetListening(bool listening, string prompt = null)
    {
        if (_keyIconText != null)
        {
            //等待按键时必须显示提示词：这一格现在要告诉玩家"请按键"，没有任何图标能表达这件事。
            //所以图标让位给文字；退出时按记住的键名 + 图标整对还原
            if (listening) _keyIconText.SetDisplay(
                string.IsNullOrEmpty(prompt) ? ListeningPrompt : prompt, null, Color.white);
            else _keyIconText.SetDisplay(_keyDisplay, _keyDisplayIcon, _keyDisplayIconColor);
        }
        else
        {
            string text = _keyDisplay;
            if (listening) text = string.IsNullOrEmpty(prompt) ? ListeningPrompt : prompt;
            SetText(_keyText, text);
        }

        ApplyClickableState(listening);
    }

    /// <summary>进入 / 退出第二格（鼠标格）的"等待按键"状态。单格行上是空操作。</summary>
    public void SetSecondListening(bool listening, string prompt = null)
    {
        if (_secondIconText != null)
        {
            if (listening) _secondIconText.SetDisplay(
                string.IsNullOrEmpty(prompt) ? ListeningPrompt : prompt, null, Color.white);
            else _secondIconText.SetDisplay(_secondDisplay, _secondDisplayIcon, _secondDisplayIconColor);
        }
        else
        {
            string text = _secondDisplay;
            if (listening) text = string.IsNullOrEmpty(prompt) ? ListeningPrompt : prompt;
            SetText(_secondText, text);
        }

        ApplyClickableState(listening);
    }

    /// <summary>可点性落在两格按钮与重置按钮上。整行置灰的颜色部分由基类 SetInteractable 负责。</summary>
    public override void SetClickable(bool clickable)
    {
        base.SetClickable(clickable);
        ApplyClickableState(false);
    }

    /// <summary>
    /// 整行置灰。基类只调暗文字（它缓存的是 TextMeshProUGUI），
    /// 图标得在这里单独转发一次 —— 不转发的话整行变灰时图标还是亮的，一眼看出没一起禁用。
    /// </summary>
    public override void SetInteractable(bool interactable)
    {
        base.SetInteractable(interactable);

        if (_keyIconText != null) _keyIconText.SetDimmed(!interactable);
        if (_secondIconText != null) _secondIconText.SetDimmed(!interactable);
    }

    /// <summary>
    /// 三个按钮的可点性只由两件事决定：本行能不能点（Clickable，置灰与临时锁都会写它）
    /// 和"是不是正在等玩家按键"。
    ///
    /// 集中在一处算，是因为两个 SetXxxListening 与 SetClickable 都会改同一批按钮：
    /// 各写一份的话，"退出等待"那条路径会把整行被置灰/被锁的状态一起点亮回来。
    /// </summary>
    private void ApplyClickableState(bool listening)
    {
        bool on = Clickable && !listening;
        if (_keyButton != null) _keyButton.interactable = on;
        if (_secondButton != null) _secondButton.interactable = on;
        if (_resetButton != null) _resetButton.interactable = on;
    }

    private void HandleKeyButtonClick()
    {
        if (OnRebindRequested != null) OnRebindRequested();
    }

    private void HandleSecondButtonClick()
    {
        if (OnSecondRebindRequested != null) OnSecondRebindRequested();
    }

    private void HandleResetClick()
    {
        if (OnResetRequested != null) OnResetRequested();
    }
}
