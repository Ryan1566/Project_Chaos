---
uid: cf000302
id: project-chaos.config-pipeline.generated-models.effect
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "效果数据类", en: "Effect Data Class"}
description:
  zh: >
      效果表对应的 C# 数据类。
      
  en: >
      C# data class for the effect table.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.433Z"
fingerprint: 85730863e48214789548721790f33e0f6f8bf19d6cce445dff1033f41729313b
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/EffectTable_EffectConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/EffectTable_EffectConfig.cs"
    description:
      zh: >
          效果数据类
          
      en: >
          Effect data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.effect
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
