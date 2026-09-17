using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChaosDebug;

public class TestPanel : TangLaoShi.BasePanel
{
    // Start is called before the first frame update
    void Start()
    {
        GetControl<Button>("StartBtn").onClick.AddListener(() =>
        {
            ChaosLog.Info(LogChannel.UI, $"{GetControl<Button>("StartBtn").name}ÒÑµã»÷");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
