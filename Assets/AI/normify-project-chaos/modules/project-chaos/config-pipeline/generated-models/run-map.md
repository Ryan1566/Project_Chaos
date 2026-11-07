---
uid: cf00030c
id: project-chaos.config-pipeline.generated-models.run-map
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "地图生成数据类", en: "Map Generation Data Class"}
description:
  zh: >
      地图生成 8 张子表对应的 C# 数据类（含新增 MapGeneration 与 ExplorationScene）。
      
  en: >
      C# data classes for map generation, including the new MapGeneration and ExplorationScene sheets.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.445Z"
fingerprint: 10e2918ac48de5a1944970590cfe247b877f2a9ed0f23382d702239bee679be4
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_MapGenerationConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_ExplorationSceneConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_MapChunkConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_ChunkSlotConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_ChunkSocketConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_MapBiomeConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_MapPresetConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_PresetLayerConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/RunMapTable_MapGenerationConfig.cs"
    description:
      zh: >
          地图生成数据类
          
      en: >
          Map generation data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.run-map
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
