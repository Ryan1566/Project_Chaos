---
uid: f1c00035
id: project-chaos.framework.settings.data
parent: project-chaos.framework.settings
name: {zh: "设置数据模型", en: "Settings Data Model"}
description:
  zh: >
      SettingsData 可写存档模型：版本号与画面/声音/玩法/按键各字段，提供默认值构造、克隆、内容比较与按分类重置。
      
  en: >
      SettingsData serializable model: versioned fields for graphics/audio/gameplay/keybind sections, plus CreateDefault, Clone, ContentEquals and per-section reset.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.755Z"
fingerprint: c380d72e9760b2b52d75cc84c426bdec295eb57f76854059dc46349a37884cf3
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsData.cs"
    line: 1
    end_line: 159
apis:
  - protocol: rpc
    path: "SettingsData.CreateDefault"
    description:
      zh: >
          创建一份默认值设置对象。
          
      en: >
          Creates a settings object with default values.
          
  - protocol: rpc
    path: "SettingsData.Clone"
    description:
      zh: >
          深拷贝一份设置对象。
          
      en: >
          Deep-copies the settings object.
          
  - protocol: rpc
    path: "SettingsData.ContentEquals"
    description:
      zh: >
          逐字段比较内容是否一致。
          
      en: >
          Compares content field by field.
          
  - protocol: rpc
    path: "SettingsData.ResetSection"
    description:
      zh: >
          把某一分类段重置为默认值。
          
      en: >
          Resets one section to defaults.
          
  - protocol: rpc
    path: "SettingsData.version"
    description:
      zh: >
          存档结构版本字段，用于兼容。
          
      en: >
          Schema version field for save compatibility.
          
deps:
  - kind: reference
    to: project-chaos.framework.settings.enums
    from_api: "rpc:SettingsData.ResetSection"
    to_api: "file:Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsEnums.cs"
    label: {zh: "设置分类", en: "Setting categories"}
---
