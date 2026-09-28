using System.Collections;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 按键设置页：输入设备 / 移动 攻击 跳跃 三个按键 / 鼠标反转与灵敏度 / 攻击触发方式 / 恢复默认按键。
///
/// ══════════════════════ 这一页与其它页最大的不同 ══════════════════════
/// 别的设置项改的只是数据（写在 Pending 里，等点「应用」）；
/// 而按键重绑定必须【立刻写进 InputActionAsset】—— 因为按键按钮上显示的就是资产里当前的键，
/// 不写进去玩家就看不到自己刚按的键，等于改键没有反馈。
/// 于是这里形成了唯一的"数据与运行时短暂不一致"窗口：
///     Pending.inputOverridesJson 记着新键（待应用），InputActionAsset 上也已经是新键。
/// 点「应用」→ 两者一致；点「返回」→ SettingsManager.RevertEdit() 会把资产回滚到已应用的那份。
/// 这个回滚是必需的，不能省（见 SettingsManager.RevertEdit 的注释）。
///
/// ══════════════════════ 移动键为什么要分两步采集 ══════════════════════
/// 移动是一条 1DAxis 复合绑定（A=负方向、D=正方向）。一次按键只能得到一个方向，
/// 所以"改移动键"实际是依次采集两次：先问左移、再问右移，两次都拿到才算改完。
/// 中途取消或超时则整条放弃 —— 只改半个方向会留下"左移是 A、右移还是个奇怪键"的残局，
/// 比不改更糟。提示词由 InputManager.GetRebindSteps 给出。
///
/// ══════════════════════ 编辑的是"当前设备"的那条绑定 ══════════════════════
/// 每个动作在键盘组与手柄组下各有一条可重绑定主绑定（另有打不掉的固定备用键，不参与重绑）。
/// 选"键盘"时改的是键盘那条，选 PS/Xbox 时改的是手柄那条 ——
/// 手柄两档改的是【同一条绑定】，区别只在按键显示名（✕○□△ 还是 A/B/X/Y）。
/// 所以切设备后必须 RefreshAll()，否则界面上显示的还是上一个设备的键名。
/// </summary>
public class KeybindSettingsPage : SettingsPageBase
{
    /// <summary>鼠标灵敏度取值域。需求第 2 项：1 ~ 10 的整数。</summary>
    private const int MinSensitivity = 1;
    private const int MaxSensitivity = 10;

    /// <summary>本页三个按键行，重绑定期间要一起置灰（防止叠出第二个重绑定）。</summary>
    private readonly List<SettingRow_Keybind> _keyRows = new List<SettingRow_Keybind>();

    /// <summary>正在跑的重绑定协程。非空即"正在等待按键"。</summary>
    private Coroutine _rebindRoutine;

    /// <summary>当前一次重绑定里，某一步是否已完成、以及是否成功。</summary>
    private bool _stepFinished;
    private bool _stepSucceeded;

    protected override void OnBind()
    {
        BindDevice();
        BindKeyRow(SettingIds.Move, InputManager.ActionMove);
        BindKeyRow(SettingIds.Attack, InputManager.ActionAttack);
        BindKeyRow(SettingIds.Jump, InputManager.ActionJump);
        BindMouse();
        BindTriggerMode();
        BindButton(SettingIds.ResetAllKeybinds, ResetAllKeybinds);
    }

    /// <summary>
    /// 输入设备。它的 onChanged 只做一件事：整体刷新 —— 换设备等于换"当前在编辑哪条绑定"，
    /// 三个按键行的显示与之后重绑定的目标全都要跟着换。
    /// </summary>
    private void BindDevice()
    {
        BindSelector(SettingIds.Device, SettingsLabels.Device,
            data => data.inputDevice,
            (data, index) => data.inputDevice = index,
            index => RefreshAll());
    }

    private void BindMouse()
    {
        BindToggle(SettingIds.MouseInvertX,
            data => data.mouseInvertX,
            (data, value) => data.mouseInvertX = value);

        BindToggle(SettingIds.MouseInvertY,
            data => data.mouseInvertY,
            (data, value) => data.mouseInvertY = value);

        BindSlider(SettingIds.MouseSensitivity,
            data => data.mouseSensitivity,
            (data, value) => data.mouseSensitivity = value,
            MinSensitivity, MaxSensitivity);
    }

    private void BindTriggerMode()
    {
        BindSelector(SettingIds.AttackTrigger, SettingsLabels.TriggerMode,
            data => data.attackTriggerMode,
            (data, index) => data.attackTriggerMode = index);
    }

    /// <summary>
    /// 一行按键控件。行只抛意图，重绑定与恢复默认都在本页处理。
    /// 回填动作单独注册：键名取决于"当前设备"，不是 Pending 里某个字段的直接映射，
    /// 所以不能用 BindSelector 那套。
    /// </summary>
    private void BindKeyRow(string id, string actionName)
    {
        SettingRow_Keybind row = FindRow<SettingRow_Keybind>(id);
        if (row == null) return;

        row.OnRebindRequested = () => StartRebind(actionName);
        row.OnResetRequested = () => ResetOne(actionName);

        _keyRows.Add(row);
        AddRefresher(data => row.SetKeyText(DisplayOf(actionName, data)));
    }

