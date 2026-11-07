---
uid: cf000310
id: project-chaos.config-pipeline.generated-models.world-map
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "世界观地图数据类", en: "World Map Data Class"}
description:
  zh: >
      世界观地图本体、势力范围与标签对应的 C# 数据类。
      
  en: >
      C# data classes for the world map layout, faction areas and labels.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.451Z"
fingerprint: 01376c14bb44970cb443380fdf985c78622fcdbb6086bb31c938fb3e8b0b09c1
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldMapTable_WorldMapConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldMapTable_WorldFactionConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldMapTable_WorldFactionLabelConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/WorldMapTable_WorldMapConfig.cs"
    description:
      zh: >
          世界观地图数据类
          
      en: >
          World map data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.world-map
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
