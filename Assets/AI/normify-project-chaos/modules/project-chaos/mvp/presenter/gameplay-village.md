---
uid: b2c00404
id: project-chaos.mvp.presenter.gameplay-village
parent: project-chaos.mvp.presenter
tags: [mvp, presenter, village]
name: {zh: "村庄 Presenter", en: "Village Presenters"}
description:
  zh: >
      实体村庄的 Presenter：村庄面板与 HUD 状态、村内办事任务，以及从世界观地图进入村庄的交接。
      
  en: >
      Presenters for the physical village: village panel and HUD state, office tasks inside the village and the world-map-to-village handoff.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.936Z"
fingerprint: 9d92a41995f4f9e481cedff1dabffd25b27d9d39d3092801ca573c71a30696f1
source:
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/VillagePresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/VillageOfficePresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/VillageWorldPresenter.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/VillagePresenter.cs"
    description:
      zh: >
          村庄 Presenter
          
      en: >
          Village presenter
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/VillageWorldPresenter.cs"
    description:
      zh: >
          村庄世界 Presenter
          
      en: >
          Village world presenter
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.gameplay.village
    label: {zh: "驱动村庄视图", en: "Drives village views"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.village
    label: {zh: "村庄生产", en: "Village production"}
---
