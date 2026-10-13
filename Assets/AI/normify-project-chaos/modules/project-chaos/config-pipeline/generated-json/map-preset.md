---
uid: 9e0d0206
id: project-chaos.config-pipeline.generated-json.map-preset
parent: project-chaos.config-pipeline.generated-json
tags: [generated, runmap]
name: {zh: "MapPreset 导出 Json", en: "MapPreset JSON"}
description:
  zh: >
      MapPreset 的运行时导出 Json（Resources 路径 Data/Json/Runtime/MapPresetConfig，不带扩展名）。由 ExcelTool/ExportExcelConfigs 生成，**禁止手改**。
      
  en: >
      Generated runtime JSON for MapPreset (Resources path Data/Json/Runtime/MapPresetConfig, no extension). Produced by ExcelTool/ExportExcelConfigs — never hand-edit.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.401Z"
fingerprint: 35750dc39f273257d12785ace8b390af6e9094eea8bcafbe16b7d125832ca1d0
source:
  - path: "Assets/Resources/Data/Json/Runtime/MapPresetConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/MapPresetConfig.json"
    description:
      zh: >
          MapPreset 运行时 Json（Resources 路径 Data/Json/Runtime/MapPresetConfig）。
          
      en: >
          Runtime JSON of MapPreset (Resources path Data/Json/Runtime/MapPresetConfig).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-preset
    from_api: "file:Assets/Resources/Data/Json/Runtime/MapPresetConfig.json"
    to_api: "file:Assets/Excel/Chaos_excel/MapPreset_原型图主表.xlsx"
    label: {zh: "由该表导出", en: "Generated from"}
---
