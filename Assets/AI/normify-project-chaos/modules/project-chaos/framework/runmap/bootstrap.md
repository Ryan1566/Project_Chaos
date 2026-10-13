---
uid: 8ae46ef5
id: project-chaos.framework.runmap.bootstrap
parent: project-chaos.framework.runmap
tags: [runmap, runtime]
name: {zh: "地图生成：运行时入口", en: "RunMap: Runtime Bootstrap"}
description:
  zh: >
      局内地图生成的运行时入口：可直接挂到场景的组件 + 静态 BuildScene 接口。串联生成 → 装配 → 装配后不变量自检（竖井通道必须畅通），并把阶段文案发布给读条界面。
      
  en: >
      Runtime entry point for in-run map generation: a drop-in MonoBehaviour plus a static BuildScene API. Drives generate → assemble → post-assembly invariant check (shaft channel must stay clear) and publishes stage text for the loading screen.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.429Z"
fingerprint: cd4e96c2c3fe256ec5650c10bca542159ea014ce465be23b9bd78c889184f8e2
source:
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapBootstrap.cs"
    line: 1
    end_line: 141
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapBootstrap.cs#L1-L141"
    description:
      zh: >
          BuildScene(runSeed, sceneIndex, parent)：运行时入口——名录（配置表，失败退灰盒）→ 生成 → 装配 → 自检，并向读条界面报阶段。
          
      en: >
          BuildScene(runSeed, sceneIndex, parent): the runtime entry — catalog (config tables, greybox fallback) → generate → assemble → post-check, reporting each stage to the loading screen.
          
  - protocol: rpc
    path: "RunMapBootstrap.BuildScene"
    description:
      zh: >
          BuildScene(runSeed, sceneIndex, parent) 与携带阶段文案的 StageChanged 事件。
          
      en: >
          BuildScene(runSeed, sceneIndex, parent) and the StageChanged event carrying the localized stage text.
          
---
