using System.Collections;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按键绑定二级界面：页签（键鼠 / 手柄）+ 手柄型号 + 两套按键行 + "恢复当前方案默认"。
/// 由一级页（KeybindSettingsPage）的「更改按键绑定」按钮进来。
///
/// ══════════ 为什么改键要独占一页 ══════════
/// 改键是"低频、且需要专注"的操作：等待按键期间整页的按键行都要锁住防重入。
/// 把它和设备选择（选哪一套 ↔ 改哪一套，本来就是一件事的两面）放在一起、
/// 从鼠标反转/灵敏度这类"顺手一拨"的开关里分出来，玩家才看得出自己在编辑什么。
///
/// ══════════ 键鼠方案的行有两个格子 ══════════
/// 键鼠方案的每个操作要能【同时】绑键盘和鼠标（点鼠标左键和按 J 都能攻击），
/// 所以键鼠行都是双格（KeyButton 键盘格 + MouseButton 鼠标格），手柄三行是单格。
/// 两套行是两个独立的节点集合，按页签 SetActive 切换，而不是一套行里藏半个格子 ——
/// 这样每套行都能按自己的列宽摆整齐，行控件也保持"只管自己那几个子节点"的单一职责。
///
/// ══════════ 一条复合绑定拆成多行 ══════════
/// 键鼠的"移动"在资产里是一条 1DAxis 复合绑定（negative = 左、positive = 右）。
/// 挤在一行显示时界面会把两段拼成 "A / D"：玩家只看到一个格子，改键还得"一次改两个方向"。
/// 所以它被拆成【左移】【右移】两行，每行只认自己那一段（partName），
/// 改键也只采集那一段。判断依据与做法是通用的：凡是"一条复合绑定含多个方向"的行都这么拆 ——
/// 见 skill: unity-split-composite-keybindings。
///
/// 手柄的移动不拆：它是 &lt;Gamepad&gt;/leftStick/x 单轴绑定，没有段可拆。
///
/// ══════════ 键名换成了图标 ══════════
/// 键名原本一律是文字，长度随语言剧烈变化（"空格" ↔ "Mouse Left Button" ↔ 韩文长句），
/// 而键格与页签的宽度是摆 prefab 时定死的 —— 切语言就溢出、压到隔壁格子上。
/// 现在每格交给 KeyIconText：有图标显示图标，没有就回退显示文字。
/// 图标从哪来（KeyIconMap）与"这个控制叫什么"（InputManager.ControlKey）都在输入域算好，
/// 本页只负责把 (文字, 图标, 颜色) 三个值转交给行。
///
/// ══════════ 数据与运行时短暂不一致（与一级页同理）══════════
/// 重绑定必须【立刻写进 InputActionAsset】—— 按键格上显示的就是资产里当前的键，
/// 不写进去玩家看不到自己刚按的键。于是同步进 Pending.inputOverridesJson 待应用，
/// 点「返回」则由 SettingsManager.RevertEdit() 把资产回滚到已应用的那份。
///
/// ══════════ 页面被收起来时必须有兜底 ══════════
/// 见 OnDisable 与 OnCancelPendingEdit：一个还在等按键的重绑，如果在玩家离开之后继续跑，
/// 他按下的第一个键就会被当成改键写进资产 —— 而他以为那次改键早就取消了。
/// </summary>
public class KeybindBindingsPage : SettingsPageBase
{
    /// <summary>本页节点名。必须与 SettingPanelBuilder 生成的节点名一致。</summary>
    public const string PageNodeName = "Page_Keybind_Bindings";

    /// <summary>
    /// 复合绑定的分段名。必须与 .inputactions 里各 part 的 name 逐字一致 ——
    /// InputManager 是按名字找段的，它不知道也不该知道"移动"有哪两段。
    /// </summary>
    private const string PartNegative = "negative";
    private const string PartPositive = "positive";

    /// <summary>键鼠方案的行（双格）：左移 / 右移 / 攻击 / 跳跃。</summary>
    private readonly List<SettingRow_Keybind> _kmRows = new List<SettingRow_Keybind>();

    /// <summary>手柄方案的三行（单格）。</summary>
    private readonly List<SettingRow_Keybind> _padRows = new List<SettingRow_Keybind>();

