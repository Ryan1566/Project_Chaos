---
uid: f1c00032
id: project-chaos.framework.localization.text-font
parent: project-chaos.framework.localization
name: {zh: "文本字体绑定", en: "Text Font Binding"}
description:
  zh: >
      LocalizedTextFont 订阅侧：挂接语言切换事件，暴露 Subscribed 标记与事件计数用于自检。
      
  en: >
      LocalizedTextFont subscription side: hooks the language-changed event, exposes Subscribed and an event counter for self-checking.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.749Z"
fingerprint: ba3dfe29dd1421029b0c897d584be144e708fcb47d10d36792b5b43a00c5b4cf
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizedTextFont.cs"
    line: 1
    end_line: 136
apis:
  - protocol: rpc
    path: "LocalizedTextFont.Subscribed"
    description:
      zh: >
          组件是否已订阅语言切换。
          
      en: >
          Whether the component is subscribed to language changes.
          
  - protocol: rpc
    path: "LocalizedTextFont.LanguageEventCount"
    description:
      zh: >
          语言切换事件计数（供自检）。
          
      en: >
          Language-change event counter (self-check).
          
  - protocol: rpc
    path: "LocalizedTextFont.applyOnLanguageChange"
    description:
      zh: >
          是否在语言切换时自动应用字体。
          
      en: >
          Whether the font is applied on language change.
          
deps:
  - kind: event
    to: project-chaos.framework.localization.manager
    from_api: "rpc:LocalizedTextFont.Subscribed"
    to_api: "rpc:LocalizationManager.OnLanguageChanged"
    label: {zh: "语言切换", en: "Language change"}
---
