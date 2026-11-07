---
uid: cf000309
id: project-chaos.config-pipeline.generated-models.progress
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "时代进度数据类", en: "Progress Data Class"}
description:
  zh: >
      时代与深度推进对应的 C# 数据类。
      
  en: >
      C# data classes for era and depth progression.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.441Z"
fingerprint: c04ebed3b74b2556b7898ba04395ff24ad77caffa236453d66eee59b4f8aeb57
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ProgressTable_EraConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/ProgressTable_DepthConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/ProgressTable_EraConfig.cs"
    description:
      zh: >
          时代进度数据类
          
      en: >
          Progress data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.progress
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
