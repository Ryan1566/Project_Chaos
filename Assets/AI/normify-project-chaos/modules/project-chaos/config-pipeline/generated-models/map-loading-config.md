---
uid: 9e0d0305
id: project-chaos.config-pipeline.generated-models.map-loading-config
parent: project-chaos.config-pipeline.generated-models
tags: [generated, runmap, loading]
name: {zh: "MapLoadingConfig 类", en: "MapLoadingConfig Class"}
description:
  zh: >
      生成的 MapLoadingConfig 类：Id / stage / sort / descCN / descTW / descEN / descJP / descKR。字段含义见源表模块 config-pipeline.table-repo.map-loading。**禁止手改**。
      
  en: >
      Generated MapLoadingConfig class: Id / stage / sort / descCN / descTW / descEN / descJP / descKR. Field meanings live in the source table module (table-repo.map-loading). Never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.695Z"
fingerprint: c9282886b92b0c7751cf1ed9ac153da972a864938b831007a12dd57c99c3a510
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapLoadingConfig.cs"
    line: 1
    end_line: 12
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapLoadingConfig.cs"
    description:
      zh: >
          生成的 MapLoadingConfig 类（public 字段，JsonUtility 可序列化）。
          
      en: >
          Generated MapLoadingConfig class (public fields, JsonUtility-serializable).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-loading
    from_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/MapLoadingConfig.cs"
    to_api: "file:Assets/Excel/Chaos_excel/MapLoading_地图生成读条表.xlsx"
    label: {zh: "由该表生成", en: "Generated from"}
---
