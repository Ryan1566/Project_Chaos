using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//记录ui面板类型的枚举，用于辅助构建动态构建路径
public enum PanelType
{
    MainPanel,//主菜单
    SavePanel,//保存面板
    SettingPanel,//设置面板
    PausePanel,//暂停面板
    PlayerStatusPanel,//状态hud，记录血量，耐力等
    InteractingPromptPanel,//提示面板
    DataPanel,//数据面板，记录着仓库属性等数值
    DialogPanel,//对话面板
    QuestPanel,//任务面板
    GainQuestPanel,//新任务领取提示面板
    ProgressPanel,//加载面板
    SavingPanel,//保存面板
    LoadingPanel,//读取存档面板
    EnddingPanel//结局面板
}
