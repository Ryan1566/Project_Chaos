---
uid: b2c00103
id: project-chaos.framework.runmap.exploration.service
parent: project-chaos.framework.runmap.exploration
tags: [runmap, exploration, fog]
name: {zh: "探索层：运行时服务", en: "Exploration: Runtime Service"}
description:
  zh: >
      探索图与迷雾的运行时持有者：按视野半径逐帧刷新、揭开迷雾格并触发 MapChanged；配套 Driver 组件负责场景存活。
      
  en: >
      Runtime owner of the exploration plan and fog: ticks the vision radius, reveals fog cells and raises MapChanged; the driver component keeps it alive in the scene.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:54:15.832Z"
fingerprint: 513ebaedea06b0c530471059c07edba4dc581757916c7a55efb95f756539f4e6
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapExplorationService.cs"
apis:
  - protocol: rpc
    path: "RunMap.RunMapExplorationService.Tick"
    description:
      zh: >
          逐帧探索刷新
          
      en: >
          Per-frame exploration tick
          
  - protocol: rpc
    path: "RunMap.RunMapExplorationService.Clear"
    description:
      zh: >
          重置探索状态
          
      en: >
          Reset exploration state
          
  - protocol: rpc
    path: "RunMap.RunMapExplorationService.VisionRadius"
    description:
      zh: >
          当前视野半径
          
      en: >
          Current vision radius
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/GameBase/RunMap/RunMapExplorationService.cs"
    description:
      zh: >
          探索运行时服务
          
      en: >
          Exploration runtime service
          
deps:
  - kind: reference
    to: project-chaos.framework.runmap.exploration.data
    label: {zh: "读探索图数据", en: "Reads exploration plan"}
  - kind: call
    to: project-chaos.framework.runmap.assembler
    label: {zh: "刷新迷雾", en: "Reveals fog cells"}
---
