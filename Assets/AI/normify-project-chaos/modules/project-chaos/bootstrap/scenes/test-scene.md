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
      
revision: b6dae190152cdcce64be3587bb098f7af5b42563
updated_at: "2026-11-10T14:57:10.701Z"
fingerprint: b0692b9edfc99a8cfd394dfd13e159ea99c90ba20ac01c764556d152075fe128
source:
  - path: "Assets/Scenes/TestScene.unity"
    line: 1
    end_line: 4604
  - path: "Assets/Scenes/URP_TestScene.unity"
  - path: "Assets/Scenes/URP_TestScene.unity"
apis:
  - protocol: file
    path: "Assets/Scenes/TestScene.unity"
    description:
      zh: >
          启动测试场景资产（挂 Entry）。
          
      en: >
          Test scene asset (hosts Entry).
          
  - protocol: file
    path: "Assets/Scenes/URP_TestScene.unity"
    description:
      zh: >
          URP 渲染与图集打包测试场景
          
      en: >
          URP rendering and atlas packing test scene
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.entry
    from_api: "file:Assets/Scenes/TestScene.unity"
    to_api: "file:Assets/Scripts/Runtime/Entry.cs#L5-L18"
    label: {zh: "启动场景挂载 Entry", en: "Startup scene hosts Entry"}
---
