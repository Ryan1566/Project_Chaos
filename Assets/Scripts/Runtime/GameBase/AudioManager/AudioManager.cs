using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 音频通道。设置面板的"声音"页就是按这四路做拉条的，
/// 所以这个枚举的成员顺序同时也是声音页里拉条的显示顺序。
/// </summary>
public enum AudioChannel
{
    /// <summary>全局音量。作为总闸乘在其它的通道上，拉到 0 时全部静音。</summary>
    Master = 0,
    /// <summary>音乐（BGM）。</summary>
    Music = 1,
    /// <summary>音效（技能、脚步、打击感等游戏内声音）。</summary>
    Sfx = 2,
    /// <summary>UI 音效（点击、翻页、开关等界面声音）。</summary>
    UI = 3,
}

/// <summary>
/// 音频管理器：BGM 与音效的播放，以及四路音量（全局/音乐/音效/UI）。
///
/// ══════════ 音量为什么要四条通道，而不是两条 ══════════
/// 原来的实现只有 bgmVolume / soundVolume 两条，且两者互相独立 ——
/// 玩家把"全局音量"拉到 0 时，音乐还会照常响。需求要的是四路，
/// 其中"全局"是【乘数】而不是第五个独立喇叭：
///     实际音量 = 全局 / 100 × 该通道 / 100
/// 这样"全局拉到 0 → 全静音"是天然成立的，不需要在每处播放代码里特判。
///
/// ══════════ 为什么没用 AudioMixer（与设计文档的差异，需要知会）══════════
/// 设计文档推荐了 AudioMixer（4 个 Group + 暴露参数 + dB 换算）。
/// 这里改用 AudioSource.volume 相乘的轻量做法，原因是【资产创建】：
/// 从代码生成一个合法的 .mixer 必须反射进 UnityEditor.Audio.AudioMixerController
/// 这类内部类型，没有公开 API；手工新建更是没法自动化。
/// 代价说清楚：以后要加混响、压缩器、闪避（对话时压低音乐）这类
/// 需要"在混音总线上处理"的效果时，必须回头改成 AudioMixer。
/// 当前只有音量需求，AudioSource.volume 完全够用，改动面也小得多。
///
/// ══════════ 关于 PauseSound ══════════
/// 保留原样的 [Obsolete(..., true)]：它从来就是空实现，且唯一的一处调用点在测试脚本里被注释掉了。
/// 不删是因为它是公开 API，删掉会让正在用它的外部代码编译不过；
/// 标 error:true 则保证了没人能误用它。
/// </summary>
public class AudioManager : SingletonBase<AudioManager>
{
    /// <summary>音量的取值范围。需求：上限 100、下限 0、步进 1。</summary>
    public const int MaxVolume = 100;

    /// <summary>四路音量的当前值（0 ~ 100），下标 = AudioChannel。</summary>
    private readonly int[] volumes = new int[]
    {
        100, // Master
        80,  // Music
        80,  // Sfx
        80,  // UI
    };

    private AudioSource bgm = null;

    private GameObject soundObj = null;

    /// <summary>正在播放的音效，连同它的所属通道 —— 改音量时要按通道重算实际音量。</summary>
    private readonly List<SoundEntry> soundList = new List<SoundEntry>();

    private class SoundEntry
    {
        public AudioSource Source;
        public AudioChannel Channel;
    }

    public AudioManager()
    {
        MonoManager.Instance.AddUpdateListener(Update);
    }

    private void Update()
    {
        for (int i = soundList.Count - 1; i >= 0; --i)
        {
            if (!soundList[i].Source.isPlaying)
            {
                GameObject.Destroy(soundList[i].Source);
                soundList.RemoveAt(i);
            }
        }
    }

    #region 音量

    /// <summary>取某一路的音量（0 ~ 100）。</summary>
    public int GetVolume(AudioChannel channel)
    {
        return volumes[(int)channel];
    }

    /// <summary>
    /// 设置某一路的音量（0 ~ 100，越界会被夹紧）。
    /// 改完立刻作用到正在播的声音上，所以设置面板拖着拉条就能听到变化。
    /// </summary>
    public void SetVolume(AudioChannel channel, int value)
    {
        volumes[(int)channel] = Mathf.Clamp(value, 0, MaxVolume);
        ApplyVolumes();
    }

