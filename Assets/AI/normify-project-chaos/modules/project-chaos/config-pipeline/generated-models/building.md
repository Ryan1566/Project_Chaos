---
uid: cf000301
id: project-chaos.config-pipeline.generated-models.building
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "建筑数据类", en: "Building Data Class"}
description:
  zh: >
      建筑表对应的 C# 数据类。
      
  en: >
      C# data class for the building table.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.433Z"
fingerprint: 27bf1d3c9342bfeff442a3098bd43dd22792cef43c5f761169f6ad096676c91d
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/BuildingTable_BuildingConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/BuildingTable_BuildingConfig.cs"
    description:
      zh: >
          建筑数据类
          
      en: >
          Building data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.building
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
