using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 画面设置页：分辨率 / 视窗模式 / 帧率上限 / 垂直同步 / 画质等级。
///
/// ══════════════════════ 分辨率这一项要单独说 ══════════════════════
/// 它是唯一一个"选项列表在运行时才定"的设置 —— 8 档预设里超出显示器能力的会被裁掉
/// （见 ResolutionHelper）。所以这里保存了一份 _options，选择器的"序号"只是这张表的下标，
/// 真正的分辨率值仍以 SettingsData 里的 width/height 为准。
///
/// 换显示器的情况：存档里的分辨率可能在新显示器上不存在（比如从 2560×1600 换到 1080p）。
/// 绑定时若发现存的值不在可用表里，就把它吸附到最接近的一档并写进暂存区 ——
/// 这样选择器显示的和实际会写入的是同一个值，不会出现"显示 1920×1080、实际是 2560×1600"的错位。
/// 吸附会让暂存区与已应用区产生差异，「应用」按钮随之亮起，玩家点一下即可固化，符合预期。
///
/// ══════════ 编辑器里的已知现象 ══════════
/// Game 视图下改分辨率/视窗模式往往看不到变化（Unity 编辑器会限制这类调用），
/// 打包后才真正生效。这不是 bug，别在编辑器里验收分辨率这一项，要出包验。
/// </summary>
public class GraphicsSettingsPage : SettingsPageBase
{
    /// <summary>帧率档位。数值即 fps，与 FrameRateOption 一一对应。</summary>
    private static readonly int[] FrameRateValues = { 30, 40, 60 };
    /// <summary>
    /// 帧率档位文案。不带"帧"字：纯数字在任何语言下都读得懂，
    /// 带单位反而要为此建三条本地化条目、还得在每种语言里重新拼一遍单位。
    /// </summary>
    private static readonly string[] FrameRateTexts = { "30", "40", "60" };

    /// <summary>当前显示器可用的分辨率档位（按显示器能力过滤过，从低到高）。</summary>
    private List<ResolutionOption> _options;

    protected override void OnBind()
    {
        BindResolution();
        BindWindowMode();
        BindFrameRate();
        BindVSync();
        BindQuality();
        
    }

    private void BindResolution()
    {
        _options = ResolutionHelper.GetAvailableOptions();

        string[] texts = new string[_options.Count];
        for (int i = 0; i < _options.Count; i++) texts[i] = _options[i].DisplayText;

        SnapUnavailableResolution();

        BindSelector(SettingIds.Resolution, texts,
            data => ResolutionHelper.IndexOfClosest(_options, data.resolutionWidth, data.resolutionHeight),
            (data, index) =>
            {
                ResolutionOption option = _options[Mathf.Clamp(index, 0, _options.Count - 1)];
                data.resolutionWidth = option.Width;
                data.resolutionHeight = option.Height;
            });
    }

    /// <summary>存档里的分辨率在当前显示器上不存在时，吸附到最接近的一档。</summary>
    private void SnapUnavailableResolution()
    {
        SettingsData data = SettingsManager.Instance.Pending;
        if (data == null || _options == null || _options.Count == 0) return;

        if (ResolutionHelper.IndexOf(_options, data.resolutionWidth, data.resolutionHeight) >= 0) return;

        ResolutionOption closest = _options[Mathf.Clamp(
            ResolutionHelper.IndexOfClosest(_options, data.resolutionWidth, data.resolutionHeight),
            0, _options.Count - 1)];

        ChaosLog.Info(LogChannel.Config,
            "存档分辨率 " + data.resolutionWidth + "×" + data.resolutionHeight +
            " 在当前显示器上不可用，已改为最接近的 " + closest.DisplayText);

        data.resolutionWidth = closest.Width;
        data.resolutionHeight = closest.Height;
    }

    private void BindWindowMode()
    {
        //预摆档位形态（SettingKeys 里登记了 Key）：档位文案与生成器共用
        //SettingsLabels.WindowMode 这一张表，顺序即 WindowModeType，所以下标可以直接当字段值用
        BindSelector(SettingIds.WindowMode, SettingsLabels.WindowMode,
            data => data.windowMode,
            (data, index) => data.windowMode = index);
    }

    /// <summary>
    /// 帧率设置
    /// </summary>
    private void BindFrameRate()
    {
        BindSelector(SettingIds.FrameRate, FrameRateTexts,
            data => IndexOfValue(FrameRateValues, data.frameRate),
            (data, index) => data.frameRate = FrameRateValues[Mathf.Clamp(index, 0, FrameRateValues.Length - 1)]);
    }

    private void BindVSync()
    {
        BindToggle(SettingIds.VSync,
            data => data.vSync,
            (data, value) => data.vSync = value);
    }

    private void BindQuality()
    {
        BindSelector(SettingIds.Quality, SettingsLabels.Quality,
            data => data.qualityLevel,
            (data, index) => data.qualityLevel = index);
    }
}
