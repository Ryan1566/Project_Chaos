using UnityEngine;

/// <summary>
/// 设置面板用到的全部枚举与显示名。
///
/// ══════════ 硬约束：成员顺序即 UI 显示顺序，数值会被写进 settings.json ══════════
/// 这些枚举的 int 值直接持久化到玩家的 settings.json。增删成员时必须【追加在末尾】，
/// 不要插到中间、也不要重排已有成员的数值 —— 否则老存档里的 int 会静默指向另一个选项
/// （典型表现：玩家升级后发现"分辨率"变成"全屏"了，而且没有任何报错）。
/// 万不得已要调整顺序时，必须同步提升 SettingsData.CurrentVersion 并写迁移逻辑。
/// </summary>

/// <summary>视窗模式。与 UnityEngine.FullScreenMode 的对应关系见 SettingsManager.MapWindowMode。</summary>
public enum WindowModeType
{
    /// <summary>无边框全屏：铺满桌面分辨率、没有标题栏，Alt+Tab 切换最快。默认项。</summary>
    BorderlessFullScreen = 0,
    /// <summary>独占全屏：真正切显示器分辨率，延迟最低，但切换时黑屏较久。</summary>
    ExclusiveFullScreen = 1,
    /// <summary>边框窗口：带标题栏，方便调试与多屏。</summary>
    Windowed = 2,
}

/// <summary>帧率上限。数值即实际 fps，选中后直接喂给 Application.targetFrameRate。</summary>
public enum FrameRateOption
{
    Fps30 = 30,
    Fps40 = 40,
    Fps60 = 60,
}

/// <summary>
/// 输入设备偏好。
///
/// ⚠ 这里选的是【显示主题】，不是两套绑定数据 —— Unity 无法可靠区分 PS 与 Xbox 手柄
/// （两者共用同一个 Gamepad 抽象，Gamepad.current.name 不稳定）。
/// 它的实际作用有两个：① 决定手柄按键的显示名用哪一列（✕/○/□/△ 还是 A/B/X/Y）；
/// ② 决定"按键自定义"这一页当前在编辑键盘绑定还是手柄绑定。
/// </summary>
public enum InputDeviceType
{
    Keyboard = 0,
    PlayStation = 1,
    Xbox = 2,
}

/// <summary>
/// 手柄型号。只影响按键显示名用哪一列（✕○□△ 还是 A/B/X/Y），不影响绑定数据。
///
/// 为什么要单独存一份：按键页现在把"键鼠 / 手柄"拆成了两个页签，
/// 切到键鼠再切回手柄时，玩家上次选的 PS/Xbox 必须还在。
/// 它由 InputDeviceType 换算而来（见 InputScheme.ModelToDevice），不是新的一套设备概念。
/// </summary>
public enum GamepadModel
{
    PlayStation = 0,
    Xbox = 1,
}

/// <summary>
/// 「输入方案」的换算中心：按键页的页签（键鼠 / 手柄）与手柄型号按钮
/// 通过它读写 SettingsData 的 inputDevice 与 gamepadModel 两个字段。
///
/// ══════════ 两个字段的分工（唯一真源只有 inputDevice）══════════
/// inputDevice     = 当前方案：Keyboard 表示键鼠；PlayStation / Xbox 表示手柄，且同时是显示主题。
/// gamepadModel    = 上次选的手柄型号，只用来回答"从键鼠切回手柄时该用哪个型号"。
/// 所以页签本身【不存档】：它是 inputDevice 推出来的，没必要再多一个字段去同步。
/// </summary>
public static class InputScheme
{
    /// <summary>当前是不是手柄方案。页签高亮、鼠标专用项的置灰都以它为准。</summary>
    public static bool IsGamepad(int inputDevice)
    {
        return (InputDeviceType)inputDevice != InputDeviceType.Keyboard;
    }

    /// <summary>手柄型号序号 → 设备枚举。越界一律当 Xbox（存档脏数据不该让界面崩）。</summary>
    public static InputDeviceType ModelToDevice(int gamepadModel)
    {
        return (GamepadModel)gamepadModel == GamepadModel.PlayStation
            ? InputDeviceType.PlayStation
            : InputDeviceType.Xbox;
    }

