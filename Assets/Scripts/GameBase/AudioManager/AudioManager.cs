using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;

//该脚本用于管理音乐音效
public class AudioManager : SingletonBase<AudioManager>
{
    private AudioSource bgm = null;

    private float bgmVolume = 0.5f;
    private float soundVolume = 0.5f;

    private GameObject soundObj = null;
    private List<AudioSource> soundList = new List<AudioSource>();

    public AudioManager()
    {
        MonoManager.Instance.AddUpdateListener(Update);
    }

    private void Update()
    {
        for(int i = soundList.Count - 1;i >= 0; --i)
        {
            if (!soundList[i].isPlaying)
            {
                GameObject.Destroy(soundList[i]);
                soundList.RemoveAt(i);
            }
        }
    }

    #region 背景音乐
    public void PlayBGM(string name)
    {
        if(bgm == null)
        {
            GameObject obj = new GameObject();
            obj.name = "bgmPlayer";
            bgm = obj.AddComponent<AudioSource>();
        }

        ResManager.Instance.LoadAsync<AudioClip>(GlobalPath.res_TestMusicPath + name,(clip) => 
        {
            bgm.clip = clip;
            bgm.volume = bgmVolume;
            bgm.loop = true;
            bgm.Play();
        });
    }

    public void ChangeBgmVolume(float volume)
    {
        bgmVolume = volume;
        if (bgm == null) return;
        bgm.volume = bgmVolume;
    }

    public void PauseBGM()
    {
        if (bgm == null) return;
        bgm.Pause();
    }

    public void StopBGM()
    {
        if (bgm == null) return;
        bgm.Stop();
    }
    #endregion

    #region 音效
    public void PlaySound(string name,bool isLoop, UnityAction<AudioSource> callback = null)
    {
        if (soundObj == null)
        {
            soundObj = new GameObject();
            soundObj.name = "SoundPlayer";
        }

        //加载成功后再添加AudioSource
        ResManager.Instance.LoadAsync<AudioClip>(GlobalPath.res_SoundPath + name, (clip) =>
        {
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = isLoop;
            source.volume = soundVolume;
            source.Play();
            soundList.Add(source);
            if (callback != null)
                callback(source);
        });
    }

    public void ChangeSoundVolume(float volume)
    {
        soundVolume = volume;
        foreach(var sound in soundList)
        {
            sound.volume = soundVolume;
        }
    }

    [Obsolete("该方法暂时未填充内容",true)]
    public void PauseSound(AudioSource source)
    {
        //TODO
    }

    public void StopSound(AudioSource source)
    {
        if (soundList.Contains(source))
        {
            soundList.Remove(source);
            source.Stop();
            //PoolManager.Instance.PushObj("音效",source);
            GameObject.Destroy(source);
        }
    }
    #endregion
}
