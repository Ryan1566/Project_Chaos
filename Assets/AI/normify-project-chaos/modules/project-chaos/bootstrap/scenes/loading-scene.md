---
uid: 5c1a7e13
id: project-chaos.bootstrap.scenes.loading-scene
parent: project-chaos.bootstrap.scenes
tags: [bootstrap, scene, loading]
name: {zh: "读条场景", en: "Loading Scene"}
description:
  zh: >
      LoadingScene.unity：独立小场景，构建清单第 2 项。Canvas 下挂着 LoadingController（组件挂在场景里，不是 prefab），刻意不叫 Canvas、不挂 Entry、不做 DontDestroyOnLoad，每次加载都是干净的新对象。
      
  en: >
      LoadingScene.unity: a standalone mini scene, build-list entry 2. LoadingController is attached under its Canvas (a scene component, not a prefab). It deliberately avoids being named Canvas, hosting Entry or using DontDestroyOnLoad, so every load starts from clean objects.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.689Z"
fingerprint: 485b77a07b7dcc65a83597662abe9626feb38eadb24a7b60c70b7591af1fdb1a
source:
  - path: "Assets/Scenes/LoadingScene.unity"
    line: 1
    end_line: 736
apis:
  - protocol: file
    path: "Assets/Scenes/LoadingScene.unity"
    description:
      zh: >
          读条场景资产（Canvas 挂 LoadingController）。
          
      en: >
          Loading scene asset (Canvas hosts the controller).
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.loading-screen.node-binding
    from_api: "file:Assets/Scenes/LoadingScene.unity"
    to_api: "file:Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L155-L211"
    label: {zh: "场景挂载 LoadingController", en: "Scene hosts LoadingController"}
---
