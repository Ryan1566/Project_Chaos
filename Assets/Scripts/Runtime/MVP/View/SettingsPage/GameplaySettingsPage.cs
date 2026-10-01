using ChaosDebug;
using LocalizationSystem;

/// <summary>
/// 游戏性设置页。
///
/// 目前只有一项：语言切换。它落在这一页是因为语言属于「游戏性」分类——
/// 与 SettingIds.Language 的前缀 "gameplay." 和 SettingsData 的「游戏性」字段分区一致。
///
/// ══════════ 后续候选（来自设计文档 4.1）══════════
/// 按对玩家的价值排序：屏幕震动强度、伤害数字开关、辅助瞄准、难度、教程提示、快速重开。
///
/// 加新项的做法：在 prefab 的本页 Content 下摆一行（用 Assets/Prefabs/UI/SettingRows/ 里的模板），
/// 填好 settingId（记得在 SettingIds 里加常量），再在下面的 OnBind 里加一行 Bind*。
/// 注意 SettingPanel.prefab 由 Assets/Editor/SettingPanelBuilder.cs 生成，
/// 手工摆的行会在下次执行「Tools/UI/生成设置面板」时被覆盖 —— 正解是同时改生成器。
/// </summary>
public class GameplaySettingsPage : SettingsPageBase
{
    protected override void OnBind()
    {
        //语言：档位就是 LanguageType 的序号，值直接存 SettingsData.language。
        //选项文案来自 LocalizationManager，所以以后启用新语言只改枚举，界面自动跟上。
        BindSelector(SettingIds.Language, GetLanguageOptions(),
            data => data.language,
            (data, index) => data.language = index,
            index => ChaosLog.Info(LogChannel.UI,
                "语言已改为：" + LocalizationManager.GetLanguageDisplayName((LanguageType)index)));
    }

    /// <summary>
    /// 语言下拉的档位文案。
    ///
    /// 用 GetAllLanguages() 而不是硬编码列表：它只返回枚举里【启用】的语言
    /// （注释掉的语言不会出现），所以启用新语言时这里不用改。
    /// 但是记得要改SettingPanel预制体，这块是写死了的
    /// 顺序与 LanguageType 的枚举值一致，所以下标 == 枚举值 == SettingsData.language。
    /// </summary>
    private static string[] GetLanguageOptions()
    {
        LanguageType[] all = LocalizationManager.GetAllLanguages();
        string[] names = new string[all.Length];
        for (int i = 0; i < all.Length; i++)
        {
            names[i] = LocalizationManager.GetLanguageDisplayName(all[i]);
        }
        return names;
    }
}
