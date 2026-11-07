---
uid: b2c00501
id: project-chaos.mvp.model.gameplay.run
parent: project-chaos.mvp.model.gameplay
tags: [mvp, model, run]
name: {zh: "局内视图模型", en: "Run View Models"}
description:
  zh: >
      局内视图状态：结算统计、地图与已探索格、控制与菜单状态、HUD 与队伍选择。
      
  en: >
      Run-side view state: settlement tally, map/explored cells, control and menu state, HUD and team selection.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:54:15.839Z"
fingerprint: 5669729e90d929b3dfd66384e75c4cce73bfddd6cc1e37a16b0ef020bf2d65d0
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunControlModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunHudModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunInteractionModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunMapModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunMenuModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunSettlementModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunTeamModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunTeamState.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/RunSettlementModel.cs"
    description:
      zh: >
          结算模型
          
      en: >
          Settlement model
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/RunMapModel.cs"
    description:
      zh: >
          局内地图模型
          
      en: >
          Run map model
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/Gameplay/RunTeamModel.cs"
    description:
      zh: >
          队伍模型
          
      en: >
          Team model
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.run-flow
    label: {zh: "持有局内状态", en: "Holds run state"}
---
