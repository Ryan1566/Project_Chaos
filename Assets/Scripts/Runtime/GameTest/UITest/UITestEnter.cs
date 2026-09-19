using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITestEnter : MonoBehaviour
{
    private Button _uiTestBtn;
    private BasePanel testPanel;

    private void Awake()
    {
        _uiTestBtn = GetComponent<Button>();
        _uiTestBtn.onClick.AddListener(ShowUI);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
    }
}
