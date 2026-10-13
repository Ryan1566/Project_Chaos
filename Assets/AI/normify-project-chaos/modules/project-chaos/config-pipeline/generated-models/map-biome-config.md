---
uid: 9e0d0303
id: project-chaos.config-pipeline.generated-models.map-biome-config
parent: project-chaos.config-pipeline.generated-models
tags: [generated, runmap]
name: {zh: "MapBiomeConfig 类", en: "MapBiomeConfig Class"}
description:
  zh: >
      生成的 MapBiomeConfig 类：Id / themeKey / nameCN / layer / retryMax / enable。字段含义见源表模块 config-pipeline.table-repo.map-biome。**禁止手改**。
      
  en: >
      Generated MapBiomeConfig class: Id / themeKey / nameCN / layer / retryMax / enable. Field meanings live in the source table module (table-repo.map-biome). Never hand-edit.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.403Z"
fingerprint: b8664235a1b6842786c65c33c0629c1290c8c2813587089bd577edbe646543c7
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapBiomeConfig.cs"
    line: 1
    end_line: 9
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MapBiomeConfig.cs"
    description:
      zh: >
          生成的 MapBiomeConfig 类（public 字段，JsonUtility 可序列化）。
          
      en: >
          Generated MapBiomeConfig class (public fields, JsonUtility-serializable).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.map-biome
    from_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/MapBiomeConfig.cs"
    to_api: "file:Assets/Excel/Chaos_excel/MapBiome_地图主题表.xlsx"
    label: {zh: "由该表生成", en: "Generated from"}
---
