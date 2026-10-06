---
uid: f1c0002f
id: project-chaos.framework.localization.manager-lookup
parent: project-chaos.framework.localization
name: {zh: "本地化取词", en: "Localization Lookup"}
description:
  zh: >
      本地化取词接口：GetText（支持格式化参数）、HasKey、SetLocalizationData、语言显示名与全语言列表。SetLocalizationData 会反向刷新场景内全部 LocalizedText（为避免依赖成环，该反向箭头只在描述里记录）。
      
  en: >
      Localization lookup API: GetText (with formatting), HasKey, SetLocalizationData, language display names and the full language list. SetLocalizationData also walks the scene and refreshes every LocalizedText (reverse edge omitted to keep the graph acyclic).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.748Z"
fingerprint: e3467c7f202c922325ea9bcf15be6ea006a25d272a717bf7c80efe6e63dd930c
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizationManager.cs"
    line: 131
    end_line: 246
apis:
  - protocol: rpc
    path: "LocalizationManager.GetText"
    description:
      zh: >
          按键取本地化文本。
          
      en: >
          Gets localized text by key.
          
  - protocol: rpc
    path: "LocalizationManager.HasKey"
    description:
      zh: >
          按键判断是否存在。
          
      en: >
          Checks whether a key exists.
          
  - protocol: rpc
    path: "LocalizationManager.SetLocalizationData"
    description:
      zh: >
          运行时替换本地化配置并刷新。
          
      en: >
          Replaces the localization data asset at runtime.
          
  - protocol: rpc
    path: "LocalizationManager.GetLanguageDisplayName"
    description:
      zh: >
          取语言的本地化显示名。
          
      en: >
          Localized language display name.
          
  - protocol: rpc
    path: "LocalizationManager.GetAllLanguages"
    description:
      zh: >
          返回全部支持的语言。
          
      en: >
          All supported languages.
          
deps:
  - kind: reference
    to: project-chaos.framework.localization.config
    from_api: "rpc:LocalizationManager.GetText"
    to_api: "rpc:LocalizationData.ContainsKey"
    label: {zh: "键存在性", en: "Key existence"}
---
