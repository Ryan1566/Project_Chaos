---
uid: cf00030f
id: project-chaos.config-pipeline.generated-models.world-location
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "世界地点数据类", en: "World Location Data Class"}
description:
  zh: >
      世界地点及其交互、时代分布、关系、临时状态与文案对应的 C# 数据类。
      
  en: >
      C# data classes for world locations, actions, era placement, relations, temporary states and text.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.450Z"
fingerprint: f88d43f554be9984106cdf44b5fec70af40c7a1e467766d8c26017ed376a9fde
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldLocationTable_LocationConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldLocationTable_LocationActionConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldLocationTable_LocationEraConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldLocationTable_LocationRelationConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldLocationTable_LocationTemporaryConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldLocationTable_LocationTextConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldLocationTable_LocationConfig.cs"
    description:
      zh: >
          世界地点数据类
          
      en: >
          World location data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.world-location
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
