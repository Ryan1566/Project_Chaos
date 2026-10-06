---
uid: f1c0001e
id: project-chaos.framework.audio.bgm
parent: project-chaos.framework.audio
name: {zh: "背景音乐播放", en: "BGM Playback"}
description:
  zh: >
      BGM 播放段：按名字播放、运行时改音量、暂停与停止；音频片段来自 Resources 的音乐目录。
      
  en: >
      BGM playback block: PlayBGM by clip name, runtime volume change, pause and stop, loading from the Resources music folder.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.739Z"
fingerprint: 1bbab6cee523c3ffabcfd4e5396869f08770bac456d42d1419768b73354fd300
source:
  - path: "Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs"
    line: 127
    end_line: 167
apis:
  - protocol: rpc
    path: "AudioManager.PlayBGM"
    description:
      zh: >
          按名字播放背景音乐。
          
      en: >
          Plays background music by name.
          
  - protocol: rpc
    path: "AudioManager.ChangeBgmVolume"
    description:
      zh: >
          运行时改变 BGM 音量。
          
      en: >
          Changes BGM volume at runtime.
          
  - protocol: rpc
    path: "AudioManager.PauseBGM"
    description:
      zh: >
          暂停背景音乐。
          
      en: >
          Pauses background music.
          
  - protocol: rpc
    path: "AudioManager.StopBGM"
    description:
      zh: >
          停止背景音乐。
          
      en: >
          Stops background music.
          
deps:
  - kind: call
    to: project-chaos.framework.resources.manager
    from_api: "rpc:AudioManager.PlayBGM"
    to_api: "rpc:ResManager.Load"
    label: {zh: "加载音频片段", en: "Load audio clip"}
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "rpc:AudioManager.PlayBGM"
    to_api: "rpc:GlobalPath.res_MusicPath"
    label: {zh: "音乐目录", en: "Music path"}
---
