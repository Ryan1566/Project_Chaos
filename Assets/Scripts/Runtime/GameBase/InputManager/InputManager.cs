using System;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 输入管理器：动作映射、按键重绑定、绑定覆盖的存取。
///
/// ══════════ 迁移说明：为什么旧版轮询还在 ══════════
/// 本类原来是纯旧版输入（Input.GetKeyDown 轮询 Escape 与 W，命中就抛 GetKeyDown/GetKeyUp 事件）。
/// 现在工程的后端是 Both（见 ProjectSettings 的 activeInputHandler），新输入系统接管
/// 移动/攻击/跳跃这三个【可自定义】的动作，而 Escape 与 W 的轮询保留不动，原因有三：
///   1. 事件契约要求保留：EventConstName.GetKeyDown/GetKeyUp 的参数是 KeyCode，
///      而新输入系统给的是 InputControl，做 KeyCode 翻译等于为了搬而搬；
///   2. 这两个键不在"可自定义"的范围内，玩家改不到它们，也就没有重绑定的需求；
///   3. 后端是 Both，旧版 API 依然可用，保留它不产生任何额外成本。
/// 等以后做 UI 导航/暂停菜单时，Escape 会直接并入新系统的 UI 动作表，那时再删这段。
///
/// ══════════ 绑定组的约定（与 ChaosInputActions.inputactions 强耦合）══════════
/// 每组下每个动作【只有一条主绑定】，它就是按键界面上可重绑的那一格。
/// 键鼠方案的三个操作各有两格（键盘 + 鼠标），所以比手柄方案多用到 "Mouse" 组：
///     Move   : Keyboard = 1DAxis 复合（A/D）          Mouse = 空（未绑定）        Gamepad = &lt;Gamepad&gt;/leftStick/x
///     Attack : Keyboard = J                           Mouse = &lt;Mouse&gt;/leftButton  Gamepad = &lt;Gamepad&gt;/buttonWest
///     Jump   : Keyboard = K                           Mouse = 空（未绑定）        Gamepad = &lt;Gamepad&gt;/buttonSouth
/// 两格【同时生效】：点鼠标左键和按 J 都能触发攻击，不存在"二选一"。这就是为什么鼠标键要有自己的组，
/// 而不是塞进 Keyboard 组当第二条绑定 —— 那样界面就没法把"键盘格"和"鼠标格"分开显示与重绑定。
///
/// 空路径 = 官方支持的"未绑定"态（InputBindingResolver 里 "Disabled if path is empty"），
/// 静默不生效、不报日志，玩家点了那一格就能绑上。
///
/// 另有 "KeyboardFixed" 组的固定备用键（方向键、空格），它们【不参与重绑定】，只作为打不掉的兜底。
///
/// ⚠ 组名比较必须按分号切分后逐项比对，不能用 Contains ——
/// "KeyboardFixed" 的字符串里含有 "Keyboard"，用 Contains 会把备用键也当成主绑定翻出来
/// （症状：跳跃行显示"K"却能改到"空格"上去，或者重绑定改了一个看不见的绑定）。
/// </summary>
public class InputManager : SingletonBase<InputManager>
{
    // 动作与绑定组的名字，必须与 ChaosInputActions.inputactions 里一致
    public const string MapName = "Player";
    public const string ActionMove = "Move";
    public const string ActionAttack = "Attack";
    public const string ActionJump = "Jump";

    public const string GroupKeyboard = "Keyboard";
    /// <summary>键鼠方案里"鼠标那一格"所在的组。空路径 = 未绑定。</summary>
    public const string GroupMouse = "Mouse";
    public const string GroupGamepad = "Gamepad";

    /// <summary>
    /// 要编辑哪一格绑定。与"输入设备"不是一回事：
    /// 设备（键鼠 / PS / Xbox）决定当前显示哪一套格子，而 kind 决定编辑的是其中哪一格。
    /// 键鼠方案的每个操作有 Keyboard 与 Mouse 两格，手柄方案只有 Gamepad 一格。
    /// </summary>
    public enum BindingKind
    {
        Keyboard = 0,
        Mouse = 1,
        Gamepad = 2,
    }

    /// <summary>kind → 绑定组名。取值必须与 .inputactions 里的 groups 字段逐字一致。</summary>
    private static string GroupName(BindingKind kind)
    {
        switch (kind)
        {
            case BindingKind.Keyboard: return GroupKeyboard;
            case BindingKind.Mouse: return GroupMouse;
            default: return GroupGamepad;
        }
    }

    /// <summary>一次重绑定要采集的一段。复合绑定（移动）会被拆成多段依次采集。</summary>
    public struct RebindStep
    {
        /// <summary>提示词，如"左移"；单段绑定为 null。</summary>
        public string Label;
        /// <summary>要写入覆盖的绑定序号。⚠ 复合绑定给的是【各部分】的序号，不是复合头。</summary>
        public int BindingIndex;
    }

    /// <summary>一次重绑定的收尾方式。</summary>
    public enum RebindResult
    {
        /// <summary>采到了新键，已写进覆盖。</summary>
        Completed,
        /// <summary>玩家按了取消键：这条绑定被【置空】，变成"未绑定"。</summary>
        Cleared,
        /// <summary>超时、程序性取消（面板被应用/返回/关闭）或启动失败：什么都没改。</summary>
        Aborted,
    }

