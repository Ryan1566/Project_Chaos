---
uid: f1c0005e
id: project-chaos.framework.dev-tests.audio-probes
parent: project-chaos.framework.dev-tests
name: {zh: "音频自测探针", en: "Audio Probes"}
description:
  zh: >
      音频自测探针：两个 OnGUI 按钮面板，直调 AudioManager 的 BGM 与音效接口，人工验证播放、音量与停止。
      
  en: >
      Audio dev probes: two OnGUI button panels that call AudioManager.PlayBGM/ChangeBgmVolume/PauseBGM/StopBGM and PlaySound/ChangeSoundVolume/StopSound.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.740Z"
fingerprint: 589065ed259b46afe46ee9859368652423b244f1d706b019679d677dec44210f
source:
  - path: "Assets/Scripts/Runtime/GameTest/AudioTest/MusicTest.cs"
    line: 1
    end_line: 31
  - path: "Assets/Scripts/Runtime/GameTest/AudioTest/SoundTest.cs"
    line: 1
    end_line: 36
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/AudioTest/MusicTest.cs"
    description:
      zh: >
          BGM 播放/音量/暂停/停止的 OnGUI 探针。
          
      en: >
          OnGUI probe for BGM play/volume/pause/stop.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameTest/AudioTest/SoundTest.cs"
    description:
      zh: >
          音效播放/音量/停止的 OnGUI 探针。
          
      en: >
          OnGUI probe for SFX play/volume/stop.
          
deps:
  - kind: call
    to: project-chaos.framework.audio.bgm
    from_api: "file:Assets/Scripts/Runtime/GameTest/AudioTest/MusicTest.cs"
    to_api: "rpc:AudioManager.PlayBGM"
    label: {zh: "BGM 探针", en: "BGM probe"}
  - kind: call
    to: project-chaos.framework.audio.sfx
    from_api: "file:Assets/Scripts/Runtime/GameTest/AudioTest/SoundTest.cs"
    to_api: "rpc:AudioManager.PlaySound"
    label: {zh: "音效探针", en: "SFX probe"}
---
