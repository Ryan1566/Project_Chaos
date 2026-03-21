using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话人物信息
/// </summary>
[Serializable]
public class DialogueCharacter
{
    public string characterId;//人物ID
    public string characterName;//人物名称
    public Color nameColor = Color.white;//名字颜色
    public Sprite avatar;//头像
    public AudioClip voiceClip;//语音音效
}

/// <summary>
/// 单条对话内容
/// </summary>
[Serializable]
public class DialogueEntry
{
    public string characterId;//说话人物ID
    [TextArea(3, 10)]
    public string dialogueText;//对话文本
    public float typingSpeed = 0.05f;//打字速度（每个字符的间隔时间，秒）
    public float autoSkipDelay = 2f;//自动跳过延迟（秒），-1表示不自动跳过
    public AudioClip voiceClip;//本条对话的语音（可选，为空则使用人物默认语音）
    public List<DialogueOption> options;//对话选项（可选）
    public string nextDialogueId;//下一条对话ID（为空则结束对话）
    public List<DialogueEvent> events;//对话触发的事件列表
}

/// <summary>
/// 对话选项
/// </summary>
[Serializable]
public class DialogueOption
{
    public string optionText;//选项文本
    public string nextDialogueId;//选择后跳转的对话ID
    public List<DialogueEvent> events;//选择时触发的事件
}

/// <summary>
/// 对话事件（用于触发游戏逻辑）
/// </summary>
[Serializable]
public class DialogueEvent
{
    public string eventName;//事件名称
    public string eventParam;//事件参数
}

/// <summary>
/// 对话配置数据（可创建为ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "DialogueConfig", menuName = "Dialogue/Dialogue Config")]
public class DialogueData : ScriptableObject
{
    public string dialogueId;//对话ID
    public string dialogueName;//对话名称
    [TextArea(2, 5)]
    public string description;//对话描述

    public List<DialogueCharacter> characters = new List<DialogueCharacter>();//参与对话的人物列表
    public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();//对话内容列表

    public bool canSkip = true;//是否可以跳过对话
    public bool autoPlay = false;//是否自动播放
    public bool showHistory = true;//是否显示历史记录按钮
    public bool pauseGame = true;//对话时是否暂停游戏
}
