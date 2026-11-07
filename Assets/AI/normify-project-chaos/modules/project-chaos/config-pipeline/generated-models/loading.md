---
uid: cf000305
id: project-chaos.config-pipeline.generated-models.loading
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "读条数据类", en: "Loading Data Class"}
description:
  zh: >
      加载文案与地图生成读条对应的 C# 数据类。
      
  en: >
      C# data classes for loading copy and map-loading segments.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.436Z"
fingerprint: 3cb6cfb797a0463fb405105976afea6ade87fb3c588488c4d1722af3c0d716f3
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/LoadingTable_LoadingConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/LoadingTable_MapLoadingConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/LoadingTable_LoadingConfig.cs"
    description:
      zh: >
          读条数据类
          
      en: >
          Loading data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.loading
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
