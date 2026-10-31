---
uid: f1c00034
id: project-chaos.framework.localization.setting-keys
parent: project-chaos.framework.localization
name: {zh: "设置选项 Key 表", en: "Setting Option Keys"}
description:
  zh: >
      SettingKeys：设置面板选项的本地化 Key 表（窗口模式/画质/触发方式/语言）与按设置 id 取表的查询入口。
      
  en: >
      SettingKeys: localization keys for setting panel options (window mode, quality, trigger mode, language) and the id-to-table lookup.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:34.940Z"
fingerprint: 5b5f18c3a92738c00bbe46d6aeb46a22c16d8e20eec6eb5eb6da16f2962f5e38
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/SettingKeys.cs"
    line: 1
    end_line: 79
apis:
  - protocol: rpc
    path: "SettingKeys.ForSetting"
    description:
      zh: >
          按设置项 id 返回本地化选项表。
          
      en: >
          Returns the localized option table for a setting id.
          
  - protocol: rpc
    path: "SettingKeys.WindowModeOptions"
    description:
      zh: >
          窗口模式选项 Key 表。
          
      en: >
          Window-mode option keys.
          
  - protocol: rpc
    path: "SettingKeys.QualityOptions"
    description:
      zh: >
          画质选项 Key 表。
          
      en: >
          Quality option keys.
          
  - protocol: rpc
    path: "SettingKeys.TriggerModeOptions"
    description:
      zh: >
          鼠标触发方式选项 Key 表。
          
      en: >
          Mouse trigger-mode option keys.
          
  - protocol: rpc
    path: "SettingKeys.LanguageOptions"
    description:
      zh: >
          语言选项 Key 表。
          
      en: >
          Language option keys.
          
deps:
  - kind: reference
    to: project-chaos.framework.settings.ids
    from_api: "rpc:SettingKeys.ForSetting"
    to_api: "rpc:SettingIds.Language"
    label: {zh: "设置项 id", en: "Setting ids"}
---
