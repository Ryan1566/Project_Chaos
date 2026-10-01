using UnityEngine;

namespace LocalizationSystem
{
/// <summary>
/// 设置界面里"运行时才知道该显示哪一档"的本地化 Key 表。
///
/// ══════════════ 为什么需要这么一张表 ══════════════
/// 选择器行的档位文案现在【预摆在预制体里】（见 SettingPanelBuilder.SelectorOptionsRow），
/// 每个档位节点各挂一个 LocalizedText，语言切换由组件自己完成 —— 界面层完全不碰翻译。
///
/// 但界面还有一件事必须知道：切档时"没有 Key 的档位该退回显示预制体里的中文原文"。
/// 所以每个档位的 Key 要有一份运行时可见的清单，与预制体里的档位节点【同序】。
/// 这就是本类的全部职责 —— 除了这个清单，没有任何文案。
///
/// ══════════════ 谁负责把 Key 写进配置 ══════════════
/// 这里只声明"哪些 Key 应该存在"，不保证"配置里真的有"。配置条目由
/// 「Tools/本地化/面板文字收集器」扫描后建立（它扫到的就是这些预制体节点）。
/// 配置里缺某一条时不会报错：界面回退显示预制体里的中文原文（见 SettingRow_Selector.ShowCurrentOption）。
///
/// ══════════════ 顺序是硬契约 ══════════════
/// 每个数组的顺序必须与对应 settingId 的枚举/档位表完全一致（WindowModeOptions[i] 对应
/// WindowModeType 的第 i 项）。顺序错了的表现是"选中某一档，界面显示隔壁那一档的文字"。
/// </summary>
public static class SettingKeys
{
    /// <summary>索引 = WindowModeType。与生成器里 SettingsLabels.WindowMode 的档位一一对应。</summary>
    public static readonly string[] WindowModeOptions =
    {
        "ui_setting_contentarea_page_graphics_viewport_content_row_windowmode_options_option_0",
        "ui_setting_contentarea_page_graphics_viewport_content_row_windowmode_options_option_1",
        "ui_setting_contentarea_page_graphics_viewport_content_row_windowmode_options_option_2",
    };

    /// <summary>索引 = QualityLevelOption。</summary>
    public static readonly string[] QualityOptions =
    {
        "ui_setting_contentarea_page_graphics_viewport_content_row_quality_options_option_0",
        "ui_setting_contentarea_page_graphics_viewport_content_row_quality_options_option_1",
        "ui_setting_contentarea_page_graphics_viewport_content_row_quality_options_option_2",
    };

    /// <summary>索引 = MouseTriggerMode。</summary>
    public static readonly string[] TriggerModeOptions =
    {
        "ui_setting_contentarea_page_keybind_viewport_content_row_attacktrigger_options_option_0",
        "ui_setting_contentarea_page_keybind_viewport_content_row_attacktrigger_options_option_1",
    };

    /// <summary>
    /// 索引 = LanguageType。与 SettingPanelBuilder.LanguageOptions 的档位一一对应。
    ///
    /// ⚠ 启用新语言时，枚举、生成器的 LanguageOptions、这里三处都要按【同一顺序】补一项。
    /// </summary>
    public static readonly string[] LanguageOptions =
    {
        "ui_setting_contentarea_page_gameplay_viewport_content_row_language_options_option_0",
        "ui_setting_contentarea_page_gameplay_viewport_content_row_language_options_option_1",
    };

    /// <summary>
    /// 按 settingId 取这张表里对应的 Key 清单；没有预摆档位的设置项返回 null。
    ///
    /// 有了它，SettingKeys 就【只有一份】，不会在 SettingsPageBase 或各地页面里被复述。
    /// </summary>
    public static string[] ForSetting(string settingId)
    {
        if (settingId == SettingIds.WindowMode) return WindowModeOptions;
        if (settingId == SettingIds.Quality) return QualityOptions;
        if (settingId == SettingIds.AttackTrigger) return TriggerModeOptions;
        if (settingId == SettingIds.Language) return LanguageOptions;
        return null;
    }
}
}