    /// <summary>
    /// 等待按键期间按下就把这一格【置空】的键，全设备一起收。
    ///
    /// ⚠ 这几个路径同时也会被 WithControlsExcluding 排除掉，所以它们永远不会被当成映射采走 ——
    /// 这是"取消逻辑优先"的实现方式：排除判定在候选判定之前，命中即 continue。
    ///
    /// ESC 与手柄的 B/圆圈在 Input System 的默认布局里本来就带着 "Back"/"Cancel" usage
    /// （Keyboard.escape 与 Gamepad.buttonEast 的 usages 都是 {"Back","Cancel"}），
    /// 而 buttonEast 的别名正是 {"b","circle"} —— 也就是 Xbox 的 B 与 PS 的圆圈本来就是同一个控制，
    /// 不需要为两个平台各写一条。
    /// 剩下的三种没有 usage，只能按显式路径列出来：Delete、Backspace（键盘的"返回键"）、
    /// 以及手柄的 select（Xbox 的 View / PS 的 Share，Unity 没给它任何 usage）。
    /// </summary>
    private static readonly string[] CancelKeyPaths =
    {
        "<Keyboard>/escape",
        "<Keyboard>/delete",
        "<Keyboard>/backspace",
        "<Gamepad>/buttonEast",
        "<Gamepad>/select",
    };

    /// <summary>等待按键的超时。不设超时的话玩家误点后界面会一直卡在"请按键…"。</summary>
    private const float RebindTimeoutSeconds = 5f;

    private bool isStart = false;

    private InputActionAsset _asset;
    private InputActionRebindingExtensions.RebindingOperation _operation;

    /// <summary>本次重绑定开始前的路径，用于冲突时与对方互换。</summary>
    private string _rebindPreviousPath;
    private string _rebindActionName;
    private int _rebindBindingIndex;

    /// <summary>本次重绑定编辑的是哪一组（键盘 / 手柄），只用于日志显示。</summary>
    private BindingKind _rebindKind;

    /// <summary>本次重绑定的收尾回调。存成字段是因为取消键是在 Tick 里发现的，
    /// 那条路径不经过 RebindingOperation 的回调，拿不到当初传进来的那个委托。</summary>
    private Action<RebindResult> _rebindOnFinished;

    /// <summary>上一次 Tick 时取消键是不是按着的，用来把"电平"自己转成"边沿"（见 IsCancelKeyEdge）。</summary>
    private bool _cancelKeyWasDown;

    public InputManager()
    {
        MonoManager.Instance.AddUpdateListener(Tick);
    }

    /// <summary>是否正在等待玩家按键。等待期间按键行会被置灰，避免叠出多个重绑定。</summary>
    public bool IsRebinding { get { return _operation != null; } }

    public static BindingKind KindOf(InputDeviceType device)
    {
        return device == InputDeviceType.Keyboard ? BindingKind.Keyboard : BindingKind.Gamepad;
    }

    /// <summary>把绑定组还原成设备枚举，只为了在日志里把键名翻成对应平台的显示名（PS 还是 Xbox）。</summary>
    private static InputDeviceType KindToDevice(BindingKind kind)
    {
        //鼠标格只在日志里用到这里，而鼠标键名与平台主题无关（DescribePath 不看 device），给哪个都行
        return kind == BindingKind.Gamepad ? InputDeviceType.PlayStation : InputDeviceType.Keyboard;
    }

    // ══════════════════ 初始化 ══════════════════

    /// <summary>加载动作表。重复调用安全。由 Entry 在启动时调用。</summary>
    public void Init()
    {
        if (_asset != null) return;

        InputActionAsset source = ResManager.Instance.Load<InputActionAsset>(GlobalPath.res_InputActionsPath);
        if (source == null)
        {
            ChaosLog.Error(LogChannel.Input,
                "找不到输入配置 Assets/Resources/" + GlobalPath.res_InputActionsPath +
                ".inputactions，按键功能整体不可用（面板仍可打开，但改键会失败）");
            return;
        }

        //必须克隆：Resources.Load 拿到的是【磁盘资产本身】，在编辑器里对它做绑定覆盖
        //会真的写进 .inputactions 文件（关掉编辑器依然在），等于玩家改一次键就把工程默认绑定改掉了。
        //克隆件的改动只活在这次运行里，落盘由 SettingsData.inputOverridesJson 负责。
        _asset = UnityEngine.Object.Instantiate(source);

        _asset.Enable();
        ChaosLog.Success(LogChannel.Input, "输入动作表已加载并启用：" + MapName);
    }

    private void EnsureInited()
    {
        if (_asset == null) Init();
    }

    // ══════════════════ 绑定覆盖的存取 ══════════════════

    /// <summary>把当前全部绑定覆盖导成 JSON 串（交给 SettingsData 保存）。</summary>
    public string SaveOverridesJson()
    {
        EnsureInited();
        if (_asset == null) return "";
        return _asset.SaveBindingOverridesAsJson();
    }

    /// <summary>
    /// 用 JSON 串替换当前全部绑定覆盖。传空串等于恢复全部默认绑定。
    /// 先清空再加载：否则旧覆盖里那些新串没提到的绑定会残留下来。
    /// </summary>
    public void LoadOverridesJson(string json)
    {
        EnsureInited();
        if (_asset == null) return;

        _asset.RemoveAllBindingOverrides();

        if (string.IsNullOrEmpty(json)) return;

        try
        {
            _asset.LoadBindingOverridesFromJson(json);
        }
        catch (Exception e)
        {
            //存档里的绑定串可能来自旧版本包（动作名/绑定数变了），解析失败就退回默认绑定，
            //不能让一条脏数据把玩家的按键功能整个搞死
            ChaosLog.Warn(LogChannel.Input, "按键绑定覆盖串解析失败，已回到默认绑定：" + e.Message);
            _asset.RemoveAllBindingOverrides();
        }
    }