    private static string DisplayOf(string actionName, SettingsData data)
    {
        return InputManager.Instance.GetBindingDisplay(
            actionName, InputManager.KindOf((InputDeviceType)data.inputDevice), (InputDeviceType)data.inputDevice);
    }

    // ══════════════════ 重绑定 ══════════════════

    private void StartRebind(string actionName)
    {
        if (_rebindRoutine != null || InputManager.Instance.IsRebinding)
        {
            //正常操作路径下点不到这里（等待期间按键行是置灰的），
            //所以走到这说明有别的入口在发重绑定请求，值得留条日志
            ChaosLog.Warn(LogChannel.Input, "已有一次按键重绑定在进行中，忽略对 " + actionName + " 的新请求");
            return;
        }
        _rebindRoutine = StartCoroutine(RebindRoutine(actionName));
    }

    /// <summary>
    /// 依次采集这个动作需要采集的每一段。任一段取消/超时，整条放弃。
    /// </summary>
    private IEnumerator RebindRoutine(string actionName)
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null)
        {
            _rebindRoutine = null;
            yield break;
        }

        InputDeviceType device = (InputDeviceType)data.inputDevice;
        InputManager.BindingKind kind = InputManager.KindOf(device);

        List<InputManager.RebindStep> steps = InputManager.Instance.GetRebindSteps(actionName, kind);
        if (steps.Count == 0)
        {
            ChaosLog.Warn(LogChannel.Input,
                actionName + " 在当前设备下没有可重绑定的绑定（" + kind + "），改键操作被跳过");
            _rebindRoutine = null;
            yield break;
        }

        SettingRow_Keybind row = FindRowFor(actionName);

        //整页按键行置灰：等待按键期间玩家还能点到别的按键按钮的话，
        //会在第一次重绑定还没收尾时发起第二次，两者互相取消，表现为"点了没反应"
        SetKeyRowsInteractable(false);

        bool allSucceeded = true;

        for (int i = 0; i < steps.Count; i++)
        {
            InputManager.RebindStep step = steps[i];

            if (row != null)
            {
                row.SetListening(true, string.IsNullOrEmpty(step.Label)
                    ? null
                    : step.Label + "：请按键…");
            }

            _stepFinished = false;
            _stepSucceeded = false;

            InputManager.Instance.BeginRebind(actionName, step.BindingIndex, kind, OnStepFinished);

            //等这一步收尾（成功、取消或超时都会回调，所以这里不会永久卡住）
            while (!_stepFinished) yield return null;

            if (row != null) row.SetListening(false);

            if (!_stepSucceeded)
            {
                allSucceeded = false;
                break;
            }
        }

        SetKeyRowsInteractable(true);

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

    private void OnStepFinished(bool success)
    {
        _stepSucceeded = success;
        _stepFinished = true;
    }

    private SettingRow_Keybind FindRowFor(string actionName)
    {
        string id;
        if (actionName == InputManager.ActionMove) id = SettingIds.Move;
        else if (actionName == InputManager.ActionAttack) id = SettingIds.Attack;
        else if (actionName == InputManager.ActionJump) id = SettingIds.Jump;
        else return null;

        for (int i = 0; i < _keyRows.Count; i++)
        {
            if (_keyRows[i].settingId == id) return _keyRows[i];
        }
        return null;
    }

    private void SetKeyRowsInteractable(bool interactable)
    {
        for (int i = 0; i < _keyRows.Count; i++)
        {
            _keyRows[i].SetInteractable(interactable);
        }
    }

    // ══════════════════ 恢复默认 ══════════════════

    /// <summary>恢复单个按键的默认。只动这一条，不动玩家改过的其它键。</summary>
    private void ResetOne(string actionName)
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null) return;

        InputManager.BindingKind kind = InputManager.KindOf((InputDeviceType)data.inputDevice);
        if (!InputManager.Instance.ResetBinding(actionName, kind)) return;

        SyncOverridesToPending();
        RefreshAll();
    }

    /// <summary>恢复全部按键默认（本页的按钮，不等同于底部"恢复默认" —— 那只管当前分类）。</summary>
    private void ResetAllKeybinds()
    {
        InputManager.Instance.ClearAllOverrides();
        SyncOverridesToPending();
        RefreshAll();
        ChaosLog.Info(LogChannel.Input, "已恢复全部按键默认（待应用）");
    }

    /// <summary>把 InputActionAsset 当前的绑定覆盖同步进暂存区。</summary>
    private static void SyncOverridesToPending()
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null) return;

        data.inputOverridesJson = InputManager.Instance.SaveOverridesJson();
        SettingsManager.Instance.NotifyPendingChanged();
    }
}