    private Button _backButton;
    private Button _tabKeyboard;
    private Button _tabGamepad;
    private Button _tabModelPs;
    private Button _tabModelXbox;
    private GameObject _modelGroup;

    /// <summary>两个页签 Label 上的图标格。页签没铺过图标时为 null，那时页签就是纯文字。</summary>
    private KeyIconText _tabKeyboardIcon;
    private KeyIconText _tabGamepadIcon;

    private GameObject _tabKeyboardSelected;
    private GameObject _tabGamepadSelected;
    private GameObject _modelPsSelected;
    private GameObject _modelXboxSelected;

    private SettingRow_Button _resetRow;

    /// <summary>正在跑的重绑定协程。非空即"正在等待按键"。</summary>
    private Coroutine _rebindRoutine;

    /// <summary>当前一次采集是否已收尾、以及是怎么收尾的（由 InputManager 的回调写入）。</summary>
    private bool _stepFinished;
    private InputManager.RebindResult _stepResult;

    /// <summary>鼠标格的提示词。设备的排除规则已经保证这一格只收得到鼠标键。</summary>
    //private const string MousePrompt = "请按鼠标键…";
    private const string MousePrompt = "--";

    protected override void OnBind()
    {
        //【必须第一个注册】刷新动作是按注册顺序跑的（SettingsPageBase.RefreshAll），
        //而本动作负责 SetActive 两套行 —— 手柄行在 prefab 里是关着的，它们的 Awake
        //要等这一刻才跑，也才把 _keyText / _secondText 这些引用找出来。
        //如果排在行自己的文字刷新动作后面，顺序就反了：文字先写进一个还是 null 的引用（静默什么也没发生），
        //行才被激活，结果手柄行永远停在 prefab 里的占位「未绑定」上。
        AddRefresher(RefreshSchemeVisuals);

        BindHeader();

        //键鼠四行：每行两个格子。
        //"移动"是一条 1DAxis 复合绑定（negative = 左、positive = 右），拆成两行 ——
        //不拆的话界面把两段拼成 "A / D"，玩家只看到一个格子、改键要一次改两个方向。
        //鼠标格也照样按段传 partName：资产里 Mouse 组被摆成与 Keyboard 组同构，
        //所以"左移"的鼠标格就是 Mouse 组的 negative，能单独改绑。
        BindKeyRow(SettingIds.MoveLeft, InputManager.ActionMove, InputManager.BindingKind.Keyboard, true, PartNegative);
        BindKeyRow(SettingIds.MoveRight, InputManager.ActionMove, InputManager.BindingKind.Keyboard, true, PartPositive);
        BindKeyRow(SettingIds.Attack, InputManager.ActionAttack, InputManager.BindingKind.Keyboard, true);
        BindKeyRow(SettingIds.Jump, InputManager.ActionJump, InputManager.BindingKind.Keyboard, true);

        //手柄三行：一个格子。
        //手柄的移动不拆：它是 <Gamepad>/leftStick/x 单轴绑定，没有 negative/positive 段
        BindKeyRow(SettingIds.MoveGamepad, InputManager.ActionMove, InputManager.BindingKind.Gamepad, false);
        BindKeyRow(SettingIds.AttackGamepad, InputManager.ActionAttack, InputManager.BindingKind.Gamepad, false);
        BindKeyRow(SettingIds.JumpGamepad, InputManager.ActionJump, InputManager.BindingKind.Gamepad, false);

        _resetRow = BindButton(SettingIds.ResetScheme, ResetScheme);
    }

    // ══════════════════ 顶部：返回 / 页签 / 手柄型号 ══════════════════

