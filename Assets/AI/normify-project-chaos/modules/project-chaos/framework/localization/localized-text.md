---
uid: f1c00030
id: project-chaos.framework.localization.localized-text
parent: project-chaos.framework.localization
name: {zh: "本地化文本组件", en: "LocalizedText Component"}
description:
  zh: >
      LocalizedText 组件：按 Key 刷新 TMP 文本，支持运行时换 Key、格式化参数与语言切换自动更新。
      
  en: >
      LocalizedText component: refreshes TMP text from the key, supports runtime key switching with format args and language-change subscription.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.748Z"
fingerprint: ac4bb43478a47a948649b4df647ba2bd4d1c308e32b10d5a56ebabd829609ccf
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizedText.cs"
    line: 1
    end_line: 185
apis:
  - protocol: rpc
    path: "LocalizedText.UpdateText"
    description:
      zh: >
          按当前语言刷新文本。
          
      en: >
          Refreshes the text from the current language.
          
  - protocol: rpc
    path: "LocalizedText.SetKey"
    description:
      zh: >
          运行时切换本地化 Key（可带格式参数）。
          
      en: >
          Switches the localization key (with format args).
          
  - protocol: rpc
    path: "LocalizedText.SetFormatArgs"
    description:
      zh: >
          设置格式化参数。
          
      en: >
          Sets the format arguments.
          
  - protocol: rpc
    path: "LocalizedText.GetCurrentText"
    description:
      zh: >
          取当前显示的文本。
          
      en: >
          Gets the text currently displayed.
          
deps:
  - kind: call
    to: project-chaos.framework.localization.manager-lookup
    from_api: "rpc:LocalizedText.UpdateText"
    to_api: "rpc:LocalizationManager.GetText"
    label: {zh: "取本地化文本", en: "Fetch text"}
  - kind: event
    to: project-chaos.framework.localization.manager
    from_api: "rpc:LocalizedText.UpdateText"
    to_api: "rpc:LocalizationManager.OnLanguageChanged"
    label: {zh: "语言切换", en: "Language change"}
---
