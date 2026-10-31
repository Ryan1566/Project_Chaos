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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:10.105Z"
fingerprint: 9e6ffb62c6e69e8235b75f41177187d991cf07c23b8aa2434f6d7e5ea062b6b8
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
