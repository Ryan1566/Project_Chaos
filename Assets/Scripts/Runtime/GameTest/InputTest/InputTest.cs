using System;
using UnityEngine;
using ChaosDebug;

public class InputTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        InputManager.Instance.StartTickOrNot(true);

        EventManager.Instance.AddListener(EventConstName.GetKeyDown,ShowTestUI);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ShowTestUI(object sender,EventArgs e)
    {
        var args = e as InputArgs;

        //if (args.keyCodeValue == KeyCode.Escape)
        //    ChaosLog.Info(LogChannel.Input, "已通过点击" + args.keyCodeValue + "触发事件");
        //else
        //    ChaosLog.Info(LogChannel.Input, "已通过点击" + args.keyCodeValue + "触发事件");

        switch (args.keyCodeValue)
        {
            case KeyCode.Escape:
                ChaosLog.Info(LogChannel.Input, "1");
                break;
            case KeyCode.W:
                ChaosLog.Info(LogChannel.Input, "2");
                break;
        }

    }
}
