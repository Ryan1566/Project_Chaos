using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

//该脚本为游戏内各脚本会使用到的全局路径，方便归档查询以及修改
//也可以选择用readonly替换掉const，根据需求决定
public static class GlobalPath
{
    #region 资源类路径
    /// <summary>
    /// 测试资源路径
    /// </summary>
    [Header("测试资源路径")]
    public const string res_TestPath = "Test/";

    /// <summary>
    /// 音乐测试资源路径
    /// </summary>
    [Header("音乐测试资源路径")]
    public const string res_TestMusicPath = "Audio/TestMusic/";

    /// <summary>
    /// 音乐资源路径
    /// </summary>
    [Header("音乐资源路径")]
    public const string res_MusicPath = "Audio/Music/";

    /// <summary>
    /// 音效资源路径
    /// </summary>
    [Header("音效资源路径")]
    public const string res_SoundPath = "Audio/Sound/";

    /// <summary>
    /// UI面板资源路径
    /// </summary>
    [Header("音效资源路径")]
    public const string res_PanelPath = "UIPanels/";

    /// <summary>
    /// 输入动作表资源路径
    /// 注意这里【没有结尾斜杠】：它是要被 ResManager.Load 直接当完整路径用的，
    /// 拼成 "Input/ChaosInputActions" 才能加载到 Assets/Resources/Input/ChaosInputActions.inputactions。
    /// </summary>
    [Header("输入资源路径")]
    public const string res_InputActionsPath = "Input/ChaosInputActions";

    /// <summary>
    /// 按键图标映射表资源路径。
    /// 和输入动作表一样【没有结尾斜杠】：它是要被 Resources.Load 直接当完整路径用的，
    /// 拼成 "Data/Input/KeyIconMap" 才能加载到 Assets/Resources/Data/Input/KeyIconMap.asset。
    ///
    /// 【注意】必须放在 Resources 下：LanguageFontMap 就是因为在 Assets/Data 下，
    /// 运行时根本查不到、只能靠组件在 Inspector 上持有引用。而图标映射表的使用方是
    /// InputManager（它没有组件可以挂引用），所以只有"放进 Resources"这一条路可走。
    /// </summary>
    [Header("输入资源路径")]
    public const string res_KeyIconMapPath = "Data/Input/KeyIconMap";
    #endregion

    #region UI 预制体搜索路径
    /// <summary>
    /// 收集面板 TMP 文字时默认扫描的目录。
    ///
    /// 与上面的 res_ 路径的区别：这里是【工程内资产目录】（Assets/ 开头的完整路径），
    /// 给编辑器工具用（AssetDatabase.FindAssets），不是 Resources.Load 的相对路径。
    /// 所以用 ui_ 前缀单独分组，别和 res_ 混用。
    ///
    /// 运行时不读这个数组 —— 只有面板文字收集器用它填默认值。
    /// 加新面板目录时往这里追加即可，工具会自动带上。
    /// </summary>
    public static readonly string[] ui_PanelPrefabSearchPaths =
    {
        "Assets/Resources/UIPanels",
    };
    #endregion

    /// <summary>
    /// 本地化配置资产的默认清单，第一个是【默认写入目标】。
    ///
    /// ══════════════ 为什么要有这个常量 ══════════════
    /// 收集器窗口的目标配置原本只靠 EditorPrefs 记住，第一次打开是空的；
    /// 而配置资产一旦改名或拆分（本项目就从单表拆成了主菜单 / 设置两张），
    /// EditorPrefs 里那条旧路径就成了死路径 —— 窗口打开时目标为空，却没有任何提示。
    /// 把清单放在这里，改名时改一处，所有本地化工具自动跟上。
    ///
    /// 运行时不读它 —— 运行时读的是场景里 LocalizationManager.localizationData。
    /// </summary>
    public static readonly string[] ui_LocalizationConfigPaths =
    {
        "Assets/Data/Localization/Sub_LD/MainMenuLocalizationConfig.asset",
        "Assets/Data/Localization/Sub_LD/SettingLocalizationConfig.asset",
    };

    /// <summary>
    /// 收集器【默认写入目标】的那张配置资产。
    ///
    /// ══════════════ 为什么不复用上面数组的第一项 ══════════════
    /// ui_LocalizationConfigPaths 回答的是"有哪几张子配置"，它的第一项是主菜单那张。
    /// 而收集器扫的面板绝大多数属于设置面板，默认指向主菜单配置会把设置页的 Key
    /// 写进别人的表里 —— 与"给个能用的默认值"的初衷正好相反。所以默认值单独给一个常量。
    ///
    /// 改这里 = 改"跑收集器时默认写哪张表"。子配置写完还要跑
    /// Tools/本地化/合并子配置，把它并进运行期真正读的那一张（ChaosLocalizationConfig）。
    /// </summary>
    public const string ui_DefaultLocalizationConfigPath =
        "Assets/Data/Localization/Sub_LD/SettingLocalizationConfig.asset";

    #region 表配置路径
    /// <summary>
    /// 配置表存储路径
    /// </summary>
    public const string data_ExcelPath = "Excel/Chaos_excel/";

    /// <summary>
    /// 导出的Json路径
    /// </summary>
    public const string data_JsonPath = "Resources/Data/Json/";

    /// <summary>
    /// 导出的class路径
    /// </summary>
    public const string data_ClassPath = "Scripts/Runtime/Config/ClassConfig/";

    /// <summary>
    /// 导出的Excel配置的Model路径
    /// </summary>
    public const string data_ExcelModelPath = "Data/Model/";

    /// <summary>
    /// 编辑器导出的存档路径
    /// </summary>
    public const string data_RecordPath = "Data/Records/";

    /// <summary>
    /// 包体导出的存档路径
    /// </summary>
    public const string data_RecordPathInPackage = "Records/";

    /// <summary>
    /// 导出的Model类持久化数据路径
    /// </summary>
    public const string data_ModelClassPath = "Scripts/Runtime/MVP/Model/Data/";
    #endregion
}
