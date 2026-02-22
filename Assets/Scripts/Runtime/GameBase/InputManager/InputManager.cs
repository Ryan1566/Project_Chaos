using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//输入管理器 管理所有按键输入
public class InputManager : SingletonBase<InputManager>
{
    private bool isStart = false;

    public InputManager()
    {
        MonoManager.Instance.AddUpdateListener(Tick);
    }

    /// <summary>
    /// 是否启用按键监听
    /// </summary>
    /// <param name="openFlag">true:开启;false:关闭</param>
    public void StartTickOrNot(bool openFlag)
    {
        isStart = openFlag;
    }

    /// <summary>
    /// 每帧调用函数
    /// </summary>
    private void Tick()
    {
        if(!isStart)
            return;

        CheckKeyCode(KeyCode.Escape);
        CheckKeyCode(KeyCode.W);
    }

    /// <summary>
    /// 根据对应按键触发事件
    /// </summary>
    /// <param name="kc">按键名</param>
    private void CheckKeyCode(KeyCode kc)
    {
        if (Input.GetKeyDown(kc))
        {
            this.TriggerEvent(EventConstName.GetKeyDown, new InputArgs
            {
                keyCodeValue = kc
            });
        }
        if (Input.GetKeyDown(kc))
        {
            this.TriggerEvent(EventConstName.GetKeyDown, new InputArgs
            {
                keyCodeValue = kc
            });
        }
    }
}
