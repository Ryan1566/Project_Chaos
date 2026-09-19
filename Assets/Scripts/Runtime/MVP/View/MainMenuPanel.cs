using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Ö÷²Ëµ¥Ãæ°å
public class MainMenuPanel : BasePanel
{
    private Button _settingBtn;

    private void Awake()
    {
        _settingBtn = transform.Find("SettingBtn").GetComponent<Button>();
        _settingBtn.onClick.AddListener(SettingEnter);
    }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void OnExit()
    {
        base.OnExit();
    }

    void SettingEnter()
    {
        UIManager.Instance.PushPanel(PanelType.SettingPanel);
    }
}
