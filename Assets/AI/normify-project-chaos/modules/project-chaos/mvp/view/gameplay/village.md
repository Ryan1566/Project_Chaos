---
uid: b2c00309
id: project-chaos.mvp.view.gameplay.village
parent: project-chaos.mvp.view.gameplay
tags: [mvp, view, village]
name: {zh: "村庄与局内内容界面", en: "Village & Run Content UI"}
description:
  zh: >
      村庄界面：村庄建筑与生产面板、村庄 HUD，以及进入地图前选择局内内容的面板。
      
  en: >
      Village screens: village building/production panel, village HUD and the run-content chooser shown before entering a map.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:51:57.367Z"
fingerprint: 07b4f6a409c90fcf3fff4b15a62c78317ccd824a10538faf4532483eca45b72a
source:
  - path: "Assets/Scripts/Runtime/MVP/View/VillagePanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/VillageHud.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/VillageOfficePanel.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunContentView.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunContentPanel.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/VillagePanel.cs"
    description:
      zh: >
          村庄面板
          
      en: >
          Village panel
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/VillageHud.cs"
    description:
      zh: >
          村庄 HUD
          
      en: >
          Village HUD
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/View/RunContentView.cs"
    description:
      zh: >
          局内内容面板
          
      en: >
          Run content panel
          
deps:
  - kind: reference
    to: project-chaos.mvp.presenter
    label: {zh: "渲染村庄", en: "Renders village"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.village
    label: {zh: "调用村庄服务", en: "Runs village services"}
---
