---
uid: b2c00102
id: project-chaos.framework.runmap.exploration.generator
parent: project-chaos.framework.runmap.exploration
tags: [runmap, exploration]
name: {zh: "探索层：生成器", en: "Exploration: Generator"}
description:
  zh: >
      生成 v3 探索地图：按双环/分层分支/汇聚三种布局摆地点与通道，插入梯子，再校验可达性与完成度覆盖。
      
  en: >
      Builds the v3 exploration map: lays out places and corridors in one of three layouts (double loop / layered branches / convergence), inserts ladders, then validates reachability and completion coverage.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.920Z"
fingerprint: 4218aacf3a94ac1f8b3c43d4e96de0a074b03b0dd6c43b9a0c1fd9a333638e3e
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/ExplorationMapGenerator.cs"
apis:
  - protocol: rpc
    path: "RunMap.ExplorationMapGenerator.Steps"
    description:
      zh: >
          生成步骤（供读条）
          
      en: >
          Generator steps for the progress bar
          
  - protocol: rpc
    path: "RunMap.ExplorationMapGenerator.Validate"
    description:
      zh: >
          生成结果的结构校验
          
      en: >
          Structural validation of a generated map
          
  - protocol: rpc
    path: "RunMap.ExplorationMapGenerator.DeadEnds"
    description:
      zh: >
          统计分叉点与死胡同
          
      en: >
          Count junctions / dead ends
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/GameBase/RunMap/ExplorationMapGenerator.cs"
    description:
      zh: >
          探索地图生成器
          
      en: >
          Exploration map generator
          
deps:
  - kind: call
    to: project-chaos.framework.runmap.exploration.data
    label: {zh: "生成探索图", en: "Builds exploration graph"}
---
