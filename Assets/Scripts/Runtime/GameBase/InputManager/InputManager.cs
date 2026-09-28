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
/// 每个动作在 "Keyboard" / "Gamepad" 两组下【各只有一条主绑定】，它就是按键界面上可重绑的那一条：
///     Move   : Keyboard = 1DAxis 复合（A/D），Gamepad = &lt;Gamepad&gt;/leftStick/x
///     Attack : Keyboard = J，                  Gamepad = &lt;Gamepad&gt;/buttonWest
///     Jump   : Keyboard = K，                  Gamepad = &lt;Gamepad&gt;/buttonSouth
/// 另有 "KeyboardFixed" 组的固定备用键（方向键、鼠标左键、空格），它们【不参与重绑定】，
/// 只作为打不掉的兜底。切"输入设备"时改的只是"当前在编辑哪一组"，不是两套资产。
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
    public const string GroupGamepad = "Gamepad";

    /// <summary>玩家当前在改键盘还是手柄的绑定。</summary>
    public enum BindingKind
    {
        Keyboard = 0,
        Gamepad = 1,
    }

    /// <summary>一次重绑定要采集的一段。复合绑定（移动）会被拆成多段依次采集。</summary>
    public struct RebindStep
    {
        /// <summary>提示词，如"左移"；单段绑定为 null。</summary>
        public string Label;
        /// <summary>要写入覆盖的绑定序号。</summary>
        public int BindingIndex;
    }

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
        return kind == BindingKind.Keyboard ? InputDeviceType.Keyboard : InputDeviceType.PlayStation;
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

    /// <summary>清掉全部绑定覆盖（"恢复所有按键默认"用）。</summary>
    public void ClearAllOverrides()
    {
        EnsureInited();
        if (_asset == null) return;
        _asset.RemoveAllBindingOverrides();
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
            for (int i = index + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
            {
                parts.Add(DescribePath(action.bindings[i].effectivePath, device));
            }
            return parts.Count > 0 ? string.Join(" / ", parts.ToArray()) : "-";
        }

        return DescribePath(action.bindings[index].effectivePath, device);
    }

    /// <summary>把一条控制路径翻成给玩家看的名字。手柄那部分按设备主题换列（PS 的 ✕○□△ 或 Xbox 的 A/B/X/Y）。</summary>
    public static string DescribePath(string path, InputDeviceType device)
    {
        if (string.IsNullOrEmpty(path)) return "-";

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
    /// 开始一次交互式重绑定。结束后回调 onFinished(success)。
    /// 超时、按 Esc、或期间又发起了另一次重绑定，都会以 success = false 收尾。
    /// </summary>
    public void BeginRebind(string actionName, int bindingIndex, BindingKind kind, Action<bool> onFinished)
    {
        EnsureInited();

        if (_asset == null)
        {
            if (onFinished != null) onFinished(false);
            return;
        }

        //上一次还没收尾就取消掉：同时挂两个 Operation 会互相抢设备输入，
        //而且哪个先回调是不确定的，回调会交叉触发
        CancelCurrentOperation();

        InputAction action = FindAction(actionName);
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            if (onFinished != null) onFinished(false);
            return;
        }

        _rebindActionName = actionName;
        _rebindBindingIndex = bindingIndex;
        _rebindKind = kind;
        _rebindPreviousPath = action.bindings[bindingIndex].effectivePath;

        InputActionRebindingExtensions.RebindingOperation operation = action.PerformInteractiveRebinding(bindingIndex)
            //鼠标的移动/滚轮本身就是"控制"，不排除的话玩家一动鼠标就被当成按键采走
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            //Esc 用来取消。不设的话按 Esc 会被当成一次按键绑定进去，玩家就再也退不出来了
            .WithCancelingThrough("<Keyboard>/escape")
            .WithTimeout(RebindTimeoutSeconds)
            //同一个键按下时会有多次 actuation，等一下再收尾可以让"按住不放"只记一次
            .OnMatchWaitForAnother(0.1f);

        //按设备组限制可采集的控制：不限制的话，给键盘绑定按个手柄键也会被接受，
        //之后"键盘"这一行上就躺着一条手柄路径，看着是键盘键、按手柄才触发
        if (kind == BindingKind.Keyboard)
        {
            operation = operation.WithControlsExcluding("<Gamepad>/*");
        }
        else
        {
            operation = operation.WithControlsExcluding("<Keyboard>/*")
                                 .WithControlsExcluding("<Mouse>/*");
        }

        _operation = operation;
        _operation.OnComplete(op => FinishRebind(true, onFinished));
        _operation.OnCancel(op => FinishRebind(false, onFinished));
        _operation.Start();
    }

    private void FinishRebind(bool success, Action<bool> onFinished)
    {
        //必须 Dispose：RebindingOperation 是 IDisposable，不释放会继续占着设备输入回调
        if (_operation != null)
        {
            _operation.Dispose();
            _operation = null;
        }

        if (success)
        {
            InputAction action = FindAction(_rebindActionName);
            if (action != null)
            {
                ResolveConflict(action, _rebindBindingIndex, _rebindPreviousPath);
                ChaosLog.Info(LogChannel.Input,
                    "按键已重绑：" + _rebindActionName + "(" + _rebindKind + ")" +
                    " -> " + DescribePath(action.bindings[_rebindBindingIndex].effectivePath,
                                          KindToDevice(_rebindKind)));
            }
        }
        else
        {
            //超时与取消都走这里。超时是静默的（没有任何异常），所以必须自己留一条日志，
            //否则"点了没反应"会被当成 UI 的问题去查
            ChaosLog.Info(LogChannel.Input, "按键重绑定结束（取消或超时）：" + _rebindActionName);
        }

        if (onFinished != null) onFinished(success);
    }

    private void CancelCurrentOperation()
    {
        if (_operation == null) return;
        _operation.Dispose();
        _operation = null;
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
        string group = (kind == BindingKind.Keyboard) ? GroupKeyboard : GroupGamepad;

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
        if (!isStart)
            return;

        CheckKeyCode(KeyCode.Escape);
        CheckKeyCode(KeyCode.W);
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
