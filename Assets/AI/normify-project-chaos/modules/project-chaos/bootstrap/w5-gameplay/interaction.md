---
uid: b28f6c40
id: project-chaos.bootstrap.w5-gameplay.interaction
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, interaction, pathfinding]
name: {zh: "局内交互与寻路", en: "Run Interaction & Pathfinding"}
description:
  zh: >
      RunInteractionService 在输入、距离、视线与目标条件都满足后执行一次交互；RunInteractable 是场景交互物，RunLadderInteractable 把一个生成出来的 MapLadder 绑成梯子入口，使用后把控制权交给 LadderClimber；Model/Presenter/View 三件套驱动交互提示 HUD。RunMapPathfinder 与 RunMapNavigator 按实际能力图执行走跳落，RunBehaviorSettings 提供行为参数。
      
  en: >
      RunInteractionService executes one interaction after input, range, visibility and target-condition checks; RunInteractable is the scene-side interactable, and RunLadderInteractable binds a generated MapLadder so using it hands control to LadderClimber; the model/presenter/view trio drives the interaction HUD. RunMapPathfinder and RunMapNavigator execute walk/jump/drop over the real capability graph, with RunBehaviorSettings supplying behaviour parameters.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.892Z"
fingerprint: 70bd4efe19aaa284103d08a0b6f5fd362d42a5bbf60f8e340c72388b548fc3b8
source:
  - path: "Assets/Scripts/Runtime/GameBase/Interaction/RunInteractionService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Interaction/RunInteractable.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Interaction/RunLadderInteractable.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/Gameplay/RunInteractionModel.cs"
  - path: "Assets/Scripts/Runtime/MVP/Presenter/Gameplay/RunInteractionPresenter.cs"
  - path: "Assets/Scripts/Runtime/MVP/View/RunInteractionHud.cs"
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapPathfinder.cs"
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunMapNavigator.cs"
  - path: "Assets/Scripts/Runtime/GameBase/RunMap/RunBehaviorSettings.cs"
apis:
  - protocol: rpc
    path: "Chaos.Gameplay.Interaction.RunInteractionService.TryExecute"
    description:
      zh: >
          校验输入、距离、视线与目标条件后执行一次交互
          
      en: >
          Execute one interaction after input, range, visibility and condition checks
          
  - protocol: rpc
    path: "RunMap.RunMapNavigator.MoveTo"
    description:
      zh: >
          按实际能力图执行走跳落，Reached 表示到达
          
      en: >
          Execute walk/jump/drop navigation; Reached indicates arrival
          
  - protocol: rpc
    path: "RunMap.RunMapPathfinder.FindPath"
    description:
      zh: >
          在地图能力图上求一条可通行路径
          
      en: >
          Find a traversable path over the map capability graph
          
  - protocol: rpc
    path: "RunMap.RunLadderInteractable.ConfigureLadder"
    description:
      zh: >
          把交互物绑定到生成出来的梯子
          
      en: >
          Bind an interactable to a generated ladder
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/GameBase/Interaction/RunLadderInteractable.cs"
    description:
      zh: >
          梯子交互入口
          
      en: >
          Ladder interaction entry
          
deps:
  - kind: call
    to: project-chaos.framework.runmap
    label: {zh: "基于能力图寻路", en: "Pathfind on capability graph"}
  - kind: reference
    to: project-chaos.mvp
    label: {zh: "交互提示走 MVP 三件套", en: "Prompt uses MVP trio"}
  - kind: call
    to: project-chaos.gameplay-3c.character.ladder
    label: {zh: "交给梯子攀爬", en: "Hands off to ladder climb"}
---
