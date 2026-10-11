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
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.716Z"
fingerprint: 7af277e88951087a2172826c339dcf45649c6d6487a3638542e82dcd3df2ee8f
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
