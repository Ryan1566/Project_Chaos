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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.744Z"
fingerprint: a6311e933110412c122c845b947c9daf6997c5cfe5c188b942be42f29f89780f
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
