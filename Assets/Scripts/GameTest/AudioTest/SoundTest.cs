using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTest : MonoBehaviour
{
    AudioSource source = null;

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 301, 300, 300), "播放音效"))
        {
            AudioManager.Instance.PlaySound("牛叫",false,(s) =>
            {
                source = s;
            });
        }
        if (GUI.Button(new Rect(301, 301, 300, 300), "调整音效音量为0.5"))
        {
            AudioManager.Instance.ChangeSoundVolume(0.5f);
        }
        if (GUI.Button(new Rect(602, 301, 300, 300), "调整音效音量为1"))
        {
            AudioManager.Instance.ChangeSoundVolume(1f);
        }
        //if (GUI.Button(new Rect(903, 301, 300, 300), "暂停音效"))
        //{
        //    AudioManager.Instance.PauseSound();
        //}
        if (GUI.Button(new Rect(1204, 301, 300, 300), "关闭音效"))
        {
            AudioManager.Instance.StopSound(source);
        }
    }
}
