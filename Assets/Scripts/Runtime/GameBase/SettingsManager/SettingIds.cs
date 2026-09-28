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

    // ══════════════════ 按键页 ══════════════════
    /// <summary>输入设备（键盘 / PS / Xbox）。只切显示主题与"当前在编辑哪条绑定"。</summary>
    public const string Device = "keybind.device";
    public const string Move = "keybind.move";
    public const string Attack = "keybind.attack";
    public const string Jump = "keybind.jump";
    public const string MouseInvertX = "keybind.mouseInvertX";
    public const string MouseInvertY = "keybind.mouseInvertY";
    public const string MouseSensitivity = "keybind.mouseSensitivity";
    public const string AttackTrigger = "keybind.attackTrigger";
    /// <summary>把全部按键绑定恢复成默认（不等同于底部"恢复默认"，那只管当前分类）。</summary>
    public const string ResetAllKeybinds = "keybind.resetAll";
}
