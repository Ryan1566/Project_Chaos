using System;
using System.IO;
using System.Text;
using ChaosDebug;
using LocalizationSystem;
using UnityEngine;

/// <summary>
/// 设置的唯一权威：读写存档、维护"已应用 / 暂存"两份数据、把设置推给引擎。
///
/// ══════════ 为什么有两份数据（Applied / Pending）══════════
/// 本面板的交互模型是【改完点「应用」才生效】（需求第 4 项拍板：不点应用按钮不生效）。
/// 于是必须有暂存层：
///   · Pending —— UI 上所有改动先写这里，玩家看到的"当前选中值"就是它；
///   · Applied —— 真正已经落盘、且已经推给 Screen / 音频 / 输入 的那一份。
/// 「应用」= Applied = Pending.Clone() → 落盘 → 推给引擎。
/// 「返回」而没点应用 = 丢弃 Pending（按键那部分还要回滚 InputActionAsset，见 RevertEdit）。
///
/// 好处是"改了一半退出"天然安全；代价是每个写入口都必须写 Pending 而不是 Applied ——
/// 页脚本里凡是改设置值的地方，一律认准 SettingsManager.Instance.Pending。
///
/// ══════════ 为什么不用工程里的 Recorder ══════════
/// Recorder 写 Application.dataPath，打包后该目录只读，只有编辑器期能写。
/// 设置必须写在 Application.persistentDataPath 下才能跟着安装目录一起可写。
/// </summary>
public class SettingsManager : SingletonBase<SettingsManager>
{
    /// <summary>暂存值发生变化（用于点亮/熄灭"应用"按钮、刷新行控件显示）。</summary>
    public event Action OnPendingChanged;

    /// <summary>设置已经应用并落盘。参数是刚生效的那份数据。</summary>
    public event Action<SettingsData> OnApplied;

    /// <summary>已应用的设置（= 已落盘 = 已推给引擎）。</summary>
    public SettingsData Applied { get; private set; }

    /// <summary>正在编辑的暂存设置。UI 上的所有改动都写这里。</summary>
    public SettingsData Pending { get; private set; }

    /// <summary>是否处于"面板编辑中"状态（BeginEdit 到 CommitEdit/RevertEdit 之间）。</summary>
    public bool IsEditing { get; private set; }

    private bool _inited;