    /// <summary>清掉全部绑定覆盖（底部"恢复默认"落在按键分类时用）。</summary>
    public void ClearAllOverrides()
    {
        EnsureInited();
        if (_asset == null) return;
        _asset.RemoveAllBindingOverrides();
    }

    /// <summary>
    /// 清掉【某一格所属的那一组】的全部绑定覆盖，别的组不动。
    /// 按键绑定二级界面的"恢复当前方案默认"就是两次调用：键鼠 = Keyboard + Mouse，手柄 = Gamepad。
    ///
    /// 之所以按组清而不是按动作清：玩家要的是"把这一套恢复成出厂"，逐个动作清一遍容易漏掉加了新动作的那天。
    /// </summary>
    public void ClearOverridesFor(BindingKind kind)
    {
        EnsureInited();
        if (_asset == null) return;

        string group = GroupName(kind);
        int cleared = 0;

        foreach (InputActionMap map in _asset.actionMaps)
        {
            foreach (InputAction action in map.actions)
            {
                //倒序遍历没有意义（RemoveBindingOverride 不改集合结构），正序即可，
                //但复合绑定的 parts 必须跟着头一起清 —— 复用 RemoveOverrideWithParts 就是为了这点
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    InputBinding binding = action.bindings[i];
                    if (binding.isPartOfComposite) continue;//部分归属复合头，跟着头处理
                    if (!HasGroup(binding, group)) continue;

                    RemoveOverrideWithParts(action, i);
                    cleared++;
                }
            }
        }

