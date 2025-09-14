using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//触发事件的拓展类,调用时直接在类等继承于Object的代码块中调用即可
public static class EventTriggerExt
{
    //sender是触发源，eventName是事件名，args是事件参数

    public static void TriggerEvent(this object sender,string eventName)//触发事件（无参）的拓展方法，this object sender 用于拓展
    {
        EventManager.Instance.TriggerEvent(eventName,sender);
    }

    public static void TriggerEvent(this object sender,string eventName,EventArgs args)//触发事件（有参）的拓展方法，this object sender 用于拓展
    {
        EventManager.Instance.TriggerEvent(eventName, sender,args);
    }
}

//事件中心，继承单例
public class EventManager : SingletonBase<EventManager>
{
    private Dictionary<string,EventHandler> handlerDict = new Dictionary<string, EventHandler>();//存储事件的字典
                                                                                                 //EventHandler是unity自带的一个通用的委托，可以涵盖各个类型的方法

    public void AddListener(string eventName,EventHandler handler)//添加监听器
    {
        if (handlerDict.ContainsKey(eventName))
            handlerDict[eventName] += handler;//字典存储多个事件，每个事件又存储多个方法，是一对多的思想，即观察者模式
        else
            handlerDict.Add(eventName, handler);//未在字典中注册就添加进字典
    }

    public void RemoveListener(string eventName,EventHandler handler)//移除监听器
    {
        if(handlerDict.ContainsKey(eventName))
            handlerDict[eventName] -= handler;
    }

    public void TriggerEvent(string eventName,object sender)//触发事件（无参）
    {
        if(handlerDict.ContainsKey(eventName))
            handlerDict[eventName]?.Invoke(sender,EventArgs.Empty);
    }

    public void TriggerEvent(string eventName,object sender,EventArgs e)//触发事件（有参）
    {
        if(handlerDict.ContainsKey(eventName))
            handlerDict[eventName]?.Invoke(sender,e);
    }

    public void Clear()//清除所有事件 防止游戏场景加载导致的内存泄露
    {
        handlerDict.Clear();
    }
}

#region 触发事件使用方式
//this.TriggerEvent(EventConstName.LoadSetting,new LoadingSettingEventArgs//初始化时加载设置
//{
//    a_isNewOrOrg = isNewOrLoad
//});
#endregion