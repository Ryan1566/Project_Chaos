---
uid: 5c1a7e14
id: project-chaos.bootstrap.scenes.test-scene
parent: project-chaos.bootstrap.scenes
tags: [bootstrap, scene, startup]
name: {zh: "启动测试场景", en: "Test Scene"}
description:
  zh: >
      旧的测试场景；2026-10-12 起已从 Build Settings 移除（index 0 由 EnterScene 接替），但场景文件保留。
      
  en: >
      Legacy test scene; as of 2026-10-12 it is no longer in Build Settings (EnterScene replaced it at index 0) but the file is kept on disk.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.571Z"
fingerprint: 839cf8a5476d18a9f696b79227e2f58b5f45c0e88202a21d86a012a4f0ce8a45
source:
  - path: "Assets/Scenes/TestScene.unity"
    line: 1
    end_line: 4604
apis:
  - protocol: file
    path: "Assets/Scenes/TestScene.unity"
    description:
      zh: >
          启动测试场景资产（挂 Entry）。
          
      en: >
          Test scene asset (hosts Entry).
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.entry
    from_api: "file:Assets/Scenes/TestScene.unity"
    to_api: "file:Assets/Scripts/Runtime/Entry.cs#L5-L18"
    label: {zh: "启动场景挂载 Entry", en: "Startup scene hosts Entry"}
---
