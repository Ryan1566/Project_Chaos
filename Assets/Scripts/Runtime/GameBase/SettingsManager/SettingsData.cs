using System;
using UnityEngine;

/// <summary>
/// 设置数据的纯数据载体 —— 只放字段，不放逻辑，方便 JsonUtility 直接序列化。
///
/// ══════════ 为什么全是 public 字段而不是属性 ══════════
/// JsonUtility 只序列化【public 字段】和 [SerializeField] 私有字段，属性一概不认。
/// 用字段可以省掉一大堆 [SerializeField]，也让 ToJson / FromJson 的往返最省心。
///
/// ══════════ 增字段是安全的，不用写迁移 ══════════
/// JsonUtility.FromJson 先在默认构造出来的实例上覆盖 JSON 里出现的字段，
/// 老存档里没有的字段会【保留字段初始值】。所以加新设置项时只要在这里写个默认值即可，
/// 老玩家读到新字段拿到的是默认值，不会变成 0 / null。
///
/// 只有【改动已有字段的含义或取值域】时才需要动 version 并写迁移。
///
/// 枚举一律用 int 存：JsonUtility 对枚举的支持不完整，且 int 在文案调整时更稳。
/// </summary>
[Serializable]
public class SettingsData
{
    /// <summary>数据结构版本。字段语义发生不兼容变化时 +1，并在 SettingsManager 里补迁移分支。</summary>
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    // ══════════════════ 画面 ══════════════════

    /// <summary>分辨率宽。初始值 1920×1080，但首次运行会被自动检测的结果覆盖（见 SettingsManager.Load）。</summary>
    public int resolutionWidth = 1920;
    /// <summary>分辨率高。</summary>
    public int resolutionHeight = 1080;
    /// <summary>WindowModeType。默认无边框全屏 —— 对单机游戏来说 Alt+Tab 体验最好。</summary>
    public int windowMode = (int)WindowModeType.BorderlessFullScreen;
    /// <summary>帧率上限，直接是 fps 数值（30/40/60）。</summary>
    public int frameRate = 60;
    /// <summary>垂直同步。注意：开启时 Unity 会忽略 targetFrameRate，由显示器刷新率决定实际上限。</summary>
    public bool vSync = false;
    /// <summary>QualityLevelOption。</summary>
    public int qualityLevel = (int)QualityLevelOption.High;

    // ══════════════════ 声音 ══════════════════
    // 四路音量统一 0~100 整数（UI 上是拉条，步进 1）。

    public int masterVolume = 100;
    public int musicVolume = 80;
    public int sfxVolume = 80;
    public int uiVolume = 80;

    // ══════════════════ 按键 ══════════════════

    /// <summary>InputDeviceType，只影响显示与"当前编辑哪条绑定"。</summary>
    public int inputDevice = (int)InputDeviceType.Keyboard;
    /// <summary>鼠标水平反转。⚠ 工程内还没有瞄准系统，本项目前【存了但没人读】。</summary>
    public bool mouseInvertX = false;
    /// <summary>鼠标垂直反转。同上，暂无人消费。</summary>
    public bool mouseInvertY = false;
    /// <summary>鼠标灵敏度，整数 1~10。实际倍率由消费者映射（见文档 待决策项 #2）。</summary>
    public int mouseSensitivity = 5;
    /// <summary>MouseTriggerMode。</summary>
    public int attackTriggerMode = (int)MouseTriggerMode.Press;
    /// <summary>
    /// InputActionAsset 的绑定覆盖串（SaveBindingOverridesAsJson 的产物）。
    /// 存成字符串而不是结构化数据：这套格式由 Input System 自己维护，
    /// 我们只负责原样保存、原样喂回去，跟着包版本升级也不会烂。
    /// 空串 = 全部使用默认绑定。
    /// </summary>
    public string inputOverridesJson = "";

    /// <summary>全默认值的一份实例。所有默认值的唯一来源就是上面的字段初始值。</summary>
    public static SettingsData CreateDefault()
    {
        return new SettingsData();
    }

    /// <summary>
    /// 浅拷贝有风险（string 是不可变的所以其实够用），这里统一走 JSON 往返，
    /// 以后加了数组/嵌套类也不用回来改拷贝逻辑。
    /// </summary>
    public SettingsData Clone()
    {
        return JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(this));
    }

    /// <summary>
    /// 内容比对（用来判断"暂存区和已应用区是否一致"，即"应用"按钮要不要点亮）。
    /// 用 JSON 串比而不是逐字段比：以后加字段不会漏比，代价是每次比较有一次小分配 ——
    /// 这个调用只发生在 UI 交互时，不在逐帧路径上。
    /// </summary>
    public bool ContentEquals(SettingsData other)
    {
        if (other == null) return false;
        return JsonUtility.ToJson(this) == JsonUtility.ToJson(other);
    }

    /// <summary>
    /// 把某个分类的设置项恢复成默认值（只动这一类的字段，其它分类原样保留）。
    /// 默认值从 CreateDefault() 取，避免"两处各写一份默认值然后慢慢跑偏"。
    /// </summary>
    /// <param name="category">要恢复的分类。</param>
    public void ResetSection(SettingCategory category)
    {
        SettingsData d = CreateDefault();
        switch (category)
        {
            case SettingCategory.Graphics:
                resolutionWidth = d.resolutionWidth;
                resolutionHeight = d.resolutionHeight;
                windowMode = d.windowMode;
                frameRate = d.frameRate;
                vSync = d.vSync;
                qualityLevel = d.qualityLevel;
                break;

            case SettingCategory.Audio:
                masterVolume = d.masterVolume;
                musicVolume = d.musicVolume;
                sfxVolume = d.sfxVolume;
                uiVolume = d.uiVolume;
                break;

            case SettingCategory.Keybind:
                inputDevice = d.inputDevice;
                mouseInvertX = d.mouseInvertX;
                mouseInvertY = d.mouseInvertY;
                mouseSensitivity = d.mouseSensitivity;
                attackTriggerMode = d.attackTriggerMode;
                inputOverridesJson = "";//空串 = 回到全部默认绑定
                break;

            case SettingCategory.Gameplay:
                // 游戏性页目前是空的（需求里明确"后续根据游戏需求添加"），
                // 这里留一个空分支而不是 default: 抛异常 —— 加了新分类而忘了补分支时，
                // 应该是"恢复默认没起作用"，而不是让玩家点一下就崩。
                break;
        }
    }
}