    /// <summary>把四路音量重新算一遍并写进所有正在播的 AudioSource。</summary>
    private void ApplyVolumes()
    {
        if (bgm != null) bgm.volume = EffectiveVolume(AudioChannel.Music);

        for (int i = 0; i < soundList.Count; i++)
        {
            //循环播放的音效（isLoop = true）会在列表里长期存在，改音量必须能覆盖到它
            soundList[i].Source.volume = EffectiveVolume(soundList[i].Channel);
        }
    }

    /// <summary>实际音量 = 全局 × 该通道。全程按 0~1 计算，不经过分贝换算。</summary>
    private float EffectiveVolume(AudioChannel channel)
    {
        return (volumes[(int)AudioChannel.Master] / (float)MaxVolume)
             * (volumes[(int)channel] / (float)MaxVolume);
    }

    #endregion

    #region 背景音乐
    public void PlayBGM(string name)
    {
        if (bgm == null)
        {
            GameObject obj = new GameObject();
            obj.name = "bgmPlayer";
            bgm = obj.AddComponent<AudioSource>();
        }

        ResManager.Instance.LoadAsync<AudioClip>(GlobalPath.res_TestMusicPath + name, (clip) =>
        {
            bgm.clip = clip;
            bgm.volume = EffectiveVolume(AudioChannel.Music);
            bgm.loop = true;
            bgm.Play();
        });
    }

    /// <summary>
    /// 改音乐音量。参数是 0 ~ 1 的旧式浮点值（保留给既有调用点）。
    /// 新代码请直接用 SetVolume(AudioChannel.Music, 0 ~ 100)。
    /// </summary>
    public void ChangeBgmVolume(float volume)
    {
        SetVolume(AudioChannel.Music, Mathf.RoundToInt(Mathf.Clamp01(volume) * MaxVolume));
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
    /// <summary>
    /// 播一个游戏内音效（走音效通道）。callback 会在加载完成、开始播放后调用，
    /// 方便调用方拿到 AudioSource 去做跟随、变调之类的处理。
    /// </summary>
    public void PlaySound(string name, bool isLoop, UnityAction<AudioSource> callback = null)
    {
        Play(name, isLoop, GlobalPath.res_SoundPath, AudioChannel.Sfx, callback);
    }

    /// <summary>
    /// 播一个界面音效（走 UI 通道）。按钮点击、翻页、开关这些应该用它 ——
    /// 玩家把"UI 音量"拉到 0 就是不想听界面音，走音效通道的话这个拉条是失效的。
    /// </summary>
    public void PlayUISound(string name, UnityAction<AudioSource> callback = null)
    {
        Play(name, false, GlobalPath.res_SoundPath, AudioChannel.UI, callback);
    }

    private void Play(string name, bool isLoop, string path, AudioChannel channel, UnityAction<AudioSource> callback)
    {
        if (soundObj == null)
        {
            soundObj = new GameObject();
            soundObj.name = "SoundPlayer";
        }

        //加载成功后再新建 AudioSource
        ResManager.Instance.LoadAsync<AudioClip>(path + name, (clip) =>
        {
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = isLoop;
            source.volume = EffectiveVolume(channel);
            source.Play();
            soundList.Add(new SoundEntry { Source = source, Channel = channel });
            if (callback != null)
                callback(source);
        });
    }

    /// <summary>
    /// 改音效音量。参数是 0 ~ 1 的旧式浮点值（保留给既有调用点）。
    /// 新代码请直接用 SetVolume(AudioChannel.Sfx, 0 ~ 100)。
    /// </summary>
    public void ChangeSoundVolume(float volume)
    {
        SetVolume(AudioChannel.Sfx, Mathf.RoundToInt(Mathf.Clamp01(volume) * MaxVolume));
    }

    [System.Obsolete("该方法暂时未实现，请勿使用", true)]
    public void PauseSound(AudioSource source)
    {
        //TODO
    }

    public void StopSound(AudioSource source)
    {
        for (int i = 0; i < soundList.Count; i++)
        {
            if (soundList[i].Source != source) continue;

            soundList.RemoveAt(i);
            source.Stop();
            GameObject.Destroy(source);
            return;
        }
    }
    #endregion
}
