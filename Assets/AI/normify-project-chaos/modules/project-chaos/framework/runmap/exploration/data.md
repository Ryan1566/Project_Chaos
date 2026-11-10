---
uid: b2c00101
id: project-chaos.framework.runmap.exploration.data
parent: project-chaos.framework.runmap.exploration
tags: [runmap, exploration]
name: {zh: "探索层：数据模型", en: "Exploration: Data Model"}
description:
  zh: >
      探索地图的数据模型：NaturalMapPlan v3，包含地点、通道、梯子、目标区域以及逐格区域/空间数组。
      
  en: >
      Data model of the exploration map: NaturalMapPlan v3 with places, links, ladders, objective regions and per-cell region/space arrays.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.919Z"
fingerprint: 9483ca849aabb4f631a0af7d97731dd101612a1fde9596f75cd6d69f910fb12f
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/ExplorationMapData.cs"
apis:
  - protocol: rpc
    path: "RunMap.NaturalMapPlan"
    description:
      zh: >
          一张生成地图的地点/通道/梯子图
          
      en: >
          Place/link/ladder graph of one generated map
          
  - protocol: rpc
    path: "RunMap.NaturalMapPlan.IsObjectiveRegion"
    description:
      zh: >
          区域是否计入目标区
          
      en: >
          Whether a region counts as objective
          
  - protocol: rpc
    path: "RunMap.MapLadder"
    description:
      zh: >
          可攀爬梯子几何
          
      en: >
          Climbable ladder geometry
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/GameBase/RunMap/ExplorationMapData.cs"
    description:
      zh: >
          探索地图数据模型
          
      en: >
          Exploration map data model
          
deps:
  - kind: reference
    to: project-chaos.config-pipeline.generated-models.run-map
    label: {zh: "探索场景配置表", en: "Exploration scene sheet"}
---
