---
uid: f1c0002e
id: project-chaos.framework.localization.manager
parent: project-chaos.framework.localization
name: {zh: "本地化管理器", en: "Localization Manager"}
description:
  zh: >
      LocalizationManager 初始化与切换：读取已保存语言、构建文本缓存、ChangeLanguage 切语言并广播 OnLanguageChanged。初始化时反向读取 SettingsManager.Applied.language（为避免依赖成环未记为箭头）。
      
  en: >
      LocalizationManager init and switching: loads saved language, builds the text cache, ChangeLanguage and the OnLanguageChanged UnityEvent.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:34.939Z"
fingerprint: 9870cb654836593ff5c627442c6dd42ac4350f510b348e8848310fb0dd542d13
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizationManager.cs"
    line: 1
    end_line: 130
apis:
  - protocol: rpc
    path: "LocalizationManager.CurrentLanguage"
    description:
      zh: >
          当前语言。
          
      en: >
          Current language.
          
  - protocol: rpc
    path: "LocalizationManager.ChangeLanguage"
    description:
      zh: >
          切换语言、重建缓存并刷新全部文本。
          
      en: >
          Switches language, rebuilds cache and refreshes all texts.
          
  - protocol: rpc
    path: "LocalizationManager.OnLanguageChanged"
    description:
      zh: >
          语言切换完成后抛出的事件。
          
      en: >
          Event raised after the language changed.
          
deps:
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:LocalizationManager.ChangeLanguage"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "本地化日志", en: "Localization logging"}
  - kind: reference
    to: project-chaos.framework.localization.config
    from_api: "rpc:LocalizationManager.ChangeLanguage"
    to_api: "rpc:LocalizationData.GetAllKeys"
    label: {zh: "配置键列表", en: "Config keys"}
---
