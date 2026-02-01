using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestPanel : TangLaoShi.BasePanel
{
    // Start is called before the first frame update
    void Start()
    {
        GetControl<Button>("StartBtn").onClick.AddListener(() =>
        {
            Debug.Log($"{GetControl<Button>("StartBtn").name}ÒÑµã»÷");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
