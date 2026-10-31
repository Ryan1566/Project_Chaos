---
uid: f1c00037
id: project-chaos.framework.settings.ids
parent: project-chaos.framework.settings
name: {zh: "设置项 id 常量", en: "Setting Ids"}
description:
  zh: >
      SettingIds 设置项 id 常量（graphics.* / audio.* / gameplay.* / keybind.*）：设置行按 id 绑定到 SettingsData 字段的唯一契约。
      
  en: >
      SettingIds: stable string ids for every setting row (graphics.*, audio.*, gameplay.*, keybind.*) used to bind UI rows to settings.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:47.487Z"
fingerprint: 4f570750ff24bfb7b0f9254f2775ee21b02d98dffbb1c86567fbec192518ca65
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingIds.cs"
    line: 1
    end_line: 69
apis:
  - protocol: rpc
    path: "SettingIds.Language"
    description:
      zh: >
          语言设置项 id。
          
      en: >
          Language setting id.
          
  - protocol: rpc
    path: "SettingIds.Resolution"
    description:
      zh: >
          分辨率设置项 id。
          
      en: >
          Resolution setting id.
          
  - protocol: rpc
    path: "SettingIds.MasterVolume"
    description:
      zh: >
          主音量设置项 id。
          
      en: >
          Master volume setting id.
          
  - protocol: rpc
    path: "SettingIds.MoveLeft"
    description:
      zh: >
          左移按键设置项 id。
          
      en: >
          Move-left keybinding setting id.
          
  - protocol: rpc
    path: "SettingIds.ResetScheme"
    description:
      zh: >
          重置按键方案设置项 id。
          
      en: >
          Reset-scheme setting id.
          
---
