/// <summary>
/// 设置行的标识符常量。
///
/// ══════════ 为什么用字符串 id 而不是直接持有引用 ══════════
/// 本面板是"全静态手工摆放"：每一行都是 prefab 里真实存在的物体，行与页之间没有代码生成步骤，
/// 所以行需要一种"我是谁"的自述方式，页才知道该把这一行的值写进哪个字段。
/// 用常量而不是散落的字面量，是为了让拼错能被 IDE 和这里的集中定义挡住。
///
/// 格式约定：&lt;分类&gt;.&lt;字段名&gt;，与 SettingsData 的字段一一对应，便于对照排查。
/// 每个页在 Awake 里会校验自己子树下所有行的 SettingId 都能被识别，
/// 认不出来的会在 Console 报一条明确的警告（参考 UIManager.ValidatePanelPrefabs 的做法）。
/// </summary>
public static class SettingIds
{
    // ══════════════════ 画面页 ══════════════════
    public const string Resolution = "graphics.resolution";
    public const string WindowMode = "graphics.windowMode";
    public const string FrameRate = "graphics.frameRate";
    public const string VSync = "graphics.vsync";
    public const string Quality = "graphics.quality";

    // ══════════════════ 声音页 ══════════════════
    public const string MasterVolume = "audio.master";
    public const string MusicVolume = "audio.music";
    public const string SfxVolume = "audio.sfx";
    public const string UiVolume = "audio.ui";

    // ══════════════════ 按键页（一级界面）══════════════════
    /// <summary>进入"按键绑定"二级界面的入口按钮。设备选择、改键、按方案重置都在那一页里。</summary>
    public const string OpenBindings = "keybind.openBindings";
    public const string MouseInvertX = "keybind.mouseInvertX";
    public const string MouseInvertY = "keybind.mouseInvertY";
    public const string MouseSensitivity = "keybind.mouseSensitivity";
    /// <summary>攻击触发方式。它属于 MouseTriggerMode，所以和上面三项一样是键鼠专用项。</summary>
    public const string AttackTrigger = "keybind.attackTrigger";

    // ══════════════════ 按键绑定二级界面 ══════════════════
    /// <summary>键鼠方案的三个操作行。每行【两个格子】：左格键盘、右格鼠标，两格可以同时生效。</summary>
    public const string Move = "keybind.move";
    public const string Attack = "keybind.attack";
    public const string Jump = "keybind.jump";

    /// <summary>手柄方案的三个操作行。每行只有一个格子（手柄按键），结构与上面三个不同。</summary>
    public const string MoveGamepad = "keybind.move.pad";
    public const string AttackGamepad = "keybind.attack.pad";
    public const string JumpGamepad = "keybind.jump.pad";

    /// <summary>把【当前页签那一套】的按键绑定恢复成默认：键鼠页签只清键鼠，手柄那套不动。</summary>
    public const string ResetScheme = "keybind.resetScheme";
}
