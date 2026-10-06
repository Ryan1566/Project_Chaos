---
uid: f1c00033
id: project-chaos.framework.localization.text-font-apply
parent: project-chaos.framework.localization
name: {zh: "字体应用", en: "Font Application"}
description:
  zh: >
      字体应用：解析映射表（组件指定/生效表/Resources 回退）、切换 TMP 字体资源，并对缺项给出告警与自检。
      
  en: >
      Font application: resolves the map (component/active/Resources fallback), switches TMP font assets and validates the map with warnings.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.749Z"
fingerprint: ba3dfe29dd1421029b0c897d584be144e708fcb47d10d36792b5b43a00c5b4cf
source:
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizedTextFont.cs"
    line: 137
    end_line: 264
apis:
  - protocol: rpc
    path: "LocalizedTextFont.ApplyFont"
    description:
      zh: >
          按当前语言应用字体。
          
      en: >
          Applies the font for the current language.
          
  - protocol: rpc
    path: "LocalizedTextFont.ForceApply"
    description:
      zh: >
          强制重新应用字体。
          
      en: >
          Forces a font re-apply.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizedTextFont.cs#L137-L264"
    description:
      zh: >
          字体解析、切换与自检实现段。
          
      en: >
          Font resolution, switching and self-check block.
          
deps:
  - kind: call
    to: project-chaos.framework.localization.font-map
    from_api: "rpc:LocalizedTextFont.ApplyFont"
    to_api: "rpc:LanguageFontMap.GetFont"
    label: {zh: "取语言字体", en: "Language font"}
  - kind: reference
    to: project-chaos.framework.localization.localized-text
    from_api: "rpc:LocalizedTextFont.ApplyFont"
    to_api: "rpc:LocalizedText.UpdateText"
    label: {zh: "文本组件", en: "Text component"}
---
