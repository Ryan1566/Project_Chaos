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
        "Assets/Prefabs/UI",
    };
    #endregion

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
