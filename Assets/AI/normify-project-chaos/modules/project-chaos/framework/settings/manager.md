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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.745Z"
fingerprint: c2be51a3f9eb4d1749f110c3c666f539cd13a4390f732106a65cc9d7217897cf
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
