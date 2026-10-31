---
uid: b5e62026
id: project-chaos.bootstrap.w5-gameplay
parent: project-chaos.bootstrap
tags: [gameplay, dispatch, encounter, persistence]
name: {zh: "W5正式玩法与收尾", en: "W5 Gameplay Flow and Persistence"}
description:
  zh: >
      派遣经LoadingScene进入WorldScene，接管已准备地图并生成队伍；三场结算或撤退后返回局外。RunEncounter按队伍生命、房间推进、敌人掉落拆分，跨场景保留整局库存、扎营标志与种子。事件奖励与一次性ID原子保存，市场交易失败回滚，胜败结算保存可幂等重试。
      
  en: >
      Dispatch enters WorldScene through LoadingScene, adopts the prepared map, and spawns the team. Encounter responsibilities are split into team health, room progression, and enemies/drops. Run inventory, camping use, and seed survive stage transitions. Event, trade, and settlement saves support safe retry.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:03:54.711Z"
fingerprint: 5343da48b7730ef09e8ef487c1ab237954a062330917c96af77fa23a80e8c768
source:
  - path: "Assets/Scripts/Runtime/GameBase/Meta/RunSceneFlow.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/DispatchService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/EventService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/MarketService.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.Team.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.Rooms.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounter.Enemies.cs"
  - path: "Assets/Scripts/Runtime/Player/RunEncounterTicker.cs"
  - path: "Assets/Scripts/Runtime/Player/RunDropItem.cs"
  - path: "Assets/Scripts/Runtime/GameBase/LocalizationManager/GameplayText.cs"
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapPathfinder.cs"
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapNavigator.cs"
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunBehaviorSettings.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/EnemyPlaceholder.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/AllyController.cs"
  - path: "Assets/Scripts/Runtime/Combat/CombatQuery.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Interaction/RunInteractionService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Interaction/RunInteractable.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunInteractionModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunInteractionPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunInteractionHud.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageSceneFlow.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageSceneBootstrap.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageSceneSettings.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillageWorkforce.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Village/VillagePlot.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Meta/VillageService.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/Production/VillageBehaviorTree.cs"
  - path: "Assets/Scripts/Runtime/NPC_AI/Production/VillageProductionNpc.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/VillageWorldModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/VillageWorldPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/VillageHud.cs"
---
