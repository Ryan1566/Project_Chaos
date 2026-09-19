using ChaosDebug;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    private Button _returnBtn;

    private void Awake()
    {
        _returnBtn = transform.Find("ReturnBtn").GetComponent<Button>();
        _returnBtn.onClick.AddListener(Return);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Return()
    {
        UIManager.Instance.PopPanel();
        ChaosLog.Info("已点击返回按钮");
    }
}
