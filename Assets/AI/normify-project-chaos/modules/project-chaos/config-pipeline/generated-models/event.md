---
uid: cf000303
id: project-chaos.config-pipeline.generated-models.event
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "剧情事件数据类", en: "Event Data Class"}
description:
  zh: >
      剧情事件与选项对应的 C# 数据类。
      
  en: >
      C# data classes for events and event options.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.434Z"
fingerprint: fd58a1e0d35999cd730550f1990d99ef21725524311a86db0026f1411c2dd6c7
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/EventTable_EventConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/EventTable_EventOptionConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/EventTable_EventConfig.cs"
    description:
      zh: >
          剧情事件数据类
          
      en: >
          Event data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.event
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
