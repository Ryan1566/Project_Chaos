using System;
using UnityEngine;

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
        //    Debug.Log("已通过点击" + args.keyCodeValue + "触发事件");
        //else
        //    Debug.Log("已通过点击" + args.keyCodeValue + "触发事件");

        switch (args.keyCodeValue)
        {
            case KeyCode.Escape:
                Debug.Log("1");
                break;
            case KeyCode.W:
                Debug.Log("2");
                break;
        }

    }
}
