---
uid: f1c0002c
id: project-chaos.framework.localization.entry
parent: project-chaos.framework.localization
name: {zh: "语言与本地化条目", en: "Language & Entry"}
description:
  zh: >
      LanguageType 枚举（简中/繁中/英/日/韩）与 LocalizationEntry 条目：Key、备注、五种语言文本及按语言读写。
      
  en: >
      LanguageType enum (5 languages) and LocalizationEntry: key, description and per-language texts with GetText/SetText.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.747Z"
fingerprint: a43836ec9d7eccc09086bde991db83bfcaa9da7c3997ca5e93cad03b191ad019
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizationData.cs"
    line: 1
    end_line: 155
apis:
  - protocol: rpc
    path: "LocalizationEntry.GetText"
    description:
      zh: >
          取某条目指定语言的文本。
          
      en: >
          Gets the text of one entry in a language.
          
  - protocol: rpc
    path: "LocalizationEntry.SetText"
    description:
      zh: >
          设置某条目指定语言的文本。
          
      en: >
          Sets the text of one entry in a language.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizationData.cs#L1-L155"
    description:
      zh: >
          LanguageType 枚举与 LocalizationEntry 条目数据结构段。
          
      en: >
          LanguageType enum and entry data block.
          
---
