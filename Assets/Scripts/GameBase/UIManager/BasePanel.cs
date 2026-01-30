using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePanel : MonoBehaviour
{
    protected UIManager uiManager;
    public UIManager setUIManager
    {
        set
        {
            uiManager = value;
        }
    }

    public virtual void OnEnter()
    {
        gameObject.SetActive(true);
        GetComponent<BasePanel>().enabled = true;
    }

    public virtual void OnExit()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 控制鼠标显示
    /// </summary>
    public void CursorEnable()
    {
        Cursor.visible = true;//显示鼠标
        Cursor.lockState = 0;//解除鼠标的限制
    }

    /// <summary>
    /// 控制鼠标隐藏
    /// </summary>
    public void CursorHide()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;//锁定并隐藏鼠标
        Cursor.lockState = CursorLockMode.Confined;//鼠标限制在游戏视图内
    }

    /// <summary>
    /// 暂停
    /// </summary>
    /// <param name="isPauseTime">暂停游戏时间</param>
    /// <param name="isPause">暂停PlayerManager的Update</param>
    public void PauseTime(bool isPauseTime, bool isPause)
    {
        if (isPauseTime)
            Time.timeScale = 0f;

        //if (isPause)
        //{
        //    GameManager.Instance.isPause = true;
        //}
    }

    /// <summary>
    /// 恢复时间
    /// </summary>
    public void ResumeTime()
    {
        Time.timeScale = 1f;
        //GameManager.Instance.isPause = false;
    }
}
