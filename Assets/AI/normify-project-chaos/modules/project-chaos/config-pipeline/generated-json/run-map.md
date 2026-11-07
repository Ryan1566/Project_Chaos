---
uid: cf00020c
id: project-chaos.config-pipeline.generated-json.run-map
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "地图生成 Json", en: "Map Generation JSON"}
description:
  zh: >
      地图生成相关 8 张子表的运行时 Json（含新增 MapGeneration 与 ExplorationScene）。
      
  en: >
      Runtime JSON for map generation: chunks, biomes, presets, plus the new MapGeneration and ExplorationScene sheets.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.422Z"
fingerprint: 23d7ad8ef951811a12d922cdec62c377751d2ebc486b7ea8b0275eabcddda664
source:
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_MapGeneration.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_ExplorationScene.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_MapChunk.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_ChunkSlot.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_ChunkSocket.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_MapBiome.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_MapPreset.json"
  - path: "Assets/Resources/Data/Json/Runtime/RunMapTable_PresetLayer.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/RunMapTable_MapGeneration.json"
    description:
      zh: >
          地图生成运行时 Json
          
      en: >
          Map generation runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.run-map
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
