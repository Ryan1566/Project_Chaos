---
uid: cf000311
id: project-chaos.config-pipeline.generated-models.world-resource
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "资源经营数据类", en: "World Resource Data Class"}
description:
  zh: >
      资源产地与产出对应的 C# 数据类。
      
  en: >
      C# data classes for resource facilities and outputs.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.452Z"
fingerprint: 4f913f08b86f4cbfdaf54ba1fecf25a7375178787680556b2c1fa51b9c7ecce9
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldResourceTable_ResourceFacilityConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldResourceTable_ResourceOutputConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldResourceTable_ResourceFacilityConfig.cs"
    description:
      zh: >
          资源经营数据类
          
      en: >
          World resource data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.world-resource
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
