---
uid: f1c0001f
id: project-chaos.framework.audio.sfx
parent: project-chaos.framework.audio
name: {zh: "音效播放", en: "Sound Effects"}
description:
  zh: >
      音效段：PlaySound/PlayUISound 播放并回调交付音源、改音量、按 AudioSource 暂停与停止。
      
  en: >
      Sound-effect block: PlaySound/PlayUISound with callback, runtime volume change, pause and stop by AudioSource.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.740Z"
fingerprint: 1bbab6cee523c3ffabcfd4e5396869f08770bac456d42d1419768b73354fd300
source:
  - path: "Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs"
    line: 168
    end_line: 239
apis:
  - protocol: rpc
    path: "AudioManager.PlaySound"
    description:
      zh: >
          播放一次性音效，可循环并带回调交付 AudioSource。
          
      en: >
          Plays a one-shot sound effect with optional loop and callback.
          
  - protocol: rpc
    path: "AudioManager.PlayUISound"
    description:
      zh: >
          播放 UI 音效（独立 UI 通道）。
          
      en: >
          Plays a UI sound effect.
          
  - protocol: rpc
    path: "AudioManager.ChangeSoundVolume"
    description:
      zh: >
          运行时改变音效音量。
          
      en: >
          Changes SFX volume at runtime.
          
  - protocol: rpc
    path: "AudioManager.PauseSound"
    description:
      zh: >
          暂停指定的音效音源。
          
      en: >
          Pauses a playing sound source.
          
  - protocol: rpc
    path: "AudioManager.StopSound"
    description:
      zh: >
          停止指定的音效音源。
          
      en: >
          Stops a playing sound source.
          
deps:
  - kind: call
    to: project-chaos.framework.resources.manager
    from_api: "rpc:AudioManager.PlaySound"
    to_api: "rpc:ResManager.Load"
    label: {zh: "加载音效片段", en: "Load audio clip"}
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "rpc:AudioManager.PlaySound"
    to_api: "rpc:GlobalPath.res_SoundPath"
    label: {zh: "音效目录", en: "Sound path"}
---
