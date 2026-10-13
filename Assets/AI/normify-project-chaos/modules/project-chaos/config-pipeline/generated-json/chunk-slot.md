---
uid: 9e0d0201
id: project-chaos.config-pipeline.generated-json.chunk-slot
parent: project-chaos.config-pipeline.generated-json
tags: [generated, runmap]
name: {zh: "ChunkSlot 导出 Json", en: "ChunkSlot JSON"}
description:
  zh: >
      ChunkSlot 的运行时导出 Json（Resources 路径 Data/Json/Runtime/ChunkSlotConfig，不带扩展名）。由 ExcelTool/ExportExcelConfigs 生成，**禁止手改**。
      
  en: >
      Generated runtime JSON for ChunkSlot (Resources path Data/Json/Runtime/ChunkSlotConfig, no extension). Produced by ExcelTool/ExportExcelConfigs — never hand-edit.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.400Z"
fingerprint: 558d90a7f18d83565a6cfc080abfa8bc8f1b4b6b5dbe8ff29c6984b919948c6c
source:
  - path: "Assets/Resources/Data/Json/Runtime/ChunkSlotConfig.json"
apis:
  - protocol: file
    path: "Assets/Resources/Data/Json/Runtime/ChunkSlotConfig.json"
    description:
      zh: >
          ChunkSlot 运行时 Json（Resources 路径 Data/Json/Runtime/ChunkSlotConfig）。
          
      en: >
          Runtime JSON of ChunkSlot (Resources path Data/Json/Runtime/ChunkSlotConfig).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.chunk-slot
    from_api: "file:Assets/Resources/Data/Json/Runtime/ChunkSlotConfig.json"
    to_api: "file:Assets/Excel/Chaos_excel/ChunkSlot_房间块标记点表.xlsx"
    label: {zh: "由该表导出", en: "Generated from"}
---
