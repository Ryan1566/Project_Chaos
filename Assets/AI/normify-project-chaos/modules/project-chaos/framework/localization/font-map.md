---
uid: f1c00031
id: project-chaos.framework.localization.font-map
parent: project-chaos.framework.localization
name: {zh: "语言字体映射", en: "Language Font Map"}
description:
  zh: >
      LanguageFontMap 配置资产：语言→TMP 字体映射，含生效表缓存、按语言取字体与条目补全。
      
  en: >
      LanguageFontMap asset: per-language TMP font assets with active-map caching, lookup and entry completion.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.747Z"
fingerprint: 4058671362204296f8215354832fe0c555b68e8a7c185de5329c6c41cc4c3914
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LanguageFontMap.cs"
    line: 1
    end_line: 121
apis:
  - protocol: rpc
    path: "LanguageFontMap.Active"
    description:
      zh: >
          当前生效的语言字体映射表。
          
      en: >
          Currently active font map.
          
  - protocol: rpc
    path: "LanguageFontMap.SetActive"
    description:
      zh: >
          设置生效映射表（含回退加载）。
          
      en: >
          Sets the active map (fallback loading).
          
  - protocol: rpc
    path: "LanguageFontMap.GetFont"
    description:
      zh: >
          取某语言对应的 TMP 字体资源。
          
      en: >
          Gets the TMP font asset for a language.
          
  - protocol: rpc
    path: "LanguageFontMap.EnsureAllLanguages"
    description:
      zh: >
          补全所有语言的映射项。
          
      en: >
          Ensures every language has an entry.
          
---
