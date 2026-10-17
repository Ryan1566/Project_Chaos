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
updated_at: "2026-10-17T09:35:47.637Z"
fingerprint: 5679929fc6eff75b2cac1cd10a2acc67dd9405e433ae740f453f9ac8a5bf33e9
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
