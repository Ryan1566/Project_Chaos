---
uid: cf00030a
id: project-chaos.config-pipeline.generated-models.retainer
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "门客数据类", en: "Retainer Data Class"}
description:
  zh: >
      门客属性与成长对应的 C# 数据类。
      
  en: >
      C# data class for retainer stats and growth.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.441Z"
fingerprint: 2f253749356e2ad13f2c76b3e72cc92f606fd8c11053b730d66fbe68957d6370
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RetainerTable_RetainerConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/RetainerTable_RetainerConfig.cs"
    description:
      zh: >
          门客数据类
          
      en: >
          Retainer data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.retainer
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
