---
uid: 9e0d0001
id: project-chaos.bootstrap.scenes.main-scene
parent: project-chaos.bootstrap.scenes
tags: [scene, todo]
name: {zh: "主场景 MainScene", en: "MainScene"}
description:
  zh: >
      新增的场景资产 Assets/Scenes/MainScene.unity：由 2026-10-11 「添加中国地图，修改部分场景布局」提交引入。目前只有 2 个 GameObject、未被任何脚本引用、也未登记进 Build Settings —— 正式入口仍是 TestScene → LoadingScene → WorldScene（见 bootstrap.build-settings）。
      
  en: >
      Newly added scene asset Assets/Scenes/MainScene.unity, landed with the 2026-10-11 commit that added the China map. It currently holds only 2 GameObjects, is referenced by no script and is not registered in Build Settings — the real entry chain is still TestScene -> LoadingScene -> WorldScene (see bootstrap.build-settings).
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.976Z"
fingerprint: c688649d0843a18828155394cc4dd503bbf7e97bd8cc772ef49fc7552ae17d75
source:
  - path: "Assets/Scenes/MainScene.unity"
apis:
  - protocol: file
    path: "Assets/Scenes/MainScene.unity"
    description:
      zh: >
          新增场景资产（8KB、2 个 GameObject）；未进 Build Settings，也无脚本引用。
          
      en: >
          New scene asset (8 KB, 2 GameObjects); not in Build Settings and referenced by no script.
          
---
