---
uid: c05b1e82
id: project-chaos.assets.localization-assets.root-config
parent: project-chaos.assets.localization-assets
name: {zh: "本地化根据配置", en: "Localization Root Config"}
description:
  zh: >
      本地化根据配置与 LanguageFontMap（把每种语言绑定到一份 TMP 字体资产）。
      
  en: >
      Root localization config and the LanguageFontMap that binds each language to a TMP font asset.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:56:27.357Z"
fingerprint: e02bee6c932388e13b30f42821a000b92c8837eea58e4159e29520dfa1c70129
source:
  - path: "Assets/Data/Localization/ChaosLocalizationConfig.asset"
  - path: "Assets/Data/Localization/LanguageFontMap.asset"
apis:
  - protocol: file
    path: "Assets/Data/Localization/ChaosLocalizationConfig.asset"
    description:
      zh: >
          根据本地化配置（32KB），持有全部语言与键值表。
          
      en: >
          Root localization config (32 KB) holding all languages and key tables.
          
  - protocol: file
    path: "Assets/Data/Localization/LanguageFontMap.asset"
    description:
      zh: >
          语言 → TMP 字体资产映射表。
          
      en: >
          Maps each language to its TMP font asset.
          
deps:
  - kind: reference
    to: project-chaos.assets.localization-assets.sub-configs
    from_api: "file:Assets/Data/Localization/ChaosLocalizationConfig.asset"
    to_api: "file:Assets/Data/Localization/Sub_LD/SettingLocalizationConfig.asset"
    label: {zh: "聚合分面板子配置", en: "Aggregates sub configs"}
  - kind: reference
    to: project-chaos.assets.fonts.cn
    from_api: "file:Assets/Data/Localization/LanguageFontMap.asset"
    to_api: "file:Assets/Fonts/cn/MSYH SDF.asset"
    label: {zh: "语言到字体映射", en: "Language to font map"}
---