        ChaosLog.Info(LogChannel.Input, "已恢复 " + group + " 组的按键默认（" + cleared + " 条绑定）");
    }

    /// <summary>把某个动作在当前设备组下的主绑定恢复默认。</summary>
    public bool ResetBinding(string actionName, BindingKind kind)
    {
        InputAction action = FindAction(actionName);
        if (action == null) return false;

        int index = FindBindingIndex(action, kind);
        if (index < 0)
        {
            ChaosLog.Warn(LogChannel.Input, actionName + " 在 " + kind + " 组下没有主绑定，无法恢复默认");
            return false;
        }

        RemoveOverrideWithParts(action, index);
        return true;
    }

    // ══════════════════ 显示 ══════════════════

    /// <summary>
    /// 取某个动作在当前设备下应显示的键名。
    /// 复合绑定会把各部分的键拼起来（移动显示成 "A / D"）。
    /// </summary>
    public string GetBindingDisplay(string actionName, BindingKind kind, InputDeviceType device)
    {
        InputAction action = FindAction(actionName);
        if (action == null) return "-";

        int index = FindBindingIndex(action, kind);
        if (index < 0) return "未绑定";

        if (action.bindings[index].isComposite)
        {
            List<string> parts = new List<string>();
            bool anyBound = false;

            for (int i = index + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
            {
                string partPath = action.bindings[i].effectivePath;
                if (!string.IsNullOrEmpty(partPath)) anyBound = true;
                parts.Add(DescribePath(partPath, device));
            }

            //整条复合都被置空（玩家在采集过程中按了取消键，见 ApplyEmptyOverride）时，
            //逐段拼会显示成"未绑定 / 未绑定"，读起来像是两个坏掉的格子。这里收成一句
            if (parts.Count > 0 && !anyBound) return "未绑定";

            return parts.Count > 0 ? string.Join(" / ", parts.ToArray()) : "-";
        }

        return DescribePath(action.bindings[index].effectivePath, device);
    }

    /// <summary>把一条控制路径翻成给玩家看的名字。手柄那部分按设备主题换列（PS 的 ✕○□△ 或 Xbox 的 A/B/X/Y）。</summary>
    public static string DescribePath(string path, InputDeviceType device)
    {
        //空路径是 Input System 认可的"这一格没绑定"（它会被直接禁用、不报错），
        //界面上就显示成"未绑定" —— 玩家点一下那一格就能绑上
        if (string.IsNullOrEmpty(path)) return "未绑定";

        if (path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase)) return GamepadName(path, device);
        if (path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase)) return MouseName(path);
        if (path.StartsWith("<Pointer>", StringComparison.OrdinalIgnoreCase)) return MouseName(path);

        return KeyboardKeyName(LastSegment(path));
    }

    /// <summary>
    /// 手柄控制路径的显示名。
    /// ⚠ 这里取的是【设备之后的一段】（leftStick/x），不是最后一个斜杠之后的一段（x）——
    /// 摇杆与方向键是"两段式"路径，只取末段的话玩家在移动键那一行上会看到一个孤零零的 "x"。
    /// </summary>
    private static string GamepadName(string path, InputDeviceType device)
    {
        string rel = RelativePath(path);

        if (rel.StartsWith("leftStick", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("rightStick", StringComparison.OrdinalIgnoreCase))
        {
            string stick = rel.StartsWith("leftStick", StringComparison.OrdinalIgnoreCase) ? "左摇杆" : "右摇杆";
            //二维摇杆的两个轴要分开说清：移动只用左右轴，说成"左摇杆"玩家会以为上下也能走
            if (rel.EndsWith("/x", StringComparison.OrdinalIgnoreCase)) return stick + " 左右";
            if (rel.EndsWith("/y", StringComparison.OrdinalIgnoreCase)) return stick + " 上下";
            return stick;
        }

        if (rel.StartsWith("dpad", StringComparison.OrdinalIgnoreCase))
        {
            if (rel.EndsWith("/up", StringComparison.OrdinalIgnoreCase)) return "方向键上";
            if (rel.EndsWith("/down", StringComparison.OrdinalIgnoreCase)) return "方向键下";
            if (rel.EndsWith("/left", StringComparison.OrdinalIgnoreCase)) return "方向键左";
            if (rel.EndsWith("/right", StringComparison.OrdinalIgnoreCase)) return "方向键右";
            return "方向键";
        }

        //剩下的都是单个按键（buttonSouth / leftShoulder / leftTrigger …），查映射表换显示列
        return SettingsLabels.GamepadButtonName(LastSegment(rel), device);
    }

    private static string MouseName(string path)
    {
        switch (LastSegment(path))
        {
            case "leftButton": return "鼠标左键";
            case "rightButton": return "鼠标右键";
            case "middleButton": return "鼠标中键";
            case "position": return "鼠标位置";
            case "delta": return "鼠标移动";
            case "scroll": return "滚轮";
            default: return "鼠标" + LastSegment(path);
        }
    }

    /// <summary>去掉 &lt;设备&gt; 前缀，留下设备内那一段路径（如 "leftStick/x"）。</summary>
    private static string RelativePath(string path)
    {
        int sep = path.IndexOf('>');
        if (sep < 0 || sep + 1 >= path.Length) return path;

        //设备名后面紧跟的那个斜杠要一起去掉：从 '>' 后一位切出来的是 "/leftStick/x"，
        //带头的斜杠会让 StartsWith("leftStick") 判false，于是又退回"只看最后一段"的老毛病
        int start = sep + 1;
        if (path[start] == '/') start++;
        return path.Substring(start);
    }

    /// <summary>路径的最后一段（如 "leftStick/x" 的 "x"、"&lt;Keyboard&gt;/a" 的 "a"）。</summary>
    private static string LastSegment(string path)
    {
        int slash = path.LastIndexOf('/');
        if (slash < 0 || slash + 1 >= path.Length) return path;
        return path.Substring(slash + 1);
    }

    private static string KeyboardKeyName(string control)
    {
        if (string.IsNullOrEmpty(control)) return "-";
        if (control.Length == 1) return control.ToUpperInvariant();

        switch (control)
        {
            case "space": return "空格";
            case "enter": return "回车";
            case "escape": return "Esc";
            case "tab": return "Tab";
            case "backspace": return "退格";
            case "leftShift": return "左Shift";
            case "rightShift": return "右Shift";
            case "leftCtrl": return "左Ctrl";
            case "rightCtrl": return "右Ctrl";
            case "leftAlt": return "左Alt";
            case "rightAlt": return "右Alt";
            case "upArrow": return "方向键上";
            case "downArrow": return "方向键下";
            case "leftArrow": return "方向键左";
            case "rightArrow": return "方向键右";
            default:
                //键名是 camelCase 的英文标识（如 leftBracket），原样显示比硬编一堆中文表更可靠
                return control;
        }
    }

    // ══════════════════ 重绑定 ══════════════════

    /// <summary>
    /// 列出某个动作在当前设备下要依次采集的段。
    /// 单条绑定是一段；复合绑定（移动的 1DAxis）会拆成"左移/右移"两段 ——
    /// 一次按键只能得到一个方向，不拆开采集就永远配不出左右移动。
    /// </summary>
    public List<RebindStep> GetRebindSteps(string actionName, BindingKind kind)
    {
        List<RebindStep> steps = new List<RebindStep>();

        InputAction action = FindAction(actionName);
        if (action == null) return steps;

        int index = FindBindingIndex(action, kind);
        if (index < 0)
        {
            ChaosLog.Warn(LogChannel.Input, actionName + " 在 " + kind + " 组下没有可重绑定的主绑定");
            return steps;
        }

        if (!action.bindings[index].isComposite)
        {
            steps.Add(new RebindStep { Label = null, BindingIndex = index });
            return steps;
        }

        for (int i = index + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
        {
            steps.Add(new RebindStep
            {
                Label = PartLabel(action.bindings[i].name),
                BindingIndex = i
            });
        }
        return steps;
    }

    /// <summary>复合绑定各部分的提示词。目前只有移动用到复合绑定。</summary>
    private static string PartLabel(string partName)
    {
        switch (partName)
        {
            case "negative": return "左移";
            case "positive": return "右移";
            case "up": return "上";
            case "down": return "下";
            case "left": return "左";
            case "right": return "右";
            default: return partName;
        }
    }

    /// <summary>
    /// 开始一次交互式重绑定。结束后回调 onFinished(result)。
    ///
    /// 三种收尾：采到键 = Completed；按了取消键 = Cleared（这条绑定被置空成"未绑定"）；
    /// 超时、面板被应用/返回/关闭、或期间又发起了另一次重绑定 = Aborted（什么都不改）。
    ///
    /// ⚠ 取消键走的是 Tick 里的轮询，不是 RebindingOperation 的 OnCancel —— 两者语义不同：
    /// OnCancel 还包着"程序性取消"，那种情况下玩家并没有表达"我要清空这一格"的意思，
    /// 只是面板被关掉了，置空会把他原来的键也一起抹掉。
    /// 区分开才不会出现"点了返回，键位反而没了"。
    /// </summary>
    public void BeginRebind(string actionName, int bindingIndex, BindingKind kind, Action<RebindResult> onFinished)
    {
        EnsureInited();

        if (_asset == null)
        {
            if (onFinished != null) onFinished(RebindResult.Aborted);
            return;
        }

        //上一次还没收尾就取消掉：同时挂两个 Operation 会互相抢设备输入，
        //而且哪个先回调是不确定的，回调会交叉触发
        CancelCurrentOperation();

        InputAction action = FindAction(actionName);
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            if (onFinished != null) onFinished(RebindResult.Aborted);
            return;
        }

        _rebindActionName = actionName;
        _rebindBindingIndex = bindingIndex;
        _rebindKind = kind;
        _rebindPreviousPath = action.bindings[bindingIndex].effectivePath;
        _rebindOnFinished = onFinished;

        //把取消键的"上次电平"对齐到此刻。否则玩家开重绑定之前就按着一个取消键
        //（比如手柄搁在腿上压着 B），起手第一帧就会被算成"刚刚按下"，一开就清空。
        //这里播种之后，只有在此之后【新按下】的才算取消。
        _cancelKeyWasDown = IsAnyCancelKeyDown();

        //⚠ 必须先把这条动作关掉。Input System 不允许对【启用中】的动作做重绑定 ——
        //  PerformInteractiveRebinding 的 WithAction 会直接抛
        //  InvalidOperationException: Cannot rebind action '...' while it is enabled。
        //  抛出来会顺着调用方（页面的协程）飞出去，协程当场死掉、"请按键…"再也退不回来。
        //  收尾时在 FinishRebind 里恢复启用。
        //
        //副作用是等待按键期间这一条动作不响应（改攻击键时点鼠标不会攻击）——
        //正好是想要的：玩家正在菜单里做改键这件事本身。
        action.Disable();

        try
        {
            StartOperation(action, actionName, bindingIndex, kind, onFinished);
        }
        catch (Exception e)
        {
            //配置阶段出错时绝不能把动作留在禁用状态：那条键会一直到本次运行结束都不响应，
            //而且界面上完全看不出来（按键格还是原来的显示）
            action.Enable();
            CancelCurrentOperation();
            ChaosLog.Error(LogChannel.Input, "启动按键重绑定失败（" + actionName + "）：" + e.Message);
            if (onFinished != null) onFinished(RebindResult.Aborted);
        }
    }

    /// <summary>
    /// 装配并启动重绑定操作。单独拆出来是为了让 BeginRebind 能用一个 try 把它整个罩住 ——
    /// 这一串链式调用里任何一步抛异常，都必须回到 BeginRebind 里把动作恢复启用。
    /// </summary>
    private void StartOperation(InputAction action, string actionName, int bindingIndex,
        BindingKind kind, Action<RebindResult> onFinished)
    {
        InputActionRebindingExtensions.RebindingOperation operation = action.PerformInteractiveRebinding(bindingIndex)
            //鼠标的移动/滚轮本身就是"控制"，不排除的话玩家一动鼠标就被当成按键采走
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            //键盘上有一个合成的兜底控制 "anyKey"（任何一个键按下它都会动）。
            //不排除的话，玩家按下的键一旦【没被别的规则排除掉、却在别处被我们排除掉】，
            //候选里就只剩它，于是那一格被绑成 <Keyboard>/anyKey —— 之后随便按哪个键都会触发这个动作。
            //实测就是这样踩到的：按 Esc 取消时那一格显示成了 "anyKey"（Esc 本身被排除，anyKey 没被排除）
            .WithControlsExcluding("<Keyboard>/anyKey")
            .WithTimeout(RebindTimeoutSeconds)
            //同一个键按下时会有多次 actuation，等一下再收尾可以让"按住不放"只记一次
            .OnMatchWaitForAnother(0.1f)
            //⚠ 必须把我们自己的取消逻辑设为唯一的那条，所以这里清空 Operation 自带的取消键。
            //PerformInteractiveRebinding 会照着 expectedControlType 偷偷塞一条：
            //    if (rebind.expectedControlType != "Button")
            //        rebind.WithCancelingThrough("<Keyboard>/escape");
            //（InputActionRebindingExtensions.cs:2757）
            //—— 注意那个条件，它意味着【只有 Button 类动作】拿不到这条默认取消键。
            //于是 ESC 的语义会随动作类型分裂：Attack 的 expectedControlType 是 Button，没有默认取消键，
            //ESC 走我们的 Tick → 置空；Move 是 Axis，被 Operation 抢先在 OnEvent 里 OnCancel →
            //Aborted，按 ESC 这一格毫无变化。同一个按键两种行为，正是"整条绑定置空"在组合键上
            //从来看不到效果的原因（排掉它之前，组合键的 ESC 永远走的是超时那条路）。
            //传空串是有意的：OnEvent 里那句判定是 !string.IsNullOrEmpty(m_CancelBinding)，
            //空串正好让它认为"没有取消键"，而这些键的"不会被采成映射"由上面的 WithControlsExcluding 负责，
            //"按下要置空"由 Tick 的 IsCancelKeyEdge 负责 —— 两件事都在我们手里，不再有一半被 Operation 抢走。
            .WithCancelingThrough(string.Empty);

        //取消键不能被采成映射 —— 这一条用排除来表达，而不是 WithCancelingThrough：
        //后者只存得下【一条】路径（RebindingOperation 里就是一个 m_CancelBinding 字段），
        //而取消键有一组；排除是按控制逐条判定的，正好能表达一组。
        //（上面那次 WithCancelingThrough 只为把 Operation 自带的默认取消键清空，不是用来登记取消键的。）
        //判定顺序也对我们有利 —— OnEvent 里排除判定在候选判定之前，命中即 continue，
        //所以"取消键优先"是排除机制本身保证的，不依赖下面 Tick 的轮询跑得多快。
        for (int i = 0; i < CancelKeyPaths.Length; i++)
        {
            operation = operation.WithControlsExcluding(CancelKeyPaths[i]);
        }

        //按格子限制可采集的控制：每一格只收自己那类设备。
        //不限制的话，给键盘格按个手柄键也会被接受，之后那一格上就躺着一条手柄路径 ——
        //看着是键盘键、按手柄才触发。三个分支是对称的：排除掉"不是本格设备"的那两类。
        switch (kind)
        {
            case BindingKind.Keyboard:
                operation = operation.WithControlsExcluding("<Gamepad>/*")
                                     .WithControlsExcluding("<Mouse>/*");
                break;

            case BindingKind.Mouse:
                //鼠标格：只收鼠标键。鼠标的移动与滚轮在上面已经排除，
                //所以这里能采到的就是左/右/中/侧键
                operation = operation.WithControlsExcluding("<Keyboard>/*")
                                     .WithControlsExcluding("<Gamepad>/*");
                break;

            default://Gamepad
                operation = operation.WithControlsExcluding("<Keyboard>/*")
                                     .WithControlsExcluding("<Mouse>/*");
                break;
        }

        _operation = operation;
        _operation.OnComplete(op => FinishRebind(RebindResult.Completed, onFinished));
        //OnCancel 只代表"程序性取消"（超时、面板被应用/返回/关闭时调的 CancelRebind）。
        //玩家按取消键那条路径不经过这里 —— 它在 Tick 里被识别，走 RebindResult.Cleared
        _operation.OnCancel(op => FinishRebind(RebindResult.Aborted, onFinished));
        _operation.Start();
    }

    private void FinishRebind(RebindResult result, Action<RebindResult> onFinished)
    {
        //必须 Dispose：RebindingOperation 是 IDisposable，不释放会继续占着设备输入回调。
        //先摘掉它再做别的：下面无论是写覆盖还是走回调，都不该再被它的回调打扰
        //（Dispose() 不会触发 OnCancel，所以这里不会自己把自己绕回来）
        if (_operation != null)
        {
            _operation.Dispose();
            _operation = null;
        }

        _rebindOnFinished = null;

        //恢复重绑定期间被临时关掉的那条动作（见 BeginRebind 里的 action.Disable）。
        //放在最前面：不管成功还是取消、也不管下面会不会出意外，动作都不能留在禁用状态
        InputAction action = FindAction(_rebindActionName);
        if (action != null && !action.enabled) action.Enable();

        if (result == RebindResult.Completed)
        {
            if (action != null)
            {
                ResolveConflict(action, _rebindBindingIndex, _rebindPreviousPath);
                ChaosLog.Info(LogChannel.Input,
                    "按键已重绑：" + _rebindActionName + "(" + _rebindKind + ")" +
                    " -> " + DescribePath(action.bindings[_rebindBindingIndex].effectivePath,
                                          KindToDevice(_rebindKind)));
            }
        }
        else if (result == RebindResult.Cleared)
        {
            if (action != null)
            {
                ApplyEmptyOverride(action, _rebindBindingIndex);
                ChaosLog.Info(LogChannel.Input,
                    "按键已置空（玩家按下取消键）：" + _rebindActionName + "(" + _rebindKind + ")");
            }
        }
        else
        {
            //超时与程序性取消都走这里。超时是静默的（没有任何异常），所以必须自己留一条日志，
            //否则"点了没反应"会被当成 UI 的问题去查
            ChaosLog.Info(LogChannel.Input, "按键重绑定结束（取消或超时）：" + _rebindActionName);
        }

        if (onFinished != null) onFinished(result);
    }

    /// <summary>
    /// 把一条绑定【置空】，也就是界面上显示的"未绑定"。
    ///
    /// ⚠ 与 RemoveOverrideWithParts 的区别（名字很像，效果相反）：
    ///   RemoveOverrideWithParts → 删掉覆盖，回落到 asset 里的默认键（恢复默认用）
    ///   ApplyEmptyOverride      → 连默认键也不要，覆盖值写成【空字符串】
    /// effectivePath 是 `overridePath ?? path`，空字符串不是 null，所以结果是空路径；
    /// InputBindingResolver 里"Disabled if path is empty"会把这条绑定整个跳过，等于没绑。
    /// 也正因为空字符串不是 null，InputBinding.hasOverrides 为真，它能被写进 settings.json
    /// （FromBinding 存的是 `overridePath ?? "null"`，回读时 `!= "null"` 就还原成空覆盖），
    /// 重启之后仍然是未绑定 —— 用 null 就丢了，会悄悄弹回默认键。
    /// </summary>
    private static void ApplyEmptyOverride(InputAction action, int bindingIndex)
    {
        if (action == null) return;

        int root = BindingRootIndex(action, bindingIndex);

        if (action.bindings[root].isComposite)
        {
            //复合绑定只置空【各部分】，复合头留着：头的 path 是复合类型名（"1DAxis"）而不是控制路径，
            //把它也覆盖成空串会让 NameAndParameters.Parse 去解析空串。各部分都空时这条复合本来
            //就什么也不产生，效果一样，还绕开了那个边界情况
            for (int i = root + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
            {
                action.ApplyBindingOverride(i, string.Empty);
            }
            return;
        }

        action.ApplyBindingOverride(root, string.Empty);
    }

    /// <summary>
    /// 从"要采集的那一段"回退到它所属的那一整条绑定。
    /// 单条绑定就是它自己；复合绑定回退到复合头 —— GetRebindSteps 给的是【各部分】的序号，
    /// 置空时只清当前这一段会留下"左移没绑、右移还绑着"的残局。
    /// </summary>
    private static int BindingRootIndex(InputAction action, int bindingIndex)
    {
        int root = bindingIndex;
        while (root > 0 && action.bindings[root].isPartOfComposite) root--;
        return root;
    }

    /// <summary>
    /// 取消正在进行的那一次交互式重绑定。没有在跑就返回 false。
    ///
    /// 面板级的「应用」「恢复默认」「返回」在"等玩家按键"期间被点到时必须调它：
    /// 不取消的话那次重绑仍在采集设备输入，玩家按下的下一个键会被写进去 ——
    /// 而他以为自己已经提交或离开了。
    /// </summary>
    public bool CancelRebind()
    {
        if (_operation == null) return false;
        CancelCurrentOperation();
        return true;
    }

    /// <summary>
    /// 收掉当前操作。走 Cancel() 而不是 Dispose() 是关键：
    /// Cancel() 会触发 OnCancel 回调，发起方（页面）才收得到"结束了"并把界面解锁；
    /// Dispose() 是静默的 —— 那条路径上页面会永远停在"请按键…"的锁里，退出等待的收尾也不会跑。
    /// </summary>
    private void CancelCurrentOperation()
    {
        if (_operation == null) return;

        InputActionRebindingExtensions.RebindingOperation op = _operation;
        op.Cancel();

        //Cancel() 只在已 Start 时才会回调（RebindingOperation.Cancel 里有 started 判断）。
        //正常路径上不会走到这里（BeginRebind 里 Start 紧跟赋值，中间没有可以让出控制权的点），
        //但留着这一手：真没启动过的话，上面那句等于没执行，必须自己释放，
        //否则它继续挂着设备输入回调，下一次重绑会和它抢输入。
        if (_operation == op)
        {
            op.Dispose();
            _operation = null;
        }
    }

    /// <summary>
    /// 冲突处理：新键已被【其它动作】占用时，把对方换成这条绑定原来的键（互换）。
    /// 不处理的话会出现"跳跃和攻击都绑在 K"，按一下两个动作一起触发，
    /// 而按键页两行都显示 K，玩家看不出哪里错了。
    /// 同一个动作内部的重复不管：同一个动作绑两次同一个键没有副作用。
    /// </summary>
    private static void ResolveConflict(InputAction action, int bindingIndex, string previousPath)
    {
        InputActionMap map = action.actionMap;
        if (map == null || string.IsNullOrEmpty(previousPath)) return;

        string newPath = action.bindings[bindingIndex].effectivePath;
        if (string.IsNullOrEmpty(newPath) || newPath == previousPath) return;

        for (int a = 0; a < map.actions.Count; a++)
        {
            InputAction other = map.actions[a];
            if (other == action) continue;

            for (int i = 0; i < other.bindings.Count; i++)
            {
                InputBinding binding = other.bindings[i];
                if (binding.isComposite) continue;//复合头自己不带控制路径
                if (binding.effectivePath != newPath) continue;

                if (binding.path == previousPath)
                {
                    //对方的默认键正好就是我们要还回去的键：直接清掉覆盖，
                    //别留一条"覆盖值等于默认值"的冗余记录（它会一直躺在 settings.json 里）
                    InputActionRebindingExtensions.RemoveBindingOverride(other, i);
                }
                else
                {
                    other.ApplyBindingOverride(i, previousPath);
                }

                ChaosLog.Info(LogChannel.Input,
                    "按键冲突：" + newPath + " 原本属于 " + other.name + "，已把 " + other.name +
                    " 换成 " + previousPath + "（互换）");
            }
        }
    }

    // ══════════════════ 查找工具 ══════════════════

    private InputAction FindAction(string actionName)
    {
        EnsureInited();
        if (_asset == null) return null;

        InputActionMap map = _asset.FindActionMap(MapName, false);
        if (map == null)
        {
            ChaosLog.Error(LogChannel.Input, "输入动作表里没有 '" + MapName + "' 这个 ActionMap");
            return null;
        }

        InputAction action = map.FindAction(actionName, false);
        if (action == null)
        {
            ChaosLog.Error(LogChannel.Input, "'" + MapName + "' 里没有 '" + actionName + "' 这个动作");
        }
        return action;
    }

    /// <summary>
    /// 找某个动作在指定设备组下的主绑定序号，找不到返回 -1。
    /// 复合绑定返回的是【复合头】的序号，它的各部分依次跟在后面（isPartOfComposite == true）。
    /// </summary>
    private static int FindBindingIndex(InputAction action, BindingKind kind)
    {
        string group = GroupName(kind);

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isPartOfComposite) continue;//部分不是"一条绑定"，它归属复合头
            if (!HasGroup(binding, group)) continue;
            return i;
        }
        return -1;
    }

    /// <summary>
    /// 按分号切分后逐项比对组名。
    /// ⚠ 不能用 Contains：组名 "KeyboardFixed" 里含有 "Keyboard"，
    /// Contains 会把固定备用键也当成主绑定，重绑定就会改错对象。
    /// </summary>
    private static bool HasGroup(InputBinding binding, string group)
    {
        if (string.IsNullOrEmpty(binding.groups)) return false;

        string[] groups = binding.groups.Split(';');
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i].Trim() == group) return true;
        }
        return false;
    }

    /// <summary>
    /// 清掉一条绑定的覆盖；若是复合绑定，连它的各部分一起清。
    /// 只清复合头会留下 parts 上的覆盖，症状是"点了恢复默认但键还是改过的"。
    /// </summary>
    private static void RemoveOverrideWithParts(InputAction action, int index)
    {
        InputActionRebindingExtensions.RemoveBindingOverride(action, index);

        if (!action.bindings[index].isComposite) return;

        for (int i = index + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
        {
            InputActionRebindingExtensions.RemoveBindingOverride(action, i);
        }
    }

    // ══════════════════ 旧版轮询（保留，见类注释）══════════════════

    /// <summary>是否启用按键监听。</summary>
    public void StartTickOrNot(bool openFlag)
    {
        isStart = openFlag;
    }

    private void Tick()
    {
        //⚠ 这一段必须在下面的 isStart 判断【之前】：
        //  那个开关管的是旧版 Escape/W 轮询，跟"正在等玩家按键"是两回事。
        //  放到后面的话，没开旧版轮询的场合按取消键会毫无反应，界面一直卡在"请按键…"
        if (_operation != null && IsCancelKeyEdge())
        {
            FinishRebind(RebindResult.Cleared, _rebindOnFinished);
        }

        if (!isStart)
            return;

        CheckKeyCode(KeyCode.Escape);
        CheckKeyCode(KeyCode.W);
    }

    /// <summary>
    /// 本次 Tick 里有没有【新按下】取消键（见 CancelKeyPaths）。只在"正在等玩家按键"期间被调用。
    ///
    /// ⚠ 这里刻意不用 KeyControl.wasPressedThisFrame。它的定义是
    ///     device.wasUpdatedThisFrame &amp;&amp; IsValueConsideredPressed(value)
    ///                              &amp;&amp; !IsValueConsideredPressed(ReadValueFromPreviousFrame())
    /// （ButtonControl.cs:121），第一项要求"这一帧输入系统恰好更新过这个设备"。
    /// 也就是说边沿的判定被交给了输入系统自己的帧记账，而输入更新与 MonoManager 的 Update
    /// 回调谁先谁后不由我们决定 —— 只要输入更新排在本回调之后，按下那一帧我们看到的是旧状态，
    /// 等到下一帧 wasUpdatedThisFrame 又已经归假，这个边沿就永远观测不到，界面会一直卡在"请按键…"
    /// 直到 5 秒超时（超时是 Aborted，不会置空）。实测踩到的正是这个。
    ///
    /// 换成电平自己记：只需要 isPressed 是准的（它是控制当前值的直接读取，与帧记账无关），
    /// 边沿由我们跟上一帧比出来，跟输入系统的调度顺序无关，也不会因为两帧并成一帧而漏掉。
    ///
    /// 为什么是逐帧来问，而不是交给 RebindingOperation：那些键已经被 WithControlsExcluding
    /// 排除了，Operation 那边根本看不见它们，也就收不到任何"玩家按了什么"的信号。
    /// 排除保证它们不会被绑进去，这里保证按下它们能收尾 —— 两件事分开做。
    /// </summary>
    private bool IsCancelKeyEdge()
    {
        bool down = IsAnyCancelKeyDown();
        bool edge = down && !_cancelKeyWasDown;
        _cancelKeyWasDown = down;
        return edge;
    }

    /// <summary>
    /// 此刻有没有任何一个取消键是按下的（电平）。
    ///
    /// 手柄遍历 Gamepad.all 而不是 Gamepad.current：current 是"最后用过的那个设备"，
    /// 而玩家按下 B 的那一帧它很可能还停在键鼠上，用 current 会漏掉。
    /// </summary>
    private static bool IsAnyCancelKeyDown()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.escapeKey.isPressed) return true;
            if (keyboard.deleteKey.isPressed) return true;
            if (keyboard.backspaceKey.isPressed) return true;
        }

        var gamepads = Gamepad.all;
        for (int i = 0; i < gamepads.Count; i++)
        {
            Gamepad pad = gamepads[i];
            if (pad.buttonEast.isPressed) return true;   //Xbox B / PS 圆圈
            if (pad.selectButton.isPressed) return true; //Xbox View / PS Share
        }

        return false;
    }

    /// <summary>根据对应按键触发事件。</summary>
    private void CheckKeyCode(KeyCode kc)
    {
        if (Input.GetKeyDown(kc))
        {
            this.TriggerEvent(EventConstName.GetKeyDown, new InputArgs
            {
                keyCodeValue = kc
            });
        }
        if (Input.GetKeyUp(kc))
        {
            this.TriggerEvent(EventConstName.GetKeyUp, new InputArgs
            {
                keyCodeValue = kc
            });
        }
    }
}
