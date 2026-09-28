using ChaosDebug;

/// <summary>
/// 游戏性设置页 —— 按需求【暂为空置】，页面上只放一段"敬请期待"的占位文字。
///
/// 需求原文："游戏性设置暂时空置，后续根据游戏需求添加"。
/// 所以这里刻意不写任何绑定，也不预留半成品控件 ——
/// 空页面比"有几个点了没反应的开关"更诚实。
///
/// ══════════ 推荐优先落在这页的候选（来自设计文档 4.1）══════════
/// 工程里已经有 LocalizationManager，所以"语言"是成本最低的一项，建议第一个加；
/// 其余按对玩家的价值排序：屏幕震动强度、伤害数字开关、辅助瞄准、难度、教程提示、快速重开。
///
/// 加新项的做法：在 prefab 的本页 Content 下摆一行（用 Assets/Prefabs/UI/SettingRows/ 里的模板），
/// 填好 settingId（记得在 SettingIds 里加常量），再在下面的 OnBind 里加一行 Bind*。
/// </summary>
public class GameplaySettingsPage : SettingsPageBase
{
    protected override void OnBind()
    {
        // 故意留空。若这页被摆上了行控件，基类的 ValidateRows 会在 Play 时逐条警告出来，
        // 免得以后有人摆了行却忘了在这里绑定。
        ChaosLog.Info(LogChannel.UI, "游戏性设置页当前为空置状态（按需求预留）");
    }
}
