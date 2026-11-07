---
uid: b2c00104
id: project-chaos.framework.runmap.exploration.presentation
parent: project-chaos.framework.runmap.exploration
tags: [runmap, exploration, render]
name: {zh: "探索层：视觉表现", en: "Exploration: Presentation"}
description:
  zh: >
      探索地图的批网格表现：用 Graphics 缓冲绘制地点、通道、梯子与目标区域，并提供 RunObjectiveMarker 交互物。
      
  en: >
      Batch mesh presentation of the exploration map: places, corridors, ladders and objective regions drawn with Graphics buffers, plus the RunObjectiveMarker interactable.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:54:15.832Z"
fingerprint: b58836937b20fb485ddbbe95cc5227dc9091fcf89361f3b159ee440cd998a22a
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/ExplorationMapPresentation.cs"
apis:
  - protocol: rpc
    path: "RunMap.ExplorationMapPresentation.Build"
    description:
      zh: >
          构建探索地图表现
          
      en: >
          Build the exploration map visuals
          
  - protocol: rpc
    path: "RunMap.RunObjectiveMarker"
    description:
      zh: >
          目标区域标记
          
      en: >
          Objective region marker
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/GameBase/RunMap/ExplorationMapPresentation.cs"
    description:
      zh: >
          探索地图表现层
          
      en: >
          Exploration map presentation
          
deps:
  - kind: reference
    to: project-chaos.framework.runmap.exploration.data
    label: {zh: "绘制探索图", en: "Draws exploration plan"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.interaction
    label: {zh: "生成目标标记", en: "Spawns objective markers"}
---
