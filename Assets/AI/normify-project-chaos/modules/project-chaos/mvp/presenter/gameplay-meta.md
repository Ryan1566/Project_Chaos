---
uid: b2c00403
id: project-chaos.mvp.presenter.gameplay-meta
parent: project-chaos.mvp.presenter
tags: [mvp, presenter, meta]
name: {zh: "局外 Presenter", en: "Meta Presenters"}
description:
  zh: >
      村庄与局外界面的 Presenter：派遣、技能树、市场、事件、背包/轮盘、办亊任务、门客招募与时代进度。
      
  en: >
      Presenters for village/meta screens: dispatch, skill tree, market, event, inventory/wheel, office tasks, retainer recruitment and era progress.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:57:10.687Z"
fingerprint: 5c592ad212711770d8729a861783b9ad3bc1c1cb12f1b3e01308608f72f922df
source:
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/DispatchPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/FamilyTreePresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/MarketPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/EventPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/InventoryPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RadialMenuPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/OfficeTasksPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RetainerPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/EraProgressPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/GameplayPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/BattlePreparationFlow.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/BattlePreparationPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/BattlePreparationFlow.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/BattlePreparationPresenter.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/DispatchPresenter.cs"
    description:
      zh: >
          派遣 Presenter
          
      en: >
          Dispatch presenter
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/FamilyTreePresenter.cs"
    description:
      zh: >
          技能树 Presenter
          
      en: >
          Skill tree presenter
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/InventoryPresenter.cs"
    description:
      zh: >
          背包 Presenter
          
      en: >
          Inventory presenter
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/BattlePreparationFlow.cs"
    description:
      zh: >
          战备界面的流程编排（打开/开始/失败重开）
          
      en: >
          Battle-prep flow orchestration (open/start/reopen)
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/BattlePreparationPresenter.cs"
    description:
      zh: >
          战备界面的 Presenter（仓库行、槽位手势、预设）
          
      en: >
          Battle-prep presenter (warehouse rows, slot gestures, presets)
          
  - protocol: rpc
    path: "rpc:BattlePreparationPresenter.EnterRun"
    description:
      zh: >
          确认战备并进入本局
          
      en: >
          Confirms loadout and enters the run
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.gameplay
    label: {zh: "驱动局外视图", en: "Drives meta views"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.meta-events
    label: {zh: "局外服务", en: "Meta services"}
---
