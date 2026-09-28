using UnityEngine;

/// <summary>
/// 声音设置页：全局 / 音乐 / 音效 / UI 四路音量，拉条 0~100、步进 1。
///
/// ══════════════════════ 关于"边拖边试听" ══════════════════════
/// 本面板的模型是"改完点应用才生效"（需求第 4 项）。但音量是个例外：
/// 拉条如果听不到声音就完全没法调，玩家只能盲拖然后点应用再听、再回来改。
/// 所以这里给四路都挂了 preview 回调，拖动时【立刻改变实际输出音量】，
/// 但存档值仍旧只写进暂存区 —— 点「应用」才落盘，点「返回」放弃则会由
/// SettingsManager.RevertEdit() 把听感也一并还原成已应用的那一份。
/// 于是既听得到，又不会"没点应用就悄悄改了设置"。
///
/// ══════════ 音量现在怎么作用到音频上 ══════════
/// 工程里没有 AudioMixer，用的是 AudioSource.volume 相乘的轻量做法，
/// 详见 AudioManager 与 AudioChannel 的注释。
/// </summary>
public class AudioSettingsPage : SettingsPageBase
{
    /// <summary>音量的取值范围。需求：上限 100、下限 0、每次拉动改变 1 点。</summary>
    private const int MinVolume = 0;
    private const int MaxVolume = 100;

    protected override void OnBind()
    {
        BindSlider(SettingIds.MasterVolume,
            data => data.masterVolume,
            (data, value) => data.masterVolume = value,
            MinVolume, MaxVolume,
            value => AudioManager.Instance.SetVolume(AudioChannel.Master, value));

        BindSlider(SettingIds.MusicVolume,
            data => data.musicVolume,
            (data, value) => data.musicVolume = value,
            MinVolume, MaxVolume,
            value => AudioManager.Instance.SetVolume(AudioChannel.Music, value));

        BindSlider(SettingIds.SfxVolume,
            data => data.sfxVolume,
            (data, value) => data.sfxVolume = value,
            MinVolume, MaxVolume,
            value => AudioManager.Instance.SetVolume(AudioChannel.Sfx, value));

        BindSlider(SettingIds.UiVolume,
            data => data.uiVolume,
            (data, value) => data.uiVolume = value,
            MinVolume, MaxVolume,
            value => AudioManager.Instance.SetVolume(AudioChannel.UI, value));
    }
}
