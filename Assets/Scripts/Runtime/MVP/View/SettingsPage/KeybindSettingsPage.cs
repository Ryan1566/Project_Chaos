using ChaosDebug;
using UnityEngine;

/// <summary>
/// 按键设置页（一级界面）：进入"按键绑定"二级界面的入口 + 鼠标专用项 + 攻击触发方式。
///
/// ══════════ 为什么按键绑定要拆到二级界面 ══════════
/// 改键是"低频、且需要专注"的操作：等待按键期间整页的按键行都要置灰防重入。
/// 而设备选择（键鼠 / 手柄）与改键其实是一件事的两面（选哪一套 ↔ 改哪一套），
/// 把它们和鼠标反转、灵敏度这些"顺手一拨"的开关堆在同一页，
/// 玩家看不出"切设备 = 换一个编辑对象"，改键时还得在一堆无关项里找自己那一行。
/// 拆出去之后这一页只剩三个随手可改的东西，二级界面里则能专心摆下两套按键行。
///
/// ══════════ 鼠标专用项为什么要跟着方案置灰 ══════════
/// 水平反转 / 垂直反转 / 灵敏度 / 攻击触发方式（MouseTriggerMode）都只有鼠标（指向）才有意义，
/// 手柄方案下改它们没有任何作用 —— 这些字段目前也还没有消费方，只在界面与存档之间往返。
/// 选择"置灰保留可见"而不是隐藏：玩家得先看见这些项存在，
/// 才知道"切回键鼠就能改"，隐藏掉只会让人以为设置项没了。
///
/// ══════════ 本页只处理"当前是哪一套方案" ══════════
/// 页签（键鼠 / 手柄）与手柄型号都在二级界面里，改动写的是 SettingsData.inputDevice 与 gamepadModel；
/// 本页的置灰刷新读的是同一个 inputDevice，因此两边天然一致，不需要额外的同步机制。
/// </summary>
public class KeybindSettingsPage : SettingsPageBase
{
    /// <summary>二级界面在本页同级里的节点名。必须与 SettingPanelBuilder 生成的节点名一致。</summary>
    public const string BindingsPageName = "Page_Keybind_Bindings";

    /// <summary>鼠标灵敏度取值域：1 ~ 10 的整数。</summary>
    private const int MinSensitivity = 1;
    private const int MaxSensitivity = 10;

    /// <summary>键鼠方案的二级界面。找不到时为 null（FindSubPage 已经报过错）。</summary>
    private SettingsPageBase _bindingsPage;

    protected override void OnBind()
    {
        //子页的"< 返回"由本页负责收回来：子页不该知道自己在谁的下面
        _bindingsPage = FindSubPage(BindingsPageName);
        if (_bindingsPage != null) _bindingsPage.OnBackRequested = CloseSubPage;

        BindButton(SettingIds.OpenBindings, OpenBindingsPage);

        SettingRow_Toggle invertX = BindToggle(SettingIds.MouseInvertX,
            data => data.mouseInvertX,
            (data, value) => data.mouseInvertX = value);

        SettingRow_Toggle invertY = BindToggle(SettingIds.MouseInvertY,
            data => data.mouseInvertY,
            (data, value) => data.mouseInvertY = value);

        SettingRow_Slider sensitivity = BindSlider(SettingIds.MouseSensitivity,
            data => data.mouseSensitivity,
            (data, value) => data.mouseSensitivity = value,
            MinSensitivity, MaxSensitivity);

        SettingRow_Selector trigger = BindSelector(SettingIds.AttackTrigger, SettingsLabels.TriggerMode,
            data => data.attackTriggerMode,
            (data, index) => data.attackTriggerMode = index);

        //上面四项都是鼠标专用。手柄方案下整行置灰 ——
        //注意刷新动作要放在最后注册：它会读 Bind* 拿到的行引用，行长什么样得先确定下来
        AddRefresher(data =>
        {
            bool usable = !InputScheme.IsGamepad(data.inputDevice);

            if (invertX != null) invertX.SetInteractable(usable);
            if (invertY != null) invertY.SetInteractable(usable);
            if (sensitivity != null) sensitivity.SetInteractable(usable);
            if (trigger != null) trigger.SetInteractable(usable);
        });
    }

    private void OpenBindingsPage()
    {
        if (_bindingsPage == null)
        {
            ChaosLog.Warn(LogChannel.UI,
                "按键绑定二级界面不可用（找不到 " + BindingsPageName + "），点「更改按键绑定」不会有反应");
            return;
        }

        ShowSubPage(_bindingsPage);
    }
}
