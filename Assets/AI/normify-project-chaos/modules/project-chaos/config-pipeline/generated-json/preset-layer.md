---
uid: 9e0d0207
id: project-chaos.config-pipeline.generated-json.preset-layer
parent: project-chaos.config-pipeline.generated-json
tags: [generated, runmap]
name: {zh: "PresetLayer 导出 Json", en: "PresetLayer JSON"}
description:
  zh: >
      PresetLayer 的运行时导出 Json（Resources 路径 Data/Json/Runtime/PresetLayerConfig，不带扩展名）。由 ExcelTool/ExportExcelConfigs 生成，**禁止手改**。
      
  en: >
      Generated runtime JSON for PresetLayer (Resources path Data/Json/Runtime/PresetLayerConfig, no extension). Produced by ExcelTool/ExportExcelConfigs — never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.693Z"
fingerprint: 9535a37d8c1dff4e844d1ed4c2079747b82e1bb2f97dcc27462e1c328ebef106
source:
  - path: "Assets/Resources/Data/Json/Runtime/PresetLayerConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/PresetLayerConfig.json"
    description:
      zh: >
          PresetLayer 运行时 Json（Resources 路径 Data/Json/Runtime/PresetLayerConfig）。
          
      en: >
          Runtime JSON of PresetLayer (Resources path Data/Json/Runtime/PresetLayerConfig).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.preset-layer
    from_api: "file:Assets/Resources/Data/Json/Runtime/PresetLayerConfig.json"
    to_api: "file:Assets/Excel/Chaos_excel/PresetLayer_原型图层序表.xlsx"
    label: {zh: "由该表导出", en: "Generated from"}
---
