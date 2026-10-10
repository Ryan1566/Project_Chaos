using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GM_UITestEnter : MonoBehaviour
{
    private Button _uiTestBtn;
    private BasePanel testPanel;

    private void Awake()
    {
        _uiTestBtn = GetComponent<Button>();
        _uiTestBtn.onClick.AddListener(ShowUI);
    }

    private void ShowUI()
    {
        if(testPanel == null)
            testPanel = UIManager.Instance.PushPanel(PanelType.MainMenuPanel);
        else
        {
            if (testPanel.gameObject.activeSelf)
                UIManager.Instance.PopPanel();
            else
                UIManager.Instance.PushPanel(PanelType.MainMenuPanel);
        }

        this.TriggerEvent(EventConstName.CloseGM);
    }
}
