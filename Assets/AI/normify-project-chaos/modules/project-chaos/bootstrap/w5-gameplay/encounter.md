---
uid: 4e7d83b1
id: project-chaos.bootstrap.w5-gameplay.encounter
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, encounter, combat]
name: {zh: "局内遭遇与战斗", en: "Run Encounter & Combat"}
description:
  zh: >
      RunEncounter 按队伍生命（Team）、房间推进（Rooms）、敌人与掉落（Enemies）拆分，RunEncounterTicker 驱动逐帧推进，地上掉落归到结算模块。战斗查询与占位敌人/友军控制支撑局内交战。
      
  en: >
      RunEncounter is split by team health (Team), room progression (Rooms), and enemies/drops (Enemies), with RunEncounterTicker as the per-frame driver and the ground drop owned by the settlement module. Combat query plus placeholder enemy and ally controllers back in-run fighting.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:03:54.690Z"
fingerprint: 90afa486d15658612762d5d1ebc2000b55a6e30fe26ac66c022d01bc701ae25e
source:
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.Team.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.Rooms.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.Enemies.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounterTicker.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/EnemyPlaceholder.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/AllyController.cs"
  - path: "Assets/Scripts/Runtime/Combat/CombatQuery.cs"
apis:
  - protocol: rpc
    path: "RunEncounter.EnterPreparedScene"
    description:
      zh: >
          接管读条地图并在其上开始本局战斗
          
      en: >
          Start combat on the map prepared by the loading pipeline
          
  - protocol: rpc
    path: "RunEncounter.Tick"
    description:
      zh: >
          推进当前房间/战斗状态（由 RunEncounterTicker 每帧驱动）
          
      en: >
          Advance the current room/combat state, driven per frame by RunEncounterTicker
          
  - protocol: rpc
    path: "RunEncounter.ApplyTeamDamage"
    description:
      zh: >
          对队伍结算伤害并判定阵亡
          
      en: >
          Apply damage to the team and resolve deaths
          
deps:
  - kind: call
    to: project-chaos.bootstrap.w5-gameplay.run-flow
    label: {zh: "在派遣流程建立的会话里跑", en: "Runs in dispatch-created run"}
  - kind: reference
    to: project-chaos.config-pipeline
    label: {zh: "读取房间与掉落配置", en: "Reads room/drop config"}
---
