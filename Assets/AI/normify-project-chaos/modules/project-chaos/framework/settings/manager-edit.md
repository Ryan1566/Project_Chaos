---
uid: f1c0003b
id: project-chaos.framework.settings.manager-edit
parent: project-chaos.framework.settings
name: {zh: "设置编辑会话", en: "Settings Edit Session"}
description:
  zh: >
      设置编辑会话：BeginEdit 建待编辑副本、CommitEdit 提交落盘、RevertEdit 丢弃、按分类重置与变更通知。
      
  en: >
      Settings edit session: BeginEdit/CommitEdit/RevertEdit with a pending copy, per-section reset and pending-changed notification.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.745Z"
fingerprint: c2be51a3f9eb4d1749f110c3c666f539cd13a4390f732106a65cc9d7217897cf
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsManager.cs"
    line: 160
    end_line: 239
apis:
  - protocol: rpc
    path: "SettingsManager.BeginEdit"
    description:
      zh: >
          把已生效设置拷贝为待编辑副本。
          
      en: >
          Copies applied settings into the pending edit copy.
          
  - protocol: rpc
    path: "SettingsManager.CommitEdit"
    description:
      zh: >
          提交待编辑设置、落盘并结束会话。
          
      en: >
          Applies pending settings, saves and closes the session.
          
  - protocol: rpc
    path: "SettingsManager.RevertEdit"
    description:
      zh: >
          丢弃待提交更改。
          
      en: >
          Discards pending changes.
          
  - protocol: rpc
    path: "SettingsManager.ResetPendingSection"
    description:
      zh: >
          把待编辑的某分类段重置为默认。
          
      en: >
          Resets one pending section to defaults.
          
  - protocol: rpc
    path: "SettingsManager.NotifyPendingChanged"
    description:
      zh: >
          广播待编辑变更事件。
          
      en: >
          Raises the OnPendingChanged event.
          
deps:
  - kind: reference
    to: project-chaos.framework.settings.data
    from_api: "rpc:SettingsManager.BeginEdit"
    to_api: "rpc:SettingsData.Clone"
    label: {zh: "待编辑副本", en: "Pending copy"}
  - kind: call
    to: project-chaos.framework.settings.manager-apply
    from_api: "rpc:SettingsManager.CommitEdit"
    to_api: "rpc:SettingsManager.ApplyToRuntime"
    label: {zh: "应用设置", en: "Apply settings"}
---
