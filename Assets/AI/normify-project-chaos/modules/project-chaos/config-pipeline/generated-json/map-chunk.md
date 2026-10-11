---
uid: 9e0d0204
id: project-chaos.config-pipeline.generated-json.map-chunk
parent: project-chaos.config-pipeline.generated-json
tags: [generated, runmap]
name: {zh: "MapChunk 导出 Json", en: "MapChunk JSON"}
description:
  zh: >
      MapChunk 的运行时导出 Json（Resources 路径 Data/Json/Runtime/MapChunkConfig，不带扩展名）。由 ExcelTool/ExportExcelConfigs 生成，**禁止手改**。
      
  en: >
      Generated runtime JSON for MapChunk (Resources path Data/Json/Runtime/MapChunkConfig, no extension). Produced by ExcelTool/ExportExcelConfigs — never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.692Z"
fingerprint: 1a984da99c660afa715bc6563d02531bb9c41d9f32cfa1a5c55ed49cb7bbf607
source:
  - path: "Assets/Resources/Data/Json/Runtime/MapChunkConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/MapChunkConfig.json"
    description:
      zh: >
          MapChunk 运行时 Json（Resources 路径 Data/Json/Runtime/MapChunkConfig）。
          
      en: >
          Runtime JSON of MapChunk (Resources path Data/Json/Runtime/MapChunkConfig).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-chunk
    from_api: "file:Assets/Resources/Data/Json/Runtime/MapChunkConfig.json"
    to_api: "file:Assets/Excel/Chaos_excel/MapChunk_房间块表.xlsx"
    label: {zh: "由该表导出", en: "Generated from"}
---
