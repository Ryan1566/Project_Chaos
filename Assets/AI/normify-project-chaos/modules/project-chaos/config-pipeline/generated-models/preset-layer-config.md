---
uid: 9e0d0307
id: project-chaos.config-pipeline.generated-models.preset-layer-config
parent: project-chaos.config-pipeline.generated-models
tags: [generated, runmap]
name: {zh: "PresetLayerConfig 类", en: "PresetLayerConfig Class"}
description:
  zh: >
      生成的 PresetLayerConfig 类：Id / presetId / layerOrder / layer / isMainPath / linkFromLayerOrder / allowDirect / linkMin / linkMax。字段含义见源表模块 config-pipeline.table-repo.preset-layer。**禁止手改**。
      
  en: >
      Generated PresetLayerConfig class: Id / presetId / layerOrder / layer / isMainPath / linkFromLayerOrder / allowDirect / linkMin / linkMax. Field meanings live in the source table module (table-repo.preset-layer). Never hand-edit.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.695Z"
fingerprint: 5b5ab26459c5892ee936b91c1dbe5bd0433c28ec4fa6f5452923b01c6886dbc7
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/PresetLayerConfig.cs"
    line: 1
    end_line: 11
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/PresetLayerConfig.cs"
    description:
      zh: >
          生成的 PresetLayerConfig 类（public 字段，JsonUtility 可序列化）。
          
      en: >
          Generated PresetLayerConfig class (public fields, JsonUtility-serializable).
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.preset-layer
    from_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/PresetLayerConfig.cs"
    to_api: "file:Assets/Excel/Chaos_excel/PresetLayer_原型图层序表.xlsx"
    label: {zh: "由该表生成", en: "Generated from"}
---