    /// <summary>存档路径：&lt;persistentDataPath&gt;/Config/settings.json。Windows 下即 %USERPROFILE%\AppData\LocalLow\&lt;公司&gt;\&lt;产品&gt;\Config\settings.json</summary>
    public static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "Config", "settings.json"); }
    }

    /// <summary>暂存区与已应用区是否有差异 —— 决定「应用」按钮是否可点。</summary>
    public bool HasPendingChanges
    {
        get { return Pending != null && Applied != null && !Pending.ContentEquals(Applied); }
    }

    // ══════════════════ 生命周期 ══════════════════

    /// <summary>
    /// 读档 + 首次运行时自动检测显示器 + 把设置推给引擎。由 Entry 在启动时调用。
    /// 重复调用是安全的（只会 Load 一次）。
    /// </summary>
    public void Init()
    {
        if (_inited) return;
        _inited = true;

        Load();
        ApplyToRuntime(Applied);

        this.TriggerEvent(EventConstName.LoadSetting, new LoadingSettingEventArgs
        {
            a_isNewOrOrg = _wasFirstRun //true = 本次是全新档（用的是默认值），false = 读到了玩家存档
        });

        ChaosLog.Success(LogChannel.Config, "设置初始化完成：" + FilePath);
    }

    /// <summary>没被显式 Init 过也能用 —— 面板可能在 Entry 之前就被打开（比如直接在编辑器里跑某个场景）。</summary>
    private void EnsureInited()
    {
        if (!_inited) Init();
    }

    private bool _wasFirstRun;

    // ══════════════════ 读 / 写 ══════════════════

    /// <summary>读存档。文件不存在、损坏、或字段缺失都退回默认值，绝不因为存档问题让游戏起不来。</summary>
    public void Load()
    {
        SettingsData data = null;

        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                if (!string.IsNullOrEmpty(json)) data = JsonUtility.FromJson<SettingsData>(json);
            }
        }
        catch (Exception e)
        {
            //存档坏了不该拦住玩家进游戏：报一条告警，然后当"没有存档"处理
            ChaosLog.Warn(LogChannel.Config, "设置存档读取失败，本次使用默认值：" + e.Message);
            data = null;
        }

        _wasFirstRun = data == null;

        if (data == null)
        {
            data = SettingsData.CreateDefault();
            ApplyFirstRunAutoDetect(data);
        }

        //预留的迁移点：以后 CurrentVersion 提升了，在这里按 data.version 补字段/换单位
        if (data.version != SettingsData.CurrentVersion)
        {
            data.version = SettingsData.CurrentVersion;
        }

        Applied = data;
        Pending = data.Clone();
    }

    /// <summary>
    /// 首次运行：按玩家显示器能力挑一档分辨率，而不是死用默认的 1920×1080。
    /// 需求里"初始化时检测玩家电脑，考虑最高 1K/2K/4K"落在这里。
    /// </summary>
    private static void ApplyFirstRunAutoDetect(SettingsData data)
    {
        ResolutionOption best = ResolutionHelper.GetBestAvailable();
        data.resolutionWidth = best.Width;
        data.resolutionHeight = best.Height;
        ChaosLog.Info(LogChannel.Config, "首次运行，按显示器能力选定分辨率：" + best.DisplayText);
    }

    /// <summary>把已应用的设置落盘。写成 UTF-8 无 BOM，带缩进方便人工查看与排查。</summary>
    public void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(FilePath, JsonUtility.ToJson(Applied, true), new UTF8Encoding(false));
            ChaosLog.Info(LogChannel.Save, "设置已保存：" + FilePath);
        }
        catch (Exception e)
        {
            //写不进去（磁盘满、权限、杀软锁文件）时只告警，不影响本次游戏内的设置生效
            ChaosLog.Error(LogChannel.Save, "设置保存失败：" + e.Message);
        }
    }

    // ══════════════════ 编辑会话 ══════════════════

    /// <summary>面板打开时调用：把已应用数据拷进暂存区，进入编辑态。</summary>
    public void BeginEdit()
    {
        EnsureInited();
        Pending = Applied.Clone();
        IsEditing = true;
        NotifyPendingChanged();
    }

    /// <summary>
    /// 点「应用」时调用：暂存区转正、落盘、推给引擎。
    /// </summary>
    public void CommitEdit()
    {
        EnsureInited();
        if (Pending == null) return;

        Applied = Pending.Clone();
        Save();
        ApplyToRuntime(Applied);
        IsEditing = false;

        if (OnApplied != null) OnApplied(Applied);

        this.TriggerEvent(EventConstName.SaveSetting, new SavingSettingEventArgs
        {
            a_settings = Applied
        });

        NotifyPendingChanged();
        ChaosLog.Success(LogChannel.Config, "设置已应用并生效");
    }

    /// <summary>
    /// 放弃未应用的改动（面板退出时若还有差异，由面板询问玩家后调用）。
    /// 除了丢弃 Pending，还必须把 InputActionAsset 的绑定覆盖回滚到已应用状态 ——
    /// 因为按键重绑定是【直接写进 InputActionAsset】的（不然按键按钮上显示的还是旧键），
    /// 它不像其它设置那样只躺在数据里，不回滚就会"没点应用但键位已经变了"。
    /// </summary>
    public void RevertEdit()
    {
        EnsureInited();
        if (Pending == null || Applied == null) return;

        Pending = Applied.Clone();
        InputManager.Instance.LoadOverridesJson(Applied.inputOverridesJson);
        ApplyAudio(Applied);//音量是"边拖边试听"的，回滚时要把听感也还原

        IsEditing = false;
        NotifyPendingChanged();
        ChaosLog.Info(LogChannel.Config, "已放弃未应用的改动");
    }

    /// <summary>把当前分类的设置项恢复成默认值（只改暂存区，仍需点「应用」才落地）。</summary>
    public void ResetPendingSection(SettingCategory category)
    {
        EnsureInited();
        if (Pending == null) return;

        Pending.ResetSection(category);

        //按键的"恢复默认"要立刻反映到 InputActionAsset 上，
        //否则按键按钮会继续显示玩家自己改过的键，看着像没生效
        if (category == SettingCategory.Keybind)
        {
            InputManager.Instance.ClearAllOverrides();
        }

        NotifyPendingChanged();
        ChaosLog.Info(LogChannel.Config, "已恢复 '" + SettingsLabels.CategoryName((int)category) + "' 的默认值（待应用）");
    }

    /// <summary>暂存值有变动时由页脚本调用，用来刷新「应用」按钮可用态。</summary>
    public void NotifyPendingChanged()
    {
        if (OnPendingChanged != null) OnPendingChanged();
    }

    // ══════════════════ 推给引擎 ══════════════════

    /// <summary>把一份设置推给引擎（分辨率、画质、音量、按键绑定、语言）。</summary>
    public void ApplyToRuntime(SettingsData data)
    {
        if (data == null) return;
        ApplyGraphics(data);
        ApplyAudio(data);
        ApplyInput(data);
        ApplyLocalization(data);
    }

    /// <summary>
    /// 把语言推给 LocalizationManager。
    ///
    /// 这里【刻意判空】，与相邻的 ApplyAudio / ApplyInput 直接取单例的风格不同：
    /// 本地化是可选的系统，场景里没有 LocalizationManager 时不该阻断其它设置生效，
    /// 只报一条 WARN 让问题看得见。
    /// </summary>
    private static void ApplyLocalization(SettingsData data)
    {
        LocalizationManager mgr = LocalizationManager.GetInstance();
        if (mgr == null)
        {
            ChaosLog.Warn(LogChannel.Localization,
                "设置里的语言未生效：场景里没有 LocalizationManager（language=" + data.language + "）");
            return;
        }

        mgr.ChangeLanguage((LanguageType)data.language);
    }

    private static void ApplyGraphics(SettingsData data)
    {
        //垂直同步与帧率上限是互斥的：vSyncCount > 0 时 Unity 会忽略 targetFrameRate。
        //所以开了垂直同步就交给显示器刷新率决定，把 targetFrameRate 设回 -1（无上限）；
        //否则才用玩家选的 30/40/60。不这样处理的话，"选了 60 帧但实际跑 144" 会被当成 bug 报上来。
        QualitySettings.vSyncCount = data.vSync ? 1 : 0;
        Application.targetFrameRate = data.vSync ? -1 : data.frameRate;

        QualitySettings.SetQualityLevel(MapQualityLevel(data.qualityLevel), true);

        FullScreenMode mode = MapWindowMode((WindowModeType)data.windowMode);
        RefreshRate rr = Screen.currentResolution.refreshRateRatio;
        float hz = (float)Screen.currentResolution.refreshRateRatio.numerator / Screen.currentResolution.refreshRateRatio.denominator;
        if (hz <= 0)
        {
            rr = new RefreshRate { numerator = (uint)data.frameRate, denominator = 1 };
        }
        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, mode, rr);

        ChaosLog.Info(LogChannel.Config,
            "画面已应用：" + data.resolutionWidth + "×" + data.resolutionHeight +
            " / " + SettingsLabels.WindowModeName(data.windowMode) +
            " / " + (data.vSync ? "垂直同步开" : data.frameRate + " 帧上限") +
            " / 画质 " + SettingsLabels.QualityName(data.qualityLevel));
    }

    private void ApplyAudio(SettingsData data)
    {
        AudioManager.Instance.SetVolume(AudioChannel.Master, data.masterVolume);
        AudioManager.Instance.SetVolume(AudioChannel.Music, data.musicVolume);
        AudioManager.Instance.SetVolume(AudioChannel.Sfx, data.sfxVolume);
        AudioManager.Instance.SetVolume(AudioChannel.UI, data.uiVolume);
    }

    private static void ApplyInput(SettingsData data)
    {
        InputManager.Instance.LoadOverridesJson(data.inputOverridesJson);
    }

    /// <summary>WindowModeType → Unity 的 FullScreenMode。三者的语义差异见枚举注释。</summary>
    public static FullScreenMode MapWindowMode(WindowModeType mode)
    {
        switch (mode)
        {
            case WindowModeType.ExclusiveFullScreen: return FullScreenMode.ExclusiveFullScreen;
            case WindowModeType.Windowed: return FullScreenMode.Windowed;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    /// <summary>
    /// 三档画质 → 工程 QualitySettings 里实际的档位索引。
    /// 工程默认有 6 档（Very Low ~ Ultra），这里取首档、中间档、末档，
    /// 而不是写死 0/1/2 —— 以后美术调整了画质档数量，这里不用跟着改。
    /// </summary>
    public static int MapQualityLevel(int option)
    {
        string[] names = QualitySettings.names;
        int count = (names != null) ? names.Length : 1;
        if (count <= 1) return 0;

        switch ((QualityLevelOption)option)
        {
            case QualityLevelOption.Low: return 0;
            case QualityLevelOption.Medium: return (count - 1) / 2;
            default: return count - 1;
        }
    }
}
