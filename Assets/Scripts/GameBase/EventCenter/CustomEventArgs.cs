using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//�ýű����ڴ洢�¼��к����Ĳ������̳���C#��װ�õ���EventArgs
//ֻҪ���̳�Mono�࣬�ű����ֺ��������Բ�����ͬ
//a_xxx ��a����args

#region ����ͼ���
/// <summary>
/// �����¼�����
/// </summary>
public class SavingEventArgs : EventArgs
{
    //public PlayerStats a_playerStats;
    //public InputHandle a_inputHandler;
}

/// <summary>
/// �����¼�����
/// </summary>
public class LoadingEventArgs : EventArgs
{
    //�����¼���ʱ����Ҫ����
}

/// <summary>
/// ���������¼�����
/// </summary>
public class SavingSettingEventArgs : EventArgs
{
    //public SettingPanel a_settingPanel;
}

/// <summary>
/// ���������¼�����
/// </summary>
public class LoadingSettingEventArgs : EventArgs
{
    public bool a_isNewOrOrg;
}

/// <summary>
/// ���ؽ����¼�����
/// </summary>
public class LoadingProgressEventArgs : EventArgs
{
    public string a_sceneName;
}
#endregion

#region ����ϵͳ
/// <summary>
/// ί�������¼�����
/// </summary>
public class DelegateQuestEventArgs : EventArgs
{
    //ί�������¼�Ŀǰû�в���

    //public Questable a_questable;
}
#endregion
