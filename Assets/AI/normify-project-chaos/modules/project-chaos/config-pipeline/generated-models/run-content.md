---
uid: cf00030b
id: project-chaos.config-pipeline.generated-models.run-content
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "局内内容数据类", en: "Run Content Data Class"}
description:
  zh: >
      局内行为与交互对应的 C# 数据类。
      
  en: >
      C# data classes for in-run behaviors and interactions.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.442Z"
fingerprint: bffda668d9e80bdb5da5c49d6d839457913acb7f505792f149cb6eab09504ef9
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunContentTable_RunBehaviorConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunContentTable_RunInteractionConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/RunContentTable_RunBehaviorConfig.cs"
    description:
      zh: >
          局内内容数据类
          
      en: >
          Run content data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.run-content
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
