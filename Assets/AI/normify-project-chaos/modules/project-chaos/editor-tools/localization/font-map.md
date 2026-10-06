---
uid: e0a10109
id: project-chaos.editor-tools.localization.font-map
parent: project-chaos.editor-tools.localization
name: {zh: "语言字体映射窗口", en: "Language Font Map Window"}
description:
  zh: >
      为每种语言指定 TMP 字体资产并核对覆盖率：按语言探测字符集、报告字体缺失字形，然后把映射批量套到 UI 预制体上。注意 LanguageFontMap.asset 在 Assets/Data 下（不在 Resources），运行时只能靠组件引用。
      
  en: >
      Assigns a TMP font asset per language and checks coverage: probes per-language characters, reports missing glyphs, then patches UI prefabs in bulk. Note LanguageFontMap.asset lives under Assets/Data (not Resources), so runtime only reaches it through component references.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.737Z"
fingerprint: 0d4e42ca459326da79fb1c10d835b5a9207048081e7fa9c435f1e4d38bf4a2b1
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/LanguageFontMapWindow.cs"
    line: 1
    end_line: 330
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/LanguageFontMapWindow.cs#L1-L330"
    description:
      zh: >
          字体映射窗口与覆盖率探测所在文件。
          
      en: >
          The font map window and coverage probing.
          
  - protocol: rpc
    path: "Menu/Tools/本地化/语言字体映射"
    description:
      zh: >
          打开语言字体映射窗口的菜单项。
          
      en: >
          Menu item opening the language font map window.
          
  - protocol: rpc
    path: "LanguageFontMapWindow.RefreshFonts"
    description:
      zh: >
          重新收集工程内可用的 TMP 字体资产。
          
      en: >
          Re-collects available TMP font assets in the project.
          
  - protocol: rpc
    path: "LanguageFontMapWindow.DescribeCoverage"
    description:
      zh: >
          统计某字体对某语言字符集的覆盖情况。
          
      en: >
          Reports a font's coverage over a language character set.
          
  - protocol: rpc
    path: "LanguageFontMapWindow.PatchAllPrefabs"
    description:
      zh: >
          把字体映射批量应用到 UI 预制体。
          
      en: >
          Applies the font map to UI prefabs in bulk.
          
---
