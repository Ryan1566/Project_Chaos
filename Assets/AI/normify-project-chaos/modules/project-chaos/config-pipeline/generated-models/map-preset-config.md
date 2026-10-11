---
uid: 9e0d0306
id: project-chaos.config-pipeline.generated-models.map-preset-config
parent: project-chaos.config-pipeline.generated-models
tags: [generated, runmap]
name: {zh: "MapPresetConfig 类", en: "MapPresetConfig Class"}
description:
  zh: >
      生成的 MapPresetConfig 类：Id / biomeId / nameCN / mainPathMin、Max / branchMin、Max / totalMin、Max / specialTreasure / specialCamp / retryMax / enable。字段含义见源表模块 config-pipeline.table-repo.map-preset。**禁止手改**。
      
  en: >
      Generated MapPresetConfig class: Id / biomeId / nameCN / mainPathMin-Max / branchMin-Max / totalMin-Max / specialTreasure / specialCamp / retryMax / enable. Field meanings live in the source table module (table-repo.map-preset). Never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.695Z"
fingerprint: ab8cad15a0cb00b37a96a2586e62e2c3568d5cd3a8b45197e9d717cd54f583ae
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapPresetConfig.cs"
    line: 1
    end_line: 13
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapPresetConfig.cs"
    description:
      zh: >
          生成的 MapPresetConfig 类（public 字段，JsonUtility 可序列化）。
          
      en: >
          Generated MapPresetConfig class (public fields, JsonUtility-serializable).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-preset
    from_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/MapPresetConfig.cs"
    to_api: "file:Assets/Excel/Chaos_excel/MapPreset_原型图主表.xlsx"
    label: {zh: "由该表生成", en: "Generated from"}
---
