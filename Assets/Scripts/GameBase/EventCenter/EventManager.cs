using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//�����¼�����չ��,����ʱֱ������ȼ̳���Object�Ĵ�����е��ü���
public static class EventTriggerExt
{
    //sender�Ǵ���Դ��eventName���¼�����args���¼�����

    public static void TriggerEvent(this object sender,string eventName)//�����¼����޲Σ�����չ������this object sender ������չ
    {
        EventManager.Instance.TriggerEvent(eventName,sender);
    }

    public static void TriggerEvent(this object sender,string eventName,EventArgs args)//�����¼����вΣ�����չ������this object sender ������չ
    {
        EventManager.Instance.TriggerEvent(eventName, sender,args);
    }
}

//�¼����ģ��̳е���
public class EventManager : SingletonBase<EventManager>
{
    private Dictionary<string,EventHandler> handlerDict = new Dictionary<string, EventHandler>();//�洢�¼����ֵ�
                                                                                                 //EventHandler��unity�Դ���һ��ͨ�õ�ί�У����Ժ��Ǹ������͵ķ���

    public void AddListener(string eventName,EventHandler handler)//��Ӽ�����
    {
        if (handlerDict.ContainsKey(eventName))
            handlerDict[eventName] += handler;//�ֵ�洢����¼���ÿ���¼��ִ洢�����������һ�Զ��˼�룬���۲���ģʽ
        else
            handlerDict.Add(eventName, handler);//δ���ֵ���ע�����ӽ��ֵ�
    }

    public void RemoveListener(string eventName,EventHandler handler)//�Ƴ�������
    {
        if(handlerDict.ContainsKey(eventName))
            handlerDict[eventName] -= handler;
    }

    public void TriggerEvent(string eventName,object sender)//�����¼����޲Σ�
    {
        if(handlerDict.ContainsKey(eventName))
            handlerDict[eventName]?.Invoke(sender,EventArgs.Empty);
    }

    public void TriggerEvent(string eventName,object sender,EventArgs e)//�����¼����вΣ�
    {
        if(handlerDict.ContainsKey(eventName))
            handlerDict[eventName]?.Invoke(sender,e);
    }

    public void Clear()//��������¼� ��ֹ��Ϸ�������ص��µ��ڴ�й¶
    {
        handlerDict.Clear();
    }
}

#region �����¼�ʹ�÷�ʽ
//this.TriggerEvent(EventConstName.LoadSetting,new LoadingSettingEventArgs//��ʼ��ʱ��������
//{
//    a_isNewOrOrg = isNewOrLoad
//});
#endregion