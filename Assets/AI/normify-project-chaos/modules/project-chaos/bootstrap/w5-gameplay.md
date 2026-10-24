---
uid: b5e62026
id: project-chaos.bootstrap.w5-gameplay
parent: project-chaos.bootstrap
tags: [gameplay, dispatch, encounter, persistence]
name: {zh: "W5正式玩法与收尾", en: "W5 Gameplay Flow and Persistence"}
description:
  zh: >
    派遣经LoadingScene进入WorldScene，接管已准备地图并生成队伍；三场结算或撤退后返回局外。
    RunEncounter按队伍生命、房间推进、敌人掉落拆分，跨场景保留整局库存、扎营标志与种子。
    事件奖励与一次性ID原子保存，市场交易失败回滚，胜败结算保存可幂等重试。
  en: >
    Dispatch enters WorldScene through LoadingScene, adopts the prepared map, and spawns the team.
    Encounter responsibilities are split into team health, room progression, and enemies/drops.
    Run inventory, camping use, and seed survive stage transitions. Event, trade, and settlement saves support safe retry.
updated_at: "2026-10-24"
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
apis:
  - protocol: rpc
    path: "RunEncounter.EnterPreparedScene"
    description: {zh: "接管读条地图并开始战斗", en: "Start combat on the prepared map"}
  - protocol: rpc
    path: "MetaProgressService.SaveProgress"
    description: {zh: "保存当前会话槽位", en: "Save the active session slot"}
  - protocol: rpc
    path: "RunMap.RunMapNavigator.MoveTo"
    description: {zh: "按实际能力图执行走跳落，Reached表示到达", en: "Execute walk/jump/drop navigation; Reached indicates arrival"}
  - protocol: rpc
    path: "Chaos.Gameplay.Interaction.RunInteractionService.TryExecute"
    description: {zh: "检查输入、距离、视线及目标条件后执行一次交互", en: "Execute one interaction after input, range, visibility and condition checks"}
---

源码人工同步记录；本轮无Normify连接器，未执行生成与策略验证。完整接口关系、证据和限制见`Assets/Chaos_Story/Story/05_程序/06_W5收尾实现与验收.md`。现有tree.json/api-index.json/normify.html仍是原生成快照，不用手改冒充重新生成。

2026-10-24：追加角色实际能力导航、敌人五态/门客前摇、统一MVP交互及暂停刚体快照。三图及数据流沿用上述入口；配置来自RunBehavior/RunInteraction，交互占位仅查看不发奖励。当前实现、运行覆盖和构建/精确恢复见`Assets/Chaos_Story/Story/05_程序/11_局内寻路战斗与交互实现及验收.md`。本次仍未执行Normify生成/策略验证。
