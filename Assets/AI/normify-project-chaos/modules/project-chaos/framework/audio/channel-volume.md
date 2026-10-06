---
uid: f1c0001d
id: project-chaos.framework.audio.channel-volume
parent: project-chaos.framework.audio
name: {zh: "音频通道与音量", en: "Audio Channels & Volume"}
description:
  zh: >
      AudioManager 通道与音量段：AudioChannel 枚举（Master/Music/Sound/UI）、音量上限常量、按通道读写音量并实时生效。
      
  en: >
      AudioManager channel/volume block: AudioChannel enum (Master/Music/Sound/UI), MaxVolume constant and per-channel GetVolume/SetVolume.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.740Z"
fingerprint: 1bbab6cee523c3ffabcfd4e5396869f08770bac456d42d1419768b73354fd300
source:
  - path: "Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs"
    line: 1
    end_line: 126
apis:
  - protocol: rpc
    path: "AudioManager.MaxVolume"
    description:
      zh: >
          音量上限常量 100。
          
      en: >
          Max volume constant (100).
          
  - protocol: rpc
    path: "AudioManager.GetVolume"
    description:
      zh: >
          取某通道当前音量（0-100）。
          
      en: >
          Gets the volume (0-100) of a channel.
          
  - protocol: rpc
    path: "AudioManager.SetVolume"
    description:
      zh: >
          设置某通道音量（0-100）。
          
      en: >
          Sets the volume (0-100) of a channel.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs#L1-L126"
    description:
      zh: >
          AudioChannel 枚举与按通道音量计算实现段。
          
      en: >
          Audio channel enum and volume block.
          
---
