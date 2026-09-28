using ChaosDebug;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//主菜单面板
public class MainMenuPanel : BasePanel
{
    private Button _startBtn;
    private Button _settingBtn;
    private Button _quitBtn;

    private void Awake()
    {
        _startBtn = transform.Find("StartBtn").GetComponent<Button>();
        _settingBtn = transform.Find("SettingBtn").GetComponent<Button>();
        _quitBtn = transform.Find("QuitBtn").GetComponent<Button>();

        _startBtn.onClick.AddListener(StartGame);
        _settingBtn.onClick.AddListener(SettingEnter);
        _quitBtn.onClick.AddListener(QuitGame);
    }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void OnExit()
    {
        base.OnExit();
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    void StartGame()
    {
        ChaosLog.Info("点击开始游戏按钮");
    }

    ///设置按钮
    void SettingEnter()
    {
        UIManager.Instance.PushPanel(PanelType.SettingPanel);
    }

    ///退出游戏
    void QuitGame()
    {
        Application.Quit();
    }
}
