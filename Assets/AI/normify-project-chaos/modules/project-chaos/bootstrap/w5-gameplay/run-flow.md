---
uid: 9c4b21a7
id: project-chaos.bootstrap.w5-gameplay.run-flow
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, dispatch, run]
name: {zh: "派遣与局外流程", en: "Dispatch & Meta Run Flow"}
description:
  zh: >
      DispatchService 派队入场与 RunSceneFlow 的跨场景流程：受理派遣→经 LoadingScene 进入 WorldScene 接管已准备地图→生成本局队伍→三场结算或撤退后回局外；含跨场景保留的整局库存、扎营标志与种子。
      
  en: >
      DispatchService accepts a dispatch and RunSceneFlow drives the cross-stage flow: dispatch is accepted, LoadingScene loads WorldScene, the prepared map is adopted, the team is spawned, then settlement or retreat returns to the meta layer, carrying run inventory, camping use, and seed across stages.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.979Z"
fingerprint: 28f4b40ea3e92b7c708167e26d42371a781045837395e38cf7ed7a5866d727e9
source:
  - path: "Assets/Scripts/Runtime/GameBase/Meta/RunSceneFlow.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/DispatchService.cs"
apis:
  - protocol: rpc
    path: "DispatchService.Dispatch"
    description:
      zh: >
          受理派遣并准备入场（校验槽位与队伍后推进局外状态）
          
      en: >
          Accept a dispatch and prepare entry after validating slots and the team
          
  - protocol: rpc
    path: "RunSceneFlow.EnterRun"
    description:
      zh: >
          经读条进入 WorldScene 并接管已准备地图
          
      en: >
          Enter WorldScene through the loading screen and adopt the prepared map
          
  - protocol: rpc
    path: "RunSceneFlow.Settle"
    description:
      zh: >
          三场结算后回局外，跨场景保留库存、扎营与种子
          
      en: >
          Return to the meta layer after settlement, preserving inventory, camping and seed
          
  - protocol: rpc
    path: "RunSceneFlow.Retreat"
    description:
      zh: >
          撤退回局外并保留本局进度
          
      en: >
          Retreat to the meta layer keeping this run's progress
          
deps:
  - kind: call
    to: project-chaos.framework.runmap
    label: {zh: "接管读条生成好的地图数据", en: "Adopt prepared map data"}
  - kind: call
    to: project-chaos.bootstrap.scene-loading
    label: {zh: "经读条切换场景", en: "Switch scenes via loading"}
---
