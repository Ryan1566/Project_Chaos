using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;

//该脚本暂时弃用！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！

#region 事件中心
// 事件响应空接口，用于支持可有可无的参数类型
public interface IEventInfo
{
}

// 带参数事件响应
public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}

// 无参数事件响应
public class EventInfo : IEventInfo
{
    public UnityAction actions;
    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}

//事件中心
// 负责注册（监听）事件、分发（触发）事件
// 事件支持 带参数 和 无参数 两种
// 带参数事件使用 EventInfo<T> 数据类型
public class EventCenter : SingletonBase<EventCenter>
{
    // 数据结构
    private Dictionary<string, IEventInfo> _eventDic = new Dictionary<string, IEventInfo>();

    // 添加带参数事件的监听
    public void AddEventListener<T>(string name, UnityAction<T> action)
    {
        // 旧事件
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo<T>).actions += action;
        // 新事件
        else
            _eventDic.Add(name, new EventInfo<T>(action));
    }

    // 添加无参数事件的监听
    public void AddEventListener(string name, UnityAction action)
    {
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo).actions += action;
        else
            _eventDic.Add(name, new EventInfo(action));
    }

    // 移除带参数事件的监听
    public void RemoveEventListener<T>(string name, UnityAction<T> action)
    {
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo<T>).actions -= action;
    }

    // 移除无参数事件的监听
    public void RemoveEventListener(string name, UnityAction action)
    {
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo).actions -= action;
    }

    // 分发带参数的事件
    public void EventTrigger<T>(string name, T info)
    {
        if (_eventDic.ContainsKey(name))
            if ((_eventDic[name] as EventInfo<T>).actions != null)
                (_eventDic[name] as EventInfo<T>).actions.Invoke(info);
    }

    // 分发无参数的事件
    public void EventTrigger(string name)
    {
        if (_eventDic.ContainsKey(name))
            if ((_eventDic[name] as EventInfo).actions != null)
                (_eventDic[name] as EventInfo).actions.Invoke();
    }

    // 清空事件监听
    // 主要用于场景切换时防止内存泄漏
    public void Clear()
    {
        _eventDic.Clear();
    }
}
#endregion