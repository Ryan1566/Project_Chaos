using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Mono管理者 负责管理Mono的生命周期、事件以及协程
//替所有「不继承 MonoBehaviour 的普通 C# 类」接管 Unity 的核心生命周期和协程能力
public class MonoController : MonoBehaviour
{
    public event UnityAction updateEvent;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if(updateEvent != null)
            updateEvent.Invoke();
    }

    /// <summary>
    /// 提供给外部 用于添加帧更新事件函数
    /// </summary>
    /// <param name="func"></param>
    public void AddUpdateListener(UnityAction func)
    {
        updateEvent += func;
    }

    /// <summary>
    /// 提供给外部 用于移除帧更新事件函数
    /// </summary>
    /// <param name="func"></param>
    public void RemoveUpdateListener(UnityAction func)
    {
        updateEvent -= func;
    }

    public void Clear()
    {
        updateEvent = null;
    }
}
