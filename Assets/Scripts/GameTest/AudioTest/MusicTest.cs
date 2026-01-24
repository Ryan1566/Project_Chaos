using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTest : MonoBehaviour
{
    private void OnGUI()
    {
        if(GUI.Button(new Rect(0, 0, 300, 300), "播放音乐"))
        {
            AudioManager.Instance.PlayBGM("With love from Vertex Studio (24)");
        }
        if (GUI.Button(new Rect(301, 0, 300, 300), "调整音乐音量为0.5"))
        {
            AudioManager.Instance.ChangeBgmVolume(0.5f);
        }
        if (GUI.Button(new Rect(602, 0, 300, 300), "调整音乐音量为1"))
        {
            AudioManager.Instance.ChangeBgmVolume(1f);
        }
        if (GUI.Button(new Rect(903, 0, 300, 300), "暂停音乐"))
        {
            AudioManager.Instance.PauseBGM();
        }
        if (GUI.Button(new Rect(1204, 0, 300, 300), "关闭音乐"))
        {
            AudioManager.Instance.StopBGM();
        }
    }
}