    private void BindHeader()
    {
        Transform header = transform.Find("Header");
        if (header == null)
        {
            ChaosLog.Error(LogChannel.UI,
                "按键绑定二级界面下找不到 Header 节点，返回按钮与键鼠/手柄页签都不可用。" +
                "节点约定见 SettingPanelBuilder.BuildKeybindBindingsPage。");
            return;
        }

        Transform modelGroup = header.Find("ModelGroup");
        _modelGroup = modelGroup != null ? modelGroup.gameObject : null;
        if (modelGroup == null)
        {
            ChaosLog.Error(LogChannel.UI, "Header 下找不到 ModelGroup，手柄型号（PS / Xbox）将无法选择");
        }

        _backButton = FindButton(header, "BackButton");
        _tabKeyboard = FindButton(header, "Tab_Keyboard");
        _tabGamepad = FindButton(header, "Tab_Gamepad");
        _tabModelPs = modelGroup != null ? FindButton(modelGroup, "ModelTab_PS") : null;
        _tabModelXbox = modelGroup != null ? FindButton(modelGroup, "ModelTab_Xbox") : null;

        //页签 Label 上的图标格。没铺过图标时为 null，那时页签保持纯文字（行为与改造前一致）
        _tabKeyboardIcon = FindIconText(_tabKeyboard);
        _tabGamepadIcon = FindIconText(_tabGamepad);

        _tabKeyboardSelected = FindMarker(_tabKeyboard, "Selected");
        _tabGamepadSelected = FindMarker(_tabGamepad, "Selected");
        _modelPsSelected = FindMarker(_tabModelPs, "Selected");
        _modelXboxSelected = FindMarker(_tabModelXbox, "Selected");

        //返回不锁：等待按键期间点它也能走。HandleBackClicked 会先把那次重绑收掉，
        //所以不存在"页面关了但还在采集输入"的窗口。锁住反而会让人以为界面卡死
        if (_backButton != null) _backButton.onClick.AddListener(HandleBackClicked);

        if (_tabKeyboard != null) _tabKeyboard.onClick.AddListener(() => SetScheme(false));
        if (_tabGamepad != null) _tabGamepad.onClick.AddListener(() => SetScheme(true));
        if (_tabModelPs != null) _tabModelPs.onClick.AddListener(() => SetGamepadModel((int)GamepadModel.PlayStation));
        if (_tabModelXbox != null) _tabModelXbox.onClick.AddListener(() => SetGamepadModel((int)GamepadModel.Xbox));
    }

    private void HandleBackClicked()
    {
        //先收掉可能正在进行的那次重绑：不然玩家回到一级页之后按下的第一个键
        //会被当成改键写进资产，而界面上已经看不到"请按键…"了
        CancelPendingEdit();

        if (OnBackRequested != null) OnBackRequested();
    }

