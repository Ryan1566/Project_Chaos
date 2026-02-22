using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//该脚本用于存储事件中函数的参数，继承了C#封装好的类EventArgs
//只要不继承Mono类，脚本名字和类名可以不用相同
//a_xxx 的a代表args

#region 输入输出
public class  InputArgs : EventArgs
{
    public KeyCode keyCodeValue;
}
#endregion

#region 保存和加载
/// <summary>
/// 保存事件参数
/// </summary>
public class SavingEventArgs : EventArgs
{
    //public PlayerStats a_playerStats;
    //public InputHandle a_inputHandler;
}

/// <summary>
/// 加载事件参数
/// </summary>
public class LoadingEventArgs : EventArgs
{
    public AsyncOperation a_operation;//传入的场景信息
}

/// <summary>
/// 保存设置事件参数
/// </summary>
public class SavingSettingEventArgs : EventArgs
{
    //public SettingPanel a_settingPanel;
}

/// <summary>
/// 加载设置事件参数
/// </summary>
public class LoadingSettingEventArgs : EventArgs
{
    public bool a_isNewOrOrg;
}

/// <summary>
/// 加载界面事件参数
/// </summary>
public class LoadingProgressEventArgs : EventArgs
{
    public string a_sceneName;
}
#endregion

#region 任务系统
/// <summary>
/// 委派任务事件参数
/// </summary>
public class DelegateQuestEventArgs : EventArgs
{
    //委派任务事件目前没有参数

    //public Questable a_questable;
}
#endregion
