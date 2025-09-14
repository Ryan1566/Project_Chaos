using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Mono管理者 负责管理Mono的生命周期、事件以及协程
public class MonoController : MonoBehaviour
{
    public event UnityAction updateEvent;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
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