    private void SetScheme(bool gamepad)
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null) return;

        //页签只是 inputDevice 的一个视图：切键鼠写 Keyboard，切手柄就写回上次选的那个型号。
        //页签状态本身不存档 —— 存档里只有 inputDevice 和 gamepadModel 两个字段
        data.inputDevice = gamepad
            ? (int)InputScheme.ModelToDevice(data.gamepadModel)
            : (int)InputDeviceType.Keyboard;

        SettingsManager.Instance.NotifyPendingChanged();
        RefreshAll();
    }

    private void SetGamepadModel(int model)
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null) return;

        //型号组只在手柄页签下可见，所以点它必然是在手柄方案里。
        //gamepadModel 记下来给"下次从键鼠切回手柄"用，inputDevice 则当场同步 ——
        //只写前者的话，按键行还会按老型号显示另一套按键名（✕○□△ 还是 A/B/X/Y）
        data.gamepadModel = model;
        data.inputDevice = (int)InputScheme.ModelToDevice(model);

        SettingsManager.Instance.NotifyPendingChanged();
        RefreshAll();
    }

    // ══════════════════ 行 ══════════════════

    /// <summary>
    /// 一行按键控件。行只抛意图（"想重绑这一格"×2、"想恢复这一行"），
    /// 重绑定与恢复默认都在本页处理 —— 行不必知道 Action 名与绑定序号这些输入域概念。
    ///
    /// withMouseSlot：这一行有没有鼠标格。键鼠行有，手柄三行没有，
    /// 后者上的 OnSecondRebindRequested 永远不会被触发（那个按钮不存在）。
    ///
    /// partName：这条行只认复合绑定里的哪一段（null = 整条绑定，不拆）。
    /// 两个格子共用同一个 partName —— 资产里 Mouse 组被摆成与 Keyboard 组同构，
    /// 所以"左移的键盘格"和"左移的鼠标格"都是各自组里的 negative。
    /// </summary>
    private void BindKeyRow(string id, string actionName, InputManager.BindingKind kind, bool withMouseSlot,
        string partName = null)
    {
        SettingRow_Keybind row = FindRow<SettingRow_Keybind>(id);
        if (row == null) return;

        if (withMouseSlot && !row.HasSecondSlot)
        {
            ChaosLog.Warn(LogChannel.UI,
                row.name + " 是键鼠行，但 prefab 里没有 MouseButton/MouseText 子节点，" +
                "鼠标格不会显示也无法重绑");
        }

        row.OnRebindRequested = () => StartRebind(actionName, kind, row, false, partName);
        if (withMouseSlot)
        {
            //键鼠行的第二格固定是鼠标组：第一格是 Keyboard，第二格就是 Mouse
            row.OnSecondRebindRequested =
                () => StartRebind(actionName, InputManager.BindingKind.Mouse, row, true, partName);
        }
        row.OnResetRequested = () => ResetRow(actionName, kind, withMouseSlot, partName);

        if (withMouseSlot) _kmRows.Add(row);
        else _padRows.Add(row);

        AddRefresher(data => RefreshRowText(row, actionName, kind, withMouseSlot, data, partName));
    }

    /// <summary>
    /// 回填一行显示的两个键名（或图标）。
    ///
    /// 键名不是 Pending 里某个字段的直接映射（它取决于当前设备、还取决于资产里的覆盖），
    /// 所以走 GetBindingVisual 而不是 Bind* 那套 —— 那套只会在字段与控件之间搬运。
    ///
    /// 文字与图标一次拿回来（BindingVisual）：两者都源于同一条控制路径，
    /// 分开取等于把路径解析两遍，迟早会出现"文字对了图标却查不到"。
    /// </summary>
    private static void RefreshRowText(SettingRow_Keybind row, string actionName,
        InputManager.BindingKind kind, bool withMouseSlot, SettingsData data, string partName)
    {
        InputDeviceType device = (InputDeviceType)data.inputDevice;

        //手柄行的显示主题跟着当前手柄型号走（PS 显示 ✕○□△，Xbox 显示 A/B/X/Y）；
        //键盘格与鼠标格固定用键盘主题 —— 鼠标键名（左键/右键）本来就不分型号
        InputManager.BindingVisual key = InputManager.Instance.GetBindingVisual(actionName, kind,
            kind == InputManager.BindingKind.Gamepad ? device : InputDeviceType.Keyboard, partName);
        row.SetKeyDisplay(key.Text, key.Icon, key.IconColor);

        if (withMouseSlot)
        {
            InputManager.BindingVisual second = InputManager.Instance.GetBindingVisual(
                actionName, InputManager.BindingKind.Mouse, InputDeviceType.Keyboard, partName);
            row.SetSecondDisplay(second.Text, second.Icon, second.IconColor);
        }
    }

    /// <summary>
    /// 页签高亮、型号组的显隐、两套行的切换。全部走 refresher 机制，
    /// 于是「应用」「恢复默认」「返回」之后的刷新会自动把它带上，不需要额外的同步入口。
    /// </summary>
    private void RefreshSchemeVisuals(SettingsData data)
    {
        bool gamepad = InputScheme.IsGamepad(data.inputDevice);

        SetMarker(_tabKeyboardSelected, !gamepad);
        SetMarker(_tabGamepadSelected, gamepad);

        //型号只在"手柄页签"下才有意义，也只在那个时候出现
        if (_modelGroup != null) _modelGroup.SetActive(gamepad);

        //手柄页签下的型号高亮：inputDevice 已经是手柄时才读得出型号，
        //键鼠方案下就用存档里记着的 gamepadModel，好让切回手柄时高亮不闪
        int model = gamepad ? InputScheme.DeviceToModel((InputDeviceType)data.inputDevice) : data.gamepadModel;
        SetMarker(_modelPsSelected, gamepad && model == (int)GamepadModel.PlayStation);
        SetMarker(_modelXboxSelected, gamepad && model == (int)GamepadModel.Xbox);

        //两套行按页签切换。只留一套在场，VerticalLayoutGroup 会自动重排，不会留下空洞
        for (int i = 0; i < _kmRows.Count; i++) _kmRows[i].gameObject.SetActive(!gamepad);
        for (int i = 0; i < _padRows.Count; i++) _padRows[i].gameObject.SetActive(gamepad);

        //页签文案换成图标。放在最后：它只依赖上面算出的 model，与行的显隐无关
        ApplyTabIcons(model);
    }

    /// <summary>
    /// 两个页签显示图标而不是文字。
    ///
    /// ══════════════ 为什么页签最需要它 ══════════════
    /// 页签是溢出最严重的一处：英文 "Keyboard &amp; Mouse" 有 16 个字符，
    /// 而 Tab_Keyboard/Label 这个节点被摆成 76×50、TMP 38pt 且不换行 ——
    /// 直接压到旁边的 Tab_Gamepad 上，且越长的语言越糟（日/韩更宽）。
    /// 图标宽度恒定，语言再换也不会动。
    ///
    /// ══════════════ 文字没有丢 ══════════════
    /// Label 上的 LocalizedText 保持原样（译文照样往里写），只是 TMP 被关掉。
    /// 于是图标缺失时会自动回退成译文，不需要在页面里再抄一份文案。
    /// </summary>
    private void ApplyTabIcons(int gamepadModel)
    {
        KeyIconMap map = KeyIconMap.Active;
        if (map == null) return;

        if (_tabKeyboardIcon != null)
        {
            _tabKeyboardIcon.SetIconOnly(map.keyboardTabIcon, map.tint);
        }

        if (_tabGamepadIcon != null)
        {
            //手柄图标跟着型号走：PS 与 Xbox 的手柄外形不同，玩家一眼能看出当前在配哪一台。
            //键鼠页签下 model 取的是存档里记着的那个，所以切来切去时图标不会跳
            _tabGamepadIcon.SetIconOnly(map.GetTabIcon(InputScheme.ModelToDevice(gamepadModel)), map.tint);
        }
    }

    private static void SetMarker(GameObject marker, bool on)
    {
        if (marker != null && marker.activeSelf != on) marker.SetActive(on);
    }

    // ══════════════════ 重绑定 ══════════════════

    /// <summary>
    /// 开始一次改键。secondSlot = 改的是这一行的鼠标格（键鼠行才有）。
    /// partName = 这一行只改复合绑定的哪一段（null = 整条），拆行之后由它决定"只问一个方向"。
    /// </summary>
    private void StartRebind(string actionName, InputManager.BindingKind kind,
        SettingRow_Keybind row, bool secondSlot, string partName)
    {
        if (_rebindRoutine != null || InputManager.Instance.IsRebinding)
        {
            //正常路径下点不到这里（等待期间整页的行与页签都锁着），
            //所以走到这说明有别的入口在发重绑定请求，值得留条日志
            ChaosLog.Warn(LogChannel.Input, "已有一次按键重绑定在进行中，忽略对 " + actionName + " 的新请求");
            return;
        }

        _rebindRoutine = StartCoroutine(RebindRoutine(actionName, kind, row, secondSlot, partName));
    }

    /// <summary>
    /// 依次采集这个动作需要采集的每一段，任一段取消/超时则整条放弃。
    ///
    /// ══════════ 为什么可能分成多段 ══════════
    /// 移动是一条 1DAxis 复合绑定（A=负方向、D=正方向）：一次按键只能得到一个方向，
    /// 所以"改移动键"曾经必须依次采集两次（先问左移、再问右移），
    /// 只改半个方向会留下"左移是 A、右移还是个奇怪键"的残局。
    ///
    /// ══════════ 拆行之后它其实只有一段 ══════════
    /// 现在界面把每个方向摆成了独立一行，partName 把采集范围钉死在一段上，
    /// 所以这个循环退化成"跑一次"。保留循环是因为它本来就是通用写法：
    /// 哪天出现一条【不拆】的复合绑定（比如手柄的某条 2DVector），它照样能把这些段依次问完。
    /// 各段的提示词由 InputManager.GetRebindSteps 给出。
    /// </summary>
    private IEnumerator RebindRoutine(string actionName, InputManager.BindingKind kind,
        SettingRow_Keybind row, bool secondSlot, string partName)
    {
        List<InputManager.RebindStep> steps = InputManager.Instance.GetRebindSteps(actionName, kind, partName);
        if (steps.Count == 0)
        {
            ChaosLog.Warn(LogChannel.Input,
                actionName + " 在 " + kind + " 组下没有可重绑定的绑定，改键操作被跳过");
            _rebindRoutine = null;
            yield break;
        }

        //等待按键期间锁住整页的行与页签：玩家还能点到别的格子的话，
        //会在第一次重绑定还没收尾时发起第二次，两者互相取消，表现为"点了没反应"。
        //注意这里用 SetClickable 而不是 SetInteractable —— 正在等待的那一格要显示"请按…"提示，
        //调暗会让提示看不清
        SetLocked(true);

        bool allSucceeded = true;

        for (int i = 0; i < steps.Count; i++)
        {
            InputManager.RebindStep step = steps[i];
            string prompt = string.IsNullOrEmpty(step.Label)
                ? (kind == InputManager.BindingKind.Mouse ? MousePrompt : null)
                : "--";

            if (row != null)
            {
                if (secondSlot) row.SetSecondListening(true, prompt);
                else row.SetListening(true, prompt);
            }

            _stepFinished = false;
            _stepResult = InputManager.RebindResult.Aborted;

            InputManager.Instance.BeginRebind(actionName, step.BindingIndex, kind, OnStepFinished);

            //等这一步收尾（采到键、按取消键、超时都会回调，所以这里不会永久卡住）
            while (!_stepFinished) yield return null;

            if (row != null)
            {
                if (secondSlot) row.SetSecondListening(false);
                else row.SetListening(false);
            }

            if (_stepResult == InputManager.RebindResult.Cleared)
            {
                //玩家按了取消键，InputManager 已经把【整条】绑定置空了（复合绑定是头+各部分一起），
                //剩下的段不用再采集 —— 再问一遍"右移请按键"是没道理的，他要的就是这一格空着。
                //走到下面的 SyncOverridesToPending：置空同样是一次真实改动，不提交的话
                //界面显示"未绑定"而存档里还是旧键，点返回再进来又变回去
                break;
            }

            if (_stepResult != InputManager.RebindResult.Completed)
            {
                allSucceeded = false;
                break;
            }
        }

        SetLocked(false);

        if (allSucceeded)
        {
            //重绑定是直接改在 InputActionAsset 上的，这里把结果同步进暂存区，
            //「应用」按钮才会亮起来 —— 不同步的话玩家改完键点返回，
            //资产已被 RevertEdit 回滚，而存档里什么都没变，看着像改键根本没用
            SyncOverridesToPending();
        }

        RefreshAll();
        _rebindRoutine = null;
    }

    private void OnStepFinished(InputManager.RebindResult result)
    {
        _stepResult = result;
        _stepFinished = true;
    }

    /// <summary>等待按键期间把整页锁住。行用 SetClickable（不调暗），页签与型号用 interactable。</summary>
    private void SetLocked(bool locked)
    {
        for (int i = 0; i < _kmRows.Count; i++) _kmRows[i].SetClickable(!locked);
        for (int i = 0; i < _padRows.Count; i++) _padRows[i].SetClickable(!locked);

        if (_resetRow != null) _resetRow.SetClickable(!locked);

        if (_tabKeyboard != null) _tabKeyboard.interactable = !locked;
        if (_tabGamepad != null) _tabGamepad.interactable = !locked;
        if (_tabModelPs != null) _tabModelPs.interactable = !locked;
        if (_tabModelXbox != null) _tabModelXbox.interactable = !locked;
    }

    /// <summary>
    /// 面板级的「应用」「恢复默认」「返回」在动手前会调到这里。
    /// 走 InputManager.CancelRebind() 而不是自己 StopCoroutine：Cancel 会触发 OnCancel 回调，
    /// 协程才会从等待里出来、把行解锁并还原提示文案。只停协程的话界面会永远停在"请按键…"。
    /// </summary>
    protected override void OnCancelPendingEdit()
    {
        InputManager.Instance.CancelRebind();
    }

    /// <summary>
    /// 兜底。页面被隐藏（切分类、收起子页、面板关闭）时绝不能留一个还在等按键的重绑：
    /// 它会在玩家已经看不到提示的情况下继续采集输入，按下的第一个键就被写进资产。
    ///
    /// 上面的"从一级页退出"都走 CancelPendingEdit，但页面也可能因为别的原因失活，
    /// 所以这里再收一次。没有在跑时全是空操作。
    /// </summary>
    private void OnDisable()
    {
        if (_rebindRoutine != null)
        {
            StopCoroutine(_rebindRoutine);
            _rebindRoutine = null;
        }

        ClearListeningState();
        InputManager.Instance.CancelRebind();
    }

    /// <summary>还原提示文案并解锁。停掉协程后必须显式走这一趟，否则行会停在锁定状态。</summary>
    private void ClearListeningState()
    {
        for (int i = 0; i < _kmRows.Count; i++) ClearListeningState(_kmRows[i]);
        for (int i = 0; i < _padRows.Count; i++) ClearListeningState(_padRows[i]);

        SetLocked(false);
    }

    private static void ClearListeningState(SettingRow_Keybind row)
    {
        //必须先恢复文案再解锁：SetListening(false) 自己会按 Clickable 重算一次可点性，
        //顺序反过来的话最后一步又被它按旧的 Clickable 覆盖回去
        row.SetListening(false);
        row.SetSecondListening(false);
    }

    // ══════════════════ 恢复默认 ══════════════════

    /// <summary>
    /// 恢复单个操作在当前页签这一套下的默认。
    /// 键鼠行要把两个格子都复位 —— 只复位键盘格会留下"鼠标格还是玩家改过的键"，
    /// 而玩家按的按钮上写的是"恢复这一行"。
    ///
    /// partName 非空时【只复位那一段】：拆行之后"左移"与"右移"是两行两格，
    /// 点"左移"的重置却把右移也一起复位，那是顺手毁掉另一格 —— 与按钮上写的意思不符。
    /// </summary>
    private void ResetRow(string actionName, InputManager.BindingKind kind, bool withMouseSlot, string partName)
    {
        bool any = InputManager.Instance.ResetBinding(actionName, kind, partName);
        if (withMouseSlot)
        {
            // |= 而不是 ||=：第二个调用必须照常执行（bool 的 |= 不短路，正好）
            any |= InputManager.Instance.ResetBinding(actionName, InputManager.BindingKind.Mouse, partName);
        }

        if (!any) return;

        SyncOverridesToPending();
        RefreshAll();
    }

    /// <summary>
    /// 只恢复【当前页签那一套】的按键默认：站在键鼠页签上就只清键鼠（Keyboard + Mouse），
    /// 手柄那套原样留着，反之亦然。清掉另一套会让玩家"只想重置键盘"的操作顺手毁掉手柄配置。
    /// </summary>
    private void ResetScheme()
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null) return;

        bool gamepad = InputScheme.IsGamepad(data.inputDevice);
        if (gamepad)
        {
            InputManager.Instance.ClearOverridesFor(InputManager.BindingKind.Gamepad);
        }
        else
        {
            //键鼠方案横跨两个组：键盘格在 Keyboard、鼠标格在 Mouse，重置要一起覆盖到
            InputManager.Instance.ClearOverridesFor(InputManager.BindingKind.Keyboard);
            InputManager.Instance.ClearOverridesFor(InputManager.BindingKind.Mouse);
        }

        SyncOverridesToPending();
        RefreshAll();

        ChaosLog.Info(LogChannel.Input,
            "已恢复" + SettingsLabels.SchemeName(gamepad ? 1 : 0) + "方案的按键默认（待应用）");
    }

    /// <summary>把 InputActionAsset 当前的绑定覆盖同步进暂存区，并让「应用」按钮亮起来。</summary>
    private static void SyncOverridesToPending()
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null) return;

        data.inputOverridesJson = InputManager.Instance.SaveOverridesJson();
        SettingsManager.Instance.NotifyPendingChanged();
    }

    // ══════════════════ 查找节点 ══════════════════

    private static Button FindButton(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            ChaosLog.Error(LogChannel.UI, parent.name + " 下找不到 " + name + "，这个按钮不可用");
            return null;
        }

        Button button = child.GetComponent<Button>();
        if (button == null) ChaosLog.Error(LogChannel.UI, parent.name + "/" + name + " 上没有 Button 组件");
        return button;
    }

    /// <summary>取按钮下的 "Selected" 选中指示物。没有就只是看不到高亮，不算错误。</summary>
    private static GameObject FindMarker(Button button, string name)
    {
        if (button == null) return null;

        Transform marker = button.transform.Find(name);
        return marker != null ? marker.gameObject : null;
    }

    /// <summary>
    /// 取页签 Label 上的图标格。页签的文字由它自己的 LocalizedText 管，
    /// 所以这里只拿"切图标 / 切文字"的开关（见 KeyIconText.SetIconOnly），不参与写文案。
    /// </summary>
    private static KeyIconText FindIconText(Button button)
    {
        if (button == null) return null;

        Transform label = button.transform.Find("Label");
        return label != null ? label.GetComponent<KeyIconText>() : null;
    }
}