    /// <summary>设备枚举 → 手柄型号序号。只在 inputDevice 已经是手柄时才有意义。</summary>
    public static int DeviceToModel(InputDeviceType device)
    {
        return device == InputDeviceType.PlayStation ? (int)GamepadModel.PlayStation : (int)GamepadModel.Xbox;
    }
}

/// <summary>攻击键的触发方式。</summary>
public enum MouseTriggerMode
{
    /// <summary>点按：按一次触发一次。</summary>
    Press = 0,
    /// <summary>长按：按住持续触发（连发）。</summary>
    Hold = 1,
}

/// <summary>画质等级。只暴露三档，内部映射到工程实际的 QualitySettings 档位。</summary>
public enum QualityLevelOption
{
    Low = 0,
    Medium = 1,
    High = 2,
}

/// <summary>主选列表的四个分类。枚举值同时用作 SettingsPageBase 的页索引，顺序即左侧按钮顺序。</summary>
public enum SettingCategory
{
    Gameplay = 0,
    Keybind = 1,
    Graphics = 2,
    Audio = 3,
}

/// <summary>
/// 枚举的显示名表。索引 = 枚举值，与上面的枚举一一对应、顺序必须一致。
/// 集中在这里是为了让"改文案"只改一个地方，也便于后续接本地化。
/// </summary>
public static class SettingsLabels
{
    /// <summary>主选列表按钮文案，索引 = SettingCategory。</summary>
    public static readonly string[] Category = { "游戏性", "按键", "画面", "声音" };

    /// <summary>索引 = WindowModeType。</summary>
    public static readonly string[] WindowMode = { "无边框全屏", "全屏", "边框窗口" };

    /// <summary>索引 = InputDeviceType。型号按钮的文案用的就是它的 1、2 两项。</summary>
    public static readonly string[] Device = { "键盘", "PS 手柄", "Xbox 手柄" };

    /// <summary>按键页二级界面的两个页签文案。索引：0 = 键鼠，1 = 手柄（不是 InputDeviceType）。</summary>
    public static readonly string[] Scheme = { "键鼠", "手柄" };

    /// <summary>索引 = MouseTriggerMode。</summary>
    public static readonly string[] TriggerMode = { "点按", "长按（连发）" };

    /// <summary>索引 = QualityLevelOption。</summary>
    public static readonly string[] Quality = { "低", "中", "高" };

    /// <summary>按索引取名字，越界时返回空串而不是抛异常 —— 显示层不该因为数据脏就崩掉。</summary>
    public static string Get(string[] table, int index)
    {
        if (table == null || index < 0 || index >= table.Length) return "";
        return table[index];
    }

    public static string CategoryName(int i) { return Get(Category, i); }
    public static string WindowModeName(int i) { return Get(WindowMode, i); }
    public static string DeviceName(int i) { return Get(Device, i); }
    public static string SchemeName(int i) { return Get(Scheme, i); }
    public static string TriggerModeName(int i) { return Get(TriggerMode, i); }
    public static string QualityName(int i) { return Get(Quality, i); }

    /// <summary>
    /// 手柄按键显示名映射。Unity 的 Gamepad 抽象按键名（buttonSouth 等）→ 各平台常见叫法。
    /// PS 与 Xbox 的物理位置一一对应，只是印字不同，所以同一行数据换一列显示即可。
    ///
    /// 本作实际用到的只有 buttonWest（攻击）与 buttonSouth（跳跃），其余为后续加键预留。
    /// </summary>
    public static string GamepadButtonName(string controlName, InputDeviceType device)
    {
        bool ps = device == InputDeviceType.PlayStation;
        switch (controlName)
        {
            case "buttonSouth": return ps ? "✕" : "A";
            case "buttonEast": return ps ? "○" : "B";
            case "buttonWest": return ps ? "□" : "X";
            case "buttonNorth": return ps ? "△" : "Y";
            case "leftShoulder": return ps ? "L1" : "LB";
            case "rightShoulder": return ps ? "R1" : "RB";
            case "leftTrigger": return ps ? "L2" : "LT";
            case "rightTrigger": return ps ? "R2" : "RT";
            case "start": return ps ? "Options" : "Menu";
            case "select": return ps ? "Create" : "View";
            case "leftStick": return ps ? "左摇杆" : "左摇杆";
            case "dpad": return "方向键";
            default: return controlName;
        }
    }
}
