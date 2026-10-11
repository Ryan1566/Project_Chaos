---
uid: 9e0d0202
id: project-chaos.config-pipeline.generated-json.chunk-socket
parent: project-chaos.config-pipeline.generated-json
tags: [generated, runmap]
name: {zh: "ChunkSocket 导出 Json", en: "ChunkSocket JSON"}
description:
  zh: >
      ChunkSocket 的运行时导出 Json（Resources 路径 Data/Json/Runtime/ChunkSocketConfig，不带扩展名）。由 ExcelTool/ExportExcelConfigs 生成，**禁止手改**。
      
  en: >
      Generated runtime JSON for ChunkSocket (Resources path Data/Json/Runtime/ChunkSocketConfig, no extension). Produced by ExcelTool/ExportExcelConfigs — never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.692Z"
fingerprint: c3c07a860c433ecb77387596c2c2bf07561ad4940364476e37f3ab2c03b6d1fd
source:
  - path: "Assets/Resources/Data/Json/Runtime/ChunkSocketConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/ChunkSocketConfig.json"
    description:
      zh: >
          ChunkSocket 运行时 Json（Resources 路径 Data/Json/Runtime/ChunkSocketConfig）。
          
      en: >
          Runtime JSON of ChunkSocket (Resources path Data/Json/Runtime/ChunkSocketConfig).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.chunk-socket
    from_api: "file:Assets/Resources/Data/Json/Runtime/ChunkSocketConfig.json"
    to_api: "file:Assets/Excel/Chaos_excel/ChunkSocket_房间块接口表.xlsx"
    label: {zh: "由该表导出", en: "Generated from"}
---
