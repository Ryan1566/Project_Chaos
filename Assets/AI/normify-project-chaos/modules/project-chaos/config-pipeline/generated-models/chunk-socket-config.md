---
uid: 9e0d0302
id: project-chaos.config-pipeline.generated-models.chunk-socket-config
parent: project-chaos.config-pipeline.generated-models
tags: [generated, runmap]
name: {zh: "ChunkSocketConfig 类", en: "ChunkSocketConfig Class"}
description:
  zh: >
      生成的 ChunkSocketConfig 类：Id / chunkId / dir / heightProfile / socketType / posX / posY。字段含义见源表模块 config-pipeline.table-repo.chunk-socket。**禁止手改**。
      
  en: >
      Generated ChunkSocketConfig class: Id / chunkId / dir / heightProfile / socketType / posX / posY. Field meanings live in the source table module (table-repo.chunk-socket). Never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.694Z"
fingerprint: c101cdb1db46b69521368a274dc2ea23924cb77d1cb70369faaf40fce9a5e489
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ChunkSocketConfig.cs"
    line: 1
    end_line: 10
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ChunkSocketConfig.cs"
    description:
      zh: >
          生成的 ChunkSocketConfig 类（public 字段，JsonUtility 可序列化）。
          
      en: >
          Generated ChunkSocketConfig class (public fields, JsonUtility-serializable).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.chunk-socket
    from_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/ChunkSocketConfig.cs"
    to_api: "file:Assets/Excel/Chaos_excel/ChunkSocket_房间块接口表.xlsx"
    label: {zh: "由该表生成", en: "Generated from"}
---
