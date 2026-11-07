---
uid: b2c00402
id: project-chaos.mvp.presenter.gameplay-run
parent: project-chaos.mvp.presenter
tags: [mvp, presenter, run]
name: {zh: "局内 Presenter", en: "Run Presenters"}
description:
  zh: >
      局内侧界面的 Presenter：结算、扎营、HUD、地图、菜单、队伍与局内内容选择。把 Model 状态转成 View 调用，并把 UI 意图回传给服务层。
      
  en: >
      Presenters for run-side screens: settlement, camp, HUD, map, menu, team and run-content chooser. They convert model state into view calls and route UI intents back to services.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:54:15.835Z"
fingerprint: 57dfde496179eefe27b56958caa75c81ed3d738a29b9205b623ca84be3c96ddb
source:
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/SettlementPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/CampPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunHudPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunInteractionPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunMapPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunMenuPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunTeamPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunContentPanelPresenter.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/SettlementPresenter.cs"
    description:
      zh: >
          结算 Presenter
          
      en: >
          Settlement presenter
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/CampPresenter.cs"
    description:
      zh: >
          扎营 Presenter
          
      en: >
          Camp presenter
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunHudPresenter.cs"
    description:
      zh: >
          HUD Presenter
          
      en: >
          Run HUD presenter
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.gameplay
    label: {zh: "驱动玩法视图", en: "Drives gameplay views"}
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.run-flow
    label: {zh: "结算与扎营", en: "Settles and camps"}
---
