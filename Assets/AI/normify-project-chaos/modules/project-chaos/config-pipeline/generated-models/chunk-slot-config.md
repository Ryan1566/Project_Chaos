---
uid: 9e0d0301
id: project-chaos.config-pipeline.generated-models.chunk-slot-config
parent: project-chaos.config-pipeline.generated-models
tags: [generated, runmap]
name: {zh: "ChunkSlotConfig 类", en: "ChunkSlotConfig Class"}
description:
  zh: >
      生成的 ChunkSlotConfig 类：Id / chunkId / slotType / posX / posY。字段含义见源表模块 config-pipeline.table-repo.chunk-slot。**禁止手改**。
      
  en: >
      Generated ChunkSlotConfig class: Id / chunkId / slotType / posX / posY. Field meanings live in the source table module (table-repo.chunk-slot). Never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.694Z"
fingerprint: 49c02c97721c966186e5144f4c3309e1f886bfd986812c6f0b1fa2c388c48107
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ChunkSlotConfig.cs"
    line: 1
    end_line: 9
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ChunkSlotConfig.cs"
    description:
      zh: >
          生成的 ChunkSlotConfig 类（public 字段，JsonUtility 可序列化）。
          
      en: >
          Generated ChunkSlotConfig class (public fields, JsonUtility-serializable).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.chunk-slot
    from_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/ChunkSlotConfig.cs"
    to_api: "file:Assets/Excel/Chaos_excel/ChunkSlot_房间块标记点表.xlsx"
    label: {zh: "由该表生成", en: "Generated from"}
---
