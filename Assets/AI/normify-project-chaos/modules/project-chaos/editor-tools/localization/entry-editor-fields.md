---
uid: e0a10106
id: project-chaos.editor-tools.localization.entry-editor-fields
parent: project-chaos.editor-tools.localization
name: {zh: "条目字段与数据管理", en: "Entry Fields and Data Management"}
description:
  zh: >
      右栏条目编辑器：按语言展开的字段编辑、单条/全部清空、完整与缺失翻译计数，以及配置资产的新建/打开/保存与新增条目。
      
  en: >
      Right-panel entry editor: per-language field editing, clearing one entry or all entries, complete/missing translation counts, plus creating, opening and saving the config asset and adding new entries.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.737Z"
fingerprint: f632328e9b94a45e950b53ac46275b6cce6123211b4d3c7a24f45471997cfe6b
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationEditorWindow.cs"
    line: 398
    end_line: 679
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationEditorWindow.cs#L398-L679"
    description:
      zh: >
          字段编辑、清空与数据生命周期所在行段。
          
      en: >
          Line range holding field editing, clearing and asset lifecycle.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.DrawEntryEditor"
    description:
      zh: >
          绘制单条本地化条目的完整编辑区。
          
      en: >
          Draws the full editor for one localization entry.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.DrawLanguageField"
    description:
      zh: >
          绘制单语言字段与清空按钮。
          
      en: >
          Draws one language field with its clear button.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.ClearAllEntries"
    description:
      zh: >
          清空全部条目的当前语言内容。
          
      en: >
          Clears the current language text of all entries.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.SaveData"
    description:
      zh: >
          保存当前编辑的本地化配置资产。
          
      en: >
          Saves the currently edited localization config asset.
          
---
