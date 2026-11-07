---
uid: c05e7a93
id: project-chaos.bootstrap.w5-gameplay.team-supply
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, supply, planned]
name: {zh: "队伍补给与门客状态", en: "Team Supply & Retainer State"}
description:
  zh: >
      计划态占位（fingerprint=pending）：门客生命/阵亡、扎营修整与局内背包带出还没有独立程序集，当前入口就在 RunEncounter.Team 与 RunSceneFlow 上，待抽出后迁移到此模块。
      
  en: >
      Planned placeholder with pending fingerprint: retainer health/death, camping/rest and in-run bag withdraw are not a separate assembly yet - the current entry points sit on RunEncounter.Team and RunSceneFlow and will move here once extracted.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.979Z"
fingerprint: 110d42751eac390a0eea57d8c3e7bb022038ea0b2c27434f75c3ed3231b64ace
source:
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.Team.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/RunSceneFlow.cs"
apis:
  - protocol: rpc
    path: "RunEncounter.Team.GetHealth"
    description:
      zh: >
          读取单个门客当前生命/阵亡状态供局内 HUD 使用
          
      en: >
          Read one retainer's current HP/death state for the in-run HUD
          
  - protocol: rpc
    path: "RunSceneFlow.Camp"
    description:
      zh: >
          队伍扎营时消耗一次扎营机会（跨场景保存在本局会话里）
          
      en: >
          Spend the camping use flag when the team camps (cross-stage, kept in the run session)
          
  - protocol: rpc
    path: "RunSceneFlow.CollectRunBag"
    description:
      zh: >
          结算前把局内背包物品转进带出清单
          
      en: >
          Move items from the in-run bag into the withdraw list before settlement
          
---
