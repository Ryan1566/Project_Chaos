---
uid: 5c1a7e14
id: project-chaos.bootstrap.scenes.test-scene
parent: project-chaos.bootstrap.scenes
tags: [bootstrap, scene, startup]
name: {zh: "启动测试场景", en: "Test Scene"}
description:
  zh: >
      TestScene.unity：构建清单第 0 项（启动场景），场景里挂 Entry 触发 UI/输入/设置的初始化。也是本工程唯一实际跑得起来的入口场景。
      
  en: >
      TestScene.unity: build-list entry 0 (the startup scene). It hosts Entry, which triggers UI, input and settings initialisation. It is also the only entry scene that actually runs in this project.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.724Z"
fingerprint: 9e25dbae3ee8584026378447d3c8ae29afefa6371a3a18d0ddc494ee41237e9a
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
