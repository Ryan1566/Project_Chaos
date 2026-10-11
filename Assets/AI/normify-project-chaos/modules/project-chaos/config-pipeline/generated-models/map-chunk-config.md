---
uid: 9e0d0304
id: project-chaos.config-pipeline.generated-models.map-chunk-config
parent: project-chaos.config-pipeline.generated-models
tags: [generated, runmap]
name: {zh: "MapChunkConfig 类", en: "MapChunkConfig Class"}
description:
  zh: >
      生成的 MapChunkConfig 类：Id / biomeId / themeKey / kind / layer / prefabPath / weight / maxCount / isUnique / enable。字段含义见源表模块 config-pipeline.table-repo.map-chunk。**禁止手改**。
      
  en: >
      Generated MapChunkConfig class: Id / biomeId / themeKey / kind / layer / prefabPath / weight / maxCount / isUnique / enable. Field meanings live in the source table module (table-repo.map-chunk). Never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.694Z"
fingerprint: 42b81644c2207e177a95433510fab232229341e20051eb8a9e513bf44aa2fbb6
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapChunkConfig.cs"
    line: 1
    end_line: 11
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapChunkConfig.cs"
    description:
      zh: >
          生成的 MapChunkConfig 类（public 字段，JsonUtility 可序列化）。
          
      en: >
          Generated MapChunkConfig class (public fields, JsonUtility-serializable).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-chunk
    from_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/MapChunkConfig.cs"
    to_api: "file:Assets/Excel/Chaos_excel/MapChunk_房间块表.xlsx"
    label: {zh: "由该表生成", en: "Generated from"}
---
