---
uid: f1c0003a
id: project-chaos.framework.settings.manager-persist
parent: project-chaos.framework.settings
name: {zh: "设置落盘", en: "Settings Persistence"}
description:
  zh: >
      设置持久化：读写 persistentDataPath/Config/settings.json，含首次运行自动适配与 SaveSetting/LoadSetting 事件广播。
      
  en: >
      Settings persistence: Load/Save to persistentDataPath/Config/settings.json with first-run auto detection and SaveSetting/LoadSetting events.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.434Z"
fingerprint: e3c323738fad836796c9ec53e74318a84dff5000a9c6107a6b9dffb68650e3e8
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsManager.cs"
    line: 90
    end_line: 159
apis:
  - protocol: rpc
    path: "SettingsManager.Load"
    description:
      zh: >
          从磁盘读取设置（含首次运行自动检测）。
          
      en: >
          Loads settings from disk (with first-run auto detect).
          
  - protocol: rpc
    path: "SettingsManager.Save"
    description:
      zh: >
          写盘并抛出设置事件。
          
      en: >
          Saves settings to disk and raises events.
          
  - protocol: rpc
    path: "SettingsManager.FilePath"
    description:
      zh: >
          settings.json 路径（persistentDataPath/Config）。
          
      en: >
          settings.json path under persistentDataPath.
          
deps:
  - kind: reference
    to: project-chaos.framework.settings.data
    from_api: "rpc:SettingsManager.Load"
    to_api: "rpc:SettingsData.Clone"
    label: {zh: "设置模型", en: "Settings model"}
  - kind: event
    to: project-chaos.framework.events.const-names
    from_api: "rpc:SettingsManager.Save"
    to_api: "rpc:EventConstName.SaveSetting"
    label: {zh: "设置已保存事件", en: "Settings saved"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:SettingsManager.Save"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "设置日志", en: "Settings logging"}
---
