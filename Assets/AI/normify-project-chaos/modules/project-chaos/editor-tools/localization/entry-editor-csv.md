---
uid: e0a10107
id: project-chaos.editor-tools.localization.entry-editor-csv
parent: project-chaos.editor-tools.localization
name: {zh: "本地化 CSV 导入导出", en: "Localization CSV Import/Export"}
description:
  zh: >
      按 7 列固定表头（Key,Description,ChineseSimplified,ChineseTraditional,English,Japanese,Korean）导出/导入本地化 CSV：引号包裹与转义、逐行解析、按 Key 更新或新增条目。导出默认目录为 Assets/Excel/Chaos_localization_excel。
      
  en: >
      Exports and imports localization CSV with the fixed 7-column header (Key, Description, zh-Hans, zh-Hant, English, Japanese, Korean): quote escaping, per-line parsing, and updating or adding entries by key. The default export folder is Assets/Excel/Chaos_localization_excel.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.737Z"
fingerprint: f632328e9b94a45e950b53ac46275b6cce6123211b4d3c7a24f45471997cfe6b
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationEditorWindow.cs"
    line: 680
    end_line: 898
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationEditorWindow.cs#L680-L898"
    description:
      zh: >
          CSV 导出/导入与解析转义所在行段。
          
      en: >
          Line range holding CSV export/import, parsing and escaping.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.ExportToCSV"
    description:
      zh: >
          把当前配置导出为 7 列 UTF-8 CSV。
          
      en: >
          Exports the current config as a 7-column UTF-8 CSV.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.ImportFromCSV"
    description:
      zh: >
          从 CSV 读入并按 Key 新增或更新条目。
          
      en: >
          Reads a CSV and adds or updates entries by key.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.ParseCSVLine"
    description:
      zh: >
          解析含引号转义的 CSV 行。
          
      en: >
          Parses one CSV line with quoted-field escapes.
          
  - protocol: rpc
    path: "LocalizationEditorWindow.EscapeCSV"
    description:
      zh: >
          转义单元格内的引号，避免写坏 CSV。
          
      en: >
          Escapes quotes inside a cell so the CSV stays valid.
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.localization-sheets
    from_api: "rpc:LocalizationEditorWindow.ExportToCSV"
    label: {zh: "CSV 落盘到表目录", en: "CSV lands in sheet folder"}
---
