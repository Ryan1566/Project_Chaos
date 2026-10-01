using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entry : MonoBehaviour
{
    private void Start()
    {
        UIManager.Instance.OnInit();

        // 顺序有讲究：输入动作表要先加载好，SettingsManager 才能把存档里的按键绑定覆盖
        // 喂给它（ApplyToRuntime 里的 ApplyInput 会调 InputManager.LoadOverridesJson）。
        // 反过来的话 LoadOverridesJson 会自己触发一次懒加载，虽然也能跑通，
        // 但那条路径上的日志顺序会让人误以为设置加载失败了，不如这里写清楚。
        InputManager.Instance.Init();
        SettingsManager.Instance.Init();
    }
}
