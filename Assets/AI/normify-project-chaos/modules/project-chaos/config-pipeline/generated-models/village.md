---
uid: cf00030e
id: project-chaos.config-pipeline.generated-models.village
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "村庄数据类", en: "Village Data Class"}
description:
  zh: >
      村庄场景布局与文本对应的 C# 数据类。
      
  en: >
      C# data classes for village scene layout and text.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.447Z"
fingerprint: f725fc259082a7eab1d9d06aec4d1ff74fcf46475d7ca22c5a388153c8f88e79
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/VillageTable_VillageSceneConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/VillageTable_VillageTextConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/VillageTable_VillageSceneConfig.cs"
    description:
      zh: >
          村庄数据类
          
      en: >
          Village data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.village
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
