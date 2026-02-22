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
    #endregion

    #region 表配置路径
    /// <summary>
    /// 配置表存储路径
    /// </summary>
    [Header("Excel资源路径")]
    public const string data_ExcelPath = "Excel/Game_Excel";

    #endregion
}
