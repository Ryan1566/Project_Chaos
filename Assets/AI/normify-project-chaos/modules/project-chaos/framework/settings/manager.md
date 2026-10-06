---
uid: f1c00039
id: project-chaos.framework.settings.manager
parent: project-chaos.framework.settings
name: {zh: "设置管理器状态", en: "Settings Manager State"}
description:
  zh: >
      SettingsManager 状态与初始化：单例建立、Applied/Pending 双快照、编辑中标记与脏数据判定，供设置页读写。
      
  en: >
      SettingsManager state and init: singleton setup, Applied/Pending snapshots, editing flag and dirty check for the settings page.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.758Z"
fingerprint: a58af6ec57f3504b214f118edda3f6ebe55cc20e6545fda34175d3a94f17020e
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsManager.cs"
    line: 1
    end_line: 89
apis:
  - protocol: rpc
    path: "SettingsManager.Init"
    description:
      zh: >
          初始化并读取设置。
          
      en: >
          Initializes and loads settings.
          
  - protocol: rpc
    path: "SettingsManager.Applied"
    description:
      zh: >
          已生效设置（权威）。
          
      en: >
          Applied settings (authoritative).
          
  - protocol: rpc
    path: "SettingsManager.Pending"
    description:
      zh: >
          编辑中的待提交设置。
          
      en: >
          Pending settings while editing.
          
  - protocol: rpc
    path: "SettingsManager.IsEditing"
    description:
      zh: >
          是否处于编辑会话中。
          
      en: >
          Whether an edit session is open.
          
  - protocol: rpc
    path: "SettingsManager.HasPendingChanges"
    description:
      zh: >
          待提交与已生效是否存在差异。
          
      en: >
          Whether pending differs from applied.
          
deps:
  - kind: call
    to: project-chaos.framework.settings.manager-persist
    from_api: "rpc:SettingsManager.Init"
    to_api: "rpc:SettingsManager.Load"
    label: {zh: "读取设置", en: "Load settings"}
  - kind: reference
    to: project-chaos.framework.settings.data
    from_api: "rpc:SettingsManager.Applied"
    to_api: "rpc:SettingsData.CreateDefault"
    label: {zh: "设置模型", en: "Settings model"}
---
