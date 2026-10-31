---
uid: b28f6c40
id: project-chaos.bootstrap.w5-gameplay.interaction
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, interaction, pathfinding]
name: {zh: "局内交互与寻路", en: "Run Interaction & Pathfinding"}
description:
  zh: >
      RunInteractionService 在输入、距离、视线与目标条件都满足后执行一次交互；RunInteractable 是场景交互物；Model/Presenter/View 三件套驱动交互提示 HUD。RunMapPathfinder 与 RunMapNavigator 按实际能力图执行走跳落，RunBehaviorSettings 提供行为参数。
      
  en: >
      RunInteractionService executes one interaction after input, range, visibility and target-condition checks; RunInteractable is the scene-side interactable; the model/presenter/view trio drives the interaction HUD. RunMapPathfinder and RunMapNavigator execute walk/jump/drop over the real capability graph, with RunBehaviorSettings supplying behaviour parameters.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:03:54.693Z"
fingerprint: 2eeffbe4848c3415e57249514bc2187dd416a8542c4f5627f01be9fcf944cb12
source:
  - path: "Assets/Scripts/Runtime/GameBase/Interaction/RunInteractionService.cs"
  - path: "Assets/Scripts/Runtime/GameBase/Interaction/RunInteractable.cs"
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
          
deps:
  - kind: call
    to: project-chaos.framework.runmap
    label: {zh: "基于能力图寻路", en: "Pathfind on capability graph"}
  - kind: reference
    to: project-chaos.mvp
    label: {zh: "交互提示走 MVP 三件套", en: "Prompt uses MVP trio"}
---
